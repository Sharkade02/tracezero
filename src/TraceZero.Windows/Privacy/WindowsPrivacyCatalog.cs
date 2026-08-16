using TraceZero.Application.Privacy;

namespace TraceZero.Windows.Privacy;

/// <summary>
/// Catalogue des traces d'activité Windows inspectées (§15). Chaque trace est expliquée en langage
/// humain. Les clés registre listées ici forment aussi la liste d'autorisation du nettoyeur.
/// </summary>
public static class WindowsPrivacyCatalog
{
    private const string ExplorerKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer";

    public static IReadOnlyList<PrivacyTraceDefinition> BuildDefinitions()
    {
        var recent = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Microsoft", "Windows", "Recent");

        return
        [
            Registry("recentdocs", "Documents récemment ouverts",
                "Windows garde la liste des fichiers que vous avez ouverts récemment, par type.",
                "Pour vous les reproposer rapidement dans l'Explorateur et les applications.",
                $@"{ExplorerKey}\RecentDocs"),

            Registry("runmru", "Commandes « Exécuter »",
                "Windows mémorise les commandes que vous avez tapées dans la boîte de dialogue Exécuter (Win+R).",
                "Pour les compléter automatiquement la prochaine fois.",
                $@"{ExplorerKey}\RunMRU"),

            Registry("typedpaths", "Chemins tapés dans l'Explorateur",
                "Windows retient les chemins et adresses saisis dans la barre d'adresse de l'Explorateur.",
                "Pour l'auto-complétion de la barre d'adresse.",
                $@"{ExplorerKey}\TypedPaths"),

            Registry("wordwheelquery", "Recherches dans l'Explorateur",
                "Windows conserve l'historique de vos recherches effectuées dans l'Explorateur de fichiers.",
                "Pour vous resuggérer vos recherches précédentes.",
                $@"{ExplorerKey}\WordWheelQuery"),

            Registry("userassist", "Programmes lancés",
                "Windows compte et date le lancement des programmes que vous utilisez (UserAssist).",
                "Pour classer vos applications les plus utilisées dans le menu Démarrer.",
                $@"{ExplorerKey}\UserAssist"),

            Registry("lastvisitedpidlmru", "Dossiers des boîtes Ouvrir/Enregistrer",
                "Windows retient les derniers dossiers utilisés dans les fenêtres « Ouvrir » et « Enregistrer sous ».",
                "Pour rouvrir les boîtes de dialogue au dernier emplacement utilisé par application.",
                $@"{ExplorerKey}\ComDlg32\LastVisitedPidlMRU"),

            Registry("opensavepidlmru", "Fichiers Ouvrir/Enregistrer",
                "Windows mémorise les fichiers récemment ouverts ou enregistrés via les boîtes de dialogue, par extension.",
                "Pour vous les reproposer dans les fenêtres « Ouvrir » et « Enregistrer sous ».",
                $@"{ExplorerKey}\ComDlg32\OpenSavePidlMRU"),

            File("recent-jumplists", "Documents récents & Jump Lists",
                "Raccourcis vers vos documents récents et listes de raccourcis (Jump Lists) des applications de la barre des tâches.",
                "Pour afficher vos fichiers récents dans l'Explorateur et au clic droit sur les icônes de la barre des tâches.",
                recent),
        ];
    }

    public static IReadOnlyList<string> RegistryAllowList() =>
        BuildDefinitions()
            .Where(d => d.Kind == PrivacyTraceKind.Registry && d.RegistrySubKey is not null)
            .Select(d => d.RegistrySubKey!)
            .ToList();

    private static PrivacyTraceDefinition Registry(string id, string name, string explanation, string why, string subKey) => new()
    {
        Id = id,
        DisplayName = name,
        Explanation = explanation,
        Why = why,
        Kind = PrivacyTraceKind.Registry,
        RegistrySubKey = subKey,
    };

    private static PrivacyTraceDefinition File(string id, string name, string explanation, string why, string root) => new()
    {
        Id = id,
        DisplayName = name,
        Explanation = explanation,
        Why = why,
        Kind = PrivacyTraceKind.File,
        FileRoot = root,
    };
}
