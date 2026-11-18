using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A116B22C556ConcurrencyTests
{
    private const string Account = "123456789012";
    private const string Connection = "c556-connection";

    private sealed class Fixture
    {
        public int StsCalls;
        public int TrailCalls;

        public readonly TaskCompletionSource<bool> LookupEntered =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public readonly TaskCompletionSource<bool> ReleaseLookup =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public readonly TaskCompletionSource<bool> StsEntered =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public readonly TaskCompletionSource<bool> ReleaseSts =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool BlockFirstLookup;
        public bool BlockSecondSts;

        public async Task<string> Run(
            IReadOnlyList<string> args,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            string command = string.Join(" ", args);

            if (command.Contains("get-caller-identity"))
            {
                int number = Interlocked.Increment(ref StsCalls);

                if (BlockSecondSts && number == 2)
                {
                    StsEntered.TrySetResult(true);
                    await ReleaseSts.Task.WaitAsync(token);
                }

                return JsonSerializer.Serialize(new
                {
                    Account,
                    Arn = "arn:aws:sts::" + Account +
                          ":assumed-role/Test/Session"
                });
            }

            if (command.Contains("lookup-events"))
            {
                int number = Interlocked.Increment(
                    ref TrailCalls);

                if (BlockFirstLookup && number == 1)
                {
                    LookupEntered.TrySetResult(true);
                    await ReleaseLookup.Task.WaitAsync(token);
                }

                return """{"Events":[]}""";
            }

            if (command.Contains("describe-vpcs"))
                return """{"Vpcs":[{"VpcId":"vpc-1"}]}""";

            if (command.Contains("describe-subnets"))
                return """{"Subnets":[]}""";

            if (command.Contains("describe-instances"))
                return """{"Reservations":[]}""";

            if (command.Contains("describe-db-instances"))
                return """{"DBInstances":[]}""";

            if (command.Contains("list-functions"))
                return """{"Functions":[]}""";

            if (command.Contains("list-buckets"))
                return """{"Buckets":[]}""";

            throw new InvalidOperationException(
                "Unexpected synthetic AWS operation.");
        }
    }

    private static async Task<AwsCliMultiServiceAdapter> Adapter(
        Fixture fixture,
        bool enabled = true)
    {
        var adapter = new AwsCliMultiServiceAdapter(
            Connection,
            "test-profile",
            ["us-east-1"],
            fixture.Run,
            enableCloudTrailCreationEvidence: enabled);

        await adapter.AuthenticateAsync(
            Connection, CancellationToken.None);

        return adapter;
    }

    private static Task<IReadOnlyList<ProviderResourceRecord>>
        Discover(
            AwsCliMultiServiceAdapter adapter,
            CancellationToken token = default)
        => adapter.DiscoverAsync(Connection, token);

    private static async Task Entered(Task signal)
        => await signal.WaitAsync(TimeSpan.FromSeconds(15));

    [Fact]
    public async Task NewDiscoverySupersedesBlockedOlderDiscovery()
    {
        var fixture = new Fixture { BlockFirstLookup = true };
        var adapter = await Adapter(fixture);
        var older = Discover(adapter);

        try
        {
            await Entered(fixture.LookupEntered.Task);

            var newer = Discover(adapter);
            fixture.ReleaseLookup.TrySetResult(true);

            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await older.WaitAsync(
                    TimeSpan.FromSeconds(15)));

            var records = await newer.WaitAsync(
                TimeSpan.FromSeconds(15));

            Assert.Single(records);

            var report = Assert.IsType<AwsCloudTrailCoverageReport>(
                adapter.LastCloudTrailCoverageReport);

            Assert.Equal(1, report.Summary.Scheduled);
            Assert.Equal(1, report.Summary.NoMatchingEvidence);
            Assert.Equal(0, report.Summary.Pending);
        }
        finally
        {
            fixture.ReleaseLookup.TrySetResult(true);
        }
    }

    [Fact]
    public async Task ReauthenticationInvalidatesBlockedDiscovery()
    {
        var fixture = new Fixture { BlockFirstLookup = true };
        var adapter = await Adapter(fixture);
        var discovery = Discover(adapter);

        try
        {
            await Entered(fixture.LookupEntered.Task);

            await adapter.AuthenticateAsync(
                Connection, CancellationToken.None);

            fixture.ReleaseLookup.TrySetResult(true);

            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await discovery.WaitAsync(
                    TimeSpan.FromSeconds(15)));

            Assert.Null(adapter.LastCloudTrailCoverageReport);
            Assert.Equal(Account, adapter.AuthenticatedAccountId);
        }
        finally
        {
            fixture.ReleaseLookup.TrySetResult(true);
        }
    }

    [Fact]
    public async Task DisconnectInvalidatesBlockedDiscovery()
    {
        var fixture = new Fixture { BlockFirstLookup = true };
        var adapter = await Adapter(fixture);
        var discovery = Discover(adapter);

        try
        {
            await Entered(fixture.LookupEntered.Task);

            await adapter.DisconnectAsync(
                Connection, CancellationToken.None);

            fixture.ReleaseLookup.TrySetResult(true);

            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await discovery.WaitAsync(
                    TimeSpan.FromSeconds(15)));

            Assert.Null(adapter.LastCloudTrailCoverageReport);
            Assert.Null(adapter.AuthenticatedAccountId);
        }
        finally
        {
            fixture.ReleaseLookup.TrySetResult(true);
        }
    }

    [Fact]
    public async Task StaleAuthenticationCannotRestoreDisconnectedIdentity()
    {
        var fixture = new Fixture { BlockSecondSts = true };
        var adapter = await Adapter(fixture);

        var authentication = adapter.AuthenticateAsync(
            Connection, CancellationToken.None);

        try
        {
            await Entered(fixture.StsEntered.Task);

            await adapter.DisconnectAsync(
                Connection, CancellationToken.None);

            fixture.ReleaseSts.TrySetResult(true);

            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await authentication.WaitAsync(
                    TimeSpan.FromSeconds(15)));

            Assert.Null(adapter.AuthenticatedAccountId);
            Assert.Null(adapter.LastCloudTrailCoverageReport);
        }
        finally
        {
            fixture.ReleaseSts.TrySetResult(true);
        }
    }

    [Fact]
    public async Task CancellationCannotPublishPartialReport()
    {
        var fixture = new Fixture { BlockFirstLookup = true };
        var adapter = await Adapter(fixture);

        using var cts = new CancellationTokenSource();
        var discovery = Discover(adapter, cts.Token);

        try
        {
            await Entered(fixture.LookupEntered.Task);
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                async () => await discovery.WaitAsync(
                    TimeSpan.FromSeconds(15)));

            Assert.Null(adapter.LastCloudTrailCoverageReport);
        }
        finally
        {
            fixture.ReleaseLookup.TrySetResult(true);
        }
    }

    [Fact]
    public async Task ReportCountsAgreeWithAssociatedEvidence()
    {
        var fixture = new Fixture();
        var adapter = await Adapter(fixture);

        var records = await Discover(adapter);

        var report = Assert.IsType<AwsCloudTrailCoverageReport>(
            adapter.LastCloudTrailCoverageReport);

        int verified = records.Count(resource =>
            resource.LifecycleEvidence?.VerifiedCreation is not null);

        Assert.Equal(verified, report.Summary.EvidenceFound);

        Assert.Equal(
            report.Summary.Scheduled,
            report.Summary.EvidenceFound +
            report.Summary.NoMatchingEvidence +
            report.Summary.Unavailable);

        Assert.Equal(0, report.Summary.Pending);
        Assert.Single(report.Entries);
    }

    [Fact]
    public async Task DisabledAdapterDoesNotPublishCoverageReport()
    {
        var fixture = new Fixture();
        var adapter = await Adapter(fixture, enabled: false);

        var records = await Discover(adapter);

        Assert.Single(records);
        Assert.Null(adapter.LastCloudTrailCoverageReport);
        Assert.Equal(0, Volatile.Read(ref fixture.TrailCalls));

        await adapter.DisconnectAsync(
            Connection, CancellationToken.None);

        Assert.Null(adapter.LastCloudTrailCoverageReport);
    }
}
