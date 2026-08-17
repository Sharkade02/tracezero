using System.Management;
using TraceZero.Application.Diagnostics;
using TraceZero.Domain.Diagnostics;

namespace TraceZero.Windows.Diagnostics;

/// <summary>
/// Inventaire des pilotes via WMI (Phase 14, étape A), en lecture seule. Interroge
/// <c>Win32_PnPSignedDriver</c> (version, fournisseur, date, signature) et croise avec
/// <c>Win32_PnPEntity</c> pour les périphériques signalés en problème par le Gestionnaire de
/// périphériques. TraceZero **n'installe ni ne télécharge aucun pilote** (§24) : cette classe ne fait
/// que lire ce que Windows expose.
/// </summary>
public sealed class DriverHealthService : IDriverHealthService
{
    public IReadOnlyList<DriverInfo> GetDrivers()
    {
        var problems = GetProblemDevices();
        var drivers = new List<DriverInfo>();

        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT DeviceName, DeviceClass, DriverVersion, DriverProviderName, Manufacturer, DriverDate, IsSigned, DeviceID FROM Win32_PnPSignedDriver");
            using var results = searcher.Get();

            foreach (var raw in results)
            {
                using var driver = (ManagementObject)raw;

                var deviceName = driver["DeviceName"]?.ToString();
                if (string.IsNullOrWhiteSpace(deviceName))
                {
                    continue; // Entrée sans périphérique nommé : ignorée.
                }

                var deviceId = driver["DeviceID"]?.ToString();
                var hasProblem = deviceId is not null && problems.TryGetValue(deviceId, out _);

                drivers.Add(new DriverInfo
                {
                    DeviceName = deviceName,
                    DeviceClass = driver["DeviceClass"]?.ToString(),
                    Version = driver["DriverVersion"]?.ToString(),
                    Provider = driver["DriverProviderName"]?.ToString(),
                    Manufacturer = driver["Manufacturer"]?.ToString(),
                    Date = ParseCimDate(driver["DriverDate"]?.ToString()),
                    IsSigned = driver["IsSigned"] is bool signed && signed,
                    HasProblem = hasProblem,
                    ProblemCode = hasProblem ? problems[deviceId!] : 0,
                });
            }
        }
        catch (Exception ex) when (ex is ManagementException or UnauthorizedAccessException or System.Runtime.InteropServices.COMException)
        {
            // WMI indisponible : liste vide plutôt qu'invention.
        }

        return drivers;
    }

    private static Dictionary<string, int> GetProblemDevices()
    {
        var problems = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT PNPDeviceID, ConfigManagerErrorCode FROM Win32_PnPEntity WHERE ConfigManagerErrorCode <> 0");
            using var results = searcher.Get();

            foreach (var raw in results)
            {
                using var entity = (ManagementObject)raw;
                var id = entity["PNPDeviceID"]?.ToString();
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                var code = entity["ConfigManagerErrorCode"] is { } value
                    ? Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture)
                    : 0;
                problems[id] = code;
            }
        }
        catch (Exception ex) when (ex is ManagementException or UnauthorizedAccessException or System.Runtime.InteropServices.COMException)
        {
        }

        return problems;
    }

    /// <summary>
    /// Convertit une date WMI CIM_DATETIME (ex. « 20230115000000.000000-000 ») en <see cref="DateOnly"/>.
    /// Public et pur pour être testable. Retourne <c>null</c> si le format est inattendu.
    /// </summary>
    public static DateOnly? ParseCimDate(string? cim)
    {
        if (string.IsNullOrWhiteSpace(cim) || cim.Length < 8)
        {
            return null;
        }

        var datePart = cim[..8];
        if (!int.TryParse(datePart.AsSpan(0, 4), out var year)
            || !int.TryParse(datePart.AsSpan(4, 2), out var month)
            || !int.TryParse(datePart.AsSpan(6, 2), out var day))
        {
            return null;
        }

        if (year is < 1900 or > 9999 || month is < 1 or > 12 || day is < 1 or > 31)
        {
            return null;
        }

        try
        {
            return new DateOnly(year, month, day);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }
}
