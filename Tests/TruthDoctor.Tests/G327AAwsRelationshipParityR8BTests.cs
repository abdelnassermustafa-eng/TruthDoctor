using System;
using System.Collections.Generic;
using System.Linq;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327AAwsRelationshipParityR8BTests
{
    private static ProviderResourceRecord Resource(
        string id, string type,
        params (string Key, string Value)[] properties)
    {
        var values = new Dictionary<string, string>();

        foreach (var (key, value) in properties)
        {
            values[key] = value;
        }

        return new ProviderResourceRecord(
            "aws", id, type, id, Properties: values);
    }

    private static IReadOnlyList<TruthDoctor.Models.Platform.InfrastructureRelationship>
        Relationships(params ProviderResourceRecord[] records)
    {
        return StandaloneDiscoveryProjector.Project(
            records, "123456789012",
            "us-east-1", "infrastructure").Relationships;
    }

    [Fact]
    public void InstanceReferencesNetworkInterfaceAndSecurityGroup()
    {
        var edges = Relationships(
            Resource("vpc-001", "virtual-network"),
            Resource("subnet-001", "subnet"),
            Resource("eni-001", "network-interface"),
            Resource("sg-001", "security-group"),
            Resource("i-001", "compute-instance",
                ("NetworkInterfaces[0].NetworkInterfaceId", "eni-001"),
                ("SecurityGroups[0].GroupId", "sg-001")));

        Assert.Contains(edges, edge =>
            edge.SourceResourceId == "i-001"
            && edge.TargetResourceId == "eni-001"
            && edge.Type == "uses");

        Assert.Contains(edges, edge =>
            edge.SourceResourceId == "i-001"
            && edge.TargetResourceId == "sg-001"
            && edge.Type == "secured-by");
    }

    [Fact]
    public void VolumeAttachmentPointsToInstance()
    {
        var edges = Relationships(
            Resource("i-001", "compute-instance"),
            Resource("vol-001", "volume",
                ("Attachments[0].InstanceId", "i-001")));

        Assert.Contains(edges, edge =>
            edge.SourceResourceId == "vol-001"
            && edge.TargetResourceId == "i-001"
            && edge.Type == "attached-to");
    }

    [Fact]
    public void NetworkInterfaceReferencesSubnetAndSecurityGroups()
    {
        var edges = Relationships(
            Resource("subnet-001", "subnet"),
            Resource("sg-001", "security-group"),
            Resource("eni-001", "network-interface",
                ("SubnetId", "subnet-001"),
                ("Groups[0].GroupId", "sg-001")));

        Assert.Contains(edges, edge =>
            edge.SourceResourceId == "eni-001"
            && edge.TargetResourceId == "subnet-001"
            && edge.Type == "hosted-on");

        Assert.Contains(edges, edge =>
            edge.SourceResourceId == "eni-001"
            && edge.TargetResourceId == "sg-001"
            && edge.Type == "secured-by");
    }

    [Fact]
    public void OtherAwsFamiliesProduceVerifiedRelationships()
    {
        var edges = Relationships(
            Resource("vpc-001", "virtual-network"),
            Resource("subnet-001", "subnet"),
            Resource("sg-001", "security-group"),
            Resource("rtb-001", "route-table",
                ("VpcId", "vpc-001")),
            Resource("nat-001", "nat-gateway",
                ("SubnetId", "subnet-001")),
            Resource("igw-001", "internet-gateway",
                ("Attachments[0].VpcId", "vpc-001")),
            Resource("db-001", "database-instance",
                ("DBSubnetGroup.VpcId", "vpc-001"),
                ("VpcSecurityGroups[0].VpcSecurityGroupId", "sg-001")),
            Resource("function-001", "function",
                ("VpcConfig.SubnetIds[0]", "subnet-001"),
                ("VpcConfig.SecurityGroupIds[0]", "sg-001")));

        Assert.Contains(edges, edge =>
            edge.SourceResourceId == "rtb-001"
            && edge.TargetResourceId == "vpc-001");

        Assert.Contains(edges, edge =>
            edge.SourceResourceId == "nat-001"
            && edge.TargetResourceId == "subnet-001");

        Assert.Contains(edges, edge =>
            edge.SourceResourceId == "igw-001"
            && edge.TargetResourceId == "vpc-001");

        Assert.Contains(edges, edge =>
            edge.SourceResourceId == "db-001"
            && edge.TargetResourceId == "sg-001"
            && edge.Type == "secured-by");

        Assert.Contains(edges, edge =>
            edge.SourceResourceId == "function-001"
            && edge.TargetResourceId == "sg-001"
            && edge.Type == "secured-by");
    }

    [Fact]
    public void MissingReferencesDoNotCreatePhantomEdges()
    {
        var edges = Relationships(
            Resource("i-001", "compute-instance",
                ("NetworkInterfaces[0].NetworkInterfaceId", "eni-missing"),
                ("SecurityGroups[0].GroupId", "sg-missing")),
            Resource("vol-001", "volume",
                ("Attachments[0].InstanceId", "i-missing")));

        Assert.Empty(edges);
    }

    [Fact]
    public void RepeatedIndexedReferencesProduceOneEdge()
    {
        var edges = Relationships(
            Resource("sg-001", "security-group"),
            Resource("i-001", "compute-instance",
                ("SecurityGroups[0].GroupId", "sg-001"),
                ("SecurityGroups[1].GroupId", "sg-001")));

        Assert.Single(edges);
        Assert.Equal("secured-by", edges[0].Type);
    }
}
