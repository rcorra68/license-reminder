namespace AvvisoScadenzaPatenti.Cli;

using AvvisoScadenzaPatenti.Core.Configuration;
using AvvisoScadenzaPatenti.Core.Entities;
using AvvisoScadenzaPatenti.Core.Enums;
using AvvisoScadenzaPatenti.Core.Interfaces;
using AvvisoScadenzaPatenti.Core.Services;
using AvvisoScadenzaPatenti.Core.Shared.FiscalCode;
using AvvisoScadenzaPatenti.Core.Shared.Sorting;
using AvvisoScadenzaPatenti.Infrastructure.Repositories;
using AvvisoScadenzaPatenti.Infrastructure.Services.Mail;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Globalization;
using System.Reflection;
using System.Text.Json;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        return await Parser.Default.ParseArguments<Options>(args)
            .MapResult(
                async opts => await WithParsedAsync(opts, args),
                errs => Task.FromResult(HandleParseErrors(errs))
            );
    }

    private static async Task<int> WithParsedAsync(Options opts, string[] args)
    {
        var hasUpdateTarget = opts.UpdateLicenseNumber is not null;
        var hasNewExpiryDate = !string.IsNullOrWhiteSpace(opts.NewExpiryDate);
        var hasNameSearch = !string.IsNullOrWhiteSpace(opts.Name);
        var hasUpcomingExpirations = opts.UpcomingExpirations?.Any() == true;
        var hasMatchCf = !string.IsNullOrWhiteSpace(opts.MatchCf);

        var mode =
            opts.Init ? RunMode.Init :
            hasUpdateTarget && hasNewExpiryDate ? RunMode.Update :
            hasUpdateTarget ? RunMode.Show :
            hasNameSearch ? RunMode.SearchByName :
            hasUpcomingExpirations ? RunMode.UpcomingExpirations :
            hasMatchCf ? RunMode.MatchFiscalCode :
            opts.SortBy is not null ? RunMode.Sort :
            RunMode.Process;

        if (mode == RunMode.Init)
        {
            InitializeConfiguration(opts.Force);
            return 0;
        }

        if (mode == RunMode.Update && string.IsNullOrWhiteSpace(opts.NewExpiryDate))
        {
            Console.WriteLine("You must specify --new-expiry-date along with --update-license.");
            return 1;
        }

        using var host = BuildHost(args, opts);
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        var ct = cts.Token;

        await host.StartAsync(ct);

        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        Log.Information("Starting License Reminder v{Version}", version);

        if (mode == RunMode.Sort)
        {
            var repo = host.Services.GetRequiredService<ILicenseRepository>();
            var licenses = repo.GetAll();
            var sorted = LicenseSorting.Sort(licenses, opts.SortBy!.Value, opts.SortOrder);
            repo.SaveAll(sorted);

            await host.StopAsync(CancellationToken.None);
            return 0;
        }

        if (mode == RunMode.SearchByName)
        {
            var licenseRepo = host.Services.GetRequiredService<ILicenseRepository>();
            var employeeRepo = host.Services.GetRequiredService<IEmployeeRepository>();

            var matches = licenseRepo.SearchByName(opts.Name!).ToList();

            if (matches.Count == 0)
            {
                Log.Warning("No licenses found for '{Query}'.", opts.Name);
                await host.StopAsync(CancellationToken.None);
                return 1;
            }

            foreach (var license in matches)
            {
                var employee = employeeRepo.GetByName(license.FirstName, license.LastName);
                var birthDateText = employee?.BirthDate is { } bd ? bd.ToString("dd/MM/yyyy") : "n/d";

                Console.WriteLine($"{license.FirstName} {license.LastName} — born on {birthDateText}");
                Console.WriteLine($"  License No. {license.LicenseNumber} — Current expiration date: {license.ExpiryDate:yyyy-MM-dd}");
                Console.WriteLine();
            }

            if (matches.Count > 1)
            {
                Console.WriteLine($"Found {matches.Count} licenses matching '{opts.Name}'. Use --update-license <number> to update the correct one.");
            }

            await host.StopAsync(CancellationToken.None);
            return 0;
        }

        if (mode == RunMode.UpcomingExpirations)
        {
            var repo = host.Services.GetRequiredService<ILicenseRepository>();
            var licenses = repo.GetAll();

            var count = opts.UpcomingExpirations!.FirstOrDefault();
            if (count <= 0)
            {
                count = 5;
            }

            var upcoming = LicenseSorting
                .Sort(licenses, CsvSortField.ExpiryDate, CsvSortOrder.Asc)
                .Take(count);

            foreach (var license in upcoming)
            {
                var expiredMarker = license.ExpiryDate < DateTime.Today ? "[SCADUTA] " : string.Empty;
                Console.WriteLine($"{expiredMarker}{license.LastName} {license.FirstName} — scadenza: {license.ExpiryDate:yyyy-MM-dd}");
            }

            await host.StopAsync(CancellationToken.None);
            return 0;
        }

        if (mode == RunMode.MatchFiscalCode)
        {
            var employeeRepo = host.Services.GetRequiredService<IEmployeeRepository>();

            var cf = opts.MatchCf!.Trim().ToUpperInvariant();

            if (cf.Length < 11)
            {
                Console.WriteLine($"Invalid fiscal code: '{opts.MatchCf}'. Expected at least 11 characters.");
                await host.StopAsync(CancellationToken.None);
                return 1;
            }

            var birthDate = FiscalCodeDecoder.ExtractBirthDate(cf);
            var surnameCode = FiscalCodeDecoder.ExtractSurnameCode(cf);
            var nameCode = FiscalCodeDecoder.ExtractNameCode(cf);

            var candidates = employeeRepo.GetAll()
                .Where(e =>
                    FiscalCodeDecoder.ComputeSurnameCode(e.LastName) == surnameCode &&
                    FiscalCodeDecoder.ComputeNameCode(e.FirstName) == nameCode)
                .ToList();

            if (candidates.Count == 0)
            {
                Console.WriteLine($"No employee found matching fiscal code '{opts.MatchCf}'.");
                await host.StopAsync(CancellationToken.None);
                return 1;
            }

            Employee selected;

            if (candidates.Count == 1)
            {
                selected = candidates[0];
            }
            else if (opts.ResolveIndex is null)
            {
                Console.WriteLine($"Found {candidates.Count} homonym candidates matching fiscal code '{opts.MatchCf}':");

                for (var i = 0; i < candidates.Count; i++)
                {
                    var c = candidates[i];
                    var currentBirthText = c.BirthDate is { } bd ? bd.ToString("dd/MM/yyyy") : "n/d";
                    Console.WriteLine($"  [{i + 1}] {c.LastName} {c.FirstName} — {c.Mail} — born on {currentBirthText}");
                }

                Console.WriteLine("Use --match-cf <CF> --resolve-index <N> to pick which one to update.");

                await host.StopAsync(CancellationToken.None);
                return 1;
            }
            else
            {
                var index = opts.ResolveIndex.Value - 1;

                if (index < 0 || index >= candidates.Count)
                {
                    Console.WriteLine($"Invalid --resolve-index {opts.ResolveIndex}. Must be between 1 and {candidates.Count}.");
                    await host.StopAsync(CancellationToken.None);
                    return 1;
                }

                selected = candidates[index];
            }

            selected.FiscalCode = cf;
            selected.BirthDate = birthDate;
            employeeRepo.Update(selected);

            Log.Information(
                "Employee {LastName} {FirstName} updated with fiscal code {FiscalCode} and birth date {BirthDate:yyyy-MM-dd}.",
                selected.LastName,
                selected.FirstName,
                selected.FiscalCode,
                selected.BirthDate);

            await host.StopAsync(CancellationToken.None);
            return 0;
        }

        if (mode == RunMode.Show)
        {
            var repo = host.Services.GetRequiredService<ILicenseRepository>();
            var license = repo.GetByLicenseNumber(opts.UpdateLicenseNumber!);

            if (license is null)
            {
                Log.Warning("No license found with number {LicenseNumber}.", opts.UpdateLicenseNumber);
                await host.StopAsync(CancellationToken.None);
                return 1;
            }

            Console.WriteLine($"{license.FirstName} {license.LastName} — license n. {license.LicenseNumber}");
            Console.WriteLine($"Current expiration: {license.ExpiryDate:yyyy-MM-dd}");

            await host.StopAsync(CancellationToken.None);
            return 0;
        }

        if (mode == RunMode.Update)
        {
            if (!DateTime.TryParseExact(
                    opts.NewExpiryDate,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var newExpiryDate))
            {
                Console.WriteLine($"Invalid date format: '{opts.NewExpiryDate}'. Usa yyyy-MM-dd.");
                await host.StopAsync(CancellationToken.None);
                return 1;
            }

            var repo = host.Services.GetRequiredService<ILicenseRepository>();
            var license = repo.GetByLicenseNumber(opts.UpdateLicenseNumber!);

            if (license is null)
            {
                Log.Warning("No license found with number {LicenseNumber}.", opts.UpdateLicenseNumber);
                await host.StopAsync(CancellationToken.None);
                return 1;
            }

            license.ExpiryDate = newExpiryDate;
            repo.SaveAll(repo.GetAll());

            Log.Information(
                "Driving licence {LicenseNumber} updated: new expiration date {ExpiryDate:yyyy-MM-dd}.",
                opts.UpdateLicenseNumber,
                newExpiryDate);

            await host.StopAsync(CancellationToken.None);
            return 0;
        }

        // Process
        var emailService = host.Services.GetRequiredService<IEmailService>();
        var orchestrator = host.Services.GetRequiredService<LicenseOrchestrator>();

        if (!await emailService.VerifyEmailConnectivityAsync(ct))
        {
            Log.Warning("SMTP Health Check failed. Licenses will be processed, but notifications might not be delivered.");
        }

        await orchestrator.ProcessLicensesAsync(ct);

        await host.StopAsync(CancellationToken.None);
        return 0;
    }

    private static IHost BuildHost(string[] args, Options opts)
    {
        var builder = Host.CreateApplicationBuilder(args);

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .CreateLogger();

        builder.Logging.ClearProviders();
        builder.Services.AddSerilog();

        ConfigureServices(builder, opts);

        return builder.Build();
    }

    private static void ConfigureServices(HostApplicationBuilder builder, Options opts)
    {
        // Options pattern (clean & standard)
        builder.Services
            .AddOptions<AppSettings>()
            .Bind(builder.Configuration.GetSection("Settings"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var dataSources = builder.Configuration.GetSection("DataSources");

        builder.Services.AddSingleton<IEmployeeRepository>(sp =>
            new EmployeeRepository(
                dataSources["EmployeesFilePath"] ?? "employees.csv",
                sp.GetRequiredService<ILogger<EmployeeRepository>>()));

        builder.Services.AddSingleton<ILicenseRepository>(sp =>
            new LicenseRepository(
                dataSources["LicensesFilePath"] ?? "licenses.csv",
                sp.GetRequiredService<ILogger<LicenseRepository>>()));

        builder.Services.AddSingleton<IUncompliantMailRepository>(sp =>
            new UncompliantMailRepository(
                dataSources["UncompliantMailsFilePath"] ?? "uncompliant_mails.csv",
                sp.GetRequiredService<ILogger<UncompliantMailRepository>>()));

        builder.Services.AddSingleton(opts);

        builder.Services.AddTransient<IEmailService, MailKitEmailService>();
        builder.Services.AddTransient<LicenseOrchestrator>();
    }

    private static int HandleParseErrors(IEnumerable<Error> errors)
    {
        if (errors.Any(e => e.Tag != ErrorType.HelpRequestedError && e.Tag != ErrorType.VersionRequestedError))
        {
            Console.WriteLine("Invalid arguments provided. Use --help for usage information.");
            return 1;
        }

        return 0;
    }

    private static void InitializeConfiguration(bool force)
    {
        const string filePath = "appsettings.json";

        if (File.Exists(filePath) && !force)
        {
            Console.WriteLine("Configuration file already exists. Use --force to overwrite.");
            return;
        }

        var defaultConfig = new
        {
            Settings = new
            {
                Smtp = new
                {
                    Host = "smtp.example.com",
                    Port = 587,
                    Username = "user@example.com",
                    Password = ""
                }
            },
            DataSources = new
            {
                EmployeesFilePath = "employees.csv",
                LicensesFilePath = "licenses.csv",
                UncompliantMailsFilePath = "uncompliant_mails.csv"
            }
        };

        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(defaultConfig, options);
            File.WriteAllText(filePath, json);

            Console.WriteLine("Successfully initialized appsettings.json.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating configuration: {ex.Message}");
        }
    }
}
