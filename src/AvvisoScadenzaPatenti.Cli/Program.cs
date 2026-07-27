namespace AvvisoScadenzaPatenti.Cli;

using AvvisoScadenzaPatenti.Cli.Commands;
using AvvisoScadenzaPatenti.Core.Configuration;
using AvvisoScadenzaPatenti.Core.Enums;
using AvvisoScadenzaPatenti.Core.Interfaces;
using AvvisoScadenzaPatenti.Core.Services;
using AvvisoScadenzaPatenti.Infrastructure.Repositories;
using AvvisoScadenzaPatenti.Infrastructure.Services.Mail;
using CommandLine;
using CommandLine.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
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
            opts.Process ? RunMode.Process :
            RunMode.ShowHelp;   // nuovo case, invece del fallback silenzioso

        if (mode == RunMode.ShowHelp)
        {
            Console.WriteLine(HelpText.AutoBuild(Parser.Default.ParseArguments<Options>(new[] { "--help" })));
            return 1;
        }

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

        ILicenseCommand command = mode switch
        {
            RunMode.Sort => host.Services.GetRequiredService<SortLicensesCommand>(),
            RunMode.SearchByName => host.Services.GetRequiredService<SearchLicenseByNameCommand>(),
            RunMode.UpcomingExpirations => host.Services.GetRequiredService<UpcomingExpirationsCommand>(),
            RunMode.MatchFiscalCode => host.Services.GetRequiredService<MatchFiscalCodeCommand>(),
            RunMode.Show => host.Services.GetRequiredService<ShowLicenseCommand>(),
            RunMode.Update => host.Services.GetRequiredService<UpdateLicenseCommand>(),
            RunMode.Process => host.Services.GetRequiredService<ProcessLicensesCommand>(),
            _ => throw new InvalidOperationException($"Unhandled run mode: {mode}")
        };

        var result = await command.ExecuteAsync(opts, ct);

        await host.StopAsync(CancellationToken.None);
        return result;
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

        builder.Services.AddTransient<SortLicensesCommand>();
        builder.Services.AddTransient<SearchLicenseByNameCommand>();
        builder.Services.AddTransient<UpcomingExpirationsCommand>();
        builder.Services.AddTransient<MatchFiscalCodeCommand>();
        builder.Services.AddTransient<ShowLicenseCommand>();
        builder.Services.AddTransient<UpdateLicenseCommand>();
        builder.Services.AddTransient<ProcessLicensesCommand>();
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
