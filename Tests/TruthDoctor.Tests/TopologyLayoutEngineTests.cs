using System;
using System.Collections.Generic;
using System.Linq;
using TruthDoctor.Graph;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class TopologyLayoutEngineTests
{
    private readonly TopologyLayoutEngine _engine =
        new();

    [Fact]
    public void RadialLayoutCentersSelectionAndPositionsEveryNode()
    {
        var topology =
            BuildTopology();

        var positions =
            _engine.Arrange(
                topology,
                TopologyLayoutMode.Radial);

        Assert.Equal(
            topology.Nodes.Count,
            positions.Count);

        Assert.Equal(
            700,
            positions["selected"].X,
            precision: 6);

        Assert.Equal(
            420,
            positions["selected"].Y,
            precision: 6);

        Assert.All(
            positions.Values,
            AssertInsideCanvas);
    }

    [Fact]
    public void HierarchicalLayoutRespectsIncomingAndOutgoingDirection()
    {
        var topology =
            BuildTopology();

        var positions =
            _engine.Arrange(
                topology,
                TopologyLayoutMode.Hierarchical);

        Assert.True(
            positions["incoming"].Y <
            positions["selected"].Y);

        Assert.True(
            positions["outgoing"].Y >
            positions["selected"].Y);

        Assert.All(
            positions.Values,
            AssertInsideCanvas);
    }

    [Fact]
    public void NetworkLayoutIsDeterministicAndKeepsSelectionCentered()
    {
        var topology =
            BuildTopology();

        var first =
            _engine.Arrange(
                topology,
                TopologyLayoutMode.Network);

        var second =
            _engine.Arrange(
                topology,
                TopologyLayoutMode.Network);

        Assert.Equal(
            first.OrderBy(item => item.Key),
            second.OrderBy(item => item.Key));

        Assert.Equal(
            700,
            first["selected"].X,
            precision: 6);

        Assert.Equal(
            420,
            first["selected"].Y,
            precision: 6);

        Assert.All(
            first.Values,
            AssertInsideCanvas);
    }

    [Fact]
    public void DomainLayoutPlacesSameDomainNodesTogether()
    {
        var topology =
            new TopologyView
            {
                SelectedResourceId =
                    "compute-selected",

                Nodes =
                [
                    Node(
                        "compute-selected",
                        isSelected: true,
                        domainId: "compute"),

                    Node(
                        "compute-volume",
                        domainId: "compute"),

                    Node(
                        "network-subnet",
                        domainId: "networking"),

                    Node(
                        "network-vpc",
                        domainId: "networking")
                ]
            };

        var positions =
            _engine.Arrange(
                topology,
                TopologyLayoutMode.Domain);

        Assert.Equal(
            topology.Nodes.Count,
            positions.Count);

        Assert.All(
            positions.Values,
            AssertInsideCanvas);

        var computeDistance =
            Distance(
                positions["compute-selected"],
                positions["compute-volume"]);

        var crossDomainDistance =
            Distance(
                positions["compute-selected"],
                positions["network-subnet"]);

        Assert.True(
            computeDistance <
            crossDomainDistance);
    }

    [Fact]
    public void DomainLayoutIsDeterministicAndCentersSelectedInGroup()
    {
        var topology =
            new TopologyView
            {
                SelectedResourceId =
                    "selected",

                Nodes =
                [
                    Node(
                        "network-two",
                        domainId: "networking"),

                    Node(
                        "selected",
                        isSelected: true,
                        domainId: "compute"),

                    Node(
                        "compute-two",
                        domainId: "compute"),

                    Node(
                        "network-one",
                        domainId: "networking")
                ]
            };

        var first =
            _engine.Arrange(
                topology,
                TopologyLayoutMode.Domain);

        var second =
            _engine.Arrange(
                topology,
                TopologyLayoutMode.Domain);

        Assert.Equal(
            first.OrderBy(item =>
                item.Key),
            second.OrderBy(item =>
                item.Key));

        Assert.NotEqual(
            first["selected"],
            first["compute-two"]);

        Assert.All(
            first.Values,
            AssertInsideCanvas);
    }

    [Fact]
    public void DomainLayoutPreventsOverlapInDenseSingleDomain()
    {
        var topology =
            new TopologyView
            {
                SelectedResourceId =
                    "node-05",

                Nodes =
                    Enumerable.Range(
                            1,
                            10)
                        .Select(index =>
                            Node(
                                $"node-{index:00}",
                                isSelected:
                                    index == 5,
                                domainId:
                                    "networking"))
                        .ToList()
            };

        var positions =
            _engine.Arrange(
                topology,
                TopologyLayoutMode.Domain);

        Assert.Equal(
            10,
            positions.Count);

        AssertNoNodeOverlap(
            positions);
    }

    [Fact]
    public void DomainLayoutPreventsOverlapAcrossNewDomains()
    {
        var domains =
            new[]
            {
                "compute",
                "networking",
                "load-balancing",
                "storage"
            };

        var nodes =
            domains
                .SelectMany(domain =>
                    Enumerable.Range(
                            1,
                            3)
                        .Select(index =>
                            Node(
                                $"{domain}-{index}",
                                domainId:
                                    domain)))
                .ToList();

        nodes[0] =
            Node(
                nodes[0].Id,
                isSelected: true,
                domainId:
                    nodes[0].DomainId);

        var topology =
            new TopologyView
            {
                SelectedResourceId =
                    nodes[0].Id,

                Nodes =
                    nodes
            };

        var positions =
            _engine.Arrange(
                topology,
                TopologyLayoutMode.Domain);

        Assert.Equal(
            nodes.Count,
            positions.Count);

        AssertNoNodeOverlap(
            positions);
    }

    [Fact]
    public void EmptyTopologyReturnsNoPositionsAndInvalidCanvasFails()
    {
        var empty =
            _engine.Arrange(
                new TopologyView(),
                TopologyLayoutMode.Radial);

        Assert.Empty(empty);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _engine.Arrange(
                BuildTopology(),
                TopologyLayoutMode.Radial,
                canvasWidth: 0));
    }

    private static TopologyView BuildTopology()
    {
        return new TopologyView
        {
            SelectedResourceId =
                "selected",

            Nodes =
            [
                Node(
                    "selected",
                    isSelected: true),

                Node("incoming"),
                Node("outgoing"),
                Node("branch")
            ],

            Edges =
            [
                new TopologyEdge
                {
                    SourceId = "incoming",
                    TargetId = "selected",
                    Relationship = "depends-on",
                    Kind = RelationshipKind.DependsOn
                },

                new TopologyEdge
                {
                    SourceId = "selected",
                    TargetId = "outgoing",
                    Relationship = "attached-to",
                    Kind = RelationshipKind.AttachedTo
                },

                new TopologyEdge
                {
                    SourceId = "outgoing",
                    TargetId = "branch",
                    Relationship = "member-of",
                    Kind = RelationshipKind.MemberOf
                }
            ]
        };
    }

    private static TopologyNode Node(
        string id,
        bool isSelected = false,
        string domainId = "test")
    {
        return new TopologyNode
        {
            Id = id,
            ProviderId = "test",
            DomainId = domainId,
            ResourceType = "test-resource",
            DisplayName = id,
            IsSelected = isSelected
        };
    }

    private static void AssertNoNodeOverlap(
        IReadOnlyDictionary<
            string,
            TopologyLayoutPosition> positions)
    {
        var ordered =
            positions
                .OrderBy(item =>
                    item.Key)
                .ToList();

        const double nodeWidth = 160;
        const double nodeHeight = 70;

        for (var leftIndex = 0;
             leftIndex < ordered.Count;
             leftIndex++)
        {
            for (var rightIndex =
                     leftIndex + 1;
                 rightIndex < ordered.Count;
                 rightIndex++)
            {
                var left =
                    ordered[leftIndex];

                var right =
                    ordered[rightIndex];

                var overlapsHorizontally =
                    left.Value.X <
                        right.Value.X +
                        nodeWidth &&
                    right.Value.X <
                        left.Value.X +
                        nodeWidth;

                var overlapsVertically =
                    left.Value.Y <
                        right.Value.Y +
                        nodeHeight &&
                    right.Value.Y <
                        left.Value.Y +
                        nodeHeight;

                Assert.False(
                    overlapsHorizontally &&
                    overlapsVertically,
                    $"Nodes '{left.Key}' and " +
                    $"'{right.Key}' overlap.");
            }
        }
    }

    private static double Distance(
        TopologyLayoutPosition left,
        TopologyLayoutPosition right)
    {
        var deltaX =
            left.X -
            right.X;

        var deltaY =
            left.Y -
            right.Y;

        return Math.Sqrt(
            (deltaX * deltaX) +
            (deltaY * deltaY));
    }

    private static void AssertInsideCanvas(
        TopologyLayoutPosition position)
    {
        Assert.InRange(
            position.X,
            0,
            1600);

        Assert.InRange(
            position.Y,
            0,
            1000);
    }
}
