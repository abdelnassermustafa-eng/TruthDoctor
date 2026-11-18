using System;
using System.Collections.Generic;
using System.Linq;
using TruthDoctor.Models.Platform;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327AAwsSecurityGroupR8E2Tests
{
    private static ProviderResourceRecord Resource(
        string id,
        string type,
        params (string Key, string Value)[] properties)
    {
        var values = new Dictionary<string, string>(
            StringComparer.Ordinal);

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

    private static List<InfrastructureRelationship> Edges(
        params ProviderResourceRecord[] resources)
        => StandaloneDiscoveryProjector.Project(
            resources,
            "123456789012",
            "us-east-1",
            "infrastructure").Relationships;

    [Fact]
    public void IngressReferenceResolvesDiscoveredSecurityGroup()
    {
        var edges = Edges(
            Resource(
                "sg-owner",
                "security-group",
                ("IpPermissions[0].UserIdGroupPairs[0].GroupId",
                 "sg-target")),
            Resource("sg-target", "security-group"));

        var edge = Assert.Single(edges);

        Assert.Equal("references", edge.Type);
        Assert.Equal(
            "IpPermissions[0].UserIdGroupPairs[0].GroupId",
            edge.Description);
    }

    [Fact]
    public void EgressReferenceResolvesDiscoveredSecurityGroup()
    {
        var edges = Edges(
            Resource(
                "sg-owner",
                "security-group",
                ("IpPermissionsEgress[0].UserIdGroupPairs[0].GroupId",
                 "sg-target")),
            Resource("sg-target", "security-group"));

        var edge = Assert.Single(edges);

        Assert.Equal("references", edge.Type);
        Assert.Equal(
            "IpPermissionsEgress[0].UserIdGroupPairs[0].GroupId",
            edge.Description);
    }

    [Fact]
    public void RepeatedIngressAndEgressReferencesDeduplicate()
    {
        var edges = Edges(
            Resource(
                "sg-owner",
                "security-group",
                ("IpPermissions[0].UserIdGroupPairs[0].GroupId",
                 "sg-target"),
                ("IpPermissions[1].UserIdGroupPairs[0].GroupId",
                 "sg-target"),
                ("IpPermissionsEgress[0].UserIdGroupPairs[0].GroupId",
                 "sg-target")),
            Resource("sg-target", "security-group"));

        var edge = Assert.Single(edges);

        Assert.Equal("references", edge.Type);
    }

    [Fact]
    public void MissingTargetAndUnrelatedFieldsDoNotResolve()
    {
        var edges = Edges(
            Resource(
                "sg-owner",
                "security-group",
                ("IpPermissions[0].UserIdGroupPairs[0].GroupId",
                 "sg-missing"),
                ("IpPermissions[0].IpRanges[0].CidrIp",
                 "10.0.0.0/24"),
                ("IpPermissions[0].UserIdGroupPairs[0].GroupName",
                 "example")));

        Assert.Empty(edges);
    }

    [Fact]
    public void SelfReferenceDoesNotCreateTopologyLoop()
    {
        var edges = Edges(
            Resource(
                "sg-owner",
                "security-group",
                ("IpPermissions[0].UserIdGroupPairs[0].GroupId",
                 "sg-owner")));

        Assert.Empty(edges);
    }
}
