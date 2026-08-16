namespace TraceZero.Persistence;

/// <summary>Emplacements des données locales de TraceZero (sous %LOCALAPPDATA%\TraceZero).</summary>
public static class TraceZeroPaths
{
    public static string DataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TraceZero");

    public static string HistoryDatabase => Path.Combine(DataDirectory, "tracezero.db");

    public static string ExclusionsFile => Path.Combine(DataDirectory, "exclusions.json");

    public static string LicenseFile => Path.Combine(DataDirectory, "license.token");
}
