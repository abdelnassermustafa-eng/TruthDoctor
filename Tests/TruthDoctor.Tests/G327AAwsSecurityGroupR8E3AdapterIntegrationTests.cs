using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327AAwsSecurityGroupR8E3AdapterIntegrationTests
{
    private const string Connection = "r8e3-synthetic-aws";

    private static AwsCliMultiServiceAdapter Create(
        string groupsJson,
        List<string> calls,
        bool includeNetworking = true,
        bool denyGroups = false)
    {
        var responses = new Dictionary<string, string>
        {
            ["get-caller-identity"] =
                """{"Account":"123456789012","Arn":"arn:aws:iam::123456789012:role/test"}""",
            ["describe-vpcs"] = """{"Vpcs":[]}""",
            ["describe-subnets"] = """{"Subnets":[]}""",
            ["describe-instances"] = """{"Reservations":[]}""",
            ["describe-db-instances"] = """{"DBInstances":[]}""",
            ["list-functions"] = """{"Functions":[]}""",
            ["list-buckets"] = """{"Buckets":[]}""",
            ["describe-route-tables"] = """{"RouteTables":[]}""",
            ["describe-internet-gateways"] = """{"InternetGateways":[]}""",
            ["describe-nat-gateways"] = """{"NatGateways":[]}""",
            ["describe-security-groups"] = groupsJson,
            ["describe-network-acls"] = """{"NetworkAcls":[]}""",
            ["describe-network-interfaces"] = """{"NetworkInterfaces":[]}""",
            ["describe-volumes"] = """{"Volumes":[]}"""
        };

        return new AwsCliMultiServiceAdapter(
            Connection,
            "test-profile",
            ["us-east-1"],
            (arguments, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                string command = arguments.FirstOrDefault(
                    responses.ContainsKey) ?? "";

                if (!responses.TryGetValue(
                    command, out string? response))
                {
                    throw new InvalidOperationException(
                        "Unexpected synthetic command");
                }

                calls.Add(command);

                if (denyGroups &&
                    command == "describe-security-groups")
                {
                    throw new AwsDiscoveryAccessDeniedException();
                }

                return Task.FromResult(response);
            },
            includeNetworking: includeNetworking);
    }

    private static async Task<
        IReadOnlyList<ProviderResourceRecord>> Discover(
        AwsCliMultiServiceAdapter adapter)
    {
        await adapter.AuthenticateAsync(
            Connection, CancellationToken.None);

        return await adapter.DiscoverAsync(
            Connection, CancellationToken.None);
    }

    [Fact]
    public async Task AdapterResolvesIngressAndEgressReferences()
    {
        const string json = """
        {
          "SecurityGroups": [
            {
              "GroupId": "sg-owner",
              "IpPermissions": [
                {
                  "UserIdGroupPairs": [
                    {"GroupId": "sg-ingress"}
                  ]
                }
              ],
              "IpPermissionsEgress": [
                {
                  "UserIdGroupPairs": [
                    {"GroupId": "sg-egress"}
                  ]
                }
              ]
            },
            {"GroupId": "sg-ingress"},
            {"GroupId": "sg-egress"}
          ]
        }
        """;

        var calls = new List<string>();
        var adapter = Create(json, calls);
        var records = await Discover(adapter);

        Assert.Equal(3, records.Count);
        Assert.Contains("describe-security-groups", calls);

        var owner = Assert.Single(records.Where(
            r => r.ResourceId == "sg-owner"));

        Assert.Equal(
            "sg-ingress",
            owner.Properties![
                "IpPermissions[0].UserIdGroupPairs[0].GroupId"]);

        var state = StandaloneDiscoveryProjector.Project(
            records, "123456789012", "us-east-1",
            "infrastructure");

        Assert.Equal(2, state.Relationships.Count);

        Assert.Contains(state.Relationships, e =>
            e.SourceResourceId == "sg-owner" &&
            e.TargetResourceId == "sg-ingress" &&
            e.Type == "references");

        Assert.Contains(state.Relationships, e =>
            e.SourceResourceId == "sg-owner" &&
            e.TargetResourceId == "sg-egress" &&
            e.Type == "references");
    }

    [Fact]
    public async Task AdapterDeduplicatesRepeatedGroupReferences()
    {
        const string json = """
        {
          "SecurityGroups": [
            {
              "GroupId":"sg-owner",
              "IpPermissions":[
                {
                  "UserIdGroupPairs":[
                    {"GroupId":"sg-target"},
                    {"GroupId":"sg-target"}
                  ]
                }
              ],
              "IpPermissionsEgress":[
                {
                  "UserIdGroupPairs":[
                    {"GroupId":"sg-target"}
                  ]
                }
              ]
            },
            {"GroupId":"sg-target"}
          ]
        }
        """;

        var adapter = Create(json, new List<string>());
        var records = await Discover(adapter);

        var state = StandaloneDiscoveryProjector.Project(
            records, "123456789012",
            "us-east-1", "infrastructure");

        var edge = Assert.Single(state.Relationships);

        Assert.Equal("sg-owner", edge.SourceResourceId);
        Assert.Equal("sg-target", edge.TargetResourceId);
        Assert.Equal("references", edge.Type);
    }

    [Fact]
    public async Task AdapterDoesNotInventMissingGroupTargets()
    {
        const string json = """
        {
          "SecurityGroups": [
            {
              "GroupId":"sg-owner",
              "IpPermissions":[
                {
                  "IpRanges":[
                    {"CidrIp":"10.0.0.0/24"}
                  ],
                  "UserIdGroupPairs":[
                    {"GroupId":"sg-not-discovered"}
                  ]
                }
              ]
            }
          ]
        }
        """;

        var adapter = Create(json, new List<string>());
        var records = await Discover(adapter);

        var state = StandaloneDiscoveryProjector.Project(
            records, "123456789012",
            "us-east-1", "infrastructure");

        Assert.Empty(state.Relationships);
    }

    [Fact]
    public async Task AdapterPreservesSecurityFiltering()
    {
        const string json = """
        {
          "SecurityGroups": [
            {
              "GroupId":"sg-owner",
              "ClientToken":"DO-NOT-EXPOSE",
              "IpPermissions":[
                {
                  "UserIdGroupPairs":[
                    {"GroupId":"sg-target"}
                  ]
                }
              ],
              "Tags":[
                {"Key":"AccessToken","Value":"SECRET"},
                {"Key":"Name","Value":"Owner Group"}
              ]
            },
            {"GroupId":"sg-target"}
          ]
        }
        """;

        var adapter = Create(json, new List<string>());
        var records = await Discover(adapter);

        var owner = Assert.Single(records.Where(
            r => r.ResourceId == "sg-owner"));

        Assert.False(owner.Properties!.ContainsKey("ClientToken"));
        Assert.False(owner.Tags!.ContainsKey("AccessToken"));
        Assert.Equal("Owner Group", owner.DisplayName);

        var state = StandaloneDiscoveryProjector.Project(
            records, "123456789012",
            "us-east-1", "infrastructure");

        var edge = Assert.Single(state.Relationships);

        Assert.Equal("references", edge.Type);
        Assert.Equal("sg-target", edge.TargetResourceId);
    }

    [Fact]
    public async Task AccessDeniedFailsClosedWithoutPublishingGraph()
    {
        const string json = """
        {
          "SecurityGroups":[
            {"GroupId":"sg-owner"}
          ]
        }
        """;

        var calls = new List<string>();
        var adapter = Create(
            json, calls,
            includeNetworking: true,
            denyGroups: true);

        await adapter.AuthenticateAsync(
            Connection, CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await adapter.DiscoverAsync(
                Connection, CancellationToken.None));

        Assert.Contains("describe-security-groups", calls);

        Assert.NotNull(adapter.LastTransition);

        Assert.Contains(
            adapter.LastTransition!.Snapshot.Coverage,
            scope =>
                scope.Status ==
                AwsDiscoveryScanStatus.AccessDenied);
    }
}
