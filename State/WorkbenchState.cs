using System;
using System.Collections.Generic;
using TruthDoctor.Models.Platform;
using TruthDoctor.Graph;

namespace TruthDoctor.State;

public sealed class WorkbenchState
{
    public PlatformState? PlatformState { get; private set; }

    public InfrastructureGraph? InfrastructureGraph
    { get; private set; }


    public InfrastructureGraphIndex? InfrastructureGraphIndex
    { get; private set; }

    public InfrastructureImpactAnalyzer? InfrastructureImpactAnalyzer
    { get; private set; }

    public InfrastructurePathAnalyzer? InfrastructurePathAnalyzer
    { get; private set; }

    public InfrastructureRelationshipAnalyzer?
        InfrastructureRelationshipAnalyzer
    { get; private set; }

    public GraphIntelligenceQueries? GraphIntelligence
    { get; private set; }

    public ResourceGraphContext? SelectedResourceContext
    { get; private set; }

    public InfrastructureResource? SelectedResource
    { get; private set; }

    public string CurrentView { get; set; } = "dashboard";

    public string SelectedProviderId { get; set; } = "";

    public string SelectedAccountId { get; set; } = "";

    public string SelectedLocation { get; set; } = "";

    public string SelectedDomainId { get; set; } = "";

    public string SearchText { get; set; } = "";

    public string SelectedStateFilter { get; set; } = "";

    public IReadOnlyList<InfrastructureResource> VisibleResources
    { get; private set; } =
        Array.Empty<InfrastructureResource>();

    public bool IsDiscovering { get; set; }

    public string LastError { get; set; } = "";

    public event EventHandler? Changed;

    public void SetPlatformState(PlatformState platformState)
    {
        ArgumentNullException.ThrowIfNull(platformState);

        PlatformState = platformState;

        SelectedProviderId =
            platformState.Context.ProviderId;

        SelectedAccountId =
            platformState.Context.AccountId;

        SelectedLocation =
            platformState.Context.DefaultLocation;

        VisibleResources =
            platformState.Resources;

        LastError = "";

        NotifyChanged();
    }


    public void SetInfrastructureGraph(
        InfrastructureGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        InfrastructureGraph = graph;
        InfrastructureGraphIndex =
            new InfrastructureGraphIndex(graph);

        InfrastructureImpactAnalyzer =
            new InfrastructureImpactAnalyzer(
                InfrastructureGraphIndex);

        InfrastructurePathAnalyzer =
            new InfrastructurePathAnalyzer(
                InfrastructureGraphIndex);

        InfrastructureRelationshipAnalyzer =
            new InfrastructureRelationshipAnalyzer(
                InfrastructureGraphIndex);

        GraphIntelligence =
            new GraphIntelligenceQueries(
                InfrastructureGraphIndex);

        if (SelectedResource is not null)
        {
            SelectedResourceContext =
                GraphIntelligence.DescribeResource(
                    SelectedResource);
        }

        NotifyChanged();
    }


    public void SetSelectedResource(
        InfrastructureResource? resource)
    {
        SelectedResource = resource;

        SelectedResourceContext =
            resource is not null &&
            GraphIntelligence is not null
                ? GraphIntelligence.DescribeResource(resource)
                : null;

        NotifyChanged();
    }

    public void SetVisibleResources(
        IReadOnlyList<InfrastructureResource> resources)
    {
        VisibleResources =
            resources ?? Array.Empty<InfrastructureResource>();

        NotifyChanged();
    }

    public void SetFailure(string message)
    {
        LastError = message ?? "";
        IsDiscovering = false;

        NotifyChanged();
    }

    public void NotifyChanged()
    {
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
