using System;
using System.Collections.Generic;
using System.Linq;
using TruthDoctor.Graph;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class InfrastructureTopologyProjectorTests
{
    private const string Application = "application";
    private const string Instance = "instance";
    private const string NetworkInterface = "network-interface";
    private const string Subnet = "subnet";
    private const string Vpc = "vpc";
    private const string SecurityGroup = "security-group";
    private const string TargetGroup = "target-group";
    private const string Isolated = "isolated";

    private readonly InfrastructureTopologyProjector _projector;

    public InfrastructureTopologyProjectorTests()
    {
        var graph =
            BuildGraph();

        _projector =
            new InfrastructureTopologyProjector(
                new InfrastructureGraphIndex(graph));
    }

    [Fact]
    public void DepthZeroContainsOnlySelectedNode()
    {
        var view =
            _projector.ProjectNeighborhood(
                Instance,
                depth: 0);

        Assert.Equal(Instance, view.SelectedResourceId);

        var node =
            Assert.Single(view.Nodes);

        Assert.Equal(Instance, node.Id);
        Assert.True(node.IsSelected);
        Assert.Empty(view.Edges);
    }

    [Fact]
    public void DepthOneContainsImmediateIncomingAndOutgoingNeighbors()
    {
        var view =
            _projector.ProjectNeighborhood(
                Instance,
                depth: 1);

        AssertNodeIds(
            view,
            Instance,
            Application,
            NetworkInterface,
            TargetGroup);

        Assert.Equal(3, view.Edges.Count);

        AssertEdge(
            view,
            Application,
            Instance,
            "depends-on",
            RelationshipKind.DependsOn);

        AssertEdge(
            view,
            Instance,
            NetworkInterface,
            "attached-to",
            RelationshipKind.AttachedTo);

        AssertEdge(
            view,
            TargetGroup,
            Instance,
            "targets",
            RelationshipKind.Targets);
    }

    [Fact]
    public void DepthTwoExpandsOneAdditionalHopAndPreservesKinds()
    {
        var view =
            _projector.ProjectNeighborhood(
                Instance,
                depth: 2);

        AssertNodeIds(
            view,
            Instance,
            Application,
            NetworkInterface,
            TargetGroup,
            Subnet,
            SecurityGroup);

        Assert.DoesNotContain(
            view.Nodes,
            node => node.Id == Vpc);

        Assert.Equal(5, view.Edges.Count);

        AssertEdge(
            view,
            NetworkInterface,
            Subnet,
            "hosted-on",
            RelationshipKind.HostedOn);

        AssertEdge(
            view,
            NetworkInterface,
            SecurityGroup,
            "secured-by",
            RelationshipKind.SecuredBy);

        var selected =
            Assert.Single(
                view.Nodes,
                node => node.IsSelected);

        Assert.Equal(Instance, selected.Id);
    }

    [Fact]
    public void DepthThreeIncludesContainmentParent()
    {
        var view =
            _projector.ProjectNeighborhood(
                Instance,
                depth: 3);

        AssertNodeIds(
            view,
            Instance,
            Application,
            NetworkInterface,
            TargetGroup,
            Subnet,
            SecurityGroup,
            Vpc);

        Assert.Equal(6, view.Edges.Count);

        AssertEdge(
            view,
            Subnet,
            Vpc,
            "member-of",
            RelationshipKind.MemberOf);
    }

    [Fact]
    public void UnknownNodeReturnsEmptyViewAndNegativeDepthFails()
    {
        var unknown =
            _projector.ProjectNeighborhood(
                "missing",
                depth: 2);

        Assert.Equal(
            "missing",
            unknown.SelectedResourceId);

        Assert.Empty(unknown.Nodes);
        Assert.Empty(unknown.Edges);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _projector.ProjectNeighborhood(
                Isolated,
                depth: -1));
    }

    private static InfrastructureGraph BuildGraph()
    {
        var graph =
            new InfrastructureGraph();

        AddNode(graph, Application, "application", "compute");
        AddNode(graph, Instance, "instance", "compute");

        AddNode(
            graph,
            NetworkInterface,
            "network-interface",
            "networking");

        AddNode(graph, Subnet, "subnet", "networking");
        AddNode(graph, Vpc, "vpc", "networking");

        AddNode(
            graph,
            SecurityGroup,
            "security-group",
            "networking");

        AddNode(
            graph,
            TargetGroup,
            "target-group",
            "load-balancing");

        AddNode(graph, Isolated, "isolated", "test");

        AddEdge(
            graph,
            Application,
            Instance,
            "depends-on");

        AddEdge(
            graph,
            Instance,
            NetworkInterface,
            "attached-to");

        AddEdge(
            graph,
            NetworkInterface,
            Subnet,
            "hosted-on");

        AddEdge(
            graph,
            NetworkInterface,
            SecurityGroup,
            "secured-by");

        AddEdge(
            graph,
            Subnet,
            Vpc,
            "member-of");

        AddEdge(
            graph,
            TargetGroup,
            Instance,
            "targets");

        return graph;
    }

    private static void AddNode(
        InfrastructureGraph graph,
        string id,
        string resourceType,
        string domainId)
    {
        graph.AddNode(
            new GraphNode
            {
                Id = id,
                ProviderId = "test",
                DomainId = domainId,
                ResourceType = resourceType,
                DisplayName = id
            });
    }

    private static void AddEdge(
        InfrastructureGraph graph,
        string sourceId,
        string targetId,
        string relationship)
    {
        graph.AddEdge(
            new GraphEdge
            {
                SourceId = sourceId,
                TargetId = targetId,
                Relationship = relationship
            });
    }

    private static void AssertNodeIds(
        TopologyView view,
        params string[] expected)
    {
        var expectedIds =
            expected
                .OrderBy(
                    id => id,
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

        var actualIds =
            view.Nodes
                .Select(node => node.Id)
                .OrderBy(
                    id => id,
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

        Assert.Equal(expectedIds, actualIds);
    }

    private static void AssertEdge(
        TopologyView view,
        string sourceId,
        string targetId,
        string relationship,
        RelationshipKind kind)
    {
        var edge =
            Assert.Single(
                view.Edges,
                candidate =>
                    candidate.SourceId == sourceId &&
                    candidate.TargetId == targetId);

        Assert.Equal(relationship, edge.Relationship);
        Assert.Equal(kind, edge.Kind);
    }
}
