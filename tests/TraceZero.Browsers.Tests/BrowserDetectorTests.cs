using TraceZero.Domain.Browsers;

namespace TraceZero.Browsers.Tests;

public sealed class BrowserDetectorTests : IDisposable
{
    private readonly string _local;
    private readonly string _roaming;

    public BrowserDetectorTests()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), "tz-browsers-" + Guid.NewGuid().ToString("N"));
        _local = Path.Combine(baseDir, "Local");
        _roaming = Path.Combine(baseDir, "Roaming");
        Directory.CreateDirectory(_local);
        Directory.CreateDirectory(_roaming);
    }

    private void CreateChromiumProfile(string relativeUserData, string profile)
    {
        var dir = Path.Combine(_local, relativeUserData, profile);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "Preferences"), "{}");
    }

    [Fact]
    public void Detects_chrome_with_multiple_profiles_and_excludes_system()
    {
        CreateChromiumProfile(@"Google\Chrome\User Data", "Default");
        CreateChromiumProfile(@"Google\Chrome\User Data", "Profile 1");
        CreateChromiumProfile(@"Google\Chrome\User Data", "System Profile");

        var chrome = new BrowserDetector(_local, _roaming)
            .DetectInstalledBrowsers()
            .Single(b => b.Kind == BrowserKind.Chrome);

        Assert.Equal(BrowserEngine.Chromium, chrome.Engine);
        Assert.Equal(2, chrome.Profiles.Count); // System Profile exclu
        Assert.Contains(chrome.Profiles, p => p.IsDefault);
    }

    [Fact]
    public void Ignores_browser_without_data_directory()
    {
        var browsers = new BrowserDetector(_local, _roaming).DetectInstalledBrowsers();
        Assert.Empty(browsers);
    }

    [Fact]
    public void Detects_firefox_profiles()
    {
        var profile = Path.Combine(_local, "Mozilla", "Firefox", "Profiles", "abcd1234.default-release");
        Directory.CreateDirectory(profile);

        var firefox = new BrowserDetector(_local, _roaming)
            .DetectInstalledBrowsers()
            .Single(b => b.Kind == BrowserKind.Firefox);

        Assert.Equal(BrowserEngine.Gecko, firefox.Engine);
        Assert.Contains(firefox.Profiles, p => p.IsDefault);
    }

    public void Dispose()
    {
        try
        {
            var baseDir = Path.GetDirectoryName(_local);
            if (baseDir is not null && Directory.Exists(baseDir))
            {
                Directory.Delete(baseDir, recursive: true);
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
