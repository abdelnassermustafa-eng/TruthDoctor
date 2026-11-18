using System;
using System.Collections.Generic;
using System.Linq;
using TruthDoctor.Models.Platform;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327AAwsCrossResourceR8DTests
{
    private static ProviderResourceRecord Record(
        string id, string type,
        params (string Key, string Value)[] properties)
    {
        var values = new Dictionary<string, string>();
        foreach (var (key, value) in properties)
            values[key] = value;

        return new ProviderResourceRecord(
            "aws", id, type, id, Properties: values);
    }

    private static List<InfrastructureRelationship> Build(
        params ProviderResourceRecord[] records)
    {
        return StandaloneDiscoveryProjector.Project(
            records, "123456789012",
            "us-east-1", "infrastructure").Relationships;
    }

    private static void Has(
        IEnumerable<InfrastructureRelationship> edges,
        string source, string target, string type)
    {
        Assert.Contains(edges, edge =>
            edge.SourceResourceId == source
            && edge.TargetResourceId == target
            && edge.Type == type);
    }

    [Fact]
    public void NetworkingFamiliesResolveVpcMembership()
    {
        var edges = Build(
            Record("vpc-1", "virtual-network"),
            Record("subnet-1", "subnet", ("VpcId", "vpc-1")),
            Record("rtb-1", "route-table", ("VpcId", "vpc-1")),
            Record("sg-1", "security-group", ("VpcId", "vpc-1")),
            Record("acl-1", "network-acl", ("VpcId", "vpc-1")));

        Assert.Equal(4, edges.Count);
        foreach (var id in new[] {
            "subnet-1", "rtb-1", "sg-1", "acl-1"
        })
            Has(edges, id, "vpc-1", "member-of");
    }

    [Fact]
    public void Ec2EniVolumeAndSecurityEdgesAreDirectional()
    {
        var edges = Build(
            Record("vpc-1", "virtual-network"),
            Record("subnet-1", "subnet"),
            Record("sg-1", "security-group"),
            Record("eni-1", "network-interface",
                ("VpcId", "vpc-1"),
                ("SubnetId", "subnet-1"),
                ("Groups[0].GroupId", "sg-1")),
            Record("i-1", "compute-instance",
                ("NetworkInterfaces[0].NetworkInterfaceId", "eni-1"),
                ("SecurityGroups[0].GroupId", "sg-1")),
            Record("vol-1", "volume",
                ("Attachments[0].InstanceId", "i-1")));

        Assert.Equal(6, edges.Count);
        Has(edges, "eni-1", "vpc-1", "member-of");
        Has(edges, "eni-1", "subnet-1", "hosted-on");
        Has(edges, "eni-1", "sg-1", "secured-by");
        Has(edges, "i-1", "eni-1", "uses");
        Has(edges, "i-1", "sg-1", "secured-by");
        Has(edges, "vol-1", "i-1", "attached-to");
    }

    [Fact]
    public void GatewaysResolveOnlyTheirDiscoveredTargets()
    {
        var edges = Build(
            Record("vpc-1", "virtual-network"),
            Record("subnet-1", "subnet"),
            Record("nat-1", "nat-gateway",
                ("VpcId", "vpc-1"),
                ("SubnetId", "subnet-1")),
            Record("igw-1", "internet-gateway",
                ("Attachments[0].VpcId", "vpc-1")));

        Assert.Equal(3, edges.Count);
        Has(edges, "nat-1", "vpc-1", "member-of");
        Has(edges, "nat-1", "subnet-1", "hosted-on");
        Has(edges, "igw-1", "vpc-1", "attached-to");
    }

    [Fact]
    public void RdsResolvesVpcSubnetAndSecurity()
    {
        var edges = Build(
            Record("vpc-1", "virtual-network"),
            Record("subnet-1", "subnet"),
            Record("sg-1", "security-group"),
            Record("db-1", "database-instance",
                ("DBSubnetGroup.VpcId", "vpc-1"),
                ("DBSubnetGroup.Subnets[0].SubnetIdentifier", "subnet-1"),
                ("VpcSecurityGroups[0].VpcSecurityGroupId", "sg-1")));

        Assert.Equal(3, edges.Count);
        Has(edges, "db-1", "vpc-1", "member-of");
        Has(edges, "db-1", "subnet-1", "uses");
        Has(edges, "db-1", "sg-1", "secured-by");
    }

    [Fact]
    public void LambdaResolvesVpcSubnetAndSecurity()
    {
        var edges = Build(
            Record("vpc-1", "virtual-network"),
            Record("subnet-1", "subnet"),
            Record("sg-1", "security-group"),
            Record("fn-1", "function",
                ("VpcConfig.VpcId", "vpc-1"),
                ("VpcConfig.SubnetIds[0]", "subnet-1"),
                ("VpcConfig.SecurityGroupIds[0]", "sg-1")));

        Assert.Equal(3, edges.Count);
        Has(edges, "fn-1", "vpc-1", "member-of");
        Has(edges, "fn-1", "subnet-1", "uses");
        Has(edges, "fn-1", "sg-1", "secured-by");
    }

    private static InfrastructureResource Direct(
        string id, string type, string account, string region,
        params (string Key, string Value)[] properties)
    {
        var values = new Dictionary<string, string>();
        foreach (var (key, value) in properties)
            values[key] = value;

        return new InfrastructureResource
        {
            ProviderId = "aws",
            AccountId = account,
            Location = region,
            ResourceId = id,
            NativeId = id,
            ResourceType = type,
            DisplayName = id,
            Properties = values
        };
    }

    [Fact]
    public void DifferentAccountOrRegionDoesNotCreateEdges()
    {
        var source = Direct(
            "i-1", "compute-instance", "account-a", "us-east-1",
            ("SubnetId", "subnet-1"));

        var edges = AwsStandaloneRelationshipBuilder.Build(
        [
            source,
            Direct("subnet-1", "subnet",
                "account-b", "us-east-1"),
            Direct("subnet-1", "subnet",
                "account-a", "us-west-2")
        ]);

        Assert.Empty(edges);
    }

    [Fact]
    public void AmbiguousNativeIdsDoNotProduceEdges()
    {
        var source = Direct(
            "i-1", "compute-instance", "account-a", "us-east-1",
            ("SubnetId", "subnet-1"));

        var first = new InfrastructureResource
        {
            ProviderId = "aws",
            AccountId = "account-a",
            Location = "us-east-1",
            ResourceId = "first-id",
            NativeId = "subnet-1",
            ResourceType = "subnet",
            DisplayName = "first"
        };

        var second = new InfrastructureResource
        {
            ProviderId = "aws",
            AccountId = "account-a",
            Location = "us-east-1",
            ResourceId = "second-id",
            NativeId = "subnet-1",
            ResourceType = "subnet",
            DisplayName = "second"
        };

        var edges = AwsStandaloneRelationshipBuilder.Build(
            [source, first, second]);

        Assert.Empty(edges);
    }

    [Fact]
    public void MissingReferencesNeverInventResources()
    {
        var edges = Build(
            Record("i-1", "compute-instance",
                ("SubnetId", "subnet-missing"),
                ("SecurityGroups[0].GroupId", "sg-missing")),
            Record("vol-1", "volume",
                ("Attachments[0].InstanceId", "i-missing")),
            Record("nat-1", "nat-gateway",
                ("SubnetId", "subnet-missing")));

        Assert.Empty(edges);
    }
}
