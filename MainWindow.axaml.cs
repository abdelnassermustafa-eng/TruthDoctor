using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using TruthDoctor.Services;
using System.Text.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace TruthDoctor;

public partial class MainWindow : Window
{
    private readonly ApiService _apiService = new();

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OnLoginClicked(object? sender, RoutedEventArgs e)
    {
        var usernameBox = this.FindControl<TextBox>("UsernameBox");
        var passwordBox = this.FindControl<TextBox>("PasswordBox");
        var statusText = this.FindControl<TextBlock>("StatusText");
        var loginButton = this.FindControl<Button>("LoginButton");

        if (usernameBox == null || passwordBox == null || statusText == null || loginButton == null)
            return;

        var username = usernameBox.Text ?? "";
        var password = passwordBox.Text ?? "";

        try
        {
            loginButton.IsEnabled = false;
            statusText.Text = "Logging in...";

            var success = await _apiService.LoginAsync(username, password);

            if (!success)
            {
                statusText.Text = "Login failed: invalid credentials";
                loginButton.IsEnabled = true;
                return;
            }

            // Check for default credentials
            var userStore = new UserStore();
            if (userStore.IsUsingDefaultCredentials())
            {
                var dialog = new ConfirmDialog(
                "You are using default credentials.\nDo you want to change them now?"
                );

                await dialog.ShowDialog(this);

                if (dialog.Result)
                {
                    var changeWindow = new ChangeCredentialsWindow();
                    await changeWindow.ShowDialog(this);
                }
            }


            statusText.Text = "Login successful. Fetching validation...";

            var validation = await _apiService.GetValidationAsync();

            // Pretty-format JSON
            string formattedJson = validation;

            try
            {
                var jsonDoc = JsonDocument.Parse(validation);
                formattedJson = JsonSerializer.Serialize(
                    jsonDoc,
                    new JsonSerializerOptions { WriteIndented = true }
                );
            }
            catch
            {
                // Keep raw text if parsing fails
            }

            // Open validation window
            var validationWindow = new ValidationWindow(formattedJson, _apiService);
            await validationWindow.ShowDialog(this);

            Console.WriteLine("RAW VALIDATION JSON:");
            Console.WriteLine(validation);

            // Reset session state
            usernameBox.Text = "";
            passwordBox.Text = "";
            statusText.Text = "Session ended. Please log in again.";
            loginButton.IsEnabled = true;
        }
        catch (HttpRequestException)
        {
            statusText.Text = "Error: Cannot reach API server";
            loginButton.IsEnabled = true;
        }
        catch (TaskCanceledException)
        {
            statusText.Text = "Error: Request timed out";
            loginButton.IsEnabled = true;
        }
        catch (Exception ex)
        {
            statusText.Text = $"Unexpected error: {ex.Message}";
            loginButton.IsEnabled = true;
        }
    }
}
