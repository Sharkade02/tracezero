using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using TraceZero.App.Services;
using TraceZero.Application.Exclusions;
using TraceZero.Domain.Exclusions;

namespace TraceZero.App.ViewModels;

/// <summary>Ligne d'exclusion affichée.</summary>
public sealed class ExclusionRowViewModel
{
    public ExclusionRowViewModel(ExclusionRule rule)
    {
        Id = rule.Id;
        DisplayName = rule.DisplayName;
        KindText = rule.Kind == ExclusionKind.Folder ? "Dossier" : "Catégorie";
    }

    public Guid Id { get; }

    public string DisplayName { get; }

    public string KindText { get; }
}

/// <summary>Page Paramètres (§40) : thème et gestion des exclusions.</summary>
public sealed partial class SettingsViewModel : PageViewModelBase
{
    private readonly IThemeService _themeService;
    private readonly IExclusionStore _exclusionStore;

    public SettingsViewModel(IThemeService themeService, IExclusionStore exclusionStore)
    {
        _themeService = themeService;
        _exclusionStore = exclusionStore;
        _themeService.ThemeChanged += (_, _) => OnPropertyChanged(nameof(IsDarkTheme));
        ReloadExclusions();
    }

    public override string Title => "Paramètres";

    public override string IconGlyph => "\U0001F527";

    public override bool IsUnderConstruction => false;

    public bool IsDarkTheme
    {
        get => _themeService.Current == AppTheme.Dark;
        set
        {
            _themeService.Apply(value ? AppTheme.Dark : AppTheme.Light);
            OnPropertyChanged();
        }
    }

    public ObservableCollection<ExclusionRowViewModel> Exclusions { get; } = [];

    [ObservableProperty]
    private bool _hasExclusions;

    [RelayCommand]
    private void AddFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Choisir un dossier à exclure du nettoyage",
        };

        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.FolderName))
        {
            _exclusionStore.Add(new ExclusionRule
            {
                Id = Guid.NewGuid(),
                Kind = ExclusionKind.Folder,
                Value = dialog.FolderName,
                DisplayName = dialog.FolderName,
                CreatedUtc = DateTimeOffset.UtcNow,
            });
            ReloadExclusions();
        }
    }

    [RelayCommand]
    private void Remove(ExclusionRowViewModel? row)
    {
        if (row is null)
        {
            return;
        }

        _exclusionStore.Remove(row.Id);
        ReloadExclusions();
    }

    private void ReloadExclusions()
    {
        Exclusions.Clear();
        foreach (var rule in _exclusionStore.GetAll())
        {
            Exclusions.Add(new ExclusionRowViewModel(rule));
        }

        HasExclusions = Exclusions.Count > 0;
    }
}
