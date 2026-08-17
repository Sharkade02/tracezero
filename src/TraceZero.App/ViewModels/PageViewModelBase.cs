using CommunityToolkit.Mvvm.ComponentModel;

namespace TraceZero.App.ViewModels;

/// <summary>
/// Base des ViewModels de page affichés dans la zone de contenu du shell.
/// </summary>
public abstract partial class PageViewModelBase : ObservableObject
{
    /// <summary>Titre affiché dans la barre latérale.</summary>
    public abstract string Title { get; }

    /// <summary>Glyphe/emoji d'icône (remplacé par une iconographie dédiée en Phase 1).</summary>
    public abstract string IconGlyph { get; }

    /// <summary>Placé dans la zone basse de la barre latérale (ex. « Soutenir »).</summary>
    public virtual bool IsFooter => false;

    /// <summary>
    /// Vrai tant que le module n'est pas connecté au moteur. Sert à afficher un état honnête
    /// « en cours de construction » (§11). Doit passer à faux quand la phase est livrée.
    /// </summary>
    public virtual bool IsUnderConstruction => true;

    /// <summary>État de sélection dans la navigation (piloté par le shell).</summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>Appelé par le shell quand la page devient active (permet de rafraîchir ses données).</summary>
    public virtual void OnActivated()
    {
    }

    /// <summary>
    /// Appelé par le shell quand la page cesse d'être active (permet d'arrêter un rafraîchissement live,
    /// libérer un minuteur, etc.). Évite qu'un moniteur continue de sonder en arrière-plan.
    /// </summary>
    public virtual void OnDeactivated()
    {
    }

    /// <summary>
    /// Appelé par le shell au changement de langue (§31). Réémet toutes les propriétés pour que les
    /// chaînes calculées (Title, messages) se relisent dans la nouvelle langue.
    /// </summary>
    public void RefreshLocalization() => OnPropertyChanged(string.Empty);
}
