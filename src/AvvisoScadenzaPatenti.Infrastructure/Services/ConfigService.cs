namespace AvvisoScadenzaPatenti.Infrastructure.Services;

using System.Text.Json;
using System.Text.Json.Nodes;

using AvvisoScadenzaPatenti.Core.Interfaces;
public class JsonConfigService : IConfigService
{
    public void SaveEncryptedPassword(string plainPassword)
    {
        // 1. Simple Base64 encoding (not cryptographically strong, just convenient)
        byte[] textBytes = System.Text.Encoding.UTF8.GetBytes(plainPassword);
        string encryptedPassword = Convert.ToBase64String(textBytes); string filePath = "appsettings.json"; try
        {
            // 2. Read the existing JSON file
            string json = File.ReadAllText(filePath);
            var jsonNode = JsonNode.Parse(json);

            // 3. Update the specific path: Settings -> Smtp -> Password
            if (jsonNode?["Settings"]?["Smtp"] is JsonObject mailServer)
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
}
