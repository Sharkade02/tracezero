using TraceZero.Application.Browsers;
using TraceZero.Application.Scanning;
using TraceZero.Domain;
using TraceZero.Domain.Browsers;

namespace TraceZero.Browsers.Tests;

file sealed class FakeBrowserDetector(params DetectedBrowser[] browsers) : IBrowserDetector
{
    public IReadOnlyList<DetectedBrowser> DetectInstalledBrowsers() => browsers;
}

public sealed class BrowserCacheScanProviderTests : IDisposable
{
    private readonly string _profileDir;

    public BrowserCacheScanProviderTests()
    {
        _profileDir = Path.Combine(Path.GetTempPath(), "tz-bcache-" + Guid.NewGuid().ToString("N"), "Default");
        Directory.CreateDirectory(_profileDir);
    }

    private static async Task<List<ScanItem>> Collect(BrowserCacheScanProvider provider)
    {
        var items = new List<ScanItem>();
        await foreach (var item in provider.ScanAsync(NullScanProgressReporter.Instance, CancellationToken.None))
        {
            items.Add(item);
        }

        return items;
    }

    [Fact]
    public async Task Aggregates_cache_dirs_into_one_item_with_sweep_roots()
    {
        var cache = Path.Combine(_profileDir, "Cache");
        var codeCache = Path.Combine(_profileDir, "Code Cache");
        Directory.CreateDirectory(cache);
        Directory.CreateDirectory(codeCache);
        File.WriteAllBytes(Path.Combine(cache, "a.bin"), new byte[1000]);
        File.WriteAllBytes(Path.Combine(codeCache, "b.bin"), new byte[500]);

        var browser = new DetectedBrowser
        {
            Kind = BrowserKind.Chrome,
            Engine = BrowserEngine.Chromium,
            DisplayName = "Google Chrome",
            DataRoot = Path.GetDirectoryName(_profileDir)!,
            Profiles = [new BrowserProfileInfo { Name = "Par défaut", Path = _profileDir, IsDefault = true }],
            IsRunning = false,
        };

        var items = await Collect(new BrowserCacheScanProvider(new FakeBrowserDetector(browser)));

        var item = Assert.Single(items);
        Assert.Equal(1500, item.SizeBytes);
        Assert.Equal(RiskLevel.Safe, item.Risk);
        Assert.True(item.SelectedByDefault);
        Assert.Contains(cache, item.SweepRoots);
        Assert.Contains(codeCache, item.SweepRoots);
    }

    [Fact]
    public async Task Running_browser_is_not_selected_by_default()
    {
        var cache = Path.Combine(_profileDir, "Cache");
        Directory.CreateDirectory(cache);
        File.WriteAllBytes(Path.Combine(cache, "a.bin"), new byte[10]);

        var browser = new DetectedBrowser
        {
            Kind = BrowserKind.Chrome,
            Engine = BrowserEngine.Chromium,
            DisplayName = "Google Chrome",
            DataRoot = Path.GetDirectoryName(_profileDir)!,
            Profiles = [new BrowserProfileInfo { Name = "Par défaut", Path = _profileDir, IsDefault = true }],
            IsRunning = true,
        };

        var items = await Collect(new BrowserCacheScanProvider(new FakeBrowserDetector(browser)));

        Assert.False(Assert.Single(items).SelectedByDefault);
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
