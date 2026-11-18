using System;
using System.Collections.Generic;
using System.Linq;
using TruthDoctor.Graph;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327AAwsDomainClassificationTests
{
    private static ProviderResourceRecord Aws(
        string id, string type, string name,
        params (string Key, string Value)[] properties)
    {
        var values = new Dictionary<string, string>();

        foreach (var (key, value) in properties)
            values[key] = value;

        return new ProviderResourceRecord(
            "aws", id, type, name, Properties: values);
    }

    [Fact]
    public void AwsResourcesProduceMultipleDomains()
    {
        var platform = StandaloneDiscoveryProjector.ProjectWithAwsDomains(
        [
            Aws("i-1", "compute-instance", "EC2"),
            Aws("vpc-1", "virtual-network", "VPC"),
            Aws("subnet-1", "subnet", "Subnet",
                ("VpcId", "vpc-1")),
            Aws("vol-1", "volume", "Volume"),
            Aws("bucket-1", "storage-bucket", "Bucket"),
            Aws("db-1", "database-instance", "Database"),
            Aws("fn-1", "function", "Lambda")
        ],
        "123456789012", "us-east-1", "infrastructure",
        classifyAwsDomains: true);

        Assert.Equal(7, platform.Resources.Count);
        Assert.Equal(5, platform.Domains.Count);

        Assert.Equal(7,
            platform.Domains.Sum(domain => domain.ResourceCount));

        Assert.Equal(1,
            platform.Domains.Single(d => d.Id == "compute").ResourceCount);

        Assert.Equal(2,
            platform.Domains.Single(d => d.Id == "networking").ResourceCount);

        Assert.Equal(2,
            platform.Domains.Single(d => d.Id == "storage").ResourceCount);

        Assert.Equal("networking",
            platform.Resources.Single(r => r.ResourceId == "vpc-1")
                .DomainId);
    }

    [Fact]
    public void ExistingProviderProjectionRemainsCompatible()
    {
        var platform = StandaloneDiscoveryProjector.Project(
        [
            new ProviderResourceRecord(
                "mock", "r-1", "compute-instance", "Example")
        ],
        "account", "location", "infrastructure");

        Assert.Single(platform.Domains);
        Assert.Equal("infrastructure",
            Assert.Single(platform.Resources).DomainId);
    }

    [Fact]
    public void ExistingAwsProjectionRemainsOptInCompatible()
    {
        var platform = StandaloneDiscoveryProjector.Project(
        [
            Aws("vpc-1", "virtual-network", "VPC")
        ],
        "123456789012", "us-east-1", "infrastructure");

        Assert.Equal("infrastructure",
            Assert.Single(platform.Resources).DomainId);
        Assert.Single(platform.Domains);
    }

    [Fact]
    public void RelationshipsSurviveCrossDomainClassification()
    {
        var platform = StandaloneDiscoveryProjector.ProjectWithAwsDomains(
        [
            Aws("vpc-1", "virtual-network", "VPC"),
            Aws("subnet-1", "subnet", "Subnet",
                ("VpcId", "vpc-1")),
            Aws("i-1", "compute-instance", "EC2",
                ("VpcId", "vpc-1"),
                ("SubnetId", "subnet-1"))
        ],
        "123456789012", "us-east-1", "infrastructure",
        classifyAwsDomains: true);

        Assert.Equal(3, platform.Relationships.Count);

        var graph = new InfrastructureGraphBuilder().Build(platform);
        Assert.Equal(3, graph.Nodes.Count);
        Assert.Equal(3, graph.Edges.Count);
        Assert.Equal(2, platform.Domains.Count);
    }
}
