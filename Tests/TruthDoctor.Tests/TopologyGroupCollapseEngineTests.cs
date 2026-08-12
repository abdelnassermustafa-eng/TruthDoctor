using System;
using System.Linq;
using TruthDoctor.Graph;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class TopologyGroupCollapseEngineTests
{
    private readonly TopologyGroupCollapseEngine _engine =
        new();

    [Fact]
    public void NoCollapsedDomainsPreservesOriginalTopology()
    {
        var topology =
            CreateTopology();

        Assert.Same(
            topology,
            _engine.Project(
                topology,
                []));
    }

    [Fact]
    public void CollapsedDomainIsReplacedBySummaryNode()
    {
        var result =
            _engine.Project(
                CreateTopology(),
                ["networking"]);

        Assert.DoesNotContain(
            result.Nodes,
            node =>
                node.Id == "vpc" ||
                node.Id == "subnet-a" ||
                node.Id == "subnet-b");

        var summary =
            Assert.Single(
                result.Nodes,
                TopologyGroupCollapseEngine
                    .IsSummaryNode);

        Assert.Equal(
            "topology-domain-summary:networking",
            summary.Id);

        Assert.Equal(
            "networking",
            summary.DomainId);

        Assert.Equal(
            "Networking · 3",
            summary.DisplayName);

        Assert.Equal(
            "3",
            summary.Properties[
                "ResourceCount"]);
    }

    [Fact]
    public void InternalDomainRelationshipsAreHidden()
    {
        var result =
            _engine.Project(
                CreateTopology(),
                ["networking"]);

        Assert.DoesNotContain(
            result.Edges,
            edge =>
                edge.Relationship ==
                "member-of");
    }

    [Fact]
    public void CrossDomainRelationshipsReconnectToSummary()
    {
        var result =
            _engine.Project(
                CreateTopology(),
                ["networking"]);

        Assert.Contains(
            result.Edges,
            edge =>
                edge.SourceId ==
                    "instance-a" &&
                edge.TargetId ==
                    "topology-domain-summary:networking" &&
                edge.Relationship ==
                    "hosted-on");

        Assert.Contains(
            result.Edges,
            edge =>
                edge.SourceId ==
                    "topology-domain-summary:networking" &&
                edge.TargetId ==
                    "database-a" &&
                edge.Relationship ==
                    "connected-to");
    }

    [Fact]
    public void EquivalentCrossDomainRelationshipsAreAggregated()
    {
        var result =
            _engine.Project(
                CreateTopology(),
                ["networking"]);

        var hostedOn =
            Assert.Single(
                result.Edges,
                edge =>
                    edge.SourceId ==
                        "instance-a" &&
                    edge.TargetId ==
                        "topology-domain-summary:networking" &&
                    edge.Relationship ==
                        "hosted-on");

        Assert.Equal(
            2,
            hostedOn.Multiplicity);
    }

    [Fact]
    public void TwoCollapsedDomainsConnectSummaryToSummary()
    {
        var result =
            _engine.Project(
                CreateTopology(),
                [
                    "networking",
                    "database"
                ]);

        Assert.Contains(
            result.Nodes,
            node =>
                node.Id ==
                "topology-domain-summary:database");

        Assert.Contains(
            result.Edges,
            edge =>
                edge.SourceId ==
                    "topology-domain-summary:networking" &&
                edge.TargetId ==
                    "topology-domain-summary:database" &&
                edge.Relationship ==
                    "connected-to");
    }

    [Fact]
    public void SelectedResourcesDomainCollapsesToSelectedSummary()
    {
        var topology =
            CreateTopology(
                selectedResourceId:
                    "subnet-a");

        var result =
            _engine.Project(
                topology,
                ["networking"]);

        var summary =
            Assert.Single(
                result.Nodes,
                TopologyGroupCollapseEngine
                    .IsSummaryNode);

        Assert.True(
            summary.IsSelected);

        Assert.Equal(
            summary.Id,
            result.SelectedResourceId);

        Assert.DoesNotContain(
            result.Nodes,
            node =>
                node.Id == "subnet-a");
    }

    [Fact]
    public void UnknownAndNewDomainsAreHandledAutomatically()
    {
        var topology =
            new TopologyView
            {
                Nodes =
                [
                    Node(
                        "future-one",
                        "future-services"),

                    Node(
                        "future-two",
                        "future-services"),

                    Node(
                        "existing",
                        "compute")
                ],

                Edges =
                [
                    Edge(
                        "existing",
                        "future-one",
                        "depends-on",
                        RelationshipKind.DependsOn)
                ]
            };

        var result =
            _engine.Project(
                topology,
                [
                    "missing",
                    "FUTURE-SERVICES"
                ]);

        var summary =
            Assert.Single(
                result.Nodes,
                TopologyGroupCollapseEngine
                    .IsSummaryNode);

        Assert.Equal(
            "Future Services · 2",
            summary.DisplayName);

        Assert.Contains(
            result.Edges,
            edge =>
                edge.SourceId == "existing" &&
                edge.TargetId == summary.Id);
    }

    [Fact]
    public void ProjectionIsDeterministic()
    {
        var topology =
            CreateTopology();

        var first =
            _engine.Project(
                topology,
                [
                    "networking",
                    "database"
                ]);

        var second =
            _engine.Project(
                topology,
                [
                    "database",
                    "networking"
                ]);

        Assert.Equal(
            first.Nodes.Select(node =>
                node.Id),
            second.Nodes.Select(node =>
                node.Id));

        Assert.Equal(
            first.Edges.Select(edge =>
                (
                    edge.SourceId,
                    edge.TargetId,
                    edge.Relationship,
                    edge.Kind,
                    edge.Multiplicity
                )),
            second.Edges.Select(edge =>
                (
                    edge.SourceId,
                    edge.TargetId,
                    edge.Relationship,
                    edge.Kind,
                    edge.Multiplicity
                )));
    }

    [Fact]
    public void NullTopologyIsRejected()
    {
        Assert.Throws<ArgumentNullException>(
            () =>
                _engine.Project(
                    null!,
                    []));
    }

    private static TopologyView CreateTopology(
        string selectedResourceId = "")
    {
        return new TopologyView
        {
            SelectedResourceId =
                selectedResourceId,

            Nodes =
            [
                Node(
                    "vpc",
                    "networking"),

                Node(
                    "subnet-a",
                    "networking"),

                Node(
                    "subnet-b",
                    "networking"),

                Node(
                    "instance-a",
                    "compute"),

                Node(
                    "database-a",
                    "database")
            ],

            Edges =
            [
                Edge(
                    "subnet-a",
                    "vpc",
                    "member-of",
                    RelationshipKind.MemberOf),

                Edge(
                    "subnet-b",
                    "vpc",
                    "member-of",
                    RelationshipKind.MemberOf),

                Edge(
                    "instance-a",
                    "subnet-a",
                    "hosted-on",
                    RelationshipKind.HostedOn),

                Edge(
                    "instance-a",
                    "subnet-b",
                    "hosted-on",
                    RelationshipKind.HostedOn),

                Edge(
                    "vpc",
                    "database-a",
                    "connected-to",
                    RelationshipKind.ConnectedTo)
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
        string targetId,
        string relationship,
        RelationshipKind kind)
    {
        return new TopologyEdge
        {
            SourceId = sourceId,
            TargetId = targetId,
            Relationship = relationship,
            Kind = kind
        };
    }
}
