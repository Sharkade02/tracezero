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
        var (sizeBytes, itemCount) = target.IsDirectory
            ? MeasureDirectory(target.Path, reporter, cancellationToken)
            : MeasureFile(target.Path);

        if (itemCount == 0)
        {
            return null;
        }

        var profileSuffix = multiProfile ? $" ({profile.Name})" : string.Empty;
        var running = browser.IsRunning;
        var runningNote = running
            ? " Fermez le navigateur pour permettre la suppression (fichiers verrouillés ignorés)."
            : string.Empty;

        return new ScanItem
        {
            Id = $"browsers.privacy::{target.Category}::{browser.Kind}::{target.Path}",
            RuleId = "browsers.privacy",
            Category = target.Category,
            SubCategory = browser.DisplayName,
            DisplayName = $"{browser.DisplayName} — {target.Label}{profileSuffix}",
            Description = target.Description + runningNote,
            PathOrIdentifier = target.Path,
            SizeBytes = sizeBytes,
            ItemCount = itemCount,
            Risk = target.Risk,
            // Trace de confidentialité : jamais cochée par défaut, l'utilisateur choisit (§3.2, §14).
            SelectedByDefault = false,
            IsLocked = running,
            AssociatedApp = browser.DisplayName,
            // Le moteur supprime définitivement (pas de Corbeille) : rester honnête.
            Reversibility = Reversibility.Irreversible,
            ActionKind = target.IsDirectory ? FileActionKind.DeleteDirectory : FileActionKind.DeleteFile,
            AllowedRoots = [target.AllowedRoot],
            SweepRoots = target.IsDirectory ? [target.Path] : [],
        };
    }

    /// <summary>Cibles de confidentialité présentes pour un profil donné (uniquement celles existantes).</summary>
    private static IEnumerable<PrivacyTarget> ResolveTargets(DetectedBrowser browser, BrowserProfileInfo profile)
    {
        if (browser.Engine == BrowserEngine.Gecko)
        {
            // Firefox : historique NON proposé (places.sqlite contient aussi les favoris).
            var cookies = Path.Combine(profile.Path, "cookies.sqlite");
            if (File.Exists(cookies))
            {
                yield return new PrivacyTarget(Category.BrowserCookies, RiskLevel.Review, "cookies", cookies,
                    profile.Path, false,
                    "Cookies du navigateur. Les vider vous déconnectera des sites où vous étiez identifié.");
            }

            var sessions = Path.Combine(profile.Path, "sessionstore-backups");
            if (Directory.Exists(sessions))
            {
                yield return new PrivacyTarget(Category.BrowserSessions, RiskLevel.Review, "session & onglets", sessions,
                    profile.Path, true,
                    "Sessions mémorisées pour la restauration des onglets. Les effacer perd les onglets restaurables.");
            }

            yield break;
        }

        // Chromium et dérivés.
        var history = Path.Combine(profile.Path, "History");
        if (File.Exists(history))
        {
            yield return new PrivacyTarget(Category.BrowserHistory, RiskLevel.Privacy, "historique de navigation", history,
                profile.Path, false,
                "Historique des sites visités et des téléchargements. Vos favoris et mots de passe ne sont pas touchés.");
        }

        // Chromium récent range les cookies sous « Network », les versions plus anciennes à la racine du profil.
        var cookiesModern = Path.Combine(profile.Path, "Network", "Cookies");
        var cookiesLegacy = Path.Combine(profile.Path, "Cookies");
        var chromiumCookies = File.Exists(cookiesModern) ? cookiesModern
            : File.Exists(cookiesLegacy) ? cookiesLegacy : null;
        if (chromiumCookies is not null)
        {
            yield return new PrivacyTarget(Category.BrowserCookies, RiskLevel.Review, "cookies", chromiumCookies,
                Path.GetDirectoryName(chromiumCookies)!, false,
                "Cookies du navigateur. Les vider vous déconnectera des sites où vous étiez identifié.");
        }

        var chromiumSessions = Path.Combine(profile.Path, "Sessions");
        if (Directory.Exists(chromiumSessions))
        {
            yield return new PrivacyTarget(Category.BrowserSessions, RiskLevel.Review, "session & onglets", chromiumSessions,
                profile.Path, true,
                "Sessions mémorisées pour la restauration des onglets. Les effacer perd les onglets restaurables.");
        }
    }

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

    /// <summary>Une cible de confidentialité concrète (fichier ou dossier) au sein d'un profil.</summary>
    private readonly record struct PrivacyTarget(
        Category Category,
        RiskLevel Risk,
        string Label,
        string Path,
        string AllowedRoot,
        bool IsDirectory,
        string Description);
}
