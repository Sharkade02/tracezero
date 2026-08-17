using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TraceZero.App.Services;
using TraceZero.Application.Diagnostics;
using TraceZero.Domain.Diagnostics;

namespace TraceZero.App.ViewModels;

/// <summary>Ligne d'inventaire d'un pilote (lecture seule).</summary>
public sealed class DriverRowViewModel
{
    public DriverRowViewModel(DriverInfo driver)
    {
        Model = driver;
        DeviceName = driver.DeviceName;
        DeviceClass = driver.DeviceClass ?? string.Empty;
        Version = string.IsNullOrWhiteSpace(driver.Version) ? "—" : driver.Version!;
        Provider = driver.Provider ?? driver.Manufacturer ?? Localizer.Get("Drivers.UnknownProvider");
        DateText = driver.Date?.ToString("d", CultureInfo.CurrentCulture) ?? "—";
        HasProblem = driver.HasProblem;

        StatusText = driver.HasProblem
            ? Localizer.Format("Drivers.Problem", driver.ProblemCode)
            : driver.IsSigned ? Localizer.Get("Drivers.Signed") : Localizer.Get("Drivers.Unsigned");
    }

    public DriverInfo Model { get; }
    public string DeviceName { get; }
    public string DeviceClass { get; }
    public string Version { get; }
    public string Provider { get; }
    public string DateText { get; }
    public bool HasProblem { get; }
    public string StatusText { get; }
}

/// <summary>
/// Page « Pilotes » (Phase 14, étape A — Driver Health). Inventaire en lecture seule (WMI). TraceZero
/// n'installe ni ne télécharge aucun pilote : la mise à jour est déléguée à Windows Update (§24),
/// jamais à une base tierce.
/// </summary>
public sealed partial class DriverHealthViewModel : PageViewModelBase
{
    private readonly IDriverHealthService _driverService;
    private readonly IToastService _toasts;
    private readonly List<DriverRowViewModel> _all = [];
    private bool _loaded;

    public DriverHealthViewModel(IDriverHealthService driverService, IToastService toasts)
    {
        _driverService = driverService;
        _toasts = toasts;
    }

    public override string Title => TraceZero.App.Services.Localizer.Get("Nav.Drivers");

    public override string IconGlyph => "\U0001F527"; // 🔧

    public override bool IsUnderConstruction => false;

    public ObservableCollection<DriverRowViewModel> Drivers { get; } = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasDrivers;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _summaryText = string.Empty;

    partial void OnSearchTextChanged(string value) => ApplyFilter();

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
            var drivers = await Task.Run(() => _driverService.GetDrivers());
            _all.Clear();
            _all.AddRange(drivers
                .OrderByDescending(d => d.HasProblem)
                .ThenBy(d => d.DeviceName, StringComparer.CurrentCultureIgnoreCase)
                .Select(d => new DriverRowViewModel(d)));

            ApplyFilter();
            _loaded = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilter()
    {
        var query = SearchText?.Trim() ?? string.Empty;
        IEnumerable<DriverRowViewModel> filtered = _all;
        if (!string.IsNullOrEmpty(query))
        {
            filtered = _all.Where(d =>
                d.DeviceName.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                || d.Provider.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                || d.DeviceClass.Contains(query, StringComparison.CurrentCultureIgnoreCase));
        }

        Drivers.Clear();
        foreach (var driver in filtered)
        {
            Drivers.Add(driver);
        }

        HasDrivers = Drivers.Count > 0;

        var problemCount = _all.Count(d => d.HasProblem);
        SummaryText = problemCount > 0
            ? Localizer.Format("Drivers.SummaryProblems", _all.Count, problemCount)
            : Localizer.Format("Drivers.SummaryOk", _all.Count);
    }

    [RelayCommand]
    private void OpenWindowsUpdate()
    {
        try
        {
            Process.Start(new ProcessStartInfo("ms-settings:windowsupdate") { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            _toasts.Show("Impossible d'ouvrir Windows Update.", ToastKind.Error);
        }
    }
}
