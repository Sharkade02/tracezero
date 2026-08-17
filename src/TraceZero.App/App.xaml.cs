using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TraceZero.App.Services;
using TraceZero.App.ViewModels;
using TraceZero.Browsers.DependencyInjection;
using TraceZero.Engine.DependencyInjection;
using TraceZero.Persistence.DependencyInjection;
using TraceZero.Storage.DependencyInjection;
using TraceZero.Windows.DependencyInjection;

namespace TraceZero.App;

public partial class App : System.Windows.Application
{
    private readonly IHost _host;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(ConfigureServices)
            .Build();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Couche de sécurité + moteurs de scan/nettoyage (§9, §12, Phase 3) + navigateurs (§14, Phase 4).
        services.AddTraceZeroWindows();
        services.AddTraceZeroEngine();
        services.AddTraceZeroBrowsers();
        services.AddTraceZeroStorage();
        services.AddTraceZeroPersistence();

        // Services d'application.
        services.AddSingleton<IThemeService, ThemeManager>();
        services.AddSingleton<ILocalizationService, LocalizationManager>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IToastService, ToastService>();
        services.AddSingleton<IDialogService, DialogService>();

        // Pages nécessitant une résolution concrète (injectées ailleurs).
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<CleanupViewModel>();
        services.AddSingleton<SecureEraseViewModel>();

        // Pages, dans l'ordre de la barre latérale (§4). L'ordre d'enregistrement pilote l'affichage.
        services.AddSingleton<PageViewModelBase>(sp => sp.GetRequiredService<DashboardViewModel>());
        services.AddSingleton<PageViewModelBase>(sp => sp.GetRequiredService<CleanupViewModel>());
        services.AddSingleton<PageViewModelBase, PrivacyViewModel>();
        services.AddSingleton<PageViewModelBase, BrowsersViewModel>();
        services.AddSingleton<PageViewModelBase, DiskSpaceViewModel>();
        services.AddSingleton<PageViewModelBase, SystemHealthViewModel>();
        services.AddSingleton<PageViewModelBase, DriverHealthViewModel>();
        services.AddSingleton<PageViewModelBase, DuplicatesViewModel>();
        services.AddSingleton<PageViewModelBase>(sp => sp.GetRequiredService<SecureEraseViewModel>());
        services.AddSingleton<PageViewModelBase, NtfsAnalysisViewModel>();
        services.AddSingleton<PageViewModelBase, ApplicationsViewModel>();
        services.AddSingleton<PageViewModelBase, SoftwareUpdateViewModel>();
        services.AddSingleton<PageViewModelBase, AutomationViewModel>();
        services.AddSingleton<PageViewModelBase, HistoryViewModel>();
        services.AddSingleton<PageViewModelBase, RestoreViewModel>();
        services.AddSingleton<PageViewModelBase, SettingsViewModel>();
        services.AddSingleton<PageViewModelBase, SupporterViewModel>();

        services.AddSingleton<AutoCleanRunner>();
        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<MainWindow>();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        await _host.StartAsync();

        // Mode sans interface pour l'automatisation planifiée (§15) : « --autoclean safe|privacy ».
        if (TryGetAutoCleanProfile(e.Args, out var profile))
        {
            try
            {
                await _host.Services.GetRequiredService<AutoCleanRunner>().RunAsync(profile);
            }
            finally
            {
                Shutdown(0);
            }

            return;
        }

        _host.Services.GetRequiredService<IThemeService>().Apply(AppTheme.Light);
        _host.Services.GetRequiredService<ILocalizationService>().LoadPersisted();

        var window = _host.Services.GetRequiredService<MainWindow>();
        window.DataContext = _host.Services.GetRequiredService<ShellViewModel>();
        window.Show();
    }

    private static bool TryGetAutoCleanProfile(string[] args, out Domain.Automation.CleaningProfile profile)
    {
        profile = Domain.Automation.CleaningProfile.Safe;
        var index = Array.FindIndex(args, a => a.Equals("--autoclean", StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            return false;
        }

        if (index + 1 < args.Length && args[index + 1].Equals("privacy", StringComparison.OrdinalIgnoreCase))
        {
            profile = Domain.Automation.CleaningProfile.Privacy;
        }

        return true;
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        using (_host)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(2));
        }

        base.OnExit(e);
    }
}
