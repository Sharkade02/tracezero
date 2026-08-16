using System.Windows;

namespace TraceZero.App;

/// <summary>
/// Shell principal. La logique vit dans <see cref="ViewModels.ShellViewModel"/> ;
/// le code-behind se limite à l'initialisation du composant (§5 : pas de logique dans le code-behind).
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
}
