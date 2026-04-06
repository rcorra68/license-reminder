using CommandLine;

using System.Text.Json;
using System.Text.Json.Nodes;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using AvvisoScadenzaPatenti.Cli;
using AvvisoScadenzaPatenti.Core.Interfaces;
using AvvisoScadenzaPatenti.Core.Services;
using AvvisoScadenzaPatenti.Infrastructure.Repositories;

// --- MAIN FLOW ---

// 1. Parse command-line arguments using the Options class
var parserResult = Parser.Default.ParseArguments<Options>(args);

// 2. Handle the successful parsing case asynchronously
// This will initialize the DI container and run the Orchestrator
await parserResult.WithParsedAsync(async opts => 
{
    // Check if the user wants to encrypt a password
    if (!string.IsNullOrEmpty(opts.PasswordToCrypt))
    {
        EncryptAndSavePassword(opts.PasswordToCrypt);
        return; // Exit the program after encryption
    }

    // Normal execution
    await RunOrchestratorAsync(opts);
});

// 3. Handle parsing errors or help/version requests synchronously
parserResult.WithNotParsed(HandleParseErrors);

// --- SEPARATED FUNCTIONS ---

// Function 1: Logic to build the host and run the Orchestrator
async Task RunOrchestratorAsync(Options opts)
{
    var builder = Host.CreateApplicationBuilder(args);

    // Call the service configuration function
    ConfigureServices(builder, opts);

    using IHost host = builder.Build();

    // Start the business logic
    var orchestrator = host.Services.GetRequiredService<LicenseOrchestrator>();
    await orchestrator.ProcessLicensesAsync();
}

// Function 2: Logic to register dependencies
void ConfigureServices(HostApplicationBuilder builder, Options opts)
{
    var dataSources = builder.Configuration.GetSection("DataSources");

    // Register Repositories with paths from appsettings.json
    builder.Services.AddSingleton<IEmployeeRepository>(sp => 
        new EmployeeRepository(dataSources["EmployeesFilePath"] ?? "employees.csv", sp.GetRequiredService<ILogger<EmployeeRepository>>()));

    builder.Services.AddSingleton<ILicenseRepository>(sp => 
        new LicenseRepository(dataSources["LicensesFilePath"] ?? "licenses.csv", sp.GetRequiredService<ILogger<LicenseRepository>>()));

    builder.Services.AddSingleton<IUncompliantMailRepository>(sp => 
        new UncompliantMailRepository(dataSources["UncompliantMailsFilePath"] ?? "uncompliant_mails.csv", sp.GetRequiredService<ILogger<UncompliantMailRepository>>()));

    // Inject Options so they are available everywhere
    builder.Services.AddSingleton(opts);
    
    // Register the Orchestrator
    builder.Services.AddTransient<LicenseOrchestrator>();
}

// Function 3: Handle command line errors
void HandleParseErrors(IEnumerable<Error> errors)
{
    if (errors.Any(e => e.Tag != ErrorType.HelpRequestedError && e.Tag != ErrorType.VersionRequestedError))
    {
        Console.WriteLine("Invalid arguments provided. Use --help for usage information.");
    }
}

void EncryptAndSavePassword(string plainPassword)
{
    // 1. Simple Base64 encoding (as seen in your previous example)
    // Note: For real security, consider using ProtectedData or a dedicated library
    byte[] textBytes = System.Text.Encoding.UTF8.GetBytes(plainPassword);
    string encryptedPassword = Convert.ToBase64String(textBytes);

    string filePath = "appsettings.json";
    
    try 
    {
        // 2. Read the existing JSON
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
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error updating configuration: {ex.Message}");
    }
}