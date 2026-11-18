using System;
using System.Collections.Generic;
using System.Linq;
using TruthDoctor.Graph;
using TruthDoctor.Models.Platform;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327AAwsSecurityGroupR8E4TopologyTests
{
    private static ProviderResourceRecord Resource(
        string id,
        params (string Key, string Value)[] properties)
    {
        var values = new Dictionary<string, string>(
            StringComparer.Ordinal);

        foreach (var (key, value) in properties)
        {
            values[key] = value;
        }

        return new ProviderResourceRecord(
            "aws", id, "security-group", id,
            Properties: values);
    }

    private static InfrastructureGraph Graph(
        params ProviderResourceRecord[] records)
    {
        var platform = StandaloneDiscoveryProjector.Project(
            records,
            "123456789012",
            "us-east-1",
            "infrastructure");

        return new InfrastructureGraphBuilder().Build(platform);
    }

    private static InfrastructureTopologyProjector Projector(
        InfrastructureGraph graph)
    {
        return new InfrastructureTopologyProjector(
            new InfrastructureGraphIndex(graph));
    }

    [Fact]
    public void FullTopologyPreservesReferenceDirectionAndType()
    {
        var graph = Graph(
            Resource("sg-owner",
                ("IpPermissions[0].UserIdGroupPairs[0].GroupId",
                 "sg-ingress"),
                ("IpPermissionsEgress[0].UserIdGroupPairs[0].GroupId",
                 "sg-egress")),
            Resource("sg-ingress"),
            Resource("sg-egress"));

        var view = Projector(graph).ProjectFullInfrastructure();

        Assert.Equal(3, view.Nodes.Count);
        Assert.Equal(2, view.Edges.Count);

        Assert.Contains(view.Edges, e =>
            e.SourceId == "sg-owner"
            && e.TargetId == "sg-ingress"
            && e.Relationship == "references"
            && e.Kind == RelationshipKind.Uses);

        Assert.Contains(view.Edges, e =>
            e.SourceId == "sg-owner"
            && e.TargetId == "sg-egress"
            && e.Relationship == "references"
            && e.Kind == RelationshipKind.Uses);
    }

    [Fact]
    public void SelectedOwnerNeighborhoodIncludesReferencedGroups()
    {
        var graph = Graph(
            Resource("sg-owner",
                ("IpPermissions[0].UserIdGroupPairs[0].GroupId",
                 "sg-target")),
            Resource("sg-target"),
            Resource("sg-isolated"));

        var view = Projector(graph).ProjectNeighborhood(
            "sg-owner", depth: 1);

        Assert.Equal(2, view.Nodes.Count);

        Assert.Contains(view.Nodes,
            n => n.Id == "sg-owner" && n.IsSelected);

        Assert.Contains(view.Nodes,
            n => n.Id == "sg-target" && !n.IsSelected);

        Assert.DoesNotContain(view.Nodes,
            n => n.Id == "sg-isolated");

        var edge = Assert.Single(view.Edges);

        Assert.Equal("sg-owner", edge.SourceId);
        Assert.Equal("sg-target", edge.TargetId);
        Assert.Equal("references", edge.Relationship);
    }

    [Fact]
    public void ReferencedGroupNeighborhoodPreservesIncomingDirection()
    {
        var graph = Graph(
            Resource("sg-owner",
                ("IpPermissionsEgress[0].UserIdGroupPairs[0].GroupId",
                 "sg-target")),
            Resource("sg-target"));

        var view = Projector(graph).ProjectNeighborhood(
            "sg-target", depth: 1);

        Assert.Equal(2, view.Nodes.Count);

        Assert.Contains(view.Nodes,
            n => n.Id == "sg-target" && n.IsSelected);

        var edge = Assert.Single(view.Edges);

        Assert.Equal("sg-owner", edge.SourceId);
        Assert.Equal("sg-target", edge.TargetId);
        Assert.Equal(RelationshipKind.Uses, edge.Kind);
    }

    [Fact]
    public void MissingReferenceDoesNotCreatePhantomTopologyEdge()
    {
        var graph = Graph(
            Resource("sg-owner",
                ("IpPermissions[0].UserIdGroupPairs[0].GroupId",
                 "sg-missing")),
            Resource("sg-unrelated"));

        var view = Projector(graph).ProjectFullInfrastructure();

        Assert.Equal(2, view.Nodes.Count);
        Assert.Empty(view.Edges);
    }

    [Fact]
    public void ReferenceSemanticsDoNotAssertTrafficConnectivity()
    {
        var semantics = new RelationshipSemanticsRegistry();

        var reference = semantics.Resolve("references");

        Assert.Equal(RelationshipKind.Uses, reference.Kind);
        Assert.True(reference.IsDependency);
        Assert.False(reference.IsConnectivity);
        Assert.False(reference.IsTrafficFlow);
        Assert.False(reference.IsSecurity);
    }
}
