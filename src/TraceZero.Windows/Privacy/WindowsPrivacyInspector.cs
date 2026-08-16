using TraceZero.Application.Privacy;
using TraceZero.Domain;
using TraceZero.Engine.IO;

namespace TraceZero.Windows.Privacy;

/// <summary>
/// Inspecte les traces d'activité Windows (§15) et construit, pour chacune, un élément prêt à
/// nettoyer via le moteur (registre allowlisté ou balayage de fichiers validé).
/// </summary>
public sealed class WindowsPrivacyInspector : IPrivacyInspector
{
    private readonly IRegistryTraceCleaner _registryCleaner;
    private readonly IReadOnlyList<PrivacyTraceDefinition> _definitions;

    public WindowsPrivacyInspector(IRegistryTraceCleaner registryCleaner)
    {
        _registryCleaner = registryCleaner;
        _definitions = WindowsPrivacyCatalog.BuildDefinitions();
    }

    public IReadOnlyList<PrivacyTraceResult> Inspect()
    {
        var results = new List<PrivacyTraceResult>(_definitions.Count);

        foreach (var definition in _definitions)
        {
            results.Add(definition.Kind == PrivacyTraceKind.Registry
                ? InspectRegistry(definition)
                : InspectFile(definition));
        }

        return results;
    }

    private PrivacyTraceResult InspectRegistry(PrivacyTraceDefinition definition)
    {
        var count = _registryCleaner.CountEntries(definition.RegistrySubKey!);

        return new PrivacyTraceResult
        {
            Definition = definition,
            IsPresent = count > 0,
            EntryCount = count,
            SizeBytes = 0,
            CleanTarget = new ScanItem
            {
                Id = $"privacy.{definition.Id}",
                RuleId = $"privacy.{definition.Id}",
                Category = Category.PrivacyTrace,
                DisplayName = definition.DisplayName,
                Description = definition.Explanation,
                PathOrIdentifier = definition.RegistrySubKey!,
                SizeBytes = 0,
                ItemCount = count,
                Risk = RiskLevel.Privacy,
                SelectedByDefault = false,
                Reversibility = Reversibility.Irreversible,
                ActionKind = FileActionKind.ClearRegistryKey,
                AllowedRoots = [],
            },
        };
    }

    private static PrivacyTraceResult InspectFile(PrivacyTraceDefinition definition)
    {
        var root = definition.FileRoot!;
        long bytes = 0;
        var count = 0;

        foreach (var entry in SafeFileEnumerator.EnumerateEntries(root, recursive: true))
        {
            bytes += entry.Length;
            count++;
        }

        return new PrivacyTraceResult
        {
            Definition = definition,
            IsPresent = count > 0,
            EntryCount = count,
            SizeBytes = bytes,
            CleanTarget = new ScanItem
            {
                Id = $"privacy.{definition.Id}",
                RuleId = $"privacy.{definition.Id}",
                Category = Category.PrivacyTrace,
                DisplayName = definition.DisplayName,
                Description = definition.Explanation,
                PathOrIdentifier = root,
                SizeBytes = bytes,
                ItemCount = count,
                Risk = RiskLevel.Privacy,
                SelectedByDefault = false,
                Reversibility = Reversibility.Irreversible,
                ActionKind = FileActionKind.DeleteDirectoryContents,
                AllowedRoots = [root],
            },
        };
    }
}
