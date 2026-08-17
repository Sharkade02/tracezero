using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TraceZero.App.Services;
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
            DiskMediaKind.Hdd => Localizer.Get("Health.Hdd"),
            DiskMediaKind.Ssd => Localizer.Get("Health.Ssd"),
            _ => Localizer.Get("Health.UnknownMedia"),
        };
        Status = disk.Status;
        StatusText = disk.Status switch
        {
            DiskHealthStatus.Healthy => Localizer.Get("Health.Healthy"),
            DiskHealthStatus.Warning => Localizer.Get("Health.Warning"),
            DiskHealthStatus.Unhealthy => Localizer.Get("Health.Unhealthy"),
            _ => Localizer.Get("Health.Unknown"),
        };
        // Détail factuel, sans alarmisme, quand Windows signale un risque.
        WarningText = disk.Status is DiskHealthStatus.Warning or DiskHealthStatus.Unhealthy
            ? Localizer.Get("Health.WarningText")
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

/// <summary>Ligne d'un module de mémoire (RAM).</summary>
public sealed class MemoryModuleRowViewModel
{
    public MemoryModuleRowViewModel(MemoryModule module)
    {
        Slot = module.Slot;
        Model = string.IsNullOrWhiteSpace(module.PartNumber)
            ? module.Manufacturer ?? Localizer.Get("Mem.UnknownModel")
            : module.PartNumber!;
        CapacityText = ByteSize.Format(module.CapacityBytes);
        TypeText = module.Type switch
        {
            MemoryType.Ddr5 => "DDR5",
            MemoryType.Ddr4 => "DDR4",
            MemoryType.Ddr3 => "DDR3",
            _ => Localizer.Get("Health.UnknownMedia"),
        };

        // Fréquence réelle, avec la nominale si elle diffère.
        var configured = module.ConfiguredSpeedMhz > 0 ? module.ConfiguredSpeedMhz : module.RatedSpeedMhz;
        SpeedText = module.RatedSpeedMhz > 0 && module.RatedSpeedMhz != configured
            ? Localizer.Format("Mem.SpeedWithRated", configured, module.RatedSpeedMhz)
            : $"{configured} MHz";

        VoltageText = module.VoltageMillivolts > 0
            ? (module.VoltageMillivolts / 1000.0).ToString("0.00", System.Globalization.CultureInfo.CurrentCulture) + " V"
            : string.Empty;
    }

    public string Slot { get; }
    public string Model { get; }
    public string CapacityText { get; }
    public string TypeText { get; }
    public string SpeedText { get; }
    public string VoltageText { get; }
    public bool HasVoltage => !string.IsNullOrEmpty(VoltageText);
}

/// <summary>Ligne d'impact au démarrage mesuré.</summary>
public sealed class StartupImpactRowViewModel
{
    public StartupImpactRowViewModel(StartupImpact impact)
    {
        Name = impact.Name;
        ImpactText = $"+{Math.Round(impact.AverageMs):N0} ms";
        SampleText = impact.SampleCount > 1
            ? Localizer.Format("Impact.Average", impact.SampleCount)
            : Localizer.Get("Impact.Single");
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
    private readonly IMemoryInfoService _memoryInfo;
    private bool _loaded;

    public SystemHealthViewModel(
        IDiskHealthService diskHealth,
        IStartupImpactService startupImpact,
        IMemoryInfoService memoryInfo)
    {
        _diskHealth = diskHealth;
        _startupImpact = startupImpact;
        _memoryInfo = memoryInfo;
    }

    public override string Title => TraceZero.App.Services.Localizer.Get("Nav.SystemHealth");

    public override string IconGlyph => "\U0001FA7A"; // 🩺

    public override bool IsUnderConstruction => false;

    public ObservableCollection<DiskHealthRowViewModel> Disks { get; } = [];

    public ObservableCollection<StartupImpactRowViewModel> Impacts { get; } = [];

    public ObservableCollection<MemoryModuleRowViewModel> Memory { get; } = [];

    [ObservableProperty]
    private bool _hasMemory;

    [ObservableProperty]
    private string _memorySummaryText = string.Empty;

    [ObservableProperty]
    private string _xmpText = string.Empty;

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
                ? Localizer.Get("Impact.Unavailable")
                : Impacts.Count == 0
                    ? Localizer.Get("Impact.None")
                    : Localizer.Get("Impact.Measured");

            var memory = await Task.Run(() => _memoryInfo.GetMemory());
            Memory.Clear();
            foreach (var module in memory.Modules)
            {
                Memory.Add(new MemoryModuleRowViewModel(module));
            }

            HasMemory = Memory.Count > 0;
            if (HasMemory)
            {
                MemorySummaryText = Localizer.Format(
                    "Mem.Summary",
                    ByteSize.Format(memory.TotalCapacityBytes),
                    memory.UsedSlots,
                    memory.TotalSlots,
                    memory.MaxCapacityBytes > 0 ? ByteSize.Format(memory.MaxCapacityBytes) : "?");
                XmpText = InferXmp(memory);
            }

            _loaded = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Inférence honnête du profil de performance (XMP/EXPO) : si un module tourne au-dessus de la
    /// fréquence de base JEDEC de son type, un profil est probablement actif. Ce n'est **pas** une lecture
    /// du SPD (impossible sans accès matériel bas niveau) — c'est une déduction, présentée comme telle.
    /// </summary>
    private static string InferXmp(MemoryReport memory)
    {
        var known = memory.Modules.Where(m => m.Type != MemoryType.Unknown).ToList();
        if (known.Count == 0)
        {
            return Localizer.Get("Mem.XmpUnknown");
        }

        var active = known.Any(m =>
        {
            var speed = m.ConfiguredSpeedMhz > 0 ? m.ConfiguredSpeedMhz : m.RatedSpeedMhz;
            var jedecBase = m.Type switch
            {
                MemoryType.Ddr5 => 4800,
                MemoryType.Ddr4 => 2133,
                MemoryType.Ddr3 => 1333,
                _ => int.MaxValue,
            };
            return speed > jedecBase;
        });

        return active ? Localizer.Get("Mem.XmpActive") : Localizer.Get("Mem.XmpInactive");
    }
}
