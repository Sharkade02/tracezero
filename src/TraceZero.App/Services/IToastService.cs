using System.Collections.ObjectModel;
using System.Windows.Threading;
using TraceZero.App.ViewModels;

namespace TraceZero.App.Services;

/// <summary>Nature d'un toast, qui pilote sa couleur d'accent (Phase 1).</summary>
public enum ToastKind
{
    Info = 0,
    Success = 1,
    Warning = 2,
    Error = 3,
}

/// <summary>
/// Notifications transitoires non bloquantes (§11). Affichées en superposition par le shell ; se
/// referment automatiquement, ou manuellement.
/// </summary>
public interface IToastService
{
    ObservableCollection<ToastViewModel> Items { get; }

    void Show(string message, ToastKind kind = ToastKind.Info);
}

/// <summary>
/// Implémentation liée à l'interface utilisateur : conserve la liste observable des toasts et
/// programme leur disparition sur le fil UI.
/// </summary>
public sealed class ToastService : IToastService
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(4.5);

    public ObservableCollection<ToastViewModel> Items { get; } = [];

    public void Show(string message, ToastKind kind = ToastKind.Info)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var toast = new ToastViewModel(message, kind, Dismiss);
        Items.Add(toast);

        var timer = new DispatcherTimer { Interval = Lifetime };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            Dismiss(toast);
        };
        timer.Start();
    }

    private void Dismiss(ToastViewModel toast) => Items.Remove(toast);
}
