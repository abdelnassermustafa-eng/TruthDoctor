using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A116B22C552AuthenticatedCloudTrailTests
{
    private static readonly DateTimeOffset End =
        new(2026, 11, 16, 12, 0, 0, TimeSpan.Zero);

    private const string Account = "111122223333";

    private sealed class FakeClock
    {
        public DateTimeOffset Now { get; private set; } = End;

        public List<TimeSpan> Waits { get; } = new();

        public Task Delay(
            TimeSpan span,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Waits.Add(span);
            Now += span;
            return Task.CompletedTask;
        }

        public AwsCloudTrailRegionalRateGate Gate()
            => new(() => Now, Delay);
    }

    private static string Identity(
        string account = Account,
        bool malformed = false)
        => malformed
            ? "{}"
            : JsonSerializer.Serialize(new
            {
                Account = account,
                Arn = "arn:aws:sts::" + account +
                    ":assumed-role/TestRole/TestSession"
            });

    private static string Page(
        string resourceId,
        string? nextToken = null)
    {
        string embedded = JsonSerializer.Serialize(new
        {
            eventSource = "ec2.amazonaws.com",
            eventName = "CreateVpc",
            eventTime = "2026-11-01T10:20:30Z",
            eventID = "cloudtrail-event-1",
            awsRegion = "us-east-1",
            recipientAccountId = Account,
            responseElements = new
            {
                vpc = new
                {
                    vpcId = resourceId
                }
            }
        });

        var envelope = new Dictionary<string, object?>
        {
            ["Events"] = new[]
            {
                new
                {
                    EventId = "cloudtrail-event-1",
                    EventName = "CreateVpc",
                    CloudTrailEvent = embedded
                }
            }
        };

        if (nextToken is not null)
            envelope["NextToken"] = nextToken;

        return JsonSerializer.Serialize(envelope);
    }

    private static Task<AwsCloudTrailEnrichmentResult> Find(
        Func<IReadOnlyList<string>,
            CancellationToken, Task<string>> runner,
        AwsCloudTrailRegionalRateGate gate,
        string expected = Account,
        CancellationToken cancellationToken = default)
        => new AwsCloudTrailAuthenticatedOrchestrator(
            runner, gate)
            .FindCreationAsync(
                "cli-user",
                expected,
                "us-east-1",
                "virtual-network",
                "vpc-1",
                End,
                cancellationToken);

    private static bool IsSts(IReadOnlyList<string> args)
        => args.Contains("get-caller-identity");

    [Fact]
    public async Task MatchingStsIdentityAllowsCreationEvidence()
    {
        int sts = 0;
        int trail = 0;
        var clock = new FakeClock();

        var result = await Find(
            (args, _) =>
            {
                if (IsSts(args))
                {
                    sts++;
                    return Task.FromResult(Identity());
                }

                trail++;
                return Task.FromResult(Page("vpc-1"));
            },
            clock.Gate());

        Assert.Equal(
            AwsCloudTrailEnrichmentStatus.EvidenceFound,
            result.Status);
        Assert.NotNull(result.Evidence);
        Assert.Equal("aws.cloudtrail", result.Evidence.Source);
        Assert.Equal(1, sts);
        Assert.Equal(1, trail);
    }

    [Fact]
    public async Task AllPaginatedRequestsUseRegionalRateGate()
    {
        int pages = 0;
        var clock = new FakeClock();
        var gate = clock.Gate();

        var result = await Find(
            (args, _) =>
            {
                if (IsSts(args))
                    return Task.FromResult(Identity());

                pages++;

                return Task.FromResult(
                    pages == 1
                        ? Page("vpc-other", "page-2")
                        : Page("vpc-1"));
            },
            gate);

        Assert.Equal(
            AwsCloudTrailEnrichmentStatus.EvidenceFound,
            result.Status);
        Assert.Equal(2, pages);
        Assert.Single(clock.Waits);
        Assert.Equal(
            TimeSpan.FromMilliseconds(500),
            clock.Waits[0]);
    }

    [Fact]
    public async Task WrongStsAccountNeverRunsCloudTrail()
    {
        int cloudTrailCalls = 0;
        var clock = new FakeClock();

        var result = await Find(
            (args, _) =>
            {
                if (IsSts(args))
                    return Task.FromResult(
                        Identity("999988887777"));

                cloudTrailCalls++;
                return Task.FromResult(Page("vpc-1"));
            },
            clock.Gate());

        Assert.Equal(
            AwsCloudTrailEnrichmentStatus.Unavailable,
            result.Status);
        Assert.Null(result.Evidence);
        Assert.Equal(0, cloudTrailCalls);
    }

    [Fact]
    public async Task MalformedStsResponseFailsClosed()
    {
        int cloudTrailCalls = 0;
        var clock = new FakeClock();

        var result = await Find(
            (args, _) =>
            {
                if (IsSts(args))
                    return Task.FromResult(Identity(malformed: true));

                cloudTrailCalls++;
                return Task.FromResult(Page("vpc-1"));
            },
            clock.Gate());

        Assert.Equal(
            AwsCloudTrailEnrichmentStatus.Unavailable,
            result.Status);
        Assert.Null(result.Evidence);
        Assert.Equal(0, cloudTrailCalls);
    }

    [Fact]
    public async Task MissingEventIsNotInventedAsCreation()
    {
        var clock = new FakeClock();

        var result = await Find(
            (args, _) => Task.FromResult(
                IsSts(args)
                    ? Identity()
                    : Page("vpc-other")),
            clock.Gate());

        Assert.Equal(
            AwsCloudTrailEnrichmentStatus.NoMatchingEvidence,
            result.Status);
        Assert.Null(result.Evidence);
    }

    [Fact]
    public async Task PermissionAndIncompletePagesAreUnavailable()
    {
        var clock = new FakeClock();

        var denied = await Find(
            (args, _) =>
            {
                if (IsSts(args))
                    return Task.FromResult(Identity());

                throw new AwsDiscoveryAccessDeniedException();
            },
            clock.Gate());

        Assert.Equal(
            AwsCloudTrailEnrichmentStatus.Unavailable,
            denied.Status);
        Assert.Null(denied.Evidence);

        int pages = 0;
        var incomplete = await Find(
            (args, _) =>
            {
                if (IsSts(args))
                    return Task.FromResult(Identity());

                pages++;
                return Task.FromResult(
                    Page("vpc-1", "next-" + pages));
            },
            clock.Gate());

        Assert.Equal(
            AwsCloudTrailEnrichmentStatus.Unavailable,
            incomplete.Status);
        Assert.Null(incomplete.Evidence);
        Assert.Equal(3, pages);
    }

    [Fact]
    public async Task CancellationStopsBeforeRunnerExecution()
    {
        var clock = new FakeClock();
        int calls = 0;

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Find(
                (_, _) =>
                {
                    calls++;
                    return Task.FromResult(Identity());
                },
                clock.Gate(),
                cancellationToken: cts.Token));

        Assert.Equal(0, calls);
    }
}
