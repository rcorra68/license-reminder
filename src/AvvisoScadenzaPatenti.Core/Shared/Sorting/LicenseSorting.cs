namespace AvvisoScadenzaPatenti.Core.Shared.Sorting;

using AvvisoScadenzaPatenti.Core.Entities;
using AvvisoScadenzaPatenti.Core.Enums;

public static class LicenseSorting
{
    public static List<License> Sort(
        IEnumerable<License> licenses,
        CsvSortField sortBy,
        CsvSortOrder order)
    {
        return (sortBy, order) switch
        {
            (CsvSortField.Name, CsvSortOrder.Asc) => licenses.OrderBy(x => x.LastName).ThenBy(x => x.FirstName).ToList(),
            (CsvSortField.Name, CsvSortOrder.Desc) => licenses.OrderByDescending(x => x.LastName).ThenBy(x => x.FirstName).ToList(),

            (CsvSortField.ExpiryDate, CsvSortOrder.Asc) => licenses.OrderBy(x => x.ExpiryDate).ToList(),
            (CsvSortField.ExpiryDate, CsvSortOrder.Desc) => licenses.OrderByDescending(x => x.ExpiryDate).ToList(),

            (CsvSortField.ReleaseDate, CsvSortOrder.Asc) => licenses.OrderBy(x => x.ReleaseDate).ToList(),
            (CsvSortField.ReleaseDate, CsvSortOrder.Desc) => licenses.OrderByDescending(x => x.ReleaseDate).ToList(),

            _ => licenses.ToList()
        };
    }
}
