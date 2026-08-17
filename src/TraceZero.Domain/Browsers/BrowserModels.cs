namespace TraceZero.Domain.Browsers;

/// <summary>Moteur du navigateur : détermine la disposition des données sur disque.</summary>
public enum BrowserEngine
{
    Chromium = 0,
    Gecko = 1,
}

/// <summary>Navigateurs pris en charge (§14).</summary>
public enum BrowserKind
{
    Chrome = 0,
    Edge = 1,
    Brave = 2,
    Firefox = 3,
    Opera = 4,
    Vivaldi = 5,
    Chromium = 6,
}

/// <summary>Un profil de navigateur détecté sur le disque.</summary>
public sealed record BrowserProfileInfo
{
    public required string Name { get; init; }

    /// <summary>Dossier racine du profil côté cache (Local). Utilisé pour le nettoyage des caches.</summary>
    public required string Path { get; init; }

    /// <summary>
    /// Racine des données de contenu (historique, cookies, sessions) lorsqu'elle diffère du dossier de
    /// cache <see cref="Path"/> — navigateurs à disposition Local/Roaming scindée (Firefox, Opera).
    /// <c>null</c> ⇒ identique à <see cref="Path"/> (cas Chrome/Edge/Brave…).
    /// </summary>
    public string? ContentPath { get; init; }

    public bool IsDefault { get; init; }

    /// <summary>Racine effective des traces de confidentialité (contenu), quelle que soit la disposition.</summary>
    public string ContentRoot => ContentPath ?? Path;
}

/// <summary>Un navigateur installé, avec ses profils et son état d'exécution.</summary>
public sealed record DetectedBrowser
{
    public required BrowserKind Kind { get; init; }

    public required BrowserEngine Engine { get; init; }

    public required string DisplayName { get; init; }

    /// <summary>Dossier « User Data » (Chromium) ou dossier des profils (Firefox).</summary>
    public required string DataRoot { get; init; }

    public required IReadOnlyList<BrowserProfileInfo> Profiles { get; init; }

    /// <summary>Le navigateur est en cours d'exécution : modifier ses données en direct est risqué (§14).</summary>
    public bool IsRunning { get; init; }
}
