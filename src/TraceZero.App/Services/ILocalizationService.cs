namespace TraceZero.App.Services;

/// <summary>Langues prises en charge (§31). fr par défaut.</summary>
public enum AppLanguage
{
    French,
    English,
    German,
    Spanish,
}

/// <summary>
/// Gère la langue de l'interface avec bascule à chaud (§31), sur le même principe que le thème :
/// un dictionnaire de chaînes est swappé dans les ressources ; les vues utilisent <c>DynamicResource</c>
/// et le code utilise <see cref="Localizer"/>.
/// </summary>
public interface ILocalizationService
{
    AppLanguage Current { get; }

    event EventHandler<AppLanguage>? LanguageChanged;

    void Apply(AppLanguage language);

    /// <summary>Charge la langue persistée (ou la valeur par défaut) et l'applique.</summary>
    void LoadPersisted();
}
