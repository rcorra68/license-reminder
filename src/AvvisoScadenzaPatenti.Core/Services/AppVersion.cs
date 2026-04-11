namespace AvvisoScadenzaPatenti.Core.Services;

using System.Reflection;
public static class AppVersion
{
    public static string Get()
    {
        return Assembly
            .GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
            ?? "unknown";
    }
}