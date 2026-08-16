using System.Windows;

namespace TraceZero.App.Services;

/// <summary>
/// Bascule le dictionnaire de couleurs actif dans les ressources de l'application.
/// Les styles utilisent <c>DynamicResource</c>, donc le changement est immédiat.
/// </summary>
public sealed class ThemeManager : IThemeService
{
    private ResourceDictionary? _activeThemeDictionary;

    public AppTheme Current { get; private set; } = AppTheme.Light;

    public event EventHandler<AppTheme>? ThemeChanged;

    public void Apply(AppTheme theme)
    {
        var app = System.Windows.Application.Current
                  ?? throw new InvalidOperationException("Application.Current est null.");

        var dictionary = new ResourceDictionary
        {
            Source = new Uri($"/TraceZero.App;component/Themes/{theme}.xaml", UriKind.Relative),
        };

        if (_activeThemeDictionary is not null)
        {
            app.Resources.MergedDictionaries.Remove(_activeThemeDictionary);
        }

        // Insérer en tête : les styles partagés (mergés ensuite) résolvent les couleurs par-dessus.
        app.Resources.MergedDictionaries.Insert(0, dictionary);
        _activeThemeDictionary = dictionary;
        Current = theme;
        ThemeChanged?.Invoke(this, theme);
    }

    public void Toggle() =>
        Apply(Current == AppTheme.Light ? AppTheme.Dark : AppTheme.Light);
}
