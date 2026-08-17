using TraceZero.Application.Rules;
using TraceZero.Domain;

namespace TraceZero.Engine.Rules;

/// <summary>
/// Catalogue des règles de nettoyage Windows standard (Phase 3).
///
/// Choix de sécurité : en Phase 3, on ne cible que des emplacements SOUS le profil utilisateur
/// (AppData\Local…), jamais sous C:\Windows. Les caches situés dans l'arborescence Windows
/// (ex. C:\Windows\Temp) sont refusés par <c>ISafePathValidator</c> et sont volontairement différés
/// (voir KNOWN_LIMITATIONS.md) : ils nécessiteront une élévation et une liste d'autorisation dédiée.
/// </summary>
public static class WindowsCleaningRules
{
    public static IReadOnlyList<FileSweepRule> BuildDefaultRules()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var userTemp = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);

        var rules = new List<FileSweepRule>();

        if (!string.IsNullOrEmpty(userTemp))
        {
            rules.Add(new FileSweepRule
            {
                Id = "windows.user-temp",
                DisplayName = "Fichiers temporaires (session)",
                NameKey = "Rule.UserTemp.Name",
                Description = "Fichiers temporaires créés par vos applications dans le dossier TEMP de votre session.",
                DescriptionKey = "Rule.UserTemp.Desc",
                Category = Category.WindowsTemp,
                Risk = RiskLevel.Safe,
                Roots = [userTemp],
                Recursive = true,
                PreserveRoot = true,
                SelectedByDefault = true,
                MinimumAge = TimeSpan.FromHours(1),
            });
        }

        if (!string.IsNullOrEmpty(localAppData))
        {
            rules.Add(new FileSweepRule
            {
                Id = "windows.crash-dumps",
                DisplayName = "Rapports de plantage",
                NameKey = "Rule.CrashDumps.Name",
                Description = "Vidages mémoire générés lorsqu'une application plante (CrashDumps).",
                DescriptionKey = "Rule.CrashDumps.Desc",
                Category = Category.CrashDumps,
                Risk = RiskLevel.Safe,
                Roots = [Path.Combine(localAppData, "CrashDumps")],
                Recursive = true,
                PreserveRoot = true,
                SelectedByDefault = true,
            });

            rules.Add(new FileSweepRule
            {
                Id = "windows.wer",
                DisplayName = "Rapports d'erreurs Windows (WER)",
                NameKey = "Rule.Wer.Name",
                Description = "Archives de rapports d'erreurs Windows en attente d'envoi.",
                DescriptionKey = "Rule.Wer.Desc",
                Category = Category.SystemLogs,
                Risk = RiskLevel.Safe,
                Roots =
                [
                    Path.Combine(localAppData, "Microsoft", "Windows", "WER", "ReportArchive"),
                    Path.Combine(localAppData, "Microsoft", "Windows", "WER", "ReportQueue"),
                ],
                Recursive = true,
                PreserveRoot = true,
                SelectedByDefault = true,
            });

            rules.Add(new FileSweepRule
            {
                Id = "windows.inetcache",
                DisplayName = "Cache Internet hérité",
                NameKey = "Rule.InetCache.Name",
                Description = "Cache des composants Windows / Internet Explorer (INetCache).",
                DescriptionKey = "Rule.InetCache.Desc",
                Category = Category.WindowsCache,
                Risk = RiskLevel.Safe,
                Roots = [Path.Combine(localAppData, "Microsoft", "Windows", "INetCache")],
                Recursive = true,
                PreserveRoot = true,
                SelectedByDefault = true,
            });

            rules.Add(new FileSweepRule
            {
                Id = "windows.thumbnails",
                DisplayName = "Miniatures et icônes en cache",
                NameKey = "Rule.Thumbnails.Name",
                Description = "Vignettes et icônes mises en cache par l'Explorateur. Régénérées automatiquement.",
                DescriptionKey = "Rule.Thumbnails.Desc",
                Category = Category.ThumbnailCache,
                Risk = RiskLevel.Safe,
                Roots = [Path.Combine(localAppData, "Microsoft", "Windows", "Explorer")],
                Recursive = false,
                IncludeGlobs = ["thumbcache_*.db", "iconcache_*.db"],
                PreserveRoot = true,
                SelectedByDefault = true,
            });
        }

        return rules;
    }
}
