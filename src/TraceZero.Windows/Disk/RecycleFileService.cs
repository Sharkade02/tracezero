using Microsoft.VisualBasic.FileIO;
using TraceZero.Application.Disk;

namespace TraceZero.Windows.Disk;

/// <summary>
/// Envoie un fichier à la Corbeille via l'API Shell (suppression réversible), jamais une suppression
/// définitive. Utilisé pour le nettoyage manuel des gros fichiers (§20).
/// </summary>
public sealed class RecycleFileService : IRecycleFileService
{
    public bool SendToRecycleBin(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return false;
            }

            FileSystem.DeleteFile(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or OperationCanceledException)
        {
            return false;
        }
    }
}
