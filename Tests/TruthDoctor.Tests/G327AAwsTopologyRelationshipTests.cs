using System.Collections.Generic;
using TruthDoctor.Graph;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327AAwsTopologyRelationshipTests
{
    private static ProviderResourceRecord Resource(
        string id, string type, string name,
        params (string Key, string Value)[] values)
    {
        var properties = new Dictionary<string, string>();

        foreach (var (key, value) in values)
            properties[key] = value;

        return new ProviderResourceRecord(
            "aws", id, type, name,
            Properties: properties);
    }

    [Fact]
    public void CreatesVpcSubnetInstanceEdges()
    {
        var platform = StandaloneDiscoveryProjector.Project(
        [
            Resource(
                "vpc-001", "virtual-network", "VPC"),
            Resource(
                "subnet-001", "subnet", "Subnet",
                ("VpcId", "vpc-001")),
            Resource(
                "i-001", "compute-instance", "Instance",
                ("VpcId", "vpc-001"),
                ("SubnetId", "subnet-001"))
        ],
        "123456789012", "us-east-1", "infrastructure");

        Assert.Equal(3, platform.Relationships.Count);

        Assert.Contains(platform.Relationships,
            edge => edge.SourceResourceId == "i-001"
                && edge.TargetResourceId == "subnet-001"
                && edge.Type == "hosted-on");

        Assert.Contains(platform.Relationships,
            edge => edge.SourceResourceId == "subnet-001"
                && edge.TargetResourceId == "vpc-001"
                && edge.Type == "member-of");
    }

    [Fact]
    public void DoesNotCreatePhantomEdges()
    {
        var platform = StandaloneDiscoveryProjector.Project(
        [
            Resource(
                "i-001", "compute-instance", "Instance",
                ("VpcId", "vpc-absent"),
                ("SubnetId", "subnet-absent"))
        ],
        "123456789012", "us-east-1", "infrastructure");

        Assert.Empty(platform.Relationships);
    }

    [Fact]
    public void GraphBuilderUsesProjectedRelationships()
    {
        var platform = StandaloneDiscoveryProjector.Project(
        [
            Resource(
                "vpc-001", "virtual-network", "VPC"),
            Resource(
                "subnet-001", "subnet", "Subnet",
                ("VpcId", "vpc-001"))
        ],
        "123456789012", "us-east-1", "infrastructure");

        var graph = new InfrastructureGraphBuilder()
            .Build(platform);

        Assert.Equal(2, graph.Nodes.Count);
        Assert.Single(graph.Edges);
    }
}
