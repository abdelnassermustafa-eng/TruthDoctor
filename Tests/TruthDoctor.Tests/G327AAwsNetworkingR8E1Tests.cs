using System;
using System.Collections.Generic;
using TruthDoctor.Models.Platform;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327AAwsNetworkingR8E1Tests
{
    private static ProviderResourceRecord Resource(
        string id, string type,
        params (string Key, string Value)[] properties)
    {
        var values = new Dictionary<string, string>();
        foreach (var (key, value) in properties)
            values[key] = value;

        return new ProviderResourceRecord(
            "aws", id, type, id, Properties: values);
    }

    private static List<InfrastructureRelationship> Edges(
        params ProviderResourceRecord[] resources)
        => StandaloneDiscoveryProjector.Project(
            resources,
            "123456789012",
            "us-east-1",
            "infrastructure").Relationships;

    private static void Has(
        List<InfrastructureRelationship> edges,
        string source, string target, string type)
    {
        Assert.Contains(edges, edge =>
            edge.SourceResourceId == source
            && edge.TargetResourceId == target
            && edge.Type == type);
    }

    [Fact]
    public void SubnetUsesExplicitlyAssociatedRouteTable()
    {
        var edges = Edges(
            Resource("subnet-1", "subnet"),
            Resource("rtb-1", "route-table",
                ("Associations[0].SubnetId", "subnet-1")));

        Assert.Single(edges);
        Has(edges, "subnet-1", "rtb-1", "uses");
    }

    [Fact]
    public void SubnetIsSecuredByAssociatedNetworkAcl()
    {
        var edges = Edges(
            Resource("subnet-1", "subnet"),
            Resource("acl-1", "network-acl",
                ("Associations[0].SubnetId", "subnet-1")));

        Assert.Single(edges);
        Has(edges, "subnet-1", "acl-1", "secured-by");
    }

    [Fact]
    public void RouteTableUsesDiscoveredNatAndInternetGateways()
    {
        var edges = Edges(
            Resource("nat-1", "nat-gateway"),
            Resource("igw-1", "internet-gateway"),
            Resource("rtb-1", "route-table",
                ("Routes[0].NatGatewayId", "nat-1"),
                ("Routes[1].GatewayId", "igw-1"),
                ("Routes[2].GatewayId", "local")));

        Assert.Equal(2, edges.Count);
        Has(edges, "rtb-1", "nat-1", "uses");
        Has(edges, "rtb-1", "igw-1", "uses");
    }

    [Fact]
    public void MissingOrUnrelatedPropertyNamesDoNotCreateEdges()
    {
        var edges = Edges(
            Resource("subnet-1", "subnet"),
            Resource("nat-1", "nat-gateway"),
            Resource("rtb-1", "route-table",
                ("Associations[0].SubnetId", "subnet-absent"),
                ("Routes[0].NatGatewayId", "nat-absent"),
                ("NotRoutes[0].NatGatewayId", "nat-1")),
            Resource("acl-1", "network-acl",
                ("Associations[0].SubnetId", "subnet-absent")));

        Assert.Empty(edges);
    }

    [Fact]
    public void RepeatedReferencesDoNotCreateDuplicateEdges()
    {
        var edges = Edges(
            Resource("subnet-1", "subnet"),
            Resource("nat-1", "nat-gateway"),
            Resource("rtb-1", "route-table",
                ("Associations[0].SubnetId", "subnet-1"),
                ("Associations[1].SubnetId", "subnet-1"),
                ("Routes[0].NatGatewayId", "nat-1"),
                ("Routes[1].NatGatewayId", "nat-1")));

        Assert.Equal(2, edges.Count);
        Has(edges, "subnet-1", "rtb-1", "uses");
        Has(edges, "rtb-1", "nat-1", "uses");
    }
}
