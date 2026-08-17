using System.Diagnostics;
using TraceZero.Application.Browsers;
using TraceZero.Domain.Browsers;

namespace TraceZero.Browsers;

/// <summary>
/// Détection des navigateurs installés par la présence de leurs dossiers de données (§14).
/// Injectable : les emplacements de base sont paramétrables pour les tests.
/// </summary>
public sealed class BrowserDetector : IBrowserDetector
{
    private readonly string _localAppData;
    private readonly string _roamingAppData;

    public BrowserDetector()
        : this(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData))
    {
    }

    public BrowserDetector(string localAppData, string roamingAppData)
    {
        _localAppData = localAppData;
        _roamingAppData = roamingAppData;
    }

    public IReadOnlyList<DetectedBrowser> DetectInstalledBrowsers()
    {
        var browsers = new List<DetectedBrowser>();

        foreach (var def in BrowserCatalog.Chromium)
        {
            var baseDir = def.UnderRoaming ? _roamingAppData : _localAppData;
            var userData = Path.Combine(baseDir, def.RelativeUserData);
            if (!Directory.Exists(userData))
            {
                continue;
            }

            var profiles = EnumerateChromiumProfiles(userData);
            if (profiles.Count == 0)
            {
                continue;
            }

            browsers.Add(new DetectedBrowser
            {
                Kind = def.Kind,
                Engine = BrowserEngine.Chromium,
                DisplayName = def.DisplayName,
                DataRoot = userData,
                Profiles = profiles,
                IsRunning = IsProcessRunning(def.ProcessName),
            });
        }

        browsers.AddRange(DetectOpera());

        var firefox = DetectFirefox();
        if (firefox is not null)
        {
            browsers.Add(firefox);
        }

        return browsers;
    }

    /// <summary>
    /// Détecte les variantes d'Opera (Chromium) dont le profil (Roaming) et le cache (Local) sont dans
    /// des bases distinctes. Le profil unique porte les deux racines : <c>Path</c> = cache (Local),
    /// <c>ContentPath</c> = données (Roaming).
    /// </summary>
    private IEnumerable<DetectedBrowser> DetectOpera()
    {
        foreach (var (displayName, relativeDir, processName) in BrowserCatalog.Opera)
        {
            var contentRoot = Path.Combine(_roamingAppData, relativeDir);
            var cacheRoot = Path.Combine(_localAppData, relativeDir);

            // Un profil Opera est marqué par « Preferences » (ou au moins un « History ») dans le Roaming.
            if (!File.Exists(Path.Combine(contentRoot, "Preferences"))
                && !File.Exists(Path.Combine(contentRoot, "History")))
            {
                continue;
            }

            yield return new DetectedBrowser
            {
                Kind = BrowserKind.Opera,
                Engine = BrowserEngine.Chromium,
                DisplayName = displayName,
                DataRoot = contentRoot,
                Profiles =
                [
                    new BrowserProfileInfo
                    {
                        Name = "Default",
                        Path = cacheRoot,
                        ContentPath = contentRoot,
                        IsDefault = true,
                    },
                ],
                IsRunning = IsProcessRunning(processName),
            };
        }
    }

    private static List<BrowserProfileInfo> EnumerateChromiumProfiles(string userData)
    {
        var profiles = new List<BrowserProfileInfo>();

        IEnumerable<string> dirs;
        try
        {
            dirs = Directory.EnumerateDirectories(userData);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or DirectoryNotFoundException)
        {
            return profiles;
        }

        foreach (var dir in dirs)
        {
            var name = Path.GetFileName(dir);

            // Un profil Chromium contient un fichier « Preferences ».
            if (!File.Exists(Path.Combine(dir, "Preferences")))
            {
                continue;
            }

            if (name.Equals("System Profile", StringComparison.OrdinalIgnoreCase)
                || name.Equals("Guest Profile", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var isDefault = name.Equals("Default", StringComparison.OrdinalIgnoreCase);
            profiles.Add(new BrowserProfileInfo
            {
                Name = isDefault ? "Default" : name,
                Path = dir,
                IsDefault = isDefault,
            });
        }

        return profiles;
    }

    private DetectedBrowser? DetectFirefox()
    {
        var profilesRoot = Path.Combine(_localAppData, "Mozilla", "Firefox", "Profiles");
        if (!Directory.Exists(profilesRoot))
        {
            return null;
        }

        var profiles = new List<BrowserProfileInfo>();
        IEnumerable<string> dirs;
        try
        {
            dirs = Directory.EnumerateDirectories(profilesRoot);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or DirectoryNotFoundException)
        {
            return null;
        }

        foreach (var dir in dirs)
        {
            var name = Path.GetFileName(dir);
            var isDefault = name.EndsWith(".default-release", StringComparison.OrdinalIgnoreCase)
                            || name.EndsWith(".default", StringComparison.OrdinalIgnoreCase);
            profiles.Add(new BrowserProfileInfo
            {
                // Path = profil Local (cache2). Le contenu (places.sqlite, cookies.sqlite, sessions) est
                // dans le profil de même nom sous Roaming — ContentPath pointe là.
                Name = name,
                Path = dir,
                ContentPath = Path.Combine(_roamingAppData, "Mozilla", "Firefox", "Profiles", name),
                IsDefault = isDefault,
            });
        }

        if (profiles.Count == 0)
        {
            return null;
        }

        return new DetectedBrowser
        {
            Kind = BrowserKind.Firefox,
            Engine = BrowserEngine.Gecko,
            DisplayName = "Mozilla Firefox",
            DataRoot = profilesRoot,
            Profiles = profiles,
            IsRunning = IsProcessRunning(BrowserCatalog.FirefoxProcessName),
        };
    }

    private static bool IsProcessRunning(string processName)
    {
        try
        {
            return Process.GetProcessesByName(processName).Length > 0;
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }
}
