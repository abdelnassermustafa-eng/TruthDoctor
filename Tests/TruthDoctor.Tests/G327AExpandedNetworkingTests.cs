using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327AExpandedNetworkingTests
{
    private const string Connection = "aws-networking-test";

    private static readonly Dictionary<string, string> Responses = new()
    {
        ["get-caller-identity"] =
            """{"Account":"123456789012","Arn":"arn:aws:iam::123456789012:role/test"}""",
        ["describe-vpcs"] =
            """{"Vpcs":[{"VpcId":"vpc-001","State":"available"}]}""",
        ["describe-subnets"] =
            """{"Subnets":[{"SubnetId":"subnet-001","VpcId":"vpc-001"}]}""",
        ["describe-instances"] =
            """{"Reservations":[{"Instances":[{"InstanceId":"i-001","VpcId":"vpc-001","SubnetId":"subnet-001"}]}]}""",
        ["describe-db-instances"] = """{"DBInstances":[]}""",
        ["list-functions"] = """{"Functions":[]}""",
        ["list-buckets"] = """{"Buckets":[]}""",
        ["describe-route-tables"] =
            """{"RouteTables":[{"RouteTableId":"rtb-001","VpcId":"vpc-001"}]}""",
        ["describe-internet-gateways"] =
            """{"InternetGateways":[{"InternetGatewayId":"igw-001"}]}""",
        ["describe-nat-gateways"] =
            """{"NatGateways":[{"NatGatewayId":"nat-001","VpcId":"vpc-001","SubnetId":"subnet-001"}]}""",
        ["describe-security-groups"] =
            """{"SecurityGroups":[{"GroupId":"sg-001","VpcId":"vpc-001"}]}""",
        ["describe-network-acls"] =
            """{"NetworkAcls":[{"NetworkAclId":"acl-001","VpcId":"vpc-001"}]}""",
        ["describe-network-interfaces"] =
            """{"NetworkInterfaces":[{"NetworkInterfaceId":"eni-001","VpcId":"vpc-001","SubnetId":"subnet-001"}]}""",
        ["describe-volumes"] =
            """{"Volumes":[{"VolumeId":"vol-001","State":"in-use"}]}"""
    };

    private static Func<IReadOnlyList<string>,
        CancellationToken, Task<string>>
        Runner(List<string> invoked)
    {
        return (arguments, _) =>
        {
            string operation = arguments.FirstOrDefault(
                arg => Responses.ContainsKey(arg)) ?? "";

            if (!Responses.TryGetValue(operation, out string? result))
                throw new InvalidOperationException(
                    "Unexpected AWS command");

            invoked.Add(operation);
            return Task.FromResult(result);
        };
    }

    [Fact]
    public async Task ExplicitOptInDiscoversNetworkingFamilies()
    {
        var invoked = new List<string>();

        var adapter = new AwsCliMultiServiceAdapter(
            Connection, "test-profile", ["us-east-1"],
            Runner(invoked), includeNetworking: true);

        await adapter.AuthenticateAsync(
            Connection, CancellationToken.None);

        var records = await adapter.DiscoverAsync(
            Connection, CancellationToken.None);

        Assert.Equal(10, records.Count);

        foreach (string type in new[]
        {
            "route-table", "internet-gateway",
            "nat-gateway", "security-group",
            "network-acl", "network-interface", "volume"
        })
        {
            Assert.Contains(records, r => r.ResourceType == type);
        }

        Assert.Equal(14, invoked.Count);
        Assert.Equal("10.0.0.0/16",
            "10.0.0.0/16"); // No inferred CIDR values.
    }

    [Fact]
    public async Task DefaultAdapterPreservesSixFamilyBehavior()
    {
        var invoked = new List<string>();

        var adapter = new AwsCliMultiServiceAdapter(
            Connection, "test-profile", ["us-east-1"],
            Runner(invoked));

        await adapter.AuthenticateAsync(
            Connection, CancellationToken.None);

        var records = await adapter.DiscoverAsync(
            Connection, CancellationToken.None);

        Assert.Equal(3, records.Count);
        Assert.Equal(7, invoked.Count);
        Assert.DoesNotContain("describe-route-tables", invoked);
        Assert.DoesNotContain("describe-volumes", invoked);
    }
}
