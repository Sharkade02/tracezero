using System.Runtime.CompilerServices;
using TraceZero.Application.Browsers;
using TraceZero.Application.Scanning;
using TraceZero.Domain;
using TraceZero.Domain.Browsers;
using TraceZero.Engine.IO;

namespace TraceZero.Browsers;

/// <summary>
/// Fournisseur de scan des traces de confidentialité des navigateurs (Phase 4) : historique de
/// navigation, cookies et sessions/onglets. Contrairement aux caches, ces données peuvent être
/// souhaitées par l'utilisateur — aucun élément n'est coché par défaut (§3.2), la suppression est
/// honnêtement présentée comme irréversible (le moteur supprime définitivement), et rien n'est
/// touché tant que le navigateur est ouvert (fichiers verrouillés : signalés, jamais forcés — §14).
///
/// Ce qui n'est JAMAIS ciblé : favoris, mots de passe (« Login Data »), données de formulaire.
/// Pour Firefox, l'historique n'est pas proposé : « places.sqlite » contient aussi les favoris —
/// le supprimer entier les perdrait (voir KNOWN_LIMITATIONS.md).
/// </summary>
public sealed class BrowserPrivacyScanProvider : IScanProvider
{
    private const int ReportEveryFiles = 128;

    private readonly IBrowserDetector _detector;

    public BrowserPrivacyScanProvider(IBrowserDetector detector) =>
        _detector = detector ?? throw new ArgumentNullException(nameof(detector));

    public string Id => "browsers.privacy";

    public string DisplayName => "Traces de navigateurs";

    public Category Category => Category.BrowserHistory;

    public async IAsyncEnumerable<ScanItem> ScanAsync(
        IScanProgressReporter reporter,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var browser in _detector.DetectInstalledBrowsers())
        {
            var multiProfile = browser.Profiles.Count > 1;

            foreach (var profile in browser.Profiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                foreach (var target in ResolveTargets(browser, profile))
                {
                    var item = BuildItem(browser, profile, multiProfile, target, reporter, cancellationToken);
                    if (item is not null)
                    {
                        yield return item;
                    }
                }

                await Task.Yield();
            }
        }
    }

    private static ScanItem? BuildItem(
        DetectedBrowser browser,
        BrowserProfileInfo profile,
        bool multiProfile,
        PrivacyTarget target,
        IScanProgressReporter reporter,
        CancellationToken cancellationToken)
    {
        var (sizeBytes, itemCount) = target.ActionKind switch
        {
            FileActionKind.DeleteDirectory => MeasureDirectory(target.Path, reporter, cancellationToken),
            // Suppression ciblée in-place : l'espace libéré n'est pas connu à l'avance (on ne l'invente pas).
            FileActionKind.ClearBrowserHistory => (0L, File.Exists(target.Path) ? 1 : 0),
            _ => MeasureFile(target.Path),
        };

        if (itemCount == 0)
        {
            return null;
        }

        var profileSuffix = multiProfile ? $" ({profile.Name})" : string.Empty;
        var (nameKey, descKey) = KeysFor(target.Category);

        return new ScanItem
        {
            Id = $"browsers.privacy::{target.Category}::{browser.Kind}::{target.Path}",
            RuleId = "browsers.privacy",
            Category = target.Category,
            SubCategory = browser.DisplayName,
            // Repli non localisé ; l'UI utilise NameKey/DescriptionKey. La note « fermez le navigateur »
            // est ajoutée par l'UI via IsLocked.
            DisplayName = $"{browser.DisplayName} — {target.Label}{profileSuffix}",
            NameKey = nameKey,
            NameArgs = [browser.DisplayName, profileSuffix],
            Description = target.Description,
            DescriptionKey = descKey,
            PathOrIdentifier = target.Path,
            SizeBytes = sizeBytes,
            ItemCount = itemCount,
            Risk = target.Risk,
            // Trace de confidentialité : jamais cochée par défaut, l'utilisateur choisit (§3.2, §14).
            SelectedByDefault = false,
            IsLocked = browser.IsRunning,
            AssociatedApp = browser.DisplayName,
            // Le moteur supprime définitivement (pas de Corbeille) : rester honnête.
            Reversibility = Reversibility.Irreversible,
            ActionKind = target.ActionKind,
            AllowedRoots = [target.AllowedRoot],
            SweepRoots = target.ActionKind == FileActionKind.DeleteDirectory ? [target.Path] : [],
        };
    }

    /// <summary>Cibles de confidentialité présentes pour un profil donné (uniquement celles existantes).</summary>
    private static IEnumerable<PrivacyTarget> ResolveTargets(DetectedBrowser browser, BrowserProfileInfo profile)
    {
        if (browser.Engine == BrowserEngine.Gecko)
        {
            // Firefox : historique via suppression CIBLÉE (places.sqlite mêle historique et favoris —
            // les favoris sont préservés, cf. FirefoxHistoryCleaner). Jamais de suppression du fichier entier.
            var places = Path.Combine(profile.ContentRoot, "places.sqlite");
            if (File.Exists(places))
            {
                yield return new PrivacyTarget(Category.BrowserHistory, RiskLevel.Privacy, "historique de navigation", places,
                    profile.ContentRoot, FileActionKind.ClearBrowserHistory,
                    "Historique des sites visités. Vos favoris sont conservés (suppression ciblée).");
            }

            var cookies = Path.Combine(profile.ContentRoot, "cookies.sqlite");
            if (File.Exists(cookies))
            {
                yield return new PrivacyTarget(Category.BrowserCookies, RiskLevel.Review, "cookies", cookies,
                    profile.ContentRoot, FileActionKind.DeleteFile,
                    "Cookies du navigateur. Les vider vous déconnectera des sites où vous étiez identifié.");
            }

            var sessions = Path.Combine(profile.ContentRoot, "sessionstore-backups");
            if (Directory.Exists(sessions))
            {
                yield return new PrivacyTarget(Category.BrowserSessions, RiskLevel.Review, "session & onglets", sessions,
                    profile.ContentRoot, FileActionKind.DeleteDirectory,
                    "Sessions mémorisées pour la restauration des onglets. Les effacer perd les onglets restaurables.");
            }

            yield break;
        }

        // Chromium et dérivés (Chrome, Edge, Brave, Vivaldi, Chromium, Opera).
        var history = Path.Combine(profile.ContentRoot, "History");
        if (File.Exists(history))
        {
            yield return new PrivacyTarget(Category.BrowserHistory, RiskLevel.Privacy, "historique de navigation", history,
                profile.ContentRoot, FileActionKind.DeleteFile,
                "Historique des sites visités et des téléchargements. Vos favoris et mots de passe ne sont pas touchés.");
        }

        // Chromium récent range les cookies sous « Network », les versions plus anciennes à la racine du profil.
        var cookiesModern = Path.Combine(profile.ContentRoot, "Network", "Cookies");
        var cookiesLegacy = Path.Combine(profile.ContentRoot, "Cookies");
        var chromiumCookies = File.Exists(cookiesModern) ? cookiesModern
            : File.Exists(cookiesLegacy) ? cookiesLegacy : null;
        if (chromiumCookies is not null)
        {
            yield return new PrivacyTarget(Category.BrowserCookies, RiskLevel.Review, "cookies", chromiumCookies,
                Path.GetDirectoryName(chromiumCookies)!, FileActionKind.DeleteFile,
                "Cookies du navigateur. Les vider vous déconnectera des sites où vous étiez identifié.");
        }

        var chromiumSessions = Path.Combine(profile.ContentRoot, "Sessions");
        if (Directory.Exists(chromiumSessions))
        {
            yield return new PrivacyTarget(Category.BrowserSessions, RiskLevel.Review, "session & onglets", chromiumSessions,
                profile.ContentRoot, FileActionKind.DeleteDirectory,
                "Sessions mémorisées pour la restauration des onglets. Les effacer perd les onglets restaurables.");
        }
    }

    /// <summary>Clés de ressource (nom, description) pour une catégorie de trace navigateur.</summary>
    private static (string NameKey, string DescriptionKey) KeysFor(Category category) => category switch
    {
        Category.BrowserHistory => ("Browsers.Item.History", "Browsers.Item.History.Desc"),
        Category.BrowserCookies => ("Browsers.Item.Cookies", "Browsers.Item.Cookies.Desc"),
        Category.BrowserSessions => ("Browsers.Item.Sessions", "Browsers.Item.Sessions.Desc"),
        _ => ("Browsers.Item.Cache", "Browsers.Item.Cache.Desc"),
    };

    private static (long Bytes, int Count) MeasureFile(string path)
    {
        try
        {
            var info = new FileInfo(path);
            return info.Exists && info.Length > 0 ? (info.Length, 1) : (0, 0);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return (0, 0);
        }
    }

    private static (long Bytes, int Count) MeasureDirectory(
        string path,
        IScanProgressReporter reporter,
        CancellationToken cancellationToken)
    {
        long bytes = 0;
        var count = 0;
        var sinceReport = 0;

        foreach (var entry in SafeFileEnumerator.EnumerateEntries(path, recursive: true, cancellationToken))
        {
            bytes += entry.Length;
            count++;
            if (++sinceReport >= ReportEveryFiles)
            {
                reporter.ReportFiles(sinceReport, path);
                sinceReport = 0;
            }
        }

        if (sinceReport > 0)
        {
            reporter.ReportFiles(sinceReport, path);
        }

        return (bytes, count);
    }

    /// <summary>Une cible de confidentialité concrète (fichier, dossier ou base à nettoyer) dans un profil.</summary>
    private readonly record struct PrivacyTarget(
        Category Category,
        RiskLevel Risk,
        string Label,
        string Path,
        string AllowedRoot,
        FileActionKind ActionKind,
        string Description);
}
