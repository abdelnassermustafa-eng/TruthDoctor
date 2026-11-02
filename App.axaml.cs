using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using TruthDoctor.Theming;

namespace TruthDoctor;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        // Register semantic resources for the
        // approved Professional Light default.
        // Workbench visual migration is separate.
        var themeResources =
            TruthDoctorAvaloniaThemeAdapter.CreateResources(
                TruthDoctorThemeContract.DefaultAppearance,
                systemUsesDark: false);

        foreach (var entry in themeResources)
        {
            Resources[entry.Key] = entry.Value;
        }

    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new TruthDoctor.Views.UniversalWorkbench.UniversalWorkbenchWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}