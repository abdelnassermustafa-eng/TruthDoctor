using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A116B22C555CoverageIntegrationTests
{
    private const string Connection = "c555-test";
    private const string Account = "123456789012";

    private sealed class Fixture
    {
        public int StsCalls;
        public int TrailCalls;

        public int VpcCount = 1;
        public bool DenyDiscovery;
        public bool DenyCloudTrail;
        public bool CancelLookup;

        public CancellationTokenSource? Cancellation;

        public Task<string> Run(
            IReadOnlyList<string> args,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            string command = string.Join(" ", args);

            if (command.Contains("get-caller-identity"))
            {
                StsCalls++;

                string account = Account;

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

                if (CancelLookup)
                {
                    Cancellation?.Cancel();
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

                // One verified creation, one absent event and
                // optionally a third resource with unavailable lookup.
                if (operation != "CreateVpc")
                    return Task.FromResult("""{"Events":[]}""");

                string inner = JsonSerializer.Serialize(new
                {
                    eventSource = "ec2.amazonaws.com",
                    eventName = "CreateVpc",
                    eventTime = "2026-11-01T10:20:30Z",
                    eventID = "c555-event-1",
                    awsRegion = "us-east-1",
                    recipientAccountId = Account,
                    responseElements = new
                    {
                        vpc = new { vpcId = "vpc-1" }
                    }
                });

                return Task.FromResult(
                    JsonSerializer.Serialize(new
                    {
                        Events = new[]
                        {
                            new
                            {
                                EventId = "c555-event-1",
                                EventName = "CreateVpc",
                                CloudTrailEvent = inner
                            }
                        }
                    }));
            }

            if (DenyDiscovery &&
                command.Contains("list-functions"))
                throw new AwsDiscoveryAccessDeniedException();

            if (command.Contains("describe-vpcs"))
            {
                var vpcs = Enumerable.Range(1, VpcCount)
                    .Select(i => new
                    {
                        VpcId = "vpc-" + i
                    }).ToArray();

                return Task.FromResult(
                    JsonSerializer.Serialize(new { Vpcs = vpcs }));
            }

            if (command.Contains("describe-subnets"))
                return Task.FromResult(
                    """{"Subnets":[{"SubnetId":"subnet-1"}]}""");

            if (command.Contains("describe-route-tables"))
                return Task.FromResult(
                    """{"RouteTables":[{"RouteTableId":"rtb-1"}]}""");

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

    private static async Task<AwsCliMultiServiceAdapter> Create(
        Fixture fixture,
        bool enabled = true)
    {
        var adapter = new AwsCliMultiServiceAdapter(
            Connection,
            "test-profile",
            ["us-east-1"],
            fixture.Run,
            includeNetworking: false,
            enableCloudTrailCreationEvidence: enabled);

        await adapter.AuthenticateAsync(
            Connection,
            CancellationToken.None);

        return adapter;
    }

    [Fact]
    public async Task DisabledDiscoveryHasNoCoverageReport()
    {
        var fixture = new Fixture();
        var adapter = await Create(fixture, enabled: false);

        var records = await adapter.DiscoverAsync(
            Connection, CancellationToken.None);

        Assert.Equal(2, records.Count);
        Assert.Null(adapter.LastCloudTrailCoverageReport);
        Assert.Equal(0, fixture.TrailCalls);
    }

    [Fact]
    public async Task CompletedReportDistinguishesFoundAndAbsent()
    {
        var fixture = new Fixture();
        var adapter = await Create(fixture);

        var records = await adapter.DiscoverAsync(
            Connection, CancellationToken.None);

        var report = Assert.IsType<AwsCloudTrailCoverageReport>(
            adapter.LastCloudTrailCoverageReport);

        Assert.Equal(2, report.Summary.Eligible);
        Assert.Equal(2, report.Summary.Scheduled);
        Assert.Equal(1, report.Summary.EvidenceFound);
        Assert.Equal(1, report.Summary.NoMatchingEvidence);
        Assert.Equal(0, report.Summary.Unavailable);
        Assert.Equal(0, report.Summary.Pending);
        Assert.Equal(0, report.Summary.NotExamined);

        Assert.Equal(2, report.Entries.Count);

        Assert.NotNull(
            Assert.Single(records,
                x => x.ResourceType == "virtual-network")
            .LifecycleEvidence?.VerifiedCreation);
    }

    [Fact]
    public async Task BudgetOverflowIsReportedNotExamined()
    {
        var fixture = new Fixture { VpcCount = 13 };
        var adapter = await Create(fixture);

        var records = await adapter.DiscoverAsync(
            Connection, CancellationToken.None);

        var report = Assert.IsType<AwsCloudTrailCoverageReport>(
            adapter.LastCloudTrailCoverageReport);

        // Thirteen VPCs and one subnet are eligible.
        Assert.Equal(14, report.Summary.Eligible);
        Assert.Equal(12, report.Summary.Scheduled);
        Assert.Equal(2, report.Summary.NotExamined);
        Assert.Equal(0, report.Summary.Pending);
        Assert.Equal(12, fixture.TrailCalls);
        Assert.Equal(14, records.Count);

        Assert.All(
            report.Entries.Skip(12),
            entry => Assert.Equal(
                AwsCloudTrailCoverageStatus.NotExamined,
                entry.Status));
    }

    [Fact]
    public async Task OptionalFailureReportsUnavailableWithoutLoss()
    {
        var fixture = new Fixture { DenyCloudTrail = true };
        var adapter = await Create(fixture);

        var records = await adapter.DiscoverAsync(
            Connection, CancellationToken.None);

        var report = Assert.IsType<AwsCloudTrailCoverageReport>(
            adapter.LastCloudTrailCoverageReport);

        Assert.Equal(2, records.Count);
        Assert.Equal(2, report.Summary.Unavailable);
        Assert.Equal(0, report.Summary.EvidenceFound);
        Assert.Equal(0, report.Summary.Pending);

        Assert.All(
            records,
            x => Assert.Null(x.LifecycleEvidence));
    }

    [Fact]
    public async Task IncompleteDiscoveryCannotPublishReport()
    {
        var fixture = new Fixture { DenyDiscovery = true };
        var adapter = await Create(fixture);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => adapter.DiscoverAsync(
                Connection, CancellationToken.None));

        Assert.Null(adapter.LastCloudTrailCoverageReport);
        Assert.Equal(0, fixture.TrailCalls);
    }

    [Fact]
    public async Task CancellationCannotPublishPartialReport()
    {
        using var cts = new CancellationTokenSource();

        var fixture = new Fixture
        {
            CancelLookup = true,
            Cancellation = cts
        };

        var adapter = await Create(fixture);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => adapter.DiscoverAsync(
                Connection, cts.Token));

        Assert.Null(adapter.LastCloudTrailCoverageReport);
        Assert.True(fixture.TrailCalls > 0);
    }

    [Fact]
    public async Task ReauthenticationClearsCompletedReport()
    {
        var fixture = new Fixture();
        var adapter = await Create(fixture);

        await adapter.DiscoverAsync(
            Connection, CancellationToken.None);

        Assert.NotNull(adapter.LastCloudTrailCoverageReport);

        await adapter.AuthenticateAsync(
            Connection, CancellationToken.None);

        Assert.Null(adapter.LastCloudTrailCoverageReport);
        Assert.Equal(Account, adapter.AuthenticatedAccountId);

        await adapter.DisconnectAsync(
            Connection, CancellationToken.None);

        Assert.Null(adapter.LastCloudTrailCoverageReport);
    }

    [Fact]
    public async Task DesktopSessionOptInControlsCloudTrailLookup()
    {
        var fixture = new Fixture();

        string? setting = Environment.GetEnvironmentVariable(
            "TRUTHDOCTOR_AWS_CLOUDTRAIL");

        bool enabled = string.Equals(
            setting, "1", StringComparison.Ordinal);

        var session = new StandaloneAwsDesktopSession(
            "synthetic-profile",
            new[] { "us-east-1" },
            fixture.Run);

        var workbench = new TruthDoctor.State.WorkbenchState();

        var platform = await session.DiscoverAsync(
            workbench,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(platform);
        Assert.True(fixture.StsCalls > 0);

        if (enabled)
        {
            Assert.True(
                fixture.TrailCalls > 0,
                "Opted-in discovery must invoke synthetic CloudTrail.");
        }
        else
        {
            Assert.Equal(0, fixture.TrailCalls);
        }
    }

}
