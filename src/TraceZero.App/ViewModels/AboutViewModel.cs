using System.Diagnostics;
using System.Reflection;
using CommunityToolkit.Mvvm.Input;
using TraceZero.App.Services;

namespace TraceZero.App.ViewModels;

/// <summary>
/// Page « À propos / Mentions » : version, licence (MIT, open source), et accès aux documents légaux
/// (licence, confidentialité, avertissement, notices tierces). Aucune donnée réseau : ouvre simplement
/// les documents publiés sur le dépôt.
/// </summary>
public sealed partial class AboutViewModel : PageViewModelBase
{
    private const string RepoUrl = "https://github.com/Sharkade02/tracezero";
    private const string BlobBase = RepoUrl + "/blob/main/";

    public override string Title => Localizer.Get("Nav.About");

    public override string IconGlyph => "ℹ"; // ℹ

    public override bool IsFooter => true;

    public override bool IsUnderConstruction => false;

    public string VersionText
    {
        get
        {
            var v = Assembly.GetExecutingAssembly().GetName().Version;
            var display = v is null ? "?" : $"{v.Major}.{v.Minor}.{v.Build}";
            return Localizer.Format("About.Version", display);
        }
    }

    [RelayCommand]
    private static void OpenRepo() => OpenUrl(RepoUrl);

    [RelayCommand]
    private static void OpenLicense() => OpenUrl(BlobBase + "LICENSE");

    [RelayCommand]
    private static void OpenPrivacy() => OpenUrl(BlobBase + "PRIVACY.md");

    [RelayCommand]
    private static void OpenDisclaimer() => OpenUrl(BlobBase + "DISCLAIMER.md");

    [RelayCommand]
    private static void OpenNotices() => OpenUrl(BlobBase + "THIRD-PARTY-NOTICES.txt");

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
