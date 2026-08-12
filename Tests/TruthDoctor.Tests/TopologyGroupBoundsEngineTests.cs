using System;
using System.Collections.Generic;
using TruthDoctor.Graph;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class TopologyGroupBoundsEngineTests
{
    private readonly TopologyGroupBoundsEngine _engine =
        new();

    [Fact]
    public void CalculatesBoundaryAroundEveryPositionedMember()
    {
        var topology =
            new TopologyView
            {
                Nodes =
                [
                    Node(
                        "instance",
                        "compute"),

                    Node(
                        "volume",
                        "compute")
                ]
            };

        var positions =
            new Dictionary<
                string,
                TopologyLayoutPosition>
            {
                ["instance"] =
                    new(100, 200),

                ["volume"] =
                    new(400, 500)
            };

        var bounds =
            Assert.Single(
                _engine.Calculate(
                    topology,
                    positions,
                    nodeWidth: 160,
                    nodeHeight: 70,
                    horizontalPadding: 20,
                    verticalPadding: 15,
                    headerHeight: 25));

        Assert.Equal(
            "compute",
            bounds.GroupId);

        Assert.Equal(
            "Compute",
            bounds.DisplayName);

        Assert.Equal(
            2,
            bounds.NodeCount);

        Assert.Equal(
            80,
            bounds.X);

        Assert.Equal(
            160,
            bounds.Y);

        Assert.Equal(
            500,
            bounds.Width);

        Assert.Equal(
            425,
            bounds.Height);
    }

    [Fact]
    public void CreatesDeterministicallyOrderedDomainBoundaries()
    {
        var topology =
            new TopologyView
            {
                Nodes =
                [
                    Node(
                        "subnet",
                        "networking"),

                    Node(
                        "instance",
                        "compute"),

                    Node(
                        "target",
                        "load-balancing")
                ]
            };

        var positions =
            new Dictionary<
                string,
                TopologyLayoutPosition>
            {
                ["subnet"] =
                    new(100, 100),

                ["instance"] =
                    new(500, 100),

                ["target"] =
                    new(900, 100)
            };

        var bounds =
            _engine.Calculate(
                topology,
                positions,
                nodeWidth: 160,
                nodeHeight: 70);

        Assert.Equal(
            [
                "compute",
                "load-balancing",
                "networking"
            ],
            bounds.Select(item =>
                item.GroupId));
    }

    [Fact]
    public void IgnoresMembersWithoutLayoutPositions()
    {
        var topology =
            new TopologyView
            {
                Nodes =
                [
                    Node(
                        "positioned",
                        "compute"),

                    Node(
                        "missing",
                        "compute"),

                    Node(
                        "entirely-missing",
                        "networking")
                ]
            };

        var positions =
            new Dictionary<
                string,
                TopologyLayoutPosition>
            {
                ["positioned"] =
                    new(300, 250)
            };

        var bounds =
            Assert.Single(
                _engine.Calculate(
                    topology,
                    positions,
                    nodeWidth: 160,
                    nodeHeight: 70));

        Assert.Equal(
            "compute",
            bounds.GroupId);

        Assert.Equal(
            1,
            bounds.NodeCount);
    }

    [Fact]
    public void EmptyTopologyReturnsNoBoundaries()
    {
        var bounds =
            _engine.Calculate(
                new TopologyView(),
                new Dictionary<
                    string,
                    TopologyLayoutPosition>(),
                nodeWidth: 160,
                nodeHeight: 70);

        Assert.Empty(bounds);
    }

    [Fact]
    public void InvalidDimensionsAreRejected()
    {
        var topology =
            new TopologyView
            {
                Nodes =
                [
                    Node(
                        "instance",
                        "compute")
                ]
            };

        var positions =
            new Dictionary<
                string,
                TopologyLayoutPosition>
            {
                ["instance"] =
                    new(100, 100)
            };

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                _engine.Calculate(
                    topology,
                    positions,
                    nodeWidth: 0,
                    nodeHeight: 70));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                _engine.Calculate(
                    topology,
                    positions,
                    nodeWidth: 160,
                    nodeHeight: 70,
                    horizontalPadding: -1));
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
}
