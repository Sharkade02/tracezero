using System.Windows;

namespace TraceZero.App.Services;

/// <summary>
/// Accès aux chaînes localisées depuis le code (§31). Résout la clé dans le dictionnaire de chaînes
/// actif ; retourne la clé elle-même si absente (jamais de crash, repli visible en développement).
/// </summary>
public static class Localizer
{
    public static string Get(string key) =>
        System.Windows.Application.Current?.TryFindResource(key) as string ?? key;

    /// <summary>Résout un modèle localisé puis applique <see cref="string.Format(string, object[])"/>.</summary>
    public static string Format(string key, params object[] args) =>
        string.Format(System.Globalization.CultureInfo.CurrentCulture, Get(key), args);
}
