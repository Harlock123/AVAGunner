using Avalonia.Controls;
using Avalonia.Input;

namespace AVAGunner.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Ensure the game control gets focus when window is shown
        Opened += (_, _) =>
        {
            GameCanvas.Focus();
        };

        // Re-focus on click anywhere in window
        PointerPressed += (_, _) =>
        {
            GameCanvas.Focus();
        };
    }
}
