namespace AvvisoScadenzaPatenti.Core.Shared.FiscalCode;

using System.Globalization;
using System.Text;

/// <summary>
/// Decodes fragments of an Italian fiscal code (codice fiscale) and computes
/// the equivalent surname/name codes for a given employee, so the two can be compared.
/// Does not decode the birthplace, and does not handle "omocodia" (digit-to-letter
/// substitution used to disambiguate identical fiscal codes).
/// </summary>
public static class FiscalCodeDecoder
{
    private static readonly Dictionary<char, int> MonthCodes = new()
    {
        ['A'] = 1,
        ['B'] = 2,
        ['C'] = 3,
        ['D'] = 4,
        ['E'] = 5,
        ['H'] = 6,
        ['L'] = 7,
        ['M'] = 8,
        ['P'] = 9,
        ['R'] = 10,
        ['S'] = 11,
        ['T'] = 12
    };

    /// <summary>
    /// Extracts the birth date encoded in a fiscal code (year, month, day).
    /// Returns null if the fiscal code is too short or malformed.
    /// The 2-digit year is resolved to a full year using a heuristic tuned for
    /// an adult workforce dataset (assumes 1900s unless that would make the
    /// person implausibly old for a "born in 2000s" reading).
    /// </summary>
    public static DateTime? ExtractBirthDate(string fiscalCode)
    {
        if (string.IsNullOrWhiteSpace(fiscalCode) || fiscalCode.Length < 11)
            return null;

        var yearPart = fiscalCode.Substring(6, 2);
        var monthChar = char.ToUpperInvariant(fiscalCode[8]);
        var dayPart = fiscalCode.Substring(9, 2);

        if (!int.TryParse(yearPart, out var year2) ||
            !MonthCodes.TryGetValue(monthChar, out var month) ||
            !int.TryParse(dayPart, out var rawDay))
        {
            return null;
        }

        // Day > 40 means the person is female (CF encoding convention).
        var day = rawDay > 40 ? rawDay - 40 : rawDay;

        if (day is < 1 or > 31)
            return null;

        var year = ResolveCentury(year2);

        try
        {
            return new DateTime(year, month, day);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    /// <summary>
    /// Resolves a 2-digit CF year to a full year, assuming an adult workforce.
    /// Prefers the 1900s reading unless that would make the person over ~100
    /// years old, in which case it falls back to the 2000s reading.
    /// </summary>
    private static int ResolveCentury(int year2)
    {
        var year1900 = 1900 + year2;
        var year2000 = 2000 + year2;

        var age1900 = DateTime.Today.Year - year1900;

        return age1900 > 100 ? year2000 : year1900;
    }

    /// <summary>Extracts the 3-letter surname code from the fiscal code (positions 1-3).</summary>
    public static string ExtractSurnameCode(string fiscalCode) =>
        fiscalCode.Length >= 3 ? fiscalCode[..3].ToUpperInvariant() : string.Empty;

    /// <summary>Extracts the 3-letter name code from the fiscal code (positions 4-6).</summary>
    public static string ExtractNameCode(string fiscalCode) =>
        fiscalCode.Length >= 6 ? fiscalCode.Substring(3, 3).ToUpperInvariant() : string.Empty;

    /// <summary>Computes the official CF surname code for the given last name.</summary>
    public static string ComputeSurnameCode(string lastName) => ComputeCode(lastName, isFirstName: false);

    /// <summary>Computes the official CF name code for the given first name.</summary>
    public static string ComputeNameCode(string firstName) => ComputeCode(firstName, isFirstName: true);

    private static string ComputeCode(string value, bool isFirstName)
    {
        var normalized = Normalize(value);
        var consonants = normalized.Where(c => !IsVowel(c)).ToList();
        var vowels = normalized.Where(IsVowel).ToList();

        List<char> selected;

        // Special rule: for first names with 4+ consonants, CF skips the 2nd one
        // (takes 1st, 3rd, 4th) to reduce collisions between common names.
        if (isFirstName && consonants.Count >= 4)
        {
            selected = [consonants[0], consonants[2], consonants[3]];
        }
        else
        {
            selected = consonants.Take(3).ToList();
            if (selected.Count < 3)
            {
                selected.AddRange(vowels.Take(3 - selected.Count));
            }
        }

        while (selected.Count < 3)
        {
            selected.Add('X');
        }

        return new string(selected.Take(3).ToArray());
    }

    private static bool IsVowel(char c) => "AEIOU".Contains(c);

    /// <summary>
    /// Uppercases, strips accents, and keeps letters only (spaces, apostrophes,
    /// hyphens are dropped) — matching how the official algorithm treats names.
    /// </summary>
    private static string Normalize(string input)
    {
        var decomposed = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsLetter(c))
                sb.Append(char.ToUpperInvariant(c));
        }

        return sb.ToString();
    }
}