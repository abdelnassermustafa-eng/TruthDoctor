using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A116B22C553V2AwsEnrichmentTests
{
    private const string Connection = "c553-test-connection";
    private const string Account = "123456789012";

    private sealed class Fixture
    {
        public int StsCalls { get; set; }
        public int TrailCalls { get; set; }

        public bool DenyCloudTrail { get; set; }
        public bool DenyDiscovery { get; set; }
        public bool MismatchSecondSts { get; set; }
        public bool ReturnEvents { get; set; } = true;

        public CancellationTokenSource? CancelOnLookup { get; set; }

        public Task<string> Run(
            IReadOnlyList<string> args,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            string command = string.Join(" ", args);

            if (command.Contains("get-caller-identity"))
            {
                StsCalls++;

                string account =
                    MismatchSecondSts && StsCalls > 1
                        ? "999988887777"
                        : Account;

                return Task.FromResult(
                    JsonSerializer.Serialize(new
                    {
                        Account = account,
                        Arn = "arn:aws:sts::" + account +
                              ":assumed-role/Test/Session"
                    }));
            }

            if (command.Contains("lookup-events"))
            {
                TrailCalls++;

                if (CancelOnLookup is not null)
                {
                    CancelOnLookup.Cancel();
                    token.ThrowIfCancellationRequested();
                }

                if (DenyCloudTrail)
                    throw new AwsDiscoveryAccessDeniedException();

                string operation = args.Any(x =>
                    x.Contains("AttributeValue=CreateVpc"))
                        ? "CreateVpc"
                        : args.Any(x =>
                            x.Contains("AttributeValue=CreateSubnet"))
                            ? "CreateSubnet"
                            : "CreateRouteTable";

                string id = operation switch
                {
                    "CreateVpc" => "vpc-001",
                    "CreateSubnet" => "subnet-001",
                    _ => "rtb-001"
                };

                if (!ReturnEvents)
                    return Task.FromResult("""{"Events":[]}""");

                string parent = operation switch
                {
                    "CreateVpc" => "vpc",
                    "CreateSubnet" => "subnet",
                    _ => "routeTable"
                };

                string property = operation switch
                {
                    "CreateVpc" => "vpcId",
                    "CreateSubnet" => "subnetId",
                    _ => "routeTableId"
                };

                string inner = JsonSerializer.Serialize(new
                {
                    eventSource = "ec2.amazonaws.com",
                    eventName = operation,
                    eventTime = "2026-11-01T10:20:30Z",
                    eventID = "event-" + operation,
                    awsRegion = "us-east-1",
                    recipientAccountId = Account,
                    responseElements =
                        new Dictionary<string, object>
                        {
                            [parent] = new Dictionary<string, string>
                            {
                                [property] = id
                            }
                        }
                });

                return Task.FromResult(
                    JsonSerializer.Serialize(new
                    {
                        Events = new[]
                        {
                            new
                            {
                                EventId = "event-" + operation,
                                EventName = operation,
                                CloudTrailEvent = inner
                            }
                        }
                    }));
            }

            if (DenyDiscovery &&
                command.Contains("list-functions"))
                throw new AwsDiscoveryAccessDeniedException();

            if (command.Contains("describe-vpcs"))
                return Task.FromResult(
                    """{"Vpcs":[{"VpcId":"vpc-001"}]}""");

            if (command.Contains("describe-subnets"))
                return Task.FromResult(
                    """{"Subnets":[{"SubnetId":"subnet-001"}]}""");

            if (command.Contains("describe-route-tables"))
                return Task.FromResult(
                    """{"RouteTables":[{"RouteTableId":"rtb-001"}]}""");

            if (command.Contains("describe-instances"))
                return Task.FromResult(
                    """{"Reservations":[]}""");

            if (command.Contains("describe-db-instances"))
                return Task.FromResult(
                    """{"DBInstances":[]}""");

            if (command.Contains("list-functions"))
                return Task.FromResult(
                    """{"Functions":[]}""");

            if (command.Contains("list-buckets"))
                return Task.FromResult(
                    """{"Buckets":[]}""");

            if (command.Contains("describe-internet-gateways"))
                return Task.FromResult(
                    """{"InternetGateways":[]}""");

            if (command.Contains("describe-nat-gateways"))
                return Task.FromResult(
                    """{"NatGateways":[]}""");

            if (command.Contains("describe-security-groups"))
                return Task.FromResult(
                    """{"SecurityGroups":[]}""");

            if (command.Contains("describe-network-acls"))
                return Task.FromResult(
                    """{"NetworkAcls":[]}""");

            if (command.Contains("describe-network-interfaces"))
                return Task.FromResult(
                    """{"NetworkInterfaces":[]}""");

            if (command.Contains("describe-volumes"))
                return Task.FromResult(
                    """{"Volumes":[]}""");

            throw new InvalidOperationException(
                "Unexpected synthetic AWS command.");
        }
    }

    private static async Task<AwsCliMultiServiceAdapter> Adapter(
        Fixture fixture,
        bool enable = false,
        bool networking = false)
    {
        var adapter = new AwsCliMultiServiceAdapter(
            Connection,
            "test-profile",
            ["us-east-1"],
            fixture.Run,
            includeNetworking: networking,
            enableCloudTrailCreationEvidence: enable);

        await adapter.AuthenticateAsync(
            Connection, CancellationToken.None);

        return adapter;
    }

    [Fact]
    public async Task DefaultAdapterNeverLooksUpCloudTrail()
    {
        var fixture = new Fixture();
        var adapter = await Adapter(fixture);

        var records = await adapter.DiscoverAsync(
            Connection, CancellationToken.None);

        Assert.Equal(2, records.Count);
        Assert.Equal(0, fixture.TrailCalls);
        Assert.Equal(1, fixture.StsCalls);
        Assert.All(records, x => Assert.Null(x.LifecycleEvidence));
    }

    [Fact]
    public async Task OptInAttachesVerifiedVpcCreationEvidence()
    {
        var fixture = new Fixture();
        var adapter = await Adapter(fixture, enable: true);

        var records = await adapter.DiscoverAsync(
            Connection, CancellationToken.None);

        var vpc = Assert.Single(
            records, x => x.ResourceType == "virtual-network");

        Assert.NotNull(vpc.LifecycleEvidence?.VerifiedCreation);
        Assert.Equal(
            "aws.cloudtrail",
            vpc.LifecycleEvidence!.VerifiedCreation!.Source);
        Assert.Equal(2, fixture.TrailCalls);
    }

    [Fact]
    public async Task OptInCoversThreeNetworkingResourceTypes()
    {
        var fixture = new Fixture();
        var adapter = await Adapter(
            fixture, enable: true, networking: true);

        var records = await adapter.DiscoverAsync(
            Connection, CancellationToken.None);

        foreach (string type in new[]
        {
            "virtual-network", "subnet", "route-table"
        })
        {
            var record = Assert.Single(
                records, x => x.ResourceType == type);

            Assert.NotNull(
                record.LifecycleEvidence?.VerifiedCreation);
        }

        Assert.Equal(3, fixture.TrailCalls);
    }

    [Fact]
    public async Task CloudTrailAccessDeniedPreservesDiscovery()
    {
        var fixture = new Fixture { DenyCloudTrail = true };
        var adapter = await Adapter(fixture, enable: true);

        var records = await adapter.DiscoverAsync(
            Connection, CancellationToken.None);

        Assert.Equal(2, records.Count);
        Assert.All(records, x => Assert.Null(x.LifecycleEvidence));
        Assert.True(fixture.TrailCalls > 0);
    }

    [Fact]
    public async Task IdentityMismatchLeavesResourcesUnenriched()
    {
        var fixture = new Fixture { MismatchSecondSts = true };
        var adapter = await Adapter(fixture, enable: true);

        var records = await adapter.DiscoverAsync(
            Connection, CancellationToken.None);

        Assert.Equal(2, records.Count);
        Assert.All(records, x => Assert.Null(x.LifecycleEvidence));
        Assert.Equal(0, fixture.TrailCalls);
    }

    [Fact]
    public async Task IncompleteDiscoveryStillFailsClosed()
    {
        var fixture = new Fixture { DenyDiscovery = true };
        var adapter = await Adapter(fixture, enable: true);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => adapter.DiscoverAsync(
                Connection, CancellationToken.None));

        Assert.Equal(0, fixture.TrailCalls);
    }

    [Fact]
    public async Task ExternalCancellationPreventsPublication()
    {
        using var cancellation = new CancellationTokenSource();

        var fixture = new Fixture
        {
            CancelOnLookup = cancellation
        };

        var adapter = await Adapter(fixture, enable: true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => adapter.DiscoverAsync(
                Connection, cancellation.Token));

        Assert.True(fixture.TrailCalls > 0);
    }
}
