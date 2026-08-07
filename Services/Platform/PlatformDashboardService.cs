using System;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.ViewModels;

namespace TruthDoctor.Services.Platform;

public sealed class PlatformDashboardService
{
    private readonly PlatformStateClient _client =
        new();

    public async Task<DashboardViewModel> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        var loggedIn =
            await _client.LoginAsync(
                "admin",
                "admin123",
                cancellationToken);

        if (!loggedIn)
        {
            throw new InvalidOperationException(
                "Unable to authenticate with TruthApi.");
        }

        var state =
            await _client.GetStateAsync(
                cancellationToken: cancellationToken);

        return DashboardViewModel.FromState(state);
    }
}
