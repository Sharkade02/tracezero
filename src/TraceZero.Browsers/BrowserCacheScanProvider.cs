using System.Runtime.CompilerServices;
using TraceZero.Application.Browsers;
using TraceZero.Application.Scanning;
using TraceZero.Domain;
using TraceZero.Domain.Browsers;
using TraceZero.Engine.IO;

namespace TraceZero.Browsers;

/// <summary>
/// Fournisseur de scan des caches de navigateurs (§14). Ne touche QUE des dossiers de cache SAFE :
/// aucun cookie, mot de passe, favori ou session n'est concerné — les connexions sont préservées
/// par nature. Un élément par (navigateur, profil), regroupant tous ses dossiers de cache.
/// </summary>
public sealed class BrowserCacheScanProvider : IScanProvider
{
    private const int ReportEveryFiles = 256;

    private readonly IBrowserDetector _detector;

    public BrowserCacheScanProvider(IBrowserDetector detector) =>
        _detector = detector ?? throw new ArgumentNullException(nameof(detector));

    public string Id => "browsers.cache";

    public string DisplayName => "Caches de navigateurs";

    public Category Category => Category.BrowserCache;

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

                var item = ScanProfile(browser, profile, multiProfile, reporter, cancellationToken);
                if (item is not null)
                {
                    yield return item;
                }

                await Task.Yield();
            }
        }
    }

    private static ScanItem? ScanProfile(
        DetectedBrowser browser,
        BrowserProfileInfo profile,
        bool multiProfile,
        IScanProgressReporter reporter,
        CancellationToken cancellationToken)
    {
        var cacheDirs = ResolveCacheDirs(browser, profile).Where(Directory.Exists).ToList();
        if (cacheDirs.Count == 0)
        {
            return null;
        }

        long totalBytes = 0;
        var fileCount = 0;
        var examinedSinceReport = 0;

        foreach (var dir in cacheDirs)
        {
            foreach (var entry in SafeFileEnumerator.EnumerateEntries(dir, recursive: true, cancellationToken))
            {
                if (++examinedSinceReport >= ReportEveryFiles)
                {
                    reporter.ReportFiles(examinedSinceReport, dir);
                    examinedSinceReport = 0;
                }

                totalBytes += entry.Length;
                fileCount++;
            }
        }

        if (examinedSinceReport > 0)
        {
            reporter.ReportFiles(examinedSinceReport, browser.DisplayName);
        }

        if (fileCount == 0)
        {
            return null;
        }

        var profileSuffix = multiProfile ? $" ({profile.Name})" : string.Empty;

        return new ScanItem
        {
            Id = $"browsers.cache::{browser.Kind}::{profile.Path}",
            RuleId = "browsers.cache",
            Category = Category.BrowserCache,
            SubCategory = browser.DisplayName,
            // Repli non localisé (consommateurs hors UI) ; l'UI utilise NameKey/DescriptionKey.
            DisplayName = $"{browser.DisplayName} — cache{profileSuffix}",
            NameKey = "Browsers.Item.Cache",
            NameArgs = [browser.DisplayName, profileSuffix],
            Description = "Fichiers de cache du navigateur, régénérés automatiquement. Vos connexions et favoris ne sont pas touchés.",
            DescriptionKey = "Browsers.Item.Cache.Desc",
            PathOrIdentifier = cacheDirs[0],
            SizeBytes = totalBytes,
            ItemCount = fileCount,
            Risk = RiskLevel.Safe,
            // Un navigateur en cours d'exécution n'est pas coché par défaut (§14) ; la note « fermez le
            // navigateur » est ajoutée par l'UI via IsLocked.
            SelectedByDefault = !browser.IsRunning,
            IsLocked = browser.IsRunning,
            AssociatedApp = browser.DisplayName,
            Reversibility = Reversibility.Irreversible,
            ActionKind = FileActionKind.DeleteDirectoryContents,
            AllowedRoots = cacheDirs,
            SweepRoots = cacheDirs,
        };
    }

    private static IEnumerable<string> ResolveCacheDirs(DetectedBrowser browser, BrowserProfileInfo profile)
    {
        if (browser.Engine == BrowserEngine.Gecko)
        {
            yield return Path.Combine(profile.Path, BrowserCatalog.FirefoxCacheDir);
            yield break;
        }

        foreach (var relative in BrowserCatalog.ChromiumCacheDirs)
        {
            yield return Path.Combine(profile.Path, relative);
        }
    }
}
