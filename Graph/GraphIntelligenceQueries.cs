using System;
using System.Collections.Generic;
using System.Linq;

namespace TruthDoctor.Graph;

/// <summary>
/// High-level provider-agnostic infrastructure graph queries.
///
/// This is the primary graph intelligence API for consumers such as
/// the Workbench, operations engine, topology engine, and future AI
/// reasoning components.
/// </summary>
public sealed class GraphIntelligenceQueries
{
    private readonly InfrastructureGraphIndex _index;

    private readonly InfrastructureGraphTraversal _traversal;

    private readonly InfrastructureImpactAnalyzer _impact;

    private readonly InfrastructurePathAnalyzer _paths;

    private readonly InfrastructureRelationshipAnalyzer
        _relationships;

    private readonly RelationshipSemanticsRegistry
        _semantics;

    public GraphIntelligenceQueries(
        InfrastructureGraphIndex index,
        RelationshipSemanticsRegistry? semantics = null)
    {
        ArgumentNullException.ThrowIfNull(index);

        _index = index;

        _semantics =
            semantics ??
            new RelationshipSemanticsRegistry();

        _traversal =
            new InfrastructureGraphTraversal(
                index,
                _semantics);

        _impact =
            new InfrastructureImpactAnalyzer(
                index,
                _semantics);

        _paths =
            new InfrastructurePathAnalyzer(
                index);

        _relationships =
            new InfrastructureRelationshipAnalyzer(
                index,
                _semantics);
    }

    /// <summary>
    /// Returns resources that directly depend on the supplied resource.
    /// </summary>
    public GraphRelationshipQueryResult WhatDependsOn(
        string resourceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            resourceId);

        var edges =
            _index
                .GetIncomingEdges(resourceId)
                .Where(IsDependency)
                .ToList();

        var resources =
            ResolveSources(edges);

        return CreateResult(
            resourceId,
            "depends-on-me",
            resources,
            edges);
    }

    /// <summary>
    /// Returns every transitive dependent of the supplied resource.
    /// This represents the broad dependency blast radius.
    /// </summary>
    public IReadOnlyList<GraphNode>
        WhatUltimatelyDependsOn(
            string resourceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            resourceId);

        return _traversal
            .GetTransitiveDependents(resourceId);
    }

    /// <summary>
    /// Returns resources on which the supplied resource directly depends.
    /// </summary>
    public GraphRelationshipQueryResult WhatDoesThisDependOn(
        string resourceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            resourceId);

        var edges =
            _index
                .GetOutgoingEdges(resourceId)
                .Where(IsDependency)
                .ToList();

        var resources =
            ResolveTargets(edges);

        return CreateResult(
            resourceId,
            "my-dependencies",
            resources,
            edges);
    }

    /// <summary>
    /// Returns all transitive dependencies of the supplied resource.
    /// </summary>
    public IReadOnlyList<GraphNode>
        WhatDoesThisUltimatelyDependOn(
            string resourceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            resourceId);

        return _traversal
            .GetTransitiveDependencies(resourceId);
    }

    /// <summary>
    /// Returns security-related resources associated with this resource.
    /// Handles either graph direction.
    /// </summary>
    public GraphRelationshipQueryResult WhatSecures(
        string resourceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            resourceId);

        var outgoing =
            _index
                .GetOutgoingEdges(resourceId)
                .Where(IsSecurity)
                .ToList();

        var incoming =
            _index
                .GetIncomingEdges(resourceId)
                .Where(IsSecurity)
                .ToList();

        var edges =
            outgoing
                .Concat(incoming)
                .DistinctBy(EdgeIdentity)
                .ToList();

        var resources =
            ResolveNeighbors(
                resourceId,
                edges);

        return CreateResult(
            resourceId,
            "security",
            resources,
            edges);
    }

    /// <summary>
    /// Returns resources directly contained by this resource.
    /// </summary>
    public GraphRelationshipQueryResult WhatDoesThisContain(
        string resourceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            resourceId);

        // "contains" points from container to child.
        var outgoingContains =
            _index
                .GetOutgoingEdges(resourceId)
                .Where(edge =>
                    IsContainmentKind(
                        edge,
                        RelationshipKind.Contains))
                .ToList();

        // "member-of" points from child to container, so an
        // incoming member is contained by this resource.
        var incomingMembers =
            _index
                .GetIncomingEdges(resourceId)
                .Where(edge =>
                    IsContainmentKind(
                        edge,
                        RelationshipKind.MemberOf))
                .ToList();

        var edges =
            outgoingContains
                .Concat(incomingMembers)
                .DistinctBy(EdgeIdentity)
                .ToList();

        var resources =
            ResolveNeighbors(
                resourceId,
                edges);

        return CreateResult(
            resourceId,
            "contains",
            resources,
            edges);
    }

    /// <summary>
    /// Returns containers or parents related to this resource.
    /// </summary>
    public GraphRelationshipQueryResult WhatContainsThis(
        string resourceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            resourceId);

        // An incoming "contains" edge identifies the parent.
        var incomingContainers =
            _index
                .GetIncomingEdges(resourceId)
                .Where(edge =>
                    IsContainmentKind(
                        edge,
                        RelationshipKind.Contains))
                .ToList();

        // An outgoing "member-of" edge identifies the parent.
        var outgoingMemberships =
            _index
                .GetOutgoingEdges(resourceId)
                .Where(edge =>
                    IsContainmentKind(
                        edge,
                        RelationshipKind.MemberOf))
                .ToList();

        var edges =
            incomingContainers
                .Concat(outgoingMemberships)
                .DistinctBy(EdgeIdentity)
                .ToList();

        var resources =
            ResolveNeighbors(
                resourceId,
                edges);

        return CreateResult(
            resourceId,
            "contained-by",
            resources,
            edges);
    }

    /// <summary>
    /// Returns connectivity and traffic-flow relationships around
    /// the supplied resource.
    /// </summary>
    public GraphRelationshipQueryResult WhatIsConnectedTo(
        string resourceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            resourceId);

        var outgoing =
            _index
                .GetOutgoingEdges(resourceId)
                .Where(IsConnectivityOrTraffic)
                .ToList();

        var incoming =
            _index
                .GetIncomingEdges(resourceId)
                .Where(IsConnectivityOrTraffic)
                .ToList();

        var edges =
            outgoing
                .Concat(incoming)
                .DistinctBy(EdgeIdentity)
                .ToList();

        var resources =
            ResolveNeighbors(
                resourceId,
                edges);

        return CreateResult(
            resourceId,
            "connectivity",
            resources,
            edges);
    }

    /// <summary>
    /// Returns the shortest known graph connection between resources.
    /// Reverse traversal is enabled by default so topology questions
    /// can traverse relationships regardless of stored edge direction.
    /// </summary>
    public GraphPathResult HowAreTheyConnected(
        string sourceId,
        string targetId,
        bool includeReverseRelationships = true)
    {
        return _paths.FindShortestPath(
            sourceId,
            targetId,
            includeReverseRelationships);
    }

    /// <summary>
    /// Returns blast-radius information for a resource.
    /// </summary>
    public ImpactAnalysisResult WhatBreaksIfChanged(
        string resourceId)
    {
        return _impact.Analyze(resourceId);
    }

    /// <summary>
    /// Returns the immediate graph neighborhood around a resource.
    /// </summary>
    public IReadOnlyList<GraphNode> WhatIsAround(
        string resourceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            resourceId);

        return _index.GetNeighbors(resourceId);
    }

    /// <summary>
    /// Returns all known resources in a domain.
    /// </summary>
    public IReadOnlyList<GraphNode> FindDomain(
        string domainId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            domainId);

        return _index.FindByDomain(domainId);
    }

    /// <summary>
    /// Returns all known resources of a universal resource type.
    /// </summary>
    public IReadOnlyList<GraphNode> FindResourceType(
        string resourceType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            resourceType);

        return _index.FindByResourceType(
            resourceType);
    }

    private bool IsDependency(
        GraphEdge edge)
    {
        return _semantics
            .Resolve(edge.Relationship)
            .IsDependency;
    }

    private bool IsSecurity(
        GraphEdge edge)
    {
        return _semantics
            .Resolve(edge.Relationship)
            .IsSecurity;
    }

    private bool IsContainmentKind(
        GraphEdge edge,
        RelationshipKind kind)
    {
        var semantic =
            _semantics.Resolve(
                edge.Relationship);

        return semantic.IsContainment &&
               semantic.Kind == kind;
    }

    private bool IsConnectivityOrTraffic(
        GraphEdge edge)
    {
        var semantic =
            _semantics.Resolve(
                edge.Relationship);

        return semantic.IsConnectivity ||
               semantic.IsTrafficFlow;
    }

    private List<GraphNode> ResolveSources(
        IEnumerable<GraphEdge> edges)
    {
        return edges
            .Select(edge =>
                _index.FindNode(edge.SourceId))
            .Where(node => node is not null)
            .Cast<GraphNode>()
            .DistinctBy(node => node.Id)
            .ToList();
    }

    private List<GraphNode> ResolveTargets(
        IEnumerable<GraphEdge> edges)
    {
        return edges
            .Select(edge =>
                _index.FindNode(edge.TargetId))
            .Where(node => node is not null)
            .Cast<GraphNode>()
            .DistinctBy(node => node.Id)
            .ToList();
    }

    private List<GraphNode> ResolveNeighbors(
        string resourceId,
        IEnumerable<GraphEdge> edges)
    {
        var result =
            new List<GraphNode>();

        foreach (var edge in edges)
        {
            var otherId =
                edge.SourceId.Equals(
                    resourceId,
                    StringComparison.OrdinalIgnoreCase)
                    ? edge.TargetId
                    : edge.SourceId;

            var node =
                _index.FindNode(otherId);

            if (node is not null)
            {
                result.Add(node);
            }
        }

        return result
            .DistinctBy(node => node.Id)
            .ToList();
    }

    private static GraphRelationshipQueryResult
        CreateResult(
            string resourceId,
            string query,
            IReadOnlyList<GraphNode> resources,
            IReadOnlyList<GraphEdge> edges)
    {
        return new GraphRelationshipQueryResult
        {
            ResourceId = resourceId,
            Query = query,
            Resources = resources,
            Relationships = edges
        };
    }


    public ResourceGraphContext DescribeResource(
        object resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        var node =
            _index.Graph.Nodes.Values
                .FirstOrDefault(candidate =>
                    ReferenceEquals(
                        candidate.Resource,
                        resource));

        if (node is null)
        {
            return new ResourceGraphContext();
        }

        return DescribeResource(node.Id);
    }

    public ResourceGraphContext DescribeResource(
        string resourceId)
    {
        var node =
            _index.FindNode(resourceId);

        return new ResourceGraphContext
        {
            Resource = node,

            Impact =
                WhatBreaksIfChanged(resourceId),

            Dependencies =
                WhatDoesThisDependOn(resourceId),

            Dependents =
                WhatDependsOn(resourceId),

            Security =
                WhatSecures(resourceId),

            Connectivity =
                WhatIsConnectedTo(resourceId),

            Contains =
                WhatDoesThisContain(resourceId),

            ContainedBy =
                WhatContainsThis(resourceId),

            Neighborhood =
                WhatIsAround(resourceId)
        };
    }


    private static string EdgeIdentity(
        GraphEdge edge)
    {
        return
            $"{edge.SourceId}\u001F" +
            $"{edge.TargetId}\u001F" +
            $"{edge.Relationship}";
    }
}
