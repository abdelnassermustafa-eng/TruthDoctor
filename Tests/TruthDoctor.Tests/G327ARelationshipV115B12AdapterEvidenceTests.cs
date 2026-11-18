using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327ARelationshipV115B12AdapterEvidenceTests
{
    private static string Response(
        IReadOnlyList<string> arguments,
        string routeTables,
        string subnets)
    {
        if (arguments.Contains("get-caller-identity"))
            return """
                {
                  "Account":"123456789012",
                  "Arn":"arn:aws:iam::123456789012:user/offline"
                }
                """;

        if (arguments.Contains("describe-route-tables"))
            return routeTables;

        if (arguments.Contains("describe-subnets"))
            return subnets;

        if (arguments.Contains("describe-vpcs"))
            return """{"Vpcs":[]}""";

        if (arguments.Contains("describe-instances"))
            return """{"Reservations":[]}""";

        if (arguments.Contains("describe-db-instances"))
            return """{"DBInstances":[]}""";

        if (arguments.Contains("list-functions"))
            return """{"Functions":[]}""";

        if (arguments.Contains("list-buckets"))
            return """{"Buckets":[]}""";

        if (arguments.Contains("describe-internet-gateways"))
            return """{"InternetGateways":[]}""";

        if (arguments.Contains("describe-nat-gateways"))
            return """{"NatGateways":[]}""";

        if (arguments.Contains("describe-security-groups"))
            return """{"SecurityGroups":[]}""";

        if (arguments.Contains("describe-network-acls"))
            return """{"NetworkAcls":[]}""";

        if (arguments.Contains("describe-network-interfaces"))
            return """{"NetworkInterfaces":[]}""";

        if (arguments.Contains("describe-volumes"))
            return """{"Volumes":[]}""";

        throw new InvalidOperationException(
            "Unexpected offline provider command.");
    }

    private const string DefaultSubnets = """
        {
          "Subnets":[
            {"SubnetId":"subnet-one","VpcId":"vpc-one"},
            {"SubnetId":"subnet-two","VpcId":"vpc-one"}
          ]
        }
        """;

    private static async Task<IReadOnlyList<ProviderResourceRecord>>
        DiscoverAsync(
            string routeTables,
            string subnets = DefaultSubnets)
    {
        var adapter = new AwsCliMultiServiceAdapter(
            "connection-001",
            "offline-profile",
            new[] { "us-east-1" },
            (arguments, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                return Task.FromResult(Response(
                    arguments, routeTables, subnets));
            },
            includeNetworking: true);

        await adapter.AuthenticateAsync(
            "connection-001",
            CancellationToken.None);

        return await adapter.DiscoverAsync(
            "connection-001",
            CancellationToken.None);
    }

    private static IReadOnlyList<InfrastructureAssociationView>
        Resolve(IReadOnlyList<ProviderResourceRecord> records)
    {
        var platform =
            StandaloneDiscoveryProjector.ProjectWithAwsDomains(
                records,
                "123456789012",
                "us-east-1",
                "network",
                true);

        return AwsStandaloneAssociationEvidenceHandoff.Build(
            platform.Resources);
    }

    [Fact]
    public async Task TwoExplicitSubnetAssociationsResolveFromRealAdapter()
    {
        const string routes = """
            {
              "RouteTables":[
                {
                  "RouteTableId":"rtb-one",
                  "VpcId":"vpc-one",
                  "Associations":[
                    {"Main":true},
                    {"SubnetId":"subnet-one"},
                    {"SubnetId":"subnet-two"}
                  ]
                }
              ]
            }
            """;

        var records = await DiscoverAsync(routes);

        var table = Assert.Single(
            records,
            item => item.ResourceType == "route-table");

        Assert.Equal(
            "subnet-one",
            table.Properties!["Associations[1].SubnetId"]);

        Assert.Equal(
            "subnet-two",
            table.Properties["Associations[2].SubnetId"]);

        var views = Resolve(records);

        Assert.Equal(2, views.Count);

        Assert.Contains(views, view =>
            view.SourceResourceId == "subnet-one" &&
            view.TargetResourceId == "rtb-one" &&
            view.EvidenceKey == "Associations[1].SubnetId");

        Assert.Contains(views, view =>
            view.SourceResourceId == "subnet-two" &&
            view.TargetResourceId == "rtb-one" &&
            view.EvidenceKey == "Associations[2].SubnetId");
    }

    [Fact]
    public async Task MissingAssociationsProduceNoResolvedEvidence()
    {
        const string routes = """
            {
              "RouteTables":[
                {"RouteTableId":"rtb-one","VpcId":"vpc-one"}
              ]
            }
            """;

        var records = await DiscoverAsync(routes);

        Assert.Empty(Resolve(records));
    }

    [Fact]
    public async Task InvalidAndUnmatchedEvidenceIsRejectedByConsumer()
    {
        const string routes = """
            {
              "RouteTables":[
                {
                  "RouteTableId":"rtb-one",
                  "VpcId":"vpc-one",
                  "Associations":[
                    null,
                    {"SubnetId":123},
                    {"SubnetId":""},
                    {"GatewayId":"igw-one"},
                    {"SubnetId":"subnet-one"},
                    {"SubnetId":"subnet-not-discovered"}
                  ]
                }
              ]
            }
            """;

        var records = await DiscoverAsync(routes);

        var table = Assert.Single(
            records,
            item => item.ResourceType == "route-table");

        // The universal normalizer may retain additional nested
        // metadata. The association consumer must reject it unless
        // it resolves against a valid, matching discovered subnet.
        Assert.Contains(
            "Associations[4].SubnetId",
            table.Properties!.Keys);

        var view = Assert.Single(Resolve(records));

        Assert.Equal("subnet-one", view.SourceResourceId);
        Assert.Equal("rtb-one", view.TargetResourceId);
        Assert.Equal(
            "Associations[4].SubnetId",
            view.EvidenceKey);
    }

    [Fact]
    public async Task NestedSubnetMetadataCannotCreateRouteTableEvidence()
    {
        const string routes = """{"RouteTables":[]}""";

        const string subnets = """
            {
              "Subnets":[
                {
                  "SubnetId":"subnet-one",
                  "VpcId":"vpc-one",
                  "Associations":[
                    {"SubnetId":"subnet-two"}
                  ]
                }
              ]
            }
            """;

        var records = await DiscoverAsync(routes, subnets);

        var subnet = Assert.Single(
            records,
            item => item.ResourceType == "subnet");

        // Preserve universal normalization behavior.
        Assert.Equal(
            "subnet-two",
            subnet.Properties!["Associations[0].SubnetId"]);

        // Only route-table records can originate verified
        // subnet-to-route-table association evidence.
        Assert.Empty(Resolve(records));
    }
}
