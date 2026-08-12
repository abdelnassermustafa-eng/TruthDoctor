using System;
using System.Linq;
using TruthDoctor.Graph;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class TopologyGroupingEngineTests
{
    private readonly TopologyGroupingEngine _engine =
        new();

    [Fact]
    public void GroupsNodesByUniversalDomain()
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
                        "compute"),

                    Node(
                        "subnet",
                        "networking"),

                    Node(
                        "load-balancer",
                        "load-balancing")
                ]
            };

        var groups =
            _engine.GroupByDomain(topology);

        Assert.Equal(
            3,
            groups.Count);

        var compute =
            Assert.Single(
                groups,
                group =>
                    group.Id == "compute");

        Assert.Equal(
            "Compute",
            compute.DisplayName);

        Assert.Equal(
            ["instance", "volume"],
            compute.NodeIds);

        Assert.Equal(
            2,
            compute.Count);

        Assert.Contains(
            groups,
            group =>
                group.Id == "networking" &&
                group.DisplayName == "Networking");

        Assert.Contains(
            groups,
            group =>
                group.Id == "load-balancing" &&
                group.DisplayName ==
                    "Load Balancing");
    }

    [Fact]
    public void DomainMatchingIsCaseInsensitiveAndNormalized()
    {
        var topology =
            new TopologyView
            {
                Nodes =
                [
                    Node(
                        "first",
                        " Networking "),

                    Node(
                        "second",
                        "NETWORKING"),

                    Node(
                        "third",
                        "networking")
                ]
            };

        var group =
            Assert.Single(
                _engine.GroupByDomain(
                    topology));

        Assert.Equal(
            "networking",
            group.Id);

        Assert.Equal(
            ["first", "second", "third"],
            group.NodeIds);
    }

    [Fact]
    public void MissingDomainsUseOtherGroup()
    {
        var topology =
            new TopologyView
            {
                Nodes =
                [
                    Node(
                        "blank",
                        ""),

                    Node(
                        "spaces",
                        "   "),

                    Node(
                        "known",
                        "compute")
                ]
            };

        var groups =
            _engine.GroupByDomain(topology);

        var other =
            Assert.Single(
                groups,
                group =>
                    group.Id == "other");

        Assert.Equal(
            "Other",
            other.DisplayName);

        Assert.Equal(
            ["blank", "spaces"],
            other.NodeIds);
    }

    [Fact]
    public void GroupingAndMembershipOrderAreDeterministic()
    {
        var topology =
            new TopologyView
            {
                Nodes =
                [
                    Node(
                        "zulu",
                        "networking",
                        "Zulu"),

                    Node(
                        "alpha",
                        "compute",
                        "Alpha"),

                    Node(
                        "bravo",
                        "networking",
                        "Bravo"),

                    Node(
                        "another-alpha",
                        "compute",
                        "Alpha")
                ]
            };

        var first =
            _engine.GroupByDomain(topology);

        var second =
            _engine.GroupByDomain(topology);

        Assert.Equal(
            first.Select(group =>
                (
                    group.Id,
                    Members:
                        string.Join(
                            ",",
                            group.NodeIds)
                )),
            second.Select(group =>
                (
                    group.Id,
                    Members:
                        string.Join(
                            ",",
                            group.NodeIds)
                )));

        Assert.Equal(
            ["compute", "networking"],
            first.Select(group =>
                group.Id));

        Assert.Equal(
            ["alpha", "another-alpha"],
            first[0].NodeIds);

        Assert.Equal(
            ["bravo", "zulu"],
            first[1].NodeIds);
    }

    [Fact]
    public void FindsGroupForNodeIgnoringIdentifierCase()
    {
        var topology =
            new TopologyView
            {
                Nodes =
                [
                    Node(
                        "instance-one",
                        "compute"),

                    Node(
                        "subnet-one",
                        "networking")
                ]
            };

        var group =
            _engine.FindGroup(
                topology,
                "INSTANCE-ONE");

        Assert.NotNull(group);

        Assert.Equal(
            "compute",
            group.Id);

        Assert.Null(
            _engine.FindGroup(
                topology,
                "missing"));
    }

    [Fact]
    public void EmptyTopologyReturnsNoGroupsAndNullIsRejected()
    {
        Assert.Empty(
            _engine.GroupByDomain(
                new TopologyView()));

        Assert.Throws<ArgumentNullException>(
            () =>
                _engine.GroupByDomain(
                    null!));
    }

    private static TopologyNode Node(
        string id,
        string domainId,
        string? displayName = null)
    {
        return new TopologyNode
        {
            Id = id,
            ProviderId = "test",
            DomainId = domainId,
            ResourceType = "test-resource",
            DisplayName =
                displayName ?? id
        };
    }
}
