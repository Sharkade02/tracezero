using System.Globalization;
using System.Management;
using TraceZero.Application.Diagnostics;
using TraceZero.Domain.Diagnostics;

namespace TraceZero.Windows.Diagnostics;

/// <summary>
/// Indice de performance Windows (WinSAT) via WMI (read-only, sans admin). Lit <c>Win32_WinSAT</c> :
/// scores CPU / mémoire / disque / graphismes et le score de base (<c>WinSPRLevel</c>), calculés par
/// Windows lui-même. Le rapport est marqué non évalué si aucune évaluation valide n'est en cache
/// (<c>WinSATAssessmentState</c> ≠ 1) — l'utilisateur peut la relancer via <c>winsat formal</c>.
/// </summary>
public sealed class PerformanceIndexService : IPerformanceIndexService
{
    // WinSATAssessmentState : 1 = Valid (une évaluation cohérente est disponible).
    private const int StateValid = 1;

    public PerformanceIndex GetIndex()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT CPUScore, MemoryScore, DiskScore, GraphicsScore, D3DScore, WinSPRLevel, WinSATAssessmentState FROM Win32_WinSAT");
            using var results = searcher.Get();

            foreach (var raw in results)
            {
                using var sat = (ManagementObject)raw;
                var assessed = AsInt(sat["WinSATAssessmentState"]) == StateValid;
                if (!assessed)
                {
                    return PerformanceIndex.Unavailable;
                }

                return new PerformanceIndex
                {
                    BaseScore = AsDouble(sat["WinSPRLevel"]),
                    CpuScore = AsDouble(sat["CPUScore"]),
                    MemoryScore = AsDouble(sat["MemoryScore"]),
                    DiskScore = AsDouble(sat["DiskScore"]),
                    GraphicsScore = AsDouble(sat["GraphicsScore"]),
                    GamingGraphicsScore = AsDouble(sat["D3DScore"]),
                    Assessed = true,
                };
            }
        }
        catch (Exception ex) when (ex is ManagementException or UnauthorizedAccessException or System.Runtime.InteropServices.COMException)
        {
        }

        return PerformanceIndex.Unavailable;
    }

    private static int AsInt(object? value)
    {
        try
        {
            return value is null ? 0 : Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            return 0;
        }
    }

    private static double AsDouble(object? value)
    {
        try
        {
            return value is null ? 0 : Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            return 0;
        }
    }
}
