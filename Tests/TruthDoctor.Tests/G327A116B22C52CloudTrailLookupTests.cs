using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Models.Platform;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A116B22C52CloudTrailLookupTests
{
    private static readonly DateTimeOffset End =
        new(2026, 11, 16, 12, 0, 0, TimeSpan.Zero);

    private static string Page(
        string operation,
        string parent,
        string identity,
        string resourceId,
        string? nextToken = null,
        string time = "2026-11-01T10:20:30Z",
        string eventId = "event-001")
    {
        string inner = JsonSerializer.Serialize(new
        {
            eventSource = "ec2.amazonaws.com",
            eventName = operation,
            eventTime = time,
            eventID = eventId,
            awsRegion = "us-east-1",
            recipientAccountId = "111122223333",
            responseElements =
                new Dictionary<string, object>
                {
                    [parent] = new Dictionary<string, string>
                    {
                        [identity] = resourceId
                    }
                }
        });

        var outer = new Dictionary<string, object?>
        {
            ["Events"] = new[]
            {
                new
                {
                    EventId = eventId,
                    EventName = operation,
                    CloudTrailEvent = inner
                }
            }
        };

        if (nextToken is not null)
            outer["NextToken"] = nextToken;

        return JsonSerializer.Serialize(outer);
    }

    private static Task<ResourceLifecycleEventEvidence?>
        Find(
            Func<IReadOnlyList<string>,
                CancellationToken, Task<string>> runner,
            string type,
            string id,
            bool authenticated = true,
            CancellationToken token = default)
        => new AwsCloudTrailCreationLookup(runner)
            .FindCreationAsync(
                "cli-user",
                "111122223333",
                "us-east-1",
                type,
                id,
                End,
                authenticated,
                token);

    [Fact]
    public async Task VpcCreationFromNestedEvent()
    {
        var evidence = await Find(
            (_, _) => Task.FromResult(
                Page(
                    "CreateVpc",
                    "vpc",
                    "vpcId",
                    "vpc-1")),
            "virtual-network",
            "vpc-1");

        Assert.NotNull(evidence);
        Assert.Equal(
            ResourceLifecycleEventKind.Created,
            evidence.Kind);
        Assert.Equal(
            "aws.cloudtrail",
            evidence.Source);
        Assert.Equal(
            "event-001",
            evidence.Reference);
    }

    [Fact]
    public async Task SubnetAndRouteTableUseCorrectOperations()
    {
        var subnet = await Find(
            (_, _) => Task.FromResult(
                Page(
                    "CreateSubnet",
                    "subnet",
                    "subnetId",
                    "subnet-1")),
            "subnet",
            "subnet-1");

        var table = await Find(
            (_, _) => Task.FromResult(
                Page(
                    "CreateRouteTable",
                    "routeTable",
                    "routeTableId",
                    "rtb-1")),
            "route-table",
            "rtb-1");

        Assert.NotNull(subnet);
        Assert.NotNull(table);
    }

    [Fact]
    public async Task ExactResourceIdIsRequired()
    {
        var evidence = await Find(
            (_, _) => Task.FromResult(
                Page(
                    "CreateVpc",
                    "vpc",
                    "vpcId",
                    "vpc-other")),
            "virtual-network",
            "vpc-1");

        Assert.Null(evidence);
    }

    [Fact]
    public async Task PaginationUsesSameScopeAndFindsMatch()
    {
        var calls = new List<IReadOnlyList<string>>();

        var evidence = await Find(
            (arguments, _) =>
            {
                calls.Add(arguments.ToArray());

                return Task.FromResult(
                    calls.Count == 1
                        ? Page(
                            "CreateVpc",
                            "vpc",
                            "vpcId",
                            "vpc-other",
                            "next-1")
                        : Page(
                            "CreateVpc",
                            "vpc",
                            "vpcId",
                            "vpc-1"));
            },
            "virtual-network",
            "vpc-1");

        Assert.NotNull(evidence);
        Assert.Equal(2, calls.Count);

        Assert.Contains(
            "--next-token",
            calls[1]);

        Assert.Contains(
            "next-1",
            calls[1]);

        Assert.Contains(
            "--no-paginate",
            calls[0]);

        Assert.Contains(
            "--no-paginate",
            calls[1]);

        Assert.Contains(
            "AttributeKey=EventName,AttributeValue=CreateVpc",
            calls[0]);
    }

    [Fact]
    public async Task IncompletePaginationFailsClosed()
    {
        int calls = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Find(
                (_, _) =>
                {
                    calls++;

                    return Task.FromResult(
                        Page(
                            "CreateVpc",
                            "vpc",
                            "vpcId",
                            "vpc-1",
                            "token-" + calls));
                },
                "virtual-network",
                "vpc-1"));

        Assert.Equal(3, calls);
    }

    [Fact]
    public async Task UnauthenticatedSourceDoesNotCallRunner()
    {
        int calls = 0;

        var evidence = await Find(
            (_, _) =>
            {
                calls++;
                return Task.FromResult("{}");
            },
            "virtual-network",
            "vpc-1",
            authenticated: false);

        Assert.Null(evidence);
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task CancellationAndMalformedPagesFailClosed()
    {
        using var canceled =
            new CancellationTokenSource();

        canceled.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Find(
                (_, _) => Task.FromResult("{}"),
                "virtual-network",
                "vpc-1",
                token: canceled.Token));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Find(
                (_, _) => Task.FromResult("{}"),
                "virtual-network",
                "vpc-1"));
    }
}
