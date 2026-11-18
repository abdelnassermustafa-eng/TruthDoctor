using System;
using System.Collections.Generic;
using TruthDoctor.Models.Platform;
using TruthDoctor.Graph;
using TruthDoctor.Services.Standalone;

namespace TruthDoctor.State;

public sealed class WorkbenchState
{
    public PlatformState? PlatformState { get; private set; }

    /// <summary>
    /// Verified association evidence for the published resource snapshot.
    /// These are not operational graph relationships.
    /// </summary>
    public IReadOnlyList<InfrastructureAssociationView> AssociationViews
    { get; private set; } =
        Array.Empty<InfrastructureAssociationView>();

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

    private bool _discoveryPublicationInProgress;

    /// <summary>
    /// Publishes a prepared discovery state and its graph with
    /// one completed workbench notification.
    /// </summary>
    /// <summary>
    /// Prepared discovery state and graph-derived objects.
    /// Preparation does not mutate the workbench.
    /// </summary>
    internal sealed class PreparedDiscoverySnapshot
    {
        internal required PlatformState Platform { get; init; }
        internal required IReadOnlyList<InfrastructureAssociationView>
            Associations { get; init; }
        internal required InfrastructureGraph Graph { get; init; }
        internal required InfrastructureGraphIndex Index { get; init; }
        internal required InfrastructureImpactAnalyzer Impact { get; init; }
        internal required InfrastructurePathAnalyzer Path { get; init; }
        internal required InfrastructureRelationshipAnalyzer Relationships { get; init; }
        internal required GraphIntelligenceQueries Intelligence { get; init; }
        internal ResourceGraphContext? SelectionContext { get; init; }
    }

    /// <summary>
    /// Prepares discovery data without publishing or notifying.
    /// </summary>
    internal PreparedDiscoverySnapshot PrepareDiscoverySnapshot(
        PlatformState platformState,
        InfrastructureGraph graph)
        => PrepareDiscoverySnapshot(
            platformState,
            graph,
            Array.Empty<InfrastructureAssociationView>());

    internal PreparedDiscoverySnapshot PrepareDiscoverySnapshot(
        PlatformState platformState,
        InfrastructureGraph graph,
        IReadOnlyList<InfrastructureAssociationView> associations)
    {
        ArgumentNullException.ThrowIfNull(platformState);
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(associations);

        if (_discoveryPublicationInProgress)
        {
            throw new InvalidOperationException(
                "Nested discovery publication is not supported.");
        }

        var index = new InfrastructureGraphIndex(graph);
        var intelligence = new GraphIntelligenceQueries(index);

        return new PreparedDiscoverySnapshot
        {
            Platform = platformState,
            Associations = new List<InfrastructureAssociationView>(
                associations).AsReadOnly(),
            Graph = graph,
            Index = index,
            Impact = new InfrastructureImpactAnalyzer(index),
            Path = new InfrastructurePathAnalyzer(index),
            Relationships =
                new InfrastructureRelationshipAnalyzer(index),
            Intelligence = intelligence,
            SelectionContext =
                SelectedResource is not null
                    ? intelligence.DescribeResource(SelectedResource)
                    : SelectedResourceContext
        };
    }

    /// <summary>
    /// Commits prepared data without notifying observers.
    /// </summary>
    internal void CommitPreparedDiscoverySnapshot(
        PreparedDiscoverySnapshot prepared)
    {
        ArgumentNullException.ThrowIfNull(prepared);

        if (_discoveryPublicationInProgress)
        {
            throw new InvalidOperationException(
                "Nested discovery publication is not supported.");
        }

        _discoveryPublicationInProgress = true;

        try
        {
            PlatformState = prepared.Platform;
            AssociationViews = prepared.Associations;
            SelectedProviderId = prepared.Platform.Context.ProviderId;
            SelectedAccountId = prepared.Platform.Context.AccountId;
            SelectedLocation =
                prepared.Platform.Context.DefaultLocation;
            VisibleResources = prepared.Platform.Resources;
            LastError = "";

            InfrastructureGraph = prepared.Graph;
            InfrastructureGraphIndex = prepared.Index;
            InfrastructureImpactAnalyzer = prepared.Impact;
            InfrastructurePathAnalyzer = prepared.Path;
            InfrastructureRelationshipAnalyzer =
                prepared.Relationships;
            GraphIntelligence = prepared.Intelligence;
            SelectedResourceContext = prepared.SelectionContext;
        }
        finally
        {
            _discoveryPublicationInProgress = false;
        }
    }

    /// <summary>
    /// Notifies observers following the authorized commit.
    /// </summary>
    internal void NotifyCommittedDiscoverySnapshot()
    {
        NotifyChanged();
    }

    /// <summary>
    /// Maintains the existing public publication API.
    /// </summary>
    public void PublishDiscoverySnapshot(
        PlatformState platformState,
        InfrastructureGraph graph)
    {
        var prepared = PrepareDiscoverySnapshot(
            platformState, graph);

        CommitPreparedDiscoverySnapshot(prepared);
        NotifyCommittedDiscoverySnapshot();
    }

    public void SetPlatformState(PlatformState platformState)
    {
        ArgumentNullException.ThrowIfNull(platformState);

        PlatformState = platformState;
        AssociationViews = Array.Empty<InfrastructureAssociationView>();

        SelectedProviderId =
            platformState.Context.ProviderId;

        SelectedAccountId =
            platformState.Context.AccountId;

        SelectedLocation =
            platformState.Context.DefaultLocation;

        VisibleResources =
            platformState.Resources;

        LastError = "";

        if (!_discoveryPublicationInProgress)
        {
            NotifyChanged();
        }
    }


    public void SetInfrastructureGraph(
        InfrastructureGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        InfrastructureGraph = graph;
        AssociationViews = Array.Empty<InfrastructureAssociationView>();
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

        if (!_discoveryPublicationInProgress)
        {
            NotifyChanged();
        }
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
