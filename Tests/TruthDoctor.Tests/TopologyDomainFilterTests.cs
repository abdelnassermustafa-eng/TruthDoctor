using System;
using System.Linq;
using TruthDoctor.Graph;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class TopologyDomainFilterTests
{
    private readonly TopologyDomainFilter _filter =
        new();

    [Fact]
    public void AllDomainsPreservesCompleteTopology()
    {
        var topology =
            CreateTopology();

        var result =
            _filter.Apply(
                topology,
                TopologyDomainFilter.AllDomains);

        Assert.Same(
            topology,
            result);

        Assert.Equal(
            4,
            result.Nodes.Count);

        Assert.Equal(
            4,
            result.Edges.Count);
    }

    [Fact]
    public void SelectedDomainContainsOnlyItsResources()
    {
        var result =
            _filter.Apply(
                CreateTopology(),
                "networking");

        Assert.Equal(
            ["subnet", "vpc"],
            result.Nodes
                .Select(node => node.Id)
                .OrderBy(id => id));

        Assert.All(
            result.Nodes,
            node =>
                Assert.Equal(
                    "networking",
                    node.DomainId));
    }

    [Fact]
    public void SelectedDomainRemovesCrossDomainAndDanglingEdges()
    {
        var result =
            _filter.Apply(
                CreateTopology(),
                "NETWORKING");

        var edge =
            Assert.Single(
                result.Edges);

        Assert.Equal(
            "subnet",
            edge.SourceId);

        Assert.Equal(
            "vpc",
            edge.TargetId);

        var includedNodeIds =
            result.Nodes
                .Select(node => node.Id)
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase);

        Assert.All(
            result.Edges,
            candidate =>
            {
                Assert.Contains(
                    candidate.SourceId,
                    includedNodeIds);

                Assert.Contains(
                    candidate.TargetId,
                    includedNodeIds);
            });
    }

    [Fact]
    public void AvailableDomainsAutomaticallyIncludesNewDomains()
    {
        var topology =
            CreateTopology();

        var domains =
            _filter.AvailableDomains(
                topology);

        Assert.Equal(
            ["compute", "database", "networking"],
            domains.Select(domain =>
                domain.Id));

        Assert.Equal(
            [1, 1, 2],
            domains.Select(domain =>
                domain.Count));
    }

    [Fact]
    public void HiddenSelectionIsClearedAndVisibleSelectionIsPreserved()
    {
        var topology =
            CreateTopology();

        var networking =
            _filter.Apply(
                topology,
                "networking");

        Assert.Equal(
            "vpc",
            networking.SelectedResourceId);

        var compute =
            _filter.Apply(
                topology,
                "compute");

        Assert.Empty(
            compute.SelectedResourceId);
    }

    [Fact]
    public void OtherDomainIncludesResourcesWithoutDomainIdentifiers()
    {
        var topology =
            new TopologyView
            {
                Nodes =
                [
                    Node(
                        "known",
                        "networking"),

                    Node(
                        "blank",
                        ""),

                    Node(
                        "spaces",
                        "   ")
                ]
            };

        var result =
            _filter.Apply(
                topology,
                "other");

        Assert.Equal(
            ["blank", "spaces"],
            result.Nodes
                .Select(node => node.Id)
                .OrderBy(id => id));
    }

    [Fact]
    public void EmptyAndUnknownDomainsAreHandledSafely()
    {
        var topology =
            new TopologyView();

        Assert.Same(
            topology,
            _filter.Apply(
                topology,
                null));

        var missing =
            _filter.Apply(
                CreateTopology(),
                "missing");

        Assert.Empty(
            missing.Nodes);

        Assert.Empty(
            missing.Edges);

        Assert.Empty(
            missing.SelectedResourceId);
    }

    private static TopologyView CreateTopology()
    {
        return new TopologyView
        {
            SelectedResourceId =
                "vpc",

            Nodes =
            [
                Node(
                    "vpc",
                    "networking"),

                Node(
                    "subnet",
                    "networking"),

                Node(
                    "instance",
                    "compute"),

                Node(
                    "database",
                    "database")
            ],

            Edges =
            [
                Edge(
                    "subnet",
                    "vpc"),

                Edge(
                    "instance",
                    "subnet"),

                Edge(
                    "instance",
                    "database"),

                Edge(
                    "database",
                    "vpc")
            ]
        };
    }

    private static TopologyNode Node(
        string id,
        string domainId)
    {
        return new TopologyNode
        {
            Id = id,
            ProviderId = "test",
            DomainId = domainId,
            ResourceType = "test-resource",
            DisplayName = id
        };
    }

    private static TopologyEdge Edge(
        string sourceId,
        string targetId)
    {
        return new TopologyEdge
        {
            SourceId = sourceId,
            TargetId = targetId,
            Relationship = "associated-with",
            Kind =
                RelationshipKind.AssociatedWith
        };
    }
}
