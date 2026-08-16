using System.Diagnostics;
using System.Globalization;
using Microsoft.Win32;
using TraceZero.Application.Apps;
using TraceZero.Domain.Apps;

namespace TraceZero.Windows.Apps;

/// <summary>
/// Liste les applications installées à partir des clés « Uninstall » du registre et lance leur
/// désinstallateur déclaré (§22). Ne supprime jamais un logiciel manuellement.
/// </summary>
public sealed class InstalledAppService : IInstalledAppService
{
    private const string UninstallPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
    private const string UninstallPathWow = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";

    public IReadOnlyList<AppInstallation> GetInstalledApps()
    {
        var apps = new Dictionary<string, AppInstallation>(StringComparer.OrdinalIgnoreCase);

        ReadFrom(Registry.LocalMachine, UninstallPath, apps);
        ReadFrom(Registry.LocalMachine, UninstallPathWow, apps);
        ReadFrom(Registry.CurrentUser, UninstallPath, apps);

        return apps.Values.OrderBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    private static void ReadFrom(RegistryKey hive, string subPath, Dictionary<string, AppInstallation> apps)
    {
        try
        {
            using var root = hive.OpenSubKey(subPath);
            if (root is null)
            {
                return;
            }

            foreach (var subKeyName in root.GetSubKeyNames())
            {
                try
                {
                    using var key = root.OpenSubKey(subKeyName);
                    var app = TryReadApp(key, subKeyName);
                    if (app is not null)
                    {
                        apps[app.Name + "|" + (app.Version ?? "")] = app;
                    }
                }
                catch (Exception ex) when (ex is System.Security.SecurityException or IOException)
                {
                }
            }
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or IOException or UnauthorizedAccessException)
        {
        }
    }

    private static AppInstallation? TryReadApp(RegistryKey? key, string subKeyName)
    {
        if (key is null)
        {
            return null;
        }

        var name = key.GetValue("DisplayName") as string;
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        // Ignorer les composants système et les mises à jour.
        if (key.GetValue("SystemComponent") is int sys && sys == 1)
        {
            return null;
        }

        if (key.GetValue("ParentKeyName") is not null || key.GetValue("ReleaseType") is "Security Update" or "Update")
        {
            return null;
        }

        long? size = key.GetValue("EstimatedSize") is int kb && kb > 0 ? (long)kb * 1024 : null;

        DateOnly? installDate = null;
        if (key.GetValue("InstallDate") is string d && d.Length == 8
            && DateOnly.TryParseExact(d, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            installDate = parsed;
        }

        return new AppInstallation
        {
            Id = subKeyName,
            Name = name.Trim(),
            Publisher = (key.GetValue("Publisher") as string)?.Trim(),
            Version = (key.GetValue("DisplayVersion") as string)?.Trim(),
            InstallDate = installDate,
            SizeBytes = size,
            InstallLocation = (key.GetValue("InstallLocation") as string)?.Trim(),
            UninstallCommand = (key.GetValue("UninstallString") as string)?.Trim(),
        };
    }

    public bool LaunchUninstaller(AppInstallation app)
    {
        if (string.IsNullOrWhiteSpace(app.UninstallCommand))
        {
            return false;
        }

        try
        {
            // On délègue au désinstallateur déclaré par l'éditeur (qui peut s'élever lui-même).
            Process.Start(new ProcessStartInfo("cmd.exe", $"/c {app.UninstallCommand}")
            {
                UseShellExecute = true,
            });
            return true;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return false;
        }
    }
}
