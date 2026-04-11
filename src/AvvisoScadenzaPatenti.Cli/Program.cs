namespace AvvisoScadenzaPatenti.Cli;

using System.Text.Json;

using AvvisoScadenzaPatenti.Core.Configuration;
using AvvisoScadenzaPatenti.Core.Interfaces;
using AvvisoScadenzaPatenti.Core.Services;
using AvvisoScadenzaPatenti.Infrastructure.Repositories;
using AvvisoScadenzaPatenti.Infrastructure.Services.Mail;

using CommandLine;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Serilog;

/// <summary>
/// Entry point class for the License Expiration Notification CLI tool.
/// </summary>
public class Program
{
    /// <summary>
    /// Main entry point of the application.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    public static void Main(string[] args)
    {
        var parserResult = Parser.Default.ParseArguments<Options>(args);

        parserResult.WithParsed(opts =>
        {
            if (opts.Init)
            {
                InitializeConfiguration(opts.Force);
                return;
            }

            RunOrchestrator(opts, args);
        });

        parserResult.WithNotParsed(HandleParseErrors);
    }

    /// <summary>
    /// Builds the host and runs the LicenseOrchestrator using the parsed options.
    /// </summary>
    /// <param name="opts">The parsed command‑line options.</param>
    /// <param name="args">Original command-line arguments for the host builder.</param>
    public static void RunOrchestrator(Options opts, string[] args)
    {
        DotNetEnv.Env.Load();

        var builder = Host.CreateApplicationBuilder(args);
        builder.Configuration.AddEnvironmentVariables();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .CreateLogger();

        builder.Logging.ClearProviders();
        builder.Services.AddSerilog();

        ConfigureServices(builder, opts);

        using IHost host = builder.Build();

        var emailService = host.Services.GetRequiredService<IEmailService>();
        var orchestrator = host.Services.GetRequiredService<LicenseOrchestrator>();

        if (!emailService.VerifyEmailConnectivity())
        {
            Log.Warning("SMTP Health Check failed. Licenses will be processed, but notifications might not be delivered.");
            return;
        }

        orchestrator.ProcessLicenses();
    }

    /// <summary>
    /// Configures the services (DI container) for the host, including repositories and orchestrator.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="opts">The parsed command‑line options.</param>
    private static void ConfigureServices(HostApplicationBuilder builder, Options opts)
    {
        var settings = builder.Configuration.GetSection("Settings").Get<AppSettings>();
        
        if (settings == null)
            throw new InvalidOperationException("Critical Error: 'Settings' section not found in appsettings.json");

        builder.Services.AddSingleton(settings);
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

    /// <summary>
    /// Handles command‑line parsing errors.
    /// </summary>
    /// <param name="errors">Sequence of parsing errors.</param>
    private static void HandleParseErrors(IEnumerable<Error> errors)
    {
        if (errors.Any(e => e.Tag != ErrorType.HelpRequestedError && e.Tag != ErrorType.VersionRequestedError))
        {
            Console.WriteLine("Invalid arguments provided. Use --help for usage information.");
        }
    }

    /// <summary>
    /// Creates a default appsettings.json if it doesn't exist.
    /// </summary>
    /// <param name="force">If true, overwrites existing configuration.</param>
    private static void InitializeConfiguration(bool force)
    {
        string filePath = "appsettings.json";
        if (File.Exists(filePath) && !force)
        {
            Console.WriteLine("Configuration file already exists. Use --force to overwrite.");
            return;
        }

        var defaultConfig = new { 
            /* ... il tuo oggetto config ... */ 
        };

        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(defaultConfig, options);
            File.WriteAllText(filePath, json);
            Console.WriteLine("Successfully initialized appsettings.json.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating configuration: {ex.Message}");
        }
    }
}