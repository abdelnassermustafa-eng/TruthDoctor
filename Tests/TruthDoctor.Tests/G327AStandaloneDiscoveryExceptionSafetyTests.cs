using System;
using TruthDoctor.Graph;
using TruthDoctor.Models.Platform;
using TruthDoctor.Services.Standalone;
using TruthDoctor.State;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327AStandaloneDiscoveryExceptionSafetyTests
{
    private static PlatformState MakePlatform(string resourceId)
    {
        ProviderResourceRecord[] records =
        [
            new ProviderResourceRecord(
                "mock-provider",
                resourceId,
                "virtual-machine",
                resourceId)
        ];

        return StandaloneDiscoveryProjector.Project(
            records,
            "account-001",
            "location-001",
            "compute");
    }

    [Fact]
    public void FailedGraphIndexPreparationPreservesPublishedSnapshot()
    {
        var workbench = new WorkbenchState();

        PlatformState original = MakePlatform("original-resource");
        InfrastructureGraph originalGraph =
            new InfrastructureGraphBuilder().Build(original);

        workbench.PublishDiscoverySnapshot(original, originalGraph);
        workbench.SetSelectedResource(original.Resources[0]);

        var priorPlatform = workbench.PlatformState;
        var priorGraph = workbench.InfrastructureGraph;
        var priorIndex = workbench.InfrastructureGraphIndex;
        var priorImpact = workbench.InfrastructureImpactAnalyzer;
        var priorPath = workbench.InfrastructurePathAnalyzer;
        var priorRelationships =
            workbench.InfrastructureRelationshipAnalyzer;
        var priorIntelligence = workbench.GraphIntelligence;
        var priorSelection = workbench.SelectedResource;
        var priorSelectionContext = workbench.SelectedResourceContext;
        var priorVisible = workbench.VisibleResources;
        var priorProvider = workbench.SelectedProviderId;
        var priorAccount = workbench.SelectedAccountId;
        var priorLocation = workbench.SelectedLocation;
        var priorDomain = workbench.SelectedDomainId;
        var priorError = workbench.LastError;

        PlatformState replacement = MakePlatform("replacement-resource");

        // Deliberate fault injection: the graph-index constructor
        // dereferences node properties. Null is invalid here.
        var malformedGraph = new InfrastructureGraph();
        malformedGraph.Nodes.Add("invalid-node", null!);

        int notifications = 0;
        workbench.Changed += (_, _) => notifications++;

        Exception? exception = Record.Exception(() =>
            workbench.PublishDiscoverySnapshot(
                replacement, malformedGraph));

        Assert.IsType<NullReferenceException>(exception);

        // A failed publication must leave the entire previous
        // workbench snapshot intact, not merely suppress Changed.
        Assert.True(
            ReferenceEquals(priorPlatform, workbench.PlatformState),
            "RED_EXPECTED_EXCEPTION_SAFE_PUBLICATION");

        Assert.Same(priorGraph, workbench.InfrastructureGraph);
        Assert.Same(priorIndex, workbench.InfrastructureGraphIndex);
        Assert.Same(priorImpact, workbench.InfrastructureImpactAnalyzer);
        Assert.Same(priorPath, workbench.InfrastructurePathAnalyzer);
        Assert.Same(
            priorRelationships,
            workbench.InfrastructureRelationshipAnalyzer);
        Assert.Same(priorIntelligence, workbench.GraphIntelligence);
        Assert.Same(priorSelection, workbench.SelectedResource);
        Assert.Same(
            priorSelectionContext,
            workbench.SelectedResourceContext);
        Assert.Same(priorVisible, workbench.VisibleResources);

        Assert.Equal(priorProvider, workbench.SelectedProviderId);
        Assert.Equal(priorAccount, workbench.SelectedAccountId);
        Assert.Equal(priorLocation, workbench.SelectedLocation);
        Assert.Equal(priorDomain, workbench.SelectedDomainId);
        Assert.Equal(priorError, workbench.LastError);
        Assert.Equal(0, notifications);

        Assert.True(
            workbench.InfrastructureGraph!.Nodes.ContainsKey(
                "original-resource"));
    }
}
