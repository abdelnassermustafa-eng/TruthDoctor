using System;
using System.Collections.Generic;
using System.Reflection;
using TruthDoctor.Graph;
using TruthDoctor.Models.Platform;
using TruthDoctor.Services.Standalone;
using TruthDoctor.State;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327AStandaloneDiscoveryProjectionRedTests
{
    [Fact]
    public void StandaloneDiscoveryProjectionPreservesResourceIdentityAndBuildsGraph()
    {
        IReadOnlyList<ProviderResourceRecord> records =
        [
            new ProviderResourceRecord(
                "mock-provider",
                "resource-001",
                "virtual-machine",
                "Sample Resource")
        ];

        // This is the intentionally missing production boundary.
        // Reflection keeps the test project compilable in RED.
        Type? projectorType =
            typeof(ProviderResourceRecord).Assembly.GetType(
                "TruthDoctor.Services.Standalone.StandaloneDiscoveryProjector",
                throwOnError: false);

        Assert.True(
            projectorType is not null,
            "RED_EXPECTED_MISSING_STANDALONE_DISCOVERY_PROJECTOR");

        MethodInfo? projectMethod = projectorType.GetMethod(
            "Project",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types:
            [
                typeof(IReadOnlyList<ProviderResourceRecord>),
                typeof(string),
                typeof(string),
                typeof(string)
            ],
            modifiers: null);

        Assert.True(
            projectMethod is not null,
            "RED_EXPECTED_MISSING_PROJECT_METHOD");

        object? result = projectMethod.Invoke(
            null,
            [
                records,
                "account-001",
                "location-001",
                "compute"
            ]);

        PlatformState platform = Assert.IsType<PlatformState>(result);
        InfrastructureResource resource = Assert.Single(platform.Resources);

        Assert.Equal("mock-provider", platform.Context.ProviderId);
        Assert.Equal("account-001", platform.Context.AccountId);
        Assert.Equal("location-001", platform.Context.DefaultLocation);

        Assert.Equal("mock-provider", resource.ProviderId);
        Assert.Equal("resource-001", resource.ResourceId);
        Assert.Equal("virtual-machine", resource.ResourceType);
        Assert.Equal("Sample Resource", resource.DisplayName);

        Assert.Equal("account-001", resource.AccountId);
        Assert.Equal("location-001", resource.Location);
        Assert.Equal("compute", resource.DomainId);
        Assert.Equal(1, platform.TotalResourceCount);

        var graph = new InfrastructureGraphBuilder().Build(platform);

        Assert.True(
            graph.Nodes.TryGetValue("resource-001", out GraphNode? node));

        Assert.NotNull(node);
        Assert.Same(resource, node.Resource);

        var workbench = new WorkbenchState();
        workbench.SetPlatformState(platform);
        workbench.SetInfrastructureGraph(graph);

        Assert.Same(platform, workbench.PlatformState);
        Assert.Same(graph, workbench.InfrastructureGraph);
        Assert.Single(workbench.VisibleResources);
        Assert.Equal("mock-provider", workbench.SelectedProviderId);
        Assert.Equal("account-001", workbench.SelectedAccountId);
        Assert.Equal("location-001", workbench.SelectedLocation);
    }
}
