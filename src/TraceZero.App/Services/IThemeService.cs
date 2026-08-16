namespace TraceZero.App.Services;

public enum AppTheme
{
    Light,
    Dark,
}

/// <summary>Gère le thème clair/sombre appliqué à l'application, avec basculement à chaud.</summary>
public interface IThemeService
{
    AppTheme Current { get; }

    event EventHandler<AppTheme>? ThemeChanged;

    void Apply(AppTheme theme);

    void Toggle();
}
