using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TraceZero.App.Services;
using TraceZero.Application.Ntfs;
using TraceZero.Domain.Ntfs;

namespace TraceZero.App.ViewModels;

/// <summary>Ligne d'un artefact NTFS analysé.</summary>
public sealed class NtfsArtifactRowViewModel
{
    public NtfsArtifactRowViewModel(NtfsArtifact artifact)
    {
        Model = artifact;
        Name = artifact.Name;
        Explanation = artifact.Explanation;
        Why = artifact.Why;
        Detail = artifact.Detail ?? string.Empty;
        IsMitigable = artifact.Status == NtfsArtifactStatus.MitigableByFreeSpaceWipe;
        StatusText = IsMitigable ? "Atténuable" : "Détectée";
    }

    public NtfsArtifact Model { get; }
    public string Name { get; }
    public string Explanation { get; }
    public string Why { get; }
    public string Detail { get; }
    public bool IsMitigable { get; }
    public string StatusText { get; }
    public bool HasDetail => !string.IsNullOrEmpty(Detail);
}

/// <summary>
/// Page « Analyse NTFS (Expert) » (Phase 8, §18). Lecture seule : décrit les traces NTFS et leur statut
/// honnête. Rien n'est « nettoyable » ici ni simulé — seul l'espace libre est atténuable, en renvoyant
/// vers le module d'effacement sécurisé (Phase 9).
/// </summary>
public sealed partial class NtfsAnalysisViewModel : PageViewModelBase
{
    private readonly INtfsAnalyzer _analyzer;
    private readonly INavigationService _navigation;
    private readonly SecureEraseViewModel _secureErase;
    private bool _loaded;

    public NtfsAnalysisViewModel(INtfsAnalyzer analyzer, INavigationService navigation, SecureEraseViewModel secureErase)
    {
        _analyzer = analyzer;
        _navigation = navigation;
        _secureErase = secureErase;
    }

    public override string Title => "Analyse NTFS";

    public override string IconGlyph => "\U0001F50E"; // 🔎

    public override bool IsUnderConstruction => false;

    public ObservableCollection<NtfsArtifactRowViewModel> Artifacts { get; } = [];

    [ObservableProperty]
    private bool _isLoading;

    public override void OnActivated()
    {
        if (!_loaded)
        {
            _ = RefreshAsync();
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsLoading = true;
        try
        {
            var results = await Task.Run(() => _analyzer.Analyze());
            Artifacts.Clear();
            foreach (var artifact in results)
            {
                Artifacts.Add(new NtfsArtifactRowViewModel(artifact));
            }

            _loaded = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void GoToSecureErase() => _navigation.RequestNavigate(_secureErase);
}
