using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;
using TraceZero.Persistence;

namespace TraceZero.App.Services;

/// <summary>
/// Bascule le dictionnaire de chaînes actif et la culture du thread (§31). Persiste le choix localement
/// (emplacement portable-aware via <see cref="TraceZeroPaths"/>).
/// </summary>
public sealed class LocalizationManager : ILocalizationService
{
    private static readonly string StateFile = TraceZeroPaths.LanguageFile;

    private ResourceDictionary? _activeDictionary;

    public AppLanguage Current { get; private set; } = AppLanguage.French;

    public event EventHandler<AppLanguage>? LanguageChanged;

    public void Apply(AppLanguage language) => Apply(language, persist: true);

    private void Apply(AppLanguage language, bool persist)
    {
        var app = System.Windows.Application.Current
                  ?? throw new InvalidOperationException("Application.Current est null.");

        var dictionary = new ResourceDictionary
        {
            Source = new Uri($"/TraceZero.App;component/Localization/Strings.{CodeOf(language)}.xaml", UriKind.Relative),
        };

        if (_activeDictionary is not null)
        {
            app.Resources.MergedDictionaries.Remove(_activeDictionary);
        }

        app.Resources.MergedDictionaries.Add(dictionary);
        _activeDictionary = dictionary;
        Current = language;

        var culture = CultureInfo.GetCultureInfo(CultureOf(language));
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        if (persist)
        {
            Persist(language);
        }

        LanguageChanged?.Invoke(this, language);
    }

    /// <summary>
    /// Au démarrage : applique la langue choisie si elle a été persistée ; sinon, suit la langue de
    /// Windows (sans la figer — l'app continue de suivre l'OS tant que l'utilisateur n'a pas choisi
    /// explicitement une langue dans les Paramètres).
    /// </summary>
    /// <summary>
    /// Applique la culture (séparateurs de nombres/dates) le plus tôt possible au démarrage, avant que
    /// WPF ne fige un contexte d'exécution — sinon la culture de l'OS (ex. fr-FR) reste active pour le
    /// formatage des nombres même quand l'UI est en anglais. À appeler en tout premier dans OnStartup.
    /// </summary>
    public static void ApplyStartupCulture()
    {
        var language = ReadPersisted() ?? DetectSystemLanguage();
        var culture = CultureInfo.GetCultureInfo(CultureOf(language));
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
    }

    public void LoadPersisted()
    {
        var persisted = ReadPersisted();
        if (persisted is { } language)
        {
            Apply(language, persist: true);
        }
        else
        {
            Apply(DetectSystemLanguage(), persist: false);
        }
    }

    /// <summary>Mappe la culture d'interface de Windows vers une langue supportée (repli : anglais).</summary>
    private static AppLanguage DetectSystemLanguage()
    {
        try
        {
            return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName switch
            {
                "fr" => AppLanguage.French,
                "de" => AppLanguage.German,
                "es" => AppLanguage.Spanish,
                "en" => AppLanguage.English,
                _ => AppLanguage.English,
            };
        }
        catch (CultureNotFoundException)
        {
            return AppLanguage.English;
        }
    }

    private static string CodeOf(AppLanguage language) => language switch
    {
        AppLanguage.English => "en",
        AppLanguage.German => "de",
        AppLanguage.Spanish => "es",
        _ => "fr",
    };

    private static string CultureOf(AppLanguage language) => language switch
    {
        AppLanguage.English => "en-US",
        AppLanguage.German => "de-DE",
        AppLanguage.Spanish => "es-ES",
        _ => "fr-FR",
    };

    /// <summary>Langue explicitement choisie et persistée, ou <c>null</c> si aucun choix valide n'existe.</summary>
    private static AppLanguage? ReadPersisted()
    {
        try
        {
            if (File.Exists(StateFile))
            {
                var value = File.ReadAllText(StateFile).Trim();
                if (Enum.TryParse<AppLanguage>(value, out var parsed))
                {
                    return parsed;
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }

        return null;
    }

    private static void Persist(AppLanguage language)
    {
        try
        {
            var dir = Path.GetDirectoryName(StateFile);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(StateFile, language.ToString());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}
