using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TraceZero.Application.Diagnostics;
using TraceZero.Domain.Common;
using TraceZero.Domain.Diagnostics;

namespace TraceZero.App.ViewModels;

/// <summary>Ligne de santé d'un disque.</summary>
public sealed class DiskHealthRowViewModel
{
    public DiskHealthRowViewModel(DiskHealth disk)
    {
        Model = disk.Model;
        SizeText = disk.SizeBytes > 0 ? ByteSize.Format(disk.SizeBytes) : string.Empty;
        MediaText = disk.Media switch
        {
            DiskMediaKind.Hdd => "Disque dur (HDD)",
            DiskMediaKind.Ssd => "SSD",
            _ => "Type inconnu",
        };
        Status = disk.Status;
        StatusText = disk.Status switch
        {
            DiskHealthStatus.Healthy => "Sain",
            DiskHealthStatus.Warning => "Avertissement",
            DiskHealthStatus.Unhealthy => "Défaillant",
            _ => "État inconnu",
        };
        // Détail factuel, sans alarmisme, quand Windows signale un risque.
        WarningText = disk.Status is DiskHealthStatus.Warning or DiskHealthStatus.Unhealthy
            ? "Windows signale un risque sur ce disque — pensez à sauvegarder vos données."
            : null;
    }

    public string Model { get; }
    public string SizeText { get; }
    public string MediaText { get; }
    public DiskHealthStatus Status { get; }
    public string StatusText { get; }
    public string? WarningText { get; }
    public bool HasWarning => WarningText is not null;
}

/// <summary>Ligne d'impact au démarrage mesuré.</summary>
public sealed class StartupImpactRowViewModel
{
    public StartupImpactRowViewModel(StartupImpact impact)
    {
        Name = impact.Name;
        ImpactText = $"+{Math.Round(impact.AverageMs):N0} ms";
        SampleText = impact.SampleCount > 1
            ? $"moyenne sur {impact.SampleCount} démarrages"
            : "1 démarrage mesuré";
    }

    public string Name { get; }
    public string ImpactText { get; }
    public string SampleText { get; }
}

/// <summary>
/// Page « Santé système » (Phase 28) : santé disque (SMART/WMI) + impact au démarrage mesuré par
/// Windows. Tout est read-only, mesuré et expliqué ; aucun score inventé, aucun « booster » (§42).
/// </summary>
public sealed partial class SystemHealthViewModel : PageViewModelBase
{
    private readonly IDiskHealthService _diskHealth;
    private readonly IStartupImpactService _startupImpact;
    private bool _loaded;

    public SystemHealthViewModel(IDiskHealthService diskHealth, IStartupImpactService startupImpact)
    {
        _diskHealth = diskHealth;
        _startupImpact = startupImpact;
    }

    public override string Title => "Santé système";

    public override string IconGlyph => "\U0001FA7A"; // 🩺

    public override bool IsUnderConstruction => false;

    public ObservableCollection<DiskHealthRowViewModel> Disks { get; } = [];

    public ObservableCollection<StartupImpactRowViewModel> Impacts { get; } = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasDisks;

    [ObservableProperty]
    private bool _hasImpacts;

    [ObservableProperty]
    private string _impactMessage = string.Empty;

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
            var disks = await Task.Run(() => _diskHealth.GetDiskHealth());
            Disks.Clear();
            foreach (var disk in disks)
            {
                Disks.Add(new DiskHealthRowViewModel(disk));
            }

            HasDisks = Disks.Count > 0;

            var report = await Task.Run(() => _startupImpact.GetRecentImpacts());
            Impacts.Clear();
            foreach (var impact in report.Impacts)
            {
                Impacts.Add(new StartupImpactRowViewModel(impact));
            }

            HasImpacts = Impacts.Count > 0;
            ImpactMessage = !report.DataAvailable
                ? "Mesures indisponibles : la lecture du journal de performances de Windows requiert des droits administrateur."
                : Impacts.Count == 0
                    ? "Aucun impact au démarrage mesuré récemment par Windows."
                    : "Impact réellement mesuré par Windows sur vos derniers démarrages. Gérez les programmes dans « Applications ».";

            _loaded = true;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
