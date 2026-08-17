using TraceZero.Domain.Browsers;

namespace TraceZero.Browsers;

/// <summary>Description statique d'un navigateur Chromium et de son implantation disque.</summary>
internal sealed record ChromiumBrowserDef(
    BrowserKind Kind,
    string DisplayName,
    bool UnderRoaming,
    string RelativeUserData,
    string ProcessName);

/// <summary>
/// Catalogue des navigateurs pris en charge en Phase 4. Opera est volontairement différé
/// (disposition cache/profil scindée Local/Roaming — voir KNOWN_LIMITATIONS.md).
/// </summary>
internal static class BrowserCatalog
{
    public static readonly IReadOnlyList<ChromiumBrowserDef> Chromium =
    [
        new(BrowserKind.Chrome, "Google Chrome", false, @"Google\Chrome\User Data", "chrome"),
        new(BrowserKind.Edge, "Microsoft Edge", false, @"Microsoft\Edge\User Data", "msedge"),
        new(BrowserKind.Brave, "Brave", false, @"BraveSoftware\Brave-Browser\User Data", "brave"),
        new(BrowserKind.Vivaldi, "Vivaldi", false, @"Vivaldi\User Data", "vivaldi"),
        new(BrowserKind.Chromium, "Chromium", false, @"Chromium\User Data", "chrome"),
    ];

    /// <summary>Dossiers de cache d'un profil Chromium, relatifs au dossier du profil. Tous SAFE.</summary>
    public static readonly IReadOnlyList<string> ChromiumCacheDirs =
    [
        "Cache",
        "Code Cache",
        "GPUCache",
        "DawnGraphiteCache",
        "DawnWebGPUCache",
        "GrShaderCache",
        "ShaderCache",
        @"Service Worker\CacheStorage",
        @"Service Worker\ScriptCache",
    ];

    /// <summary>
    /// Navigateurs Opera (Chromium) à disposition Local/Roaming scindée : le profil (historique, cookies,
    /// sessions) est sous Roaming, le cache sous Local. Détectés à part du flux Chromium générique.
    /// </summary>
    public static readonly IReadOnlyList<(string DisplayName, string RelativeDir, string ProcessName)> Opera =
    [
        ("Opera", @"Opera Software\Opera Stable", "opera"),
        ("Opera GX", @"Opera Software\Opera GX Stable", "opera"),
    ];

    public const string FirefoxProcessName = "firefox";

    /// <summary>Dossier de cache d'un profil Firefox, relatif au dossier du profil (dans LocalAppData).</summary>
    public const string FirefoxCacheDir = "cache2";
}
