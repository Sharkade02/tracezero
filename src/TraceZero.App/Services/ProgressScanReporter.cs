using TraceZero.Application.Scanning;

namespace TraceZero.App.Services;

/// <summary>
/// Adapte <see cref="IScanProgressReporter"/> vers un <see cref="IProgress{T}"/> de nombre de
/// fichiers, qui marshalle vers le thread UI. Sûr à appeler depuis un thread d'arrière-plan.
/// </summary>
public sealed class ProgressScanReporter(IProgress<long> filesProgress) : IScanProgressReporter
{
    private long _total;

    public void ReportFiles(int fileDelta, string currentPath)
    {
        var total = Interlocked.Add(ref _total, fileDelta);
        filesProgress.Report(total);
    }
}
