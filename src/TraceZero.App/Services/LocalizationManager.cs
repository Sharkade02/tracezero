using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;

namespace TraceZero.App.Services;

/// <summary>
/// Bascule le dictionnaire de chaînes actif et la culture du thread (§31). Persiste le choix localement.
/// </summary>
public sealed class LocalizationManager : ILocalizationService
{
    private static readonly string StateFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TraceZero", "language.txt");

    private ResourceDictionary? _activeDictionary;

    public AppLanguage Current { get; private set; } = AppLanguage.French;

    public event EventHandler<AppLanguage>? LanguageChanged;

    public void Apply(AppLanguage language)
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

        Persist(language);
        LanguageChanged?.Invoke(this, language);
    }

    public void LoadPersisted() => Apply(ReadPersisted());

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

    private static AppLanguage ReadPersisted()
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

        return AppLanguage.French;
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
