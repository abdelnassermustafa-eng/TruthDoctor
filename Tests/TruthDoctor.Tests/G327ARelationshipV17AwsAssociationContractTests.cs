using System.Collections.Generic;
using TruthDoctor.Graph;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327ARelationshipV17AwsAssociationContractTests
{
    private static ProviderResourceRecord Resource(
        string id,
        string type,
        params (string Key, string Value)[] properties)
    {
        var values = new Dictionary<string, string>();

        foreach (var (key, value) in properties)
        {
            values[key] = value;
        }

        return new ProviderResourceRecord(
            "aws",
            id,
            type,
            id,
            Properties: values);
    }

    [Fact]
    public void ExplicitRouteAssociationPreservesCanonicalDependency()
    {
        var platform = StandaloneDiscoveryProjector.Project(
        [
            Resource("subnet-1", "subnet"),
            Resource(
                "rtb-1",
                "route-table",
                ("Associations[0].SubnetId", "subnet-1"))
        ],
        "123456789012",
        "us-east-1",
        "infrastructure");

        var declared = Assert.Single(platform.Relationships);

        Assert.Equal("subnet-1", declared.SourceResourceId);
        Assert.Equal("rtb-1", declared.TargetResourceId);
        Assert.Equal("uses", declared.Type);

        var graph = new InfrastructureGraphBuilder()
            .Build(platform);

        Assert.Equal(2, graph.Nodes.Count);

        var graphEdge = Assert.Single(graph.Edges);

        Assert.Equal("subnet-1", graphEdge.SourceId);
        Assert.Equal("rtb-1", graphEdge.TargetId);
        Assert.Equal("uses", graphEdge.Relationship);

        var index = new InfrastructureGraphIndex(graph);

        var topology = new InfrastructureTopologyProjector(index)
            .ProjectFullInfrastructure();

        var topologyEdge = Assert.Single(topology.Edges);

        Assert.Equal("uses", topologyEdge.Relationship);
        Assert.Equal(RelationshipKind.Uses, topologyEdge.Kind);

        var analyzer = new InfrastructureRelationshipAnalyzer(index);

        Assert.Single(
            analyzer.GetDependencyRelationships("subnet-1"));

        Assert.DoesNotContain(
            graph.Edges,
            edge => edge.Relationship == "associated-with");

        var association = new RelationshipSemanticsRegistry()
            .Resolve("associated-with");

        Assert.Equal(
            RelationshipKind.AssociatedWith,
            association.Kind);

        Assert.False(association.IsDependency);
        Assert.False(association.IsConnectivity);
        Assert.False(association.IsTrafficFlow);
    }

    [Fact]
    public void RouteAndNetworkAclAssociationsPreservePathAndSecurity()
    {
        var platform = StandaloneDiscoveryProjector.Project(
        [
            Resource("subnet-1", "subnet"),
            Resource(
                "rtb-1",
                "route-table",
                ("Associations[0].SubnetId", "subnet-1")),
            Resource(
                "acl-1",
                "network-acl",
                ("Associations[0].SubnetId", "subnet-1"))
        ],
        "123456789012",
        "us-east-1",
        "infrastructure");

        Assert.Equal(2, platform.Relationships.Count);

        Assert.Contains(
            platform.Relationships,
            edge =>
                edge.SourceResourceId == "subnet-1"
                && edge.TargetResourceId == "rtb-1"
                && edge.Type == "uses");

        Assert.Contains(
            platform.Relationships,
            edge =>
                edge.SourceResourceId == "subnet-1"
                && edge.TargetResourceId == "acl-1"
                && edge.Type == "secured-by");

        var graph = new InfrastructureGraphBuilder()
            .Build(platform);

        Assert.Equal(3, graph.Nodes.Count);
        Assert.Equal(2, graph.Edges.Count);

        var index = new InfrastructureGraphIndex(graph);

        var analyzer = new InfrastructureRelationshipAnalyzer(index);

        Assert.Equal(
            2,
            analyzer.GetDependencyRelationships("subnet-1").Count);

        var security = Assert.Single(
            analyzer.GetSecurityRelationships("subnet-1"));

        Assert.Equal("acl-1", security.TargetId);
        Assert.Equal("secured-by", security.Relationship);

        Assert.DoesNotContain(
            graph.Edges,
            edge => edge.Relationship == "associated-with");

        Assert.DoesNotContain(
            graph.Edges,
            edge => edge.Relationship == "connected-to"
                    || edge.Relationship == "routes-through"
                    || edge.Relationship == "serves"
                    || edge.Relationship == "targets");

        var path = new InfrastructurePathAnalyzer(index)
            .FindShortestPath(
                "subnet-1",
                "rtb-1",
                includeReverseRelationships: false);

        Assert.True(path.Found);

        var pathEdge = Assert.Single(path.Edges);

        Assert.Equal("subnet-1", pathEdge.SourceId);
        Assert.Equal("rtb-1", pathEdge.TargetId);
        Assert.Equal("uses", pathEdge.Relationship);

        var topology = new InfrastructureTopologyProjector(index)
            .ProjectFullInfrastructure();

        Assert.Equal(3, topology.Nodes.Count);
        Assert.Equal(2, topology.Edges.Count);

        Assert.Contains(
            topology.Edges,
            edge => edge.Kind == RelationshipKind.Uses);

        Assert.Contains(
            topology.Edges,
            edge => edge.Kind == RelationshipKind.SecuredBy);
    }
}
