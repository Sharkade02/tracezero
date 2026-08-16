using CommunityToolkit.Mvvm.ComponentModel;
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

    public string DisplayName => Model.DisplayName;

    public string Description => Model.Description ?? string.Empty;

    public long SizeBytes => Model.SizeBytes;

    public string SizeText => ByteSize.Format(Model.SizeBytes);

    public RiskLevel Risk => Model.Risk;

    public string RiskLabel => Risk switch
    {
        RiskLevel.Safe => "Sans risque",
        RiskLevel.Privacy => "Confidentialité",
        RiskLevel.Review => "À vérifier",
        _ => string.Empty,
    };

    public string CountText => Model.ItemCount > 0 ? $"{Model.ItemCount:N0} élément(s)" : string.Empty;
}
