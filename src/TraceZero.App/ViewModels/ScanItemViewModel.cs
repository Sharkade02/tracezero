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

    public string DisplayName => Model.NameKey is { } nk ? Localizer.Get(nk) : Model.DisplayName;

    public string Description => Model.DescriptionKey is { } dk
        ? Localizer.Get(dk)
        : Model.Description ?? string.Empty;

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
