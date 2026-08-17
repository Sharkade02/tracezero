using System.Security.Cryptography;
using TraceZero.Application.Erasure;
using TraceZero.Application.Safety;
using TraceZero.Domain.Erasure;

namespace TraceZero.Engine.Erasure;

/// <summary>
/// Effacement sécurisé de fichier (§19). La cible est choisie par l'utilisateur, mais le garde-fou
/// refuse tout ce qui est dangereux : dossiers système (Windows, Program Files), racines de volume,
/// points d'analyse (jonctions/liens), répertoires et fichiers absents. L'écrasement précède la
/// suppression ; sur SSD/NVMe le résultat n'est jamais présenté comme garanti (voir l'UI).
/// </summary>
public sealed class SecureEraser : ISecureFileEraser
{
    private const int BufferSize = 1 << 20; // 1 Mo

    private readonly IKnownFolders _knownFolders;

    public SecureEraser(IKnownFolders knownFolders) => _knownFolders = knownFolders;

    public string? ValidateTarget(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "Aucun fichier indiqué.";
        }

        string full;
        try
        {
            full = Path.GetFullPath(path);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return "Chemin invalide.";
        }

        if (!File.Exists(full))
        {
            return Directory.Exists(full)
                ? "Cible refusée : c'est un dossier, pas un fichier."
                : "Fichier introuvable.";
        }

        // Racine de volume (ex. C:\) — jamais.
        var root = Path.GetPathRoot(full);
        if (string.Equals(full.TrimEnd(Path.DirectorySeparatorChar), root?.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
        {
            return "Cible refusée : racine de volume.";
        }

        // Jonctions / liens symboliques — jamais suivis.
        try
        {
            if ((File.GetAttributes(full) & FileAttributes.ReparsePoint) != 0)
            {
                return "Cible refusée : point d'analyse (jonction/lien).";
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return "Cible inaccessible.";
        }

        // Dossiers système protégés (Windows, Program Files…).
        foreach (var container in _knownFolders.ForbiddenSystemContainers)
        {
            if (IsUnder(full, container))
            {
                return "Cible refusée : fichier système protégé.";
            }
        }

        return null;
    }

    public async Task<SecureEraseResult> EraseFileAsync(
        string path, SecureEraseMethod method, CancellationToken cancellationToken = default)
    {
        var rejection = ValidateTarget(path);
        if (rejection is not null)
        {
            return new SecureEraseResult { Success = false, Path = path, Error = rejection };
        }

        var full = Path.GetFullPath(path);
        var passes = method == SecureEraseMethod.ReinforcedOverwrite ? 3 : 1;

        try
        {
            // On retire l'attribut lecture seule pour pouvoir écrire, sans jamais forcer un verrou.
            var attributes = File.GetAttributes(full);
            if ((attributes & FileAttributes.ReadOnly) != 0)
            {
                File.SetAttributes(full, attributes & ~FileAttributes.ReadOnly);
            }

            await OverwriteAsync(full, passes, cancellationToken);

            File.Delete(full);

            return new SecureEraseResult { Success = true, Path = full, PassesApplied = passes };
        }
        catch (OperationCanceledException)
        {
            return new SecureEraseResult { Success = false, Path = full, Error = "Effacement annulé." };
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            // Fichier verrouillé / inaccessible : jamais forcé, échec honnête.
            return new SecureEraseResult { Success = false, Path = full, Error = "Fichier verrouillé ou inaccessible." };
        }
    }

    private static async Task OverwriteAsync(string path, int passes, CancellationToken cancellationToken)
    {
        var length = new FileInfo(path).Length;

        // FileOptions.WriteThrough : on force l'écriture réelle sur le support à chaque passe.
        await using var stream = new FileStream(
            path, FileMode.Open, FileAccess.Write, FileShare.None, BufferSize, FileOptions.WriteThrough);

        var buffer = new byte[BufferSize];

        for (var pass = 0; pass < passes; pass++)
        {
            stream.Seek(0, SeekOrigin.Begin);
            FillBuffer(buffer, pass);

            var remaining = length;
            while (remaining > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var chunk = (int)Math.Min(buffer.Length, remaining);
                if (pass % 2 == 0)
                {
                    // Passes aléatoires (0 et 2).
                    RandomNumberGenerator.Fill(buffer.AsSpan(0, chunk));
                }

                await stream.WriteAsync(buffer.AsMemory(0, chunk), cancellationToken);
                remaining -= chunk;
            }

            await stream.FlushAsync(cancellationToken);
        }
    }

    private static void FillBuffer(byte[] buffer, int pass)
    {
        // Passe impaire = motif fixe (0xFF) ; passes paires remplies aléatoirement à la volée.
        if (pass % 2 == 1)
        {
            Array.Fill(buffer, (byte)0xFF);
        }
    }

    private static bool IsUnder(string path, string container)
    {
        if (string.IsNullOrEmpty(container))
        {
            return false;
        }

        var normalizedContainer = container.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return path.StartsWith(normalizedContainer, StringComparison.OrdinalIgnoreCase)
            || string.Equals(path.TrimEnd(Path.DirectorySeparatorChar), container.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
    }
}
