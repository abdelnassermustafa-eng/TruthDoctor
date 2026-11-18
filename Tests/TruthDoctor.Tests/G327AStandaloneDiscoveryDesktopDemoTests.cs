using System;
using System.Linq;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using TruthDoctor.State;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327AStandaloneDiscoveryDesktopDemoTests
{
    [Fact]
    public async Task LocalDemoPublishesResourcesAndTopology()
    {
        var demo = new StandaloneLocalDemoSession();
        var workbench = new WorkbenchState();

        int notifications = 0;

        workbench.Changed += (_, _) =>
        {
            if (workbench.PlatformState is not null)
            {
                Assert.NotNull(workbench.InfrastructureGraph);
            }

            notifications++;
        };

        var result = await demo.DiscoverAsync(workbench);

        Assert.Same(result, workbench.PlatformState);
        Assert.Equal("local-demo", result.Context.ProviderId);
        Assert.Equal("local-demo", result.Context.DefaultLocation);
        Assert.Equal(3, result.Resources.Count);
        Assert.Equal(3, result.TotalResourceCount);
        Assert.Equal(3, workbench.InfrastructureGraph!.Nodes.Count);

        Assert.Contains(
            result.Resources, r => r.ResourceId == "demo-vm-001");
        Assert.Contains(
            result.Resources, r => r.ResourceId == "demo-network-001");
        Assert.Contains(
            result.Resources, r => r.ResourceId == "demo-storage-001");

        Assert.Equal(1, notifications);
    }

    [Fact]
    public async Task LocalDemoRefreshSupportsExplicitLocation()
    {
        var demo = new StandaloneLocalDemoSession();
        var workbench = new WorkbenchState();

        await demo.DiscoverAsync(workbench);

        var result = await demo.DiscoverAsync(
            workbench, "test-region-001");

        Assert.Equal(
            "test-region-001", result.Context.DefaultLocation);

        Assert.All(result.Resources, resource =>
            Assert.Equal("test-region-001", resource.Location));

        Assert.Same(result, workbench.PlatformState);
        Assert.Equal(3, workbench.VisibleResources.Count);
        Assert.Equal(3, workbench.InfrastructureGraph!.Nodes.Count);
    }
}
