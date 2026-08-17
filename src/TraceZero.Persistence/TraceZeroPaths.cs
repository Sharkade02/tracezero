namespace TraceZero.Persistence;

/// <summary>
/// Emplacements des données locales de TraceZero (§29). Deux modes :
/// <list type="bullet">
///   <item><b>Installé</b> : données sous <c>%LOCALAPPDATA%\TraceZero</c>.</item>
///   <item><b>Portable</b> : si un marqueur <see cref="PortableMarker"/> est présent à côté de
///     l'exécutable, les données vont dans <c>&lt;dossier de l'exe&gt;\Data</c> — aucune écriture
///     cachée ailleurs.</item>
/// </list>
/// </summary>
public static class TraceZeroPaths
{
    /// <summary>Nom du fichier marqueur qui active le mode portable (placé à côté de l'exe).</summary>
    public const string PortableMarker = "tracezero.portable";

    /// <summary>Vrai si l'application s'exécute en mode portable.</summary>
    public static bool IsPortable { get; } =
        File.Exists(Path.Combine(AppContext.BaseDirectory, PortableMarker));

    public static string DataDirectory { get; } = ResolveDataDirectory(
        AppContext.BaseDirectory,
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        IsPortable);

    public static string HistoryDatabase => Path.Combine(DataDirectory, "tracezero.db");

    public static string ExclusionsFile => Path.Combine(DataDirectory, "exclusions.json");

    public static string LicenseFile => Path.Combine(DataDirectory, "license.token");

    public static string LanguageFile => Path.Combine(DataDirectory, "language.txt");

    /// <summary>
    /// Résout le dossier de données. Pur et testable : portable → <paramref name="baseDir"/>\Data,
    /// sinon <paramref name="localAppData"/>\TraceZero.
    /// </summary>
    public static string ResolveDataDirectory(string baseDir, string localAppData, bool portable) =>
        portable
            ? Path.Combine(baseDir, "Data")
            : Path.Combine(localAppData, "TraceZero");
}
