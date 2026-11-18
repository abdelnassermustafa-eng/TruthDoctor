using System;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using TruthDoctor.State;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A6P136SinglePublicationTests
{
    [Fact]
    public async Task SuccessfulDiscoveryPublishesOneSnapshot()
    {
        var workbench = new WorkbenchState();
        var session = new StandaloneLocalDemoSession();

        int notifications = 0;
        workbench.Changed += (_, _) => notifications++;

        var result = await session.DiscoverAsync(workbench);

        Assert.Same(result, workbench.PlatformState);
        Assert.NotNull(workbench.InfrastructureGraph);
        Assert.NotNull(workbench.InfrastructureGraphIndex);
        Assert.Equal(1, notifications);
    }

    [Fact]
    public async Task CancelledDiscoveryDoesNotPublishSnapshot()
    {
        var workbench = new WorkbenchState();
        var session = new StandaloneLocalDemoSession();

        int notifications = 0;
        workbench.Changed += (_, _) => notifications++;

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await session.DiscoverAsync(
                workbench,
                cancellationToken: cancellation.Token));

        Assert.Null(workbench.PlatformState);
        Assert.Null(workbench.InfrastructureGraph);
        Assert.Equal(0, notifications);
    }

    [Fact]
    public async Task SequentialDiscoveriesPublishOnceEach()
    {
        var workbench = new WorkbenchState();
        var session = new StandaloneLocalDemoSession();

        int notifications = 0;
        workbench.Changed += (_, _) => notifications++;

        var first = await session.DiscoverAsync(workbench);

        Assert.Same(first, workbench.PlatformState);
        Assert.Equal(1, notifications);

        var second = await session.DiscoverAsync(workbench);

        Assert.Same(second, workbench.PlatformState);
        Assert.NotNull(workbench.InfrastructureGraph);
        Assert.Equal(2, notifications);
    }
}
