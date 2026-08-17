using System.Security.Cryptography;
using TraceZero.Application.Erasure;
using TraceZero.Domain.Erasure;

namespace TraceZero.Engine.Erasure;

/// <summary>
/// Effacement de l'espace libre d'un lecteur (§19). Écrit un unique fichier temporaire de remplissage
/// jusqu'à saturation (ou une limite), puis le supprime : les fichiers existants ne sont <b>jamais</b>
/// touchés. Opération annulable, avec estimation. Sur SSD/NVMe, l'efficacité n'est pas garantie
/// (l'avertissement honnête est présenté par l'UI) — cette classe se contente d'écrire de façon contrôlée.
/// </summary>
public sealed class FreeSpaceWiper : IFreeSpaceWiper
{
    private const int BufferSize = 1 << 20; // 1 Mo

    public async Task<FreeSpaceWipeResult> WipeAsync(
        string workingDirectory,
        long maxBytes,
        IProgress<FreeSpaceWipeProgress>? progress,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workingDirectory) || !Directory.Exists(workingDirectory))
        {
            return new FreeSpaceWipeResult { Success = false, BytesWritten = 0, Error = "Dossier de travail introuvable." };
        }

        long estimate;
        try
        {
            var root = Path.GetPathRoot(Path.GetFullPath(workingDirectory)) ?? workingDirectory;
            var available = new DriveInfo(root).AvailableFreeSpace;
            estimate = maxBytes > 0 ? Math.Min(maxBytes, available) : available;
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
        {
            estimate = maxBytes;
        }

        var fillPath = Path.Combine(workingDirectory, $"tracezero-freespace-{Guid.NewGuid():N}.tmp");
        var buffer = new byte[BufferSize];
        RandomNumberGenerator.Fill(buffer);

        long written = 0;
        var canceled = false;

        try
        {
            await using (var stream = new FileStream(
                fillPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, BufferSize, FileOptions.WriteThrough))
            {
                while (maxBytes <= 0 || written < maxBytes)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        canceled = true;
                        break;
                    }

                    var chunk = maxBytes > 0 ? (int)Math.Min(buffer.Length, maxBytes - written) : buffer.Length;

                    try
                    {
                        await stream.WriteAsync(buffer.AsMemory(0, chunk), cancellationToken);
                    }
                    catch (IOException)
                    {
                        // Disque plein : c'est le but recherché, on s'arrête proprement.
                        break;
                    }

                    written += chunk;
                    progress?.Report(new FreeSpaceWipeProgress { BytesWritten = written, EstimatedTotalBytes = estimate });
                }

                await stream.FlushAsync(CancellationToken.None);
            }

            return new FreeSpaceWipeResult { Success = !canceled, BytesWritten = written, Canceled = canceled };
        }
        catch (OperationCanceledException)
        {
            return new FreeSpaceWipeResult { Success = false, BytesWritten = written, Canceled = true };
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new FreeSpaceWipeResult { Success = false, BytesWritten = written, Error = "Écriture impossible sur ce lecteur." };
        }
        finally
        {
            // Le fichier de remplissage est toujours retiré, y compris en cas d'annulation/erreur.
            TryDelete(fillPath);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}
