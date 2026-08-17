using TraceZero.Application.Browsers;
using TraceZero.Application.Scanning;
using TraceZero.Domain;
using TraceZero.Domain.Browsers;

namespace TraceZero.Browsers.Tests;

file sealed class FakePrivacyDetector(params DetectedBrowser[] browsers) : IBrowserDetector
{
    public IReadOnlyList<DetectedBrowser> DetectInstalledBrowsers() => browsers;
}

public sealed class BrowserPrivacyScanProviderTests : IDisposable
{
    private readonly string _profileDir;

    public BrowserPrivacyScanProviderTests()
    {
        _profileDir = Path.Combine(Path.GetTempPath(), "tz-bpriv-" + Guid.NewGuid().ToString("N"), "Default");
        Directory.CreateDirectory(_profileDir);
    }

    private static async Task<List<ScanItem>> Collect(BrowserPrivacyScanProvider provider)
    {
        var items = new List<ScanItem>();
        await foreach (var item in provider.ScanAsync(NullScanProgressReporter.Instance, CancellationToken.None))
        {
            items.Add(item);
        }

        return items;
    }

    private DetectedBrowser Chromium(bool running = false) => new()
    {
        Kind = BrowserKind.Chrome,
        Engine = BrowserEngine.Chromium,
        DisplayName = "Google Chrome",
        DataRoot = Path.GetDirectoryName(_profileDir)!,
        Profiles = [new BrowserProfileInfo { Name = "Par défaut", Path = _profileDir, IsDefault = true }],
        IsRunning = running,
    };

    [Fact]
    public async Task Surfaces_history_cookies_and_sessions_as_separate_items()
    {
        File.WriteAllBytes(Path.Combine(_profileDir, "History"), new byte[400]);
        Directory.CreateDirectory(Path.Combine(_profileDir, "Network"));
        File.WriteAllBytes(Path.Combine(_profileDir, "Network", "Cookies"), new byte[200]);
        var sessions = Path.Combine(_profileDir, "Sessions");
        Directory.CreateDirectory(sessions);
        File.WriteAllBytes(Path.Combine(sessions, "Session_1"), new byte[100]);

        var items = await Collect(new BrowserPrivacyScanProvider(new FakePrivacyDetector(Chromium())));

        Assert.Equal(3, items.Count);
        Assert.Contains(items, i => i.Category == Category.BrowserHistory && i.Risk == RiskLevel.Privacy);
        Assert.Contains(items, i => i.Category == Category.BrowserCookies && i.Risk == RiskLevel.Review);
        Assert.Contains(items, i => i.Category == Category.BrowserSessions && i.Risk == RiskLevel.Review);
    }

    [Fact]
    public async Task Nothing_is_selected_by_default_and_deletion_is_irreversible()
    {
        File.WriteAllBytes(Path.Combine(_profileDir, "History"), new byte[400]);

        var items = await Collect(new BrowserPrivacyScanProvider(new FakePrivacyDetector(Chromium())));

        var item = Assert.Single(items);
        Assert.False(item.SelectedByDefault);
        Assert.Equal(Reversibility.Irreversible, item.Reversibility);
    }

    [Fact]
    public async Task History_is_a_single_file_delete_scoped_to_the_profile()
    {
        var history = Path.Combine(_profileDir, "History");
        File.WriteAllBytes(history, new byte[400]);

        var items = await Collect(new BrowserPrivacyScanProvider(new FakePrivacyDetector(Chromium())));

        var item = Assert.Single(items);
        Assert.Equal(FileActionKind.DeleteFile, item.ActionKind);
        Assert.Equal(history, item.PathOrIdentifier);
        Assert.Contains(_profileDir, item.AllowedRoots);
    }

    [Fact]
    public async Task Running_browser_marks_items_locked()
    {
        File.WriteAllBytes(Path.Combine(_profileDir, "History"), new byte[400]);

        var items = await Collect(new BrowserPrivacyScanProvider(new FakePrivacyDetector(Chromium(running: true))));

        Assert.True(Assert.Single(items).IsLocked);
    }

    [Fact]
    public async Task Firefox_history_uses_targeted_deletion_not_whole_file()
    {
        // places.sqlite mêle historique et favoris : l'item doit exister mais via suppression CIBLÉE
        // (ClearBrowserHistory), jamais une suppression de fichier entier qui perdrait les favoris.
        File.WriteAllBytes(Path.Combine(_profileDir, "places.sqlite"), new byte[400]);
        File.WriteAllBytes(Path.Combine(_profileDir, "cookies.sqlite"), new byte[200]);

        var firefox = new DetectedBrowser
        {
            Kind = BrowserKind.Firefox,
            Engine = BrowserEngine.Gecko,
            DisplayName = "Mozilla Firefox",
            DataRoot = Path.GetDirectoryName(_profileDir)!,
            Profiles = [new BrowserProfileInfo { Name = "default", Path = _profileDir, IsDefault = true }],
            IsRunning = false,
        };

        var items = await Collect(new BrowserPrivacyScanProvider(new FakePrivacyDetector(firefox)));

        var history = Assert.Single(items, i => i.Category == Category.BrowserHistory);
        Assert.Equal(FileActionKind.ClearBrowserHistory, history.ActionKind);
        Assert.Contains(items, i => i.Category == Category.BrowserCookies);
    }

    [Fact]
    public async Task Firefox_privacy_targets_are_read_from_the_roaming_content_root()
    {
        // Firefox : cache (Path) en Local, contenu (ContentPath) en Roaming. Les cibles doivent venir
        // de ContentPath, pas de Path.
        var contentRoot = Path.Combine(Path.GetDirectoryName(_profileDir)!, "roaming-profile");
        Directory.CreateDirectory(contentRoot);
        File.WriteAllBytes(Path.Combine(contentRoot, "cookies.sqlite"), new byte[200]);

        var firefox = new DetectedBrowser
        {
            Kind = BrowserKind.Firefox,
            Engine = BrowserEngine.Gecko,
            DisplayName = "Mozilla Firefox",
            DataRoot = contentRoot,
            Profiles = [new BrowserProfileInfo { Name = "default", Path = _profileDir, ContentPath = contentRoot, IsDefault = true }],
            IsRunning = false,
        };

        var items = await Collect(new BrowserPrivacyScanProvider(new FakePrivacyDetector(firefox)));

        var cookies = Assert.Single(items, i => i.Category == Category.BrowserCookies);
        Assert.Equal(Path.Combine(contentRoot, "cookies.sqlite"), cookies.PathOrIdentifier);
    }

    [Fact]
    public async Task Absent_targets_produce_no_items()
    {
        // Profil vide : rien à proposer.
        var items = await Collect(new BrowserPrivacyScanProvider(new FakePrivacyDetector(Chromium())));

        Assert.Empty(items);
    }

    public void Dispose()
    {
        try
        {
            var baseDir = Path.GetDirectoryName(_profileDir);
            if (baseDir is not null && Directory.Exists(baseDir))
            {
                Directory.Delete(baseDir, recursive: true);
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
