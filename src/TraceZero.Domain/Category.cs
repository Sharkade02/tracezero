namespace TraceZero.Domain;

/// <summary>
/// Grandes catégories fonctionnelles d'un élément scanné. Sert au regroupement dans l'UI et à
/// l'application des profils d'automatisation.
/// </summary>
public enum Category
{
    Unknown = 0,
    WindowsTemp = 1,
    WindowsCache = 2,
    CrashDumps = 3,
    ThumbnailCache = 4,
    RecycleBin = 5,
    BrowserCache = 6,
    BrowserHistory = 7,
    BrowserCookies = 8,
    BrowserSessions = 9,
    PrivacyTrace = 10,
    SystemLogs = 11,
    LargeFile = 12,
    Duplicate = 13,
    ApplicationLeftover = 14,
    StartupEntry = 15,
}
