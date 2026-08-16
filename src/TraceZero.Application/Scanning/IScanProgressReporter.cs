namespace TraceZero.Application.Scanning;

/// <summary>
/// Permet à un fournisseur de signaler sa progression fine (fichiers examinés) pendant le balayage,
/// pour un retour visuel fluide (§23). Les implémentations doivent être sûres pour un appel
/// concurrent et limiter la fréquence des mises à jour.
/// </summary>
public interface IScanProgressReporter
{
    /// <summary>Signale que <paramref name="fileDelta"/> fichiers supplémentaires ont été examinés.</summary>
    void ReportFiles(int fileDelta, string currentPath);
}

/// <summary>Rapporteur inactif (tests, appels sans progression).</summary>
public sealed class NullScanProgressReporter : IScanProgressReporter
{
    public static NullScanProgressReporter Instance { get; } = new();

    public void ReportFiles(int fileDelta, string currentPath)
    {
    }
}
