using CommunityToolkit.Mvvm.ComponentModel;
using TraceZero.App.Services;
using TraceZero.Domain;
using TraceZero.Domain.Common;

namespace TraceZero.App.ViewModels;

/// <summary>Enveloppe UI d'un <see cref="ScanItem"/> : ajoute l'état de sélection et le formatage.</summary>
public partial class ScanItemViewModel : ObservableObject
{
    public ScanItemViewModel(ScanItem model)
    {
        Model = model;
        _isSelected = model.SelectedByDefault;
    }

    public ScanItem Model { get; }

    [ObservableProperty]
    private bool _isSelected;

    public string DisplayName => Model.NameKey is { } nk
        ? (Model.NameArgs.Count > 0 ? Localizer.Format(nk, [.. Model.NameArgs]) : Localizer.Get(nk))
        : Model.DisplayName;

    public string Description
    {
        get
        {
            var text = Model.DescriptionKey is { } dk
                ? (Model.DescriptionArgs.Count > 0 ? Localizer.Format(dk, [.. Model.DescriptionArgs]) : Localizer.Get(dk))
                : Model.Description ?? string.Empty;

            // Note « fichier verrouillé » ajoutée de façon localisée (ex. navigateur ouvert).
            if (Model.IsLocked)
            {
                text = (text.Length > 0 ? text + " " : string.Empty) + Localizer.Get("Cleanup.LockedNote");
            }

            return text;
        }
    }

    public long SizeBytes => Model.SizeBytes;

    public string SizeText => ByteSize.Format(Model.SizeBytes);

    public RiskLevel Risk => Model.Risk;

    public string RiskLabel => Risk switch
    {
        RiskLevel.Safe => Localizer.Get("Risk.Safe"),
        RiskLevel.Privacy => Localizer.Get("Risk.Privacy"),
        RiskLevel.Review => Localizer.Get("Risk.Review"),
        _ => string.Empty,
    };

    public string CountText => Model.ItemCount > 0 ? Localizer.Format("Common.Items", Model.ItemCount) : string.Empty;
}
