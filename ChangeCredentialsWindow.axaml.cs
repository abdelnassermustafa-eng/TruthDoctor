using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using TruthDoctor.Services;

namespace TruthDoctor;

public partial class ChangeCredentialsWindow : Window
{
    public ChangeCredentialsWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnSaveClicked(object? sender, RoutedEventArgs e)
    {
        var usernameBox = this.FindControl<TextBox>("UsernameBox");
        var passwordBox = this.FindControl<TextBox>("PasswordBox");

        if (usernameBox == null || passwordBox == null)
            return;

        var store = new UserStore();
        store.SaveUsers(new()
        {
            new User
            {
                Username = usernameBox.Text ?? "admin",
                Password = passwordBox.Text ?? "admin123",
                Role = "Admin"
            }
        });

        Close();
    }
}
