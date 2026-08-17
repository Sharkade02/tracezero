using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TraceZero.App.ViewModels;

/// <summary>
/// Une confirmation modale (§11). Le résultat est exposé via <see cref="Completion"/> ; les commandes
/// Confirmer/Annuler le résolvent. Aucune option destructive n'est présélectionnée.
/// </summary>
public sealed partial class ModalViewModel : ObservableObject
{
    private readonly TaskCompletionSource<bool> _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ModalViewModel(string title, string message, string confirmText, string cancelText, bool isDestructive)
    {
        Title = title;
        Message = message;
        ConfirmText = confirmText;
        CancelText = cancelText;
        IsDestructive = isDestructive;
    }

    public string Title { get; }

    public string Message { get; }

    public string ConfirmText { get; }

    public string CancelText { get; }

    /// <summary>Vrai pour une action destructive (bouton de confirmation en rouge).</summary>
    public bool IsDestructive { get; }

    /// <summary>Se termine avec le choix de l'utilisateur (vrai = confirmé).</summary>
    public Task<bool> Completion => _completion.Task;

    [RelayCommand]
    private void Confirm() => _completion.TrySetResult(true);

    [RelayCommand]
    private void Cancel() => _completion.TrySetResult(false);
}
