using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace TruthDoctor.Views.Dialogs;

public partial class TopologySavedViewNameDialog : Window
{
    public TopologySavedViewNameDialog()
    {
        InitializeComponent();

        Opened += (_, _) =>
        {
            SavedViewNameTextBox.Focus();
            SavedViewNameTextBox.SelectAll();
        };
    }

    public TopologySavedViewNameDialog(
        string title,
        string prompt,
        string acceptButtonText,
        string initialName = "")
        : this()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);
        ArgumentException.ThrowIfNullOrWhiteSpace(
            acceptButtonText);

        Title = title;

        DialogTitleText.Text =
            title;

        DialogPromptText.Text =
            prompt;

        AcceptButton.Content =
            acceptButtonText;

        SavedViewNameTextBox.Text =
            initialName;

        UpdateAcceptanceState();
    }

    private void SavedViewNameTextBox_OnTextChanged(
        object? sender,
        TextChangedEventArgs eventArgs)
    {
        UpdateAcceptanceState();
    }

    private void AcceptButton_OnClick(
        object? sender,
        RoutedEventArgs eventArgs)
    {
        var normalizedName =
            SavedViewNameTextBox.Text?.Trim() ?? "";

        if (normalizedName.Length == 0)
        {
            ShowValidation(
                "A saved-view name is required.");

            return;
        }

        if (normalizedName.Length > 100)
        {
            ShowValidation(
                "The saved-view name cannot exceed 100 characters.");

            return;
        }

        Close(
            normalizedName);
    }

    private void CancelButton_OnClick(
        object? sender,
        RoutedEventArgs eventArgs)
    {
        Close(
            null);
    }

    private void UpdateAcceptanceState()
    {
        var normalizedName =
            SavedViewNameTextBox.Text?.Trim() ?? "";

        AcceptButton.IsEnabled =
            normalizedName.Length is > 0 and <= 100;

        ValidationText.IsVisible =
            false;

        ValidationText.Text =
            "";
    }

    private void ShowValidation(
        string message)
    {
        ValidationText.Text =
            message;

        ValidationText.IsVisible =
            true;

        SavedViewNameTextBox.Focus();
        SavedViewNameTextBox.SelectAll();
    }
}
