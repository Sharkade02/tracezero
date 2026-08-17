using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TraceZero.App.Services;

namespace TraceZero.App.ViewModels;

/// <summary>Un toast affiché : message, nature (couleur) et fermeture manuelle (Phase 1).</summary>
public sealed partial class ToastViewModel : ObservableObject
{
    private readonly Action<ToastViewModel> _dismiss;

    public ToastViewModel(string message, ToastKind kind, Action<ToastViewModel> dismiss)
    {
        Message = message;
        Kind = kind;
        _dismiss = dismiss;
    }

    public string Message { get; }

    public ToastKind Kind { get; }

    public string Glyph => Kind switch
    {
        ToastKind.Success => "✔", // ✔
        ToastKind.Warning => "⚠", // ⚠
        ToastKind.Error => "✖",   // ✖
        _ => "ℹ",                 // ℹ
    };

    [RelayCommand]
    private void Dismiss() => _dismiss(this);
}
