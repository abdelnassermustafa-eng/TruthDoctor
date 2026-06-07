using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace TruthDoctor;

public partial class ConfirmDialog : Window
{
    public bool Result { get; private set; }

    public ConfirmDialog(string message)
    {
        InitializeComponent();

        var text = this.FindControl<TextBlock>("MessageText");
        if (text != null)
            text.Text = message;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnYesClicked(object? sender, RoutedEventArgs e)
    {
        Result = true;
        Close();
    }

    private void OnNoClicked(object? sender, RoutedEventArgs e)
    {
        Result = false;
        Close();
    }
}
