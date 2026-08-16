using System.Text.Json;
using System.Text.Json.Serialization;
using TraceZero.Application.Exclusions;
using TraceZero.Domain;
using TraceZero.Domain.Exclusions;

namespace TraceZero.Persistence;

/// <summary>
/// Stocke les règles d'exclusion dans un fichier JSON local (§16). Simple et lisible.
/// </summary>
public sealed class JsonExclusionStore : IExclusionStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _filePath;
    private readonly Lock _gate = new();
    private readonly List<ExclusionRule> _rules;

    public JsonExclusionStore(string filePath)
    {
        _filePath = filePath;
        _rules = Load(filePath);
    }

    public IReadOnlyList<ExclusionRule> GetAll()
    {
        lock (_gate)
        {
            return _rules.ToList();
        }
    }

    public void Add(ExclusionRule rule)
    {
        lock (_gate)
        {
            _rules.Add(rule);
            Save();
        }
    }

    public void Remove(Guid id)
    {
        lock (_gate)
        {
            _rules.RemoveAll(r => r.Id == id);
            Save();
        }
    }

    public bool IsExcluded(ScanItem item)
    {
        lock (_gate)
        {
            foreach (var rule in _rules)
            {
                if (rule.Kind == ExclusionKind.Category)
                {
                    if (Enum.TryParse<Category>(rule.Value, out var category) && item.Category == category)
                    {
                        return true;
                    }
                }
                else if (MatchesFolder(rule.Value, item))
                {
                    return true;
                }
            }

            return false;
        }
    }

    private static bool MatchesFolder(string folder, ScanItem item)
    {
        var normalizedFolder = NormalizeFolder(folder);
        if (normalizedFolder is null)
        {
            return false;
        }

        foreach (var path in EnumeratePaths(item))
        {
            string full;
            try
            {
                full = Path.GetFullPath(path);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                continue;
            }

            if (full.Equals(normalizedFolder.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase)
                || full.StartsWith(normalizedFolder, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> EnumeratePaths(ScanItem item)
    {
        yield return item.PathOrIdentifier;
        foreach (var root in item.SweepRoots)
        {
            yield return root;
        }

        foreach (var root in item.AllowedRoots)
        {
            yield return root;
        }
    }

    private static string? NormalizeFolder(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder))
        {
            return null;
        }

        try
        {
            var full = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar);
            return full + Path.DirectorySeparatorChar;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }

    private static List<ExclusionRule> Load(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return [];
            }

            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<ExclusionRule>>(json, Options) ?? [];
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    private void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(_filePath, JsonSerializer.Serialize(_rules, Options));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Persistance best-effort ; l'exclusion reste active en mémoire pour la session.
        }
    }
}
