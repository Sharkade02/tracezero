using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using TraceZero.Application.Automation;
using TraceZero.Domain.Automation;

namespace TraceZero.Windows.Automation;

/// <summary>
/// Planifie le nettoyage automatique via le Planificateur de tâches Windows (schtasks), sans service
/// permanent (§15). La configuration est persistée en JSON local.
/// </summary>
public sealed class AutomationService : IAutomationService
{
    private const string TaskName = @"TraceZero\AutoClean";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _configPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TraceZero", "automation.json");

    public AutomationConfig GetConfig()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                return JsonSerializer.Deserialize<AutomationConfig>(File.ReadAllText(_configPath), JsonOptions)
                       ?? AutomationConfig.Default;
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
        }

        return AutomationConfig.Default;
    }

    public bool Apply(AutomationConfig config)
    {
        SaveConfig(config);
        return config.Enabled ? CreateTask(config) : DeleteTask();
    }

    private void SaveConfig(AutomationConfig config)
    {
        try
        {
            var directory = Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(_configPath, JsonSerializer.Serialize(config, JsonOptions));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static bool CreateTask(AutomationConfig config)
    {
        var exe = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exe))
        {
            return false;
        }

        var profileArg = config.Profile == CleaningProfile.Privacy ? "privacy" : "safe";
        // schtasks /TR : le chemin de l'exe est entre guillemets réels ; ArgumentList gère l'échappement.
        var action = $"\"{exe}\" --autoclean {profileArg}";

        var args = new List<string>
        {
            "/Create", "/F",
            "/TN", TaskName,
            "/TR", action,
        };

        switch (config.Trigger)
        {
            case AutomationTrigger.Weekly:
                args.AddRange(["/SC", "WEEKLY", "/ST", "12:00"]);
                break;
            case AutomationTrigger.Monthly:
                args.AddRange(["/SC", "MONTHLY", "/ST", "12:00"]);
                break;
            case AutomationTrigger.AtLogon:
                args.AddRange(["/SC", "ONLOGON"]);
                break;
        }

        return RunSchtasks(args);
    }

    private static bool DeleteTask() => RunSchtasks(["/Delete", "/F", "/TN", TaskName]);

    private static bool RunSchtasks(IEnumerable<string> arguments)
    {
        try
        {
            var psi = new ProcessStartInfo("schtasks.exe")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            foreach (var arg in arguments)
            {
                psi.ArgumentList.Add(arg);
            }

            using var process = Process.Start(psi);
            if (process is null)
            {
                return false;
            }

            process.WaitForExit(15_000);
            return process.ExitCode == 0;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return false;
        }
    }
}
