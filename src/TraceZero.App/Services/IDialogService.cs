using CommunityToolkit.Mvvm.ComponentModel;
using TraceZero.App.ViewModels;

namespace TraceZero.App.Services;

/// <summary>
/// Boîtes de dialogue modales de confirmation (§11), affichées en superposition par le shell plutôt
/// que via un <c>MessageBox</c> système : cohérence visuelle et thème. Une action destructive est
/// signalée visuellement mais jamais présélectionnée.
/// </summary>
public interface IDialogService
{
    ModalViewModel? Active { get; }

    bool HasActive { get; }

    /// <summary>Affiche une confirmation et attend le choix de l'utilisateur (vrai = confirmé).</summary>
    Task<bool> ConfirmAsync(
        string title,
        string message,
        string confirmText = "Confirmer",
        string cancelText = "Annuler",
        bool destructive = false);
}

/// <summary>Implémentation liée à l'UI : expose la modale active pour le shell.</summary>
public sealed partial class DialogService : ObservableObject, IDialogService
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasActive))]
    private ModalViewModel? _active;

    public bool HasActive => Active is not null;

    public async Task<bool> ConfirmAsync(
        string title,
        string message,
        string confirmText = "Confirmer",
        string cancelText = "Annuler",
        bool destructive = false)
    {
        var modal = new ModalViewModel(title, message, confirmText, cancelText, destructive);
        Active = modal;
        try
        {
            return await modal.Completion;
        }
        finally
        {
            Active = null;
        }
    }
}
