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
        bool isSelected = false)
    {
        return new TopologyNode
        {
            Id = id,
            ProviderId = "test",
            DomainId = "test",
            ResourceType = "test-resource",
            DisplayName = id,
            IsSelected = isSelected
        };
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
