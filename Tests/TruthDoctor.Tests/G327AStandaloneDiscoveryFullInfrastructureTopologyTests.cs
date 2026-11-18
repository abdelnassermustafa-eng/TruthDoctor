using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TruthDoctor.Controllers.Workbench;
using TruthDoctor.Services.Standalone;
using TruthDoctor.State;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327AStandaloneDiscoveryFullInfrastructureTopologyTests
{
    private static int CountCollection(object value)
    {
        return ((IEnumerable)value).Cast<object>().Count();
    }

    [Fact]
    public async Task FullInfrastructureIncludesDisconnectedDemoResources()
    {
        var state = new WorkbenchState();
        var demo = new StandaloneLocalDemoSession();

        await demo.DiscoverAsync(state);

        Assert.Equal(3, state.PlatformState!.Resources.Count);
        Assert.Equal(3, state.InfrastructureGraph!.Nodes.Count);
        Assert.Empty(state.InfrastructureGraph.Edges);

        // Full-infrastructure projection must be an explicit
        // capability. Do not alter the selected-resource
        // neighborhood behavior merely to make this test pass.
        var controller = new WorkbenchGraphContextController(state);

        MethodInfo? method =
            typeof(WorkbenchGraphContextController).GetMethod(
                "BuildFullInfrastructureTopology",
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null);

        Assert.True(
            method is not null,
            "RED_EXPECTED_FULL_INFRASTRUCTURE_TOPOLOGY_API");

        object? result = method!.Invoke(controller, null);

        Assert.NotNull(result);

        var nodes = result!.GetType().GetProperty("Nodes");
        var edges = result.GetType().GetProperty("Edges");

        Assert.NotNull(nodes);
        Assert.NotNull(edges);

        Assert.Equal(
            3,
            CountCollection(nodes!.GetValue(result)!));

        Assert.Equal(
            0,
            CountCollection(edges!.GetValue(result)!));

        // Full projection must not implicitly change
        // workbench resource selection.
        Assert.Null(state.SelectedResource);
    }
}
