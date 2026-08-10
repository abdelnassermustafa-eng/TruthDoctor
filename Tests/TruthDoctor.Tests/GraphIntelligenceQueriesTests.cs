using System;
using System.Collections.Generic;
using System.Linq;
using TruthDoctor.Graph;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class GraphIntelligenceQueriesTests
{
    private const string Application = "application";
    private const string Instance = "instance";
    private const string NetworkInterface = "network-interface";
    private const string Subnet = "subnet";
    private const string Vpc = "vpc";
    private const string SecurityGroup = "security-group";
    private const string TargetGroup = "target-group";
    private const string Isolated = "isolated";

    private readonly InfrastructureGraph _graph;
    private readonly GraphIntelligenceQueries _queries;

    public GraphIntelligenceQueriesTests()
    {
        _graph = BuildGraph();

        _queries =
            new GraphIntelligenceQueries(
                new InfrastructureGraphIndex(_graph));
    }

    [Fact]
    public void ForwardShortestPathFindsEveryHop()
    {
        var result =
            _queries.HowAreTheyConnected(
                Application,
                Vpc,
                includeReverseRelationships: false);

        Assert.True(result.Found);

        Assert.Equal(
            new[]
            {
                Application,
                Instance,
                NetworkInterface,
                Subnet,
                Vpc
            },
            result.Nodes
                .Select(node => node.Id)
                .ToArray());

        Assert.Equal(4, result.Edges.Count);
    }

    [Fact]
    public void ReverseShortestPathRequiresReverseTraversal()
    {
        var reverseEnabled =
            _queries.HowAreTheyConnected(
                Vpc,
                Application,
                includeReverseRelationships: true);

        var reverseDisabled =
            _queries.HowAreTheyConnected(
                Vpc,
                Application,
                includeReverseRelationships: false);

        Assert.True(reverseEnabled.Found);

        Assert.Equal(
            new[]
            {
                Vpc,
                Subnet,
                NetworkInterface,
                Instance,
                Application
            },
            reverseEnabled.Nodes
                .Select(node => node.Id)
                .ToArray());

        Assert.False(reverseDisabled.Found);
        Assert.Empty(reverseDisabled.Nodes);
        Assert.Empty(reverseDisabled.Edges);
    }

    [Fact]
    public void UnknownOrDisconnectedResourceReturnsNoPath()
    {
        var unknown =
            _queries.HowAreTheyConnected(
                Application,
                "missing");

        var disconnected =
            _queries.HowAreTheyConnected(
                Application,
                Isolated);

        Assert.False(unknown.Found);
        Assert.False(disconnected.Found);
    }

    [Fact]
    public void TransitiveDependenciesFollowOnlyDependencyEdges()
    {
        var dependencies =
            _queries.WhatDoesThisUltimatelyDependOn(
                Application);

        AssertIds(
            dependencies,
            Instance,
            NetworkInterface,
            Subnet,
            SecurityGroup);

        Assert.DoesNotContain(
            dependencies,
            node => node.Id == Vpc);
    }

    [Fact]
    public void TransitiveDependentsFollowOnlyDependencyEdges()
    {
        var dependents =
            _queries.WhatUltimatelyDependsOn(
                Subnet);

        AssertIds(
            dependents,
            NetworkInterface,
            Instance,
            Application,
            TargetGroup);

        Assert.DoesNotContain(
            dependents,
            node => node.Id == Vpc);
    }

    [Fact]
    public void SecurityRelationshipsResolveFromBothEndpoints()
    {
        var fromInterface =
            _queries.WhatSecures(NetworkInterface);

        var fromSecurityGroup =
            _queries.WhatSecures(SecurityGroup);

        AssertIds(
            fromInterface.Resources,
            SecurityGroup);

        AssertIds(
            fromSecurityGroup.Resources,
            NetworkInterface);
    }

    [Fact]
    public void TrafficConnectivityResolvesFromBothEndpoints()
    {
        var fromTargetGroup =
            _queries.WhatIsConnectedTo(TargetGroup);

        var fromInstance =
            _queries.WhatIsConnectedTo(Instance);

        AssertIds(
            fromTargetGroup.Resources,
            Instance);

        AssertIds(
            fromInstance.Resources,
            TargetGroup);
    }

    [Fact]
    public void ContainmentDirectionIsInterpretedCorrectly()
    {
        var vpcContains =
            _queries.WhatDoesThisContain(Vpc);

        var subnetContainedBy =
            _queries.WhatContainsThis(Subnet);

        AssertIds(
            vpcContains.Resources,
            Subnet);

        AssertIds(
            subnetContainedBy.Resources,
            Vpc);

        Assert.Empty(
            _queries
                .WhatDoesThisContain(Subnet)
                .Resources);

        Assert.Empty(
            _queries
                .WhatContainsThis(Vpc)
                .Resources);
    }

    [Fact]
    public void BlastRadiusExcludesContainmentEdges()
    {
        var subnetImpact =
            _queries.WhatBreaksIfChanged(Subnet);

        var vpcImpact =
            _queries.WhatBreaksIfChanged(Vpc);

        Assert.Equal(
            1,
            subnetImpact.DirectDependentCount);

        Assert.Equal(
            4,
            subnetImpact.TotalAffectedCount);

        AssertIds(
            subnetImpact.AffectedResources,
            NetworkInterface,
            Instance,
            Application,
            TargetGroup);

        Assert.Equal(
            0,
            vpcImpact.DirectDependentCount);

        Assert.Equal(
            0,
            vpcImpact.TotalAffectedCount);
    }

    private static InfrastructureGraph BuildGraph()
    {
        var graph =
            new InfrastructureGraph();

        AddNode(
            graph,
            Application,
            "application",
            "compute");

        AddNode(
            graph,
            Instance,
            "instance",
            "compute");

        AddNode(
            graph,
            NetworkInterface,
            "network-interface",
            "networking");

        AddNode(
            graph,
            Subnet,
            "subnet",
            "networking");

        AddNode(
            graph,
            Vpc,
            "vpc",
            "networking");

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

        AddNode(
            graph,
            Isolated,
            "isolated",
            "test");

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

    private static void AssertIds(
        IEnumerable<GraphNode> nodes,
        params string[] expected)
    {
        var expectedIds =
            expected
                .OrderBy(
                    id => id,
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

        var actualIds =
            nodes
                .Select(node => node.Id)
                .OrderBy(
                    id => id,
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

        Assert.Equal(
            expectedIds,
            actualIds);
    }
}
