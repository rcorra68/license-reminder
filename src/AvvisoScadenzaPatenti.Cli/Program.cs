using System.Text.Json;
using System.Text.Json.Nodes;

using AvvisoScadenzaPatenti.Cli;
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

using static Org.BouncyCastle.Math.EC.ECCurve;

// --- MAIN FLOW ---

// 1. Parse command-line arguments using the Options class
var parserResult = Parser.Default.ParseArguments<Options>(args);

// 2. Handle the successful parsing case asynchronously
// This will initialize the DI container and run the orchestrator
await parserResult.WithParsedAsync(async opts =>
{
    if (opts.Init)
    {
        InitializeConfiguration(opts.Force);
        return;
    }

    // Check if the user wants to encrypt a password and save it to appsettings.json
    if (!string.IsNullOrEmpty(opts.PasswordToCrypt))
    {
        EncryptAndSavePassword(opts.PasswordToCrypt);
        return; // Exit the program after encryption, no further processing
    }

    // Normal execution: run the orchestrator that processes licenses and employees
    await RunOrchestratorAsync(opts);
});

// 3. Handle parsing errors or help/version requests synchronously
parserResult.WithNotParsed(HandleParseErrors);

// --- SEPARATED FUNCTIONS ---

/// <summary>
/// Builds the host and runs the LicenseOrchestrator using the parsed options.
/// </summary>
/// <param name="opts">The parsed command‑line options.</param>
async Task RunOrchestratorAsync(Options opts)
{
    var builder = Host.CreateApplicationBuilder(args);

    // Set up Serilog using the configuration from appsettings.json.
    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .CreateLogger();

    // Replace the default logging providers with Serilog.
    builder.Logging.ClearProviders();
    builder.Services.AddSerilog();

    // Register services, repositories, orchestrator, and other dependencies.
    ConfigureServices(builder, opts);

    using IHost host = builder.Build();

    // Perform an SMTP health check via the email service.
    var emailService = host.Services.GetRequiredService<IEmailService>();
    var orchestrator = host.Services.GetRequiredService<LicenseOrchestrator>();

    // Internal logging inside VerifyEmailConnectivity will provide details.
    if (!emailService.VerifyEmailConnectivity())
    {
        Log.Warning("SMTP Health Check failed. Licenses will be processed, but notifications might not be delivered.");
        return;
    }

    // Execute the core business logic: process licenses.
    await orchestrator.ProcessLicensesAsync();
}

/// <summary>
/// Configures the services (DI container) for the host, including repositories and orchestrator.
/// Reads paths from the appsettings.json section "DataSources".
/// </summary>
/// <param name="builder">The host application builder.</param>
/// <param name="opts">The parsed command‑line options.</param>
void ConfigureServices(HostApplicationBuilder builder, Options opts)
{
    // Load the application settings section
    var settings = builder.Configuration.GetSection("Settings").Get<AppSettings>();
    if (settings == null)
    {
        // The Settings section is required for the application to function correctly
        throw new InvalidOperationException("Critical Error: 'Settings' section not found in appsettings.json");
    }

    builder.Services.AddSingleton(settings);

    // Retrieve the DataSources configuration section for later service setup
    var dataSources = builder.Configuration.GetSection("DataSources");

    // Register repositories with file paths from appsettings.json (with fallbacks)
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

    // Inject Options so they are available everywhere in the DI container
    builder.Services.AddSingleton(opts);

    // Register the Email Service
    builder.Services.AddTransient<IEmailService, MailKitEmailService>();

    // Register the orchestrator as a transient service
    builder.Services.AddTransient<LicenseOrchestrator>();
}

/// <summary>
/// Handles command‑line parsing errors.
/// If the user requested help or version, it exits without error.
/// Otherwise it prints a brief error message and lets the application exit.
/// </summary>
/// <param name="errors">Sequence of parsing errors.</param>
void HandleParseErrors(IEnumerable<Error> errors)
{
    if (errors.Any(e => e.Tag != ErrorType.HelpRequestedError && e.Tag != ErrorType.VersionRequestedError))
    {
        Console.WriteLine("Invalid arguments provided. Use --help for usage information.");
    }
}

/// <summary>
/// Encrypts the given password using Base64 encoding and saves it into appsettings.json
/// under the path "Settings.MailServer.Password".
/// For real security, consider using ProtectedData or a dedicated secret‑handling library.
/// </summary>
/// <param name="plainPassword">The plain text password to encrypt and store.</param>
void EncryptAndSavePassword(string plainPassword)
{
    // 1. Simple Base64 encoding (not cryptographically strong, just convenient)
    byte[] textBytes = System.Text.Encoding.UTF8.GetBytes(plainPassword);
    string encryptedPassword = Convert.ToBase64String(textBytes);

    string filePath = "appsettings.json";

    try
    {
        // 2. Read the existing JSON file
        string json = File.ReadAllText(filePath);
        var jsonNode = JsonNode.Parse(json);

        // 3. Update the specific path: Settings -> MailServer -> Password
        if (jsonNode?["Settings"]?["MailServer"] is JsonObject mailServer)
        {
            mailServer["Password"] = encryptedPassword;

            // 4. Write back to file with nice indentation
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(filePath, jsonNode.ToJsonString(options));

            Console.WriteLine("Successfully encrypted and saved the password to appsettings.json.");
        }
        else
        {
            Console.WriteLine("Error: Could not find Settings.MailServer section in appsettings.json.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error updating configuration: {ex.Message}");
    }
}

/// <summary>
/// Creates a default appsettings.json if it doesn't exist.
/// </summary>
void InitializeConfiguration(bool force)
{
    string filePath = "appsettings.json";

    if (File.Exists(filePath) && !force)
    {
        Console.WriteLine("Configuration file already exists. Use --force to overwrite.");
        return;
    }

    var defaultConfig = new
    {
        Settings = new
        {
            MailServer = new
            {
                Host = "smtp.example.com",
                Port = 587,
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
        string json = JsonSerializer.Serialize(defaultConfig, options);
        File.WriteAllText(filePath, json);

        Console.WriteLine(force && File.Exists(filePath)
            ? "Successfully re-initialized appsettings.json."
            : "Successfully created appsettings.json.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error creating configuration: {ex.Message}");
    }
}
