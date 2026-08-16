using System.Reflection;

namespace TraceZero.App.Services;

/// <summary>Informations d'application (version, etc.).</summary>
public static class AppInfo
{
    public static string Version { get; } =
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.1.0";
}
