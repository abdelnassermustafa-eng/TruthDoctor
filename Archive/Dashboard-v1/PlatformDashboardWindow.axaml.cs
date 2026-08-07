using System;
using System.Threading.Tasks;
using TruthDoctor.Services.Platform;
using TruthDoctor.ViewModels;
using Avalonia.Controls;
using Avalonia.Threading;

namespace TruthDoctor;

public partial class PlatformDashboardWindow : Window
{

    private readonly PlatformDashboardService
        _dashboardService = new();

    private DashboardViewModel? _dashboard;

    private readonly DispatcherTimer _clockTimer;

    public PlatformDashboardWindow()
    {
        InitializeComponent();
        Opened += async (_, _) => await LoadPlatformStateAsync();

        _clockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        _clockTimer.Tick += (_, _) => UpdateClock();
        _clockTimer.Start();

        UpdateClock();
    }

    private void UpdateClock()
    {
        CurrentTimeText.Text =
            DateTime.Now.ToString("h:mm:ss tt");
    }


    private async Task LoadPlatformStateAsync()
    {
        try
        {
            _dashboard =
                await _dashboardService.LoadAsync();

            DataContext = _dashboard;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

}
