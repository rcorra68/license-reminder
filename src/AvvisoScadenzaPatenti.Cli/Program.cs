namespace AvvisoScadenzaPatenti.Cli;

using System.Text.Json;

using AvvisoScadenzaPatenti.Core.Configuration;
using AvvisoScadenzaPatenti.Core.Interfaces;
using AvvisoScadenzaPatenti.Core.Services;
using AvvisoScadenzaPatenti.Infrastructure.Repositories;
using AvvisoScadenzaPatenti.Infrastructure.Services;
using AvvisoScadenzaPatenti.Infrastructure.Services.Mail;

using CommandLine;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Serilog;

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
        if (opts.Init)
        {
            InitializeConfiguration(opts.Force);
            return 0;
        }

        using var host = BuildHost(args, opts);

        var emailService = host.Services.GetRequiredService<IEmailService>();
        var orchestrator = host.Services.GetRequiredService<LicenseOrchestrator>();

        // CancellationToken con timeout (best practice per cron job)
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        var ct = cts.Token;

        if (!await emailService.VerifyEmailConnectivityAsync(ct))
        {
            Log.Warning("SMTP Health Check failed. Licenses will be processed, but notifications might not be delivered.");
        }

        await orchestrator.ProcessLicensesAsync(ct);

        return 0;
    }

    private static IHost BuildHost(string[] args, Options opts)
    {
        var builder = Host.CreateApplicationBuilder(args);

        var environment =
            Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? "Production";

        builder.Environment.EnvironmentName = environment;

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

        // Infrastructure services
        builder.Services.AddSingleton<IConfigService, JsonConfigService>();
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
