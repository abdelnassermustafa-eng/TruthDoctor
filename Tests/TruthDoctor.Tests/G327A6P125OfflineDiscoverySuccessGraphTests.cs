using System;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Controllers.Workbench;
using TruthDoctor.Graph;
using TruthDoctor.Models.Platform;
using TruthDoctor.State;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A6P125OfflineDiscoverySuccessGraphTests
{
    [Fact]
    public async Task ValidOfflinePlatformCompletesWorkbenchDiscovery()
    {
        var workbench = (WorkbenchState)(
            Activator.CreateInstance(typeof(WorkbenchState))
            ?? throw new InvalidOperationException(
                "WorkbenchState fixture unavailable."));

        var platform = (PlatformState)(
            Activator.CreateInstance(typeof(PlatformState))
            ?? throw new InvalidOperationException(
                "PlatformState fixture unavailable."));

        // Use the actual production graph builder.
        // No mocked HTTP response or live provider.
        var graph =
            new InfrastructureGraphBuilder().Build(platform);

        Assert.NotNull(graph);

        int invoked = 0;
        int started = 0;
        int completed = 0;
        int failed = 0;
        string? observedLocation = null;

        var controller = new WorkbenchDiscoveryController(
            workbench,
            (string? location, CancellationToken token) =>
            {
                invoked++;
                observedLocation = location;
                return Task.FromResult(platform);
            });

        controller.DiscoveryStarted += (_, _) => started++;
        controller.DiscoveryCompleted += (_, _) => completed++;
        controller.DiscoveryFailed += (_, _) => failed++;

        bool succeeded = await controller.DiscoverAsync(
            "offline-success-location");

        Assert.True(succeeded);
        Assert.Equal(1, invoked);
        Assert.Equal(1, started);
        Assert.Equal(1, completed);
        Assert.Equal(0, failed);
        Assert.False(workbench.IsDiscovering);

        Assert.Equal(
            "offline-success-location",
            observedLocation);

        Assert.Equal(
            "offline-success-location",
            workbench.SelectedLocation);
    }
}
