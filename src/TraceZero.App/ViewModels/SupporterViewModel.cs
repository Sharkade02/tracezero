using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TraceZero.Application.Licensing;

namespace TraceZero.App.ViewModels;

public sealed record SupporterTier(string Label, int Amount, bool Recommended);

/// <summary>
/// Page « Soutenir TraceZero » (§27). Le logiciel reste pleinement utilisable gratuitement ; le
/// soutien est volontaire, sans abonnement forcé, avec activation par jeton signé hors ligne.
/// </summary>
public sealed partial class SupporterViewModel : PageViewModelBase
{
    // À remplacer par l'URL réelle de la page de soutien lors du déploiement.
    private const string SupportBaseUrl = "https://tracezero.app/soutenir";

    private readonly ILicenseService _licenseService;

    public SupporterViewModel(ILicenseService licenseService)
    {
        _licenseService = licenseService;
        RefreshStatus();
    }

    public override string Title => TraceZero.App.Services.Localizer.Get("Nav.Supporter");
    public override string IconGlyph => "❤";
    public override bool IsFooter => true;
    public override bool IsUnderConstruction => false;

    public IReadOnlyList<SupporterTier> Tiers { get; } =
    [
        new("10 €", 10, false),
        new("19 €", 19, true),
        new("29 €", 29, false),
        new("49 €", 49, false),
    ];

    [ObservableProperty]
    private bool _isSupporter;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private string _licenseInput = string.Empty;

    [ObservableProperty]
    private string? _activationMessage;

    private void RefreshStatus()
    {
        var status = _licenseService.Status;
        IsSupporter = status.IsSupporter;
        StatusText = status.IsSupporter
            ? $"Merci {status.SupporterName} ! Vous soutenez TraceZero. 💙"
            : "Vous utilisez la version gratuite — complète et sans publicité.";
    }

    [RelayCommand]
    private void Contribute(SupporterTier? tier)
    {
        var amount = tier?.Amount ?? 0;
        OpenUrl(amount > 0 ? $"{SupportBaseUrl}?montant={amount}" : SupportBaseUrl);
    }

    [RelayCommand]
    private void OpenOtherAmount() => OpenUrl(SupportBaseUrl);

    [RelayCommand]
    private void Activate()
    {
        if (string.IsNullOrWhiteSpace(LicenseInput))
        {
            ActivationMessage = "Collez votre code de soutien.";
            return;
        }

        if (_licenseService.TryActivate(LicenseInput.Trim()))
        {
            ActivationMessage = "Code validé, merci pour votre soutien !";
            LicenseInput = string.Empty;
        }
        else
        {
            ActivationMessage = "Ce code de soutien n'est pas valide.";
        }

        RefreshStatus();
    }

    [RelayCommand]
    private void Deactivate()
    {
        _licenseService.Deactivate();
        ActivationMessage = null;
        RefreshStatus();
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
        }
    }
}
