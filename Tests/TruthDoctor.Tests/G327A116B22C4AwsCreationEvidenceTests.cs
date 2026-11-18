using System;
using System.Collections.Generic;
using System.Text.Json;
using TruthDoctor.Models.Platform;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A116B22C4AwsCreationEvidenceTests
{
    private static JsonDocument Event(
        string operation,
        string parent,
        string idField,
        string id,
        string account = "111122223333",
        string region = "us-east-1",
        string time = "2026-11-01T10:20:30Z",
        string? error = null)
    {
        var fields = new Dictionary<string, object?>
        {
            ["eventSource"] = "ec2.amazonaws.com",
            ["eventName"] = operation,
            ["eventTime"] = time,
            ["eventID"] = "event-001",
            ["awsRegion"] = region,
            ["recipientAccountId"] = account,
            ["responseElements"] = new Dictionary<string, object>
            {
                [parent] = new Dictionary<string, string>
                {
                    [idField] = id
                }
            }
        };

        if (error is not null)
            fields["errorCode"] = error;

        return JsonDocument.Parse(
            JsonSerializer.Serialize(fields));
    }

    private static ResourceLifecycleEventEvidence? Extract(
        JsonElement root,
        string type,
        string id,
        bool trusted = true,
        string account = "111122223333",
        string region = "us-east-1")
        => AwsCloudTrailCreationEvidenceExtractor.TryExtract(
            root, type, id, account, region, trusted);

    [Fact]
    public void VpcCreationEventProducesVerifiedCreation()
    {
        using var json = Event(
            "CreateVpc", "vpc", "vpcId", "vpc-001");

        var result = Extract(
            json.RootElement, "virtual-network", "vpc-001");

        Assert.NotNull(result);
        Assert.Equal(
            ResourceLifecycleEventKind.Created,
            result.Kind);
        Assert.Equal(
            ResourceLifecycleEvidenceQuality.ProviderReported,
            result.Quality);
        Assert.Equal("aws.cloudtrail", result.Source);
        Assert.Equal("event-001", result.Reference);
        Assert.Equal(
            new DateTimeOffset(
                2026, 11, 1, 10, 20, 30, TimeSpan.Zero),
            result.OccurredAt);
    }

    [Fact]
    public void SubnetCreationEventMatchesExactSubnet()
    {
        using var json = Event(
            "CreateSubnet",
            "subnet",
            "subnetId",
            "subnet-001");

        Assert.NotNull(Extract(
            json.RootElement, "subnet", "subnet-001"));
        Assert.Null(Extract(
            json.RootElement, "subnet", "subnet-002"));
    }

    [Fact]
    public void RouteTableCreationEventMatchesExactTable()
    {
        using var json = Event(
            "CreateRouteTable",
            "routeTable",
            "routeTableId",
            "rtb-001");

        Assert.NotNull(Extract(
            json.RootElement, "route-table", "rtb-001"));
        Assert.Null(Extract(
            json.RootElement, "virtual-network", "rtb-001"));
    }

    [Fact]
    public void WrongAccountOrRegionCannotProduceCreation()
    {
        using var json = Event(
            "CreateVpc", "vpc", "vpcId", "vpc-001");

        Assert.Null(Extract(
            json.RootElement,
            "virtual-network",
            "vpc-001",
            account: "999999999999"));

        Assert.Null(Extract(
            json.RootElement,
            "virtual-network",
            "vpc-001",
            region: "us-west-2"));
    }

    [Fact]
    public void FailedAndUntrustedEventsAreRejected()
    {
        using var failure = Event(
            "CreateSubnet",
            "subnet",
            "subnetId",
            "subnet-001",
            error: "UnauthorizedOperation");

        Assert.Null(Extract(
            failure.RootElement, "subnet", "subnet-001"));

        using var success = Event(
            "CreateSubnet",
            "subnet",
            "subnetId",
            "subnet-001");

        Assert.Null(Extract(
            success.RootElement,
            "subnet",
            "subnet-001",
            trusted: false));
    }

    [Fact]
    public void MalformedTimeAndUnsupportedTypeRemainUnknown()
    {
        using var invalid = Event(
            "CreateVpc",
            "vpc",
            "vpcId",
            "vpc-001",
            time: "not-a-date");

        Assert.Null(Extract(
            invalid.RootElement,
            "virtual-network",
            "vpc-001"));

        using var valid = Event(
            "CreateVpc", "vpc", "vpcId", "vpc-001");

        Assert.Null(Extract(
            valid.RootElement,
            "compute-instance",
            "vpc-001"));
    }
}
