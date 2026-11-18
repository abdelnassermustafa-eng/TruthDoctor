using System;
using System.Globalization;
using System.Text.Json;
using TruthDoctor.Models.Platform;

namespace TruthDoctor.Services.Standalone;

/// <summary>
/// Validates a single CloudTrail creation event for an exact
/// already-discovered AWS networking resource.
/// This service does not call AWS or certify the origin of JSON.
/// The caller must separately authenticate the source.
/// </summary>
public static class AwsCloudTrailCreationEvidenceExtractor
{
    public static ResourceLifecycleEventEvidence? TryExtract(
        JsonElement eventPayload,
        string resourceType,
        string resourceId,
        string accountId,
        string region,
        bool authenticatedProviderSource)
    {
        if (!authenticatedProviderSource)
            return null;

        if (string.IsNullOrWhiteSpace(resourceId) ||
            string.IsNullOrWhiteSpace(accountId) ||
            string.IsNullOrWhiteSpace(region))
            return null;

        string? operation;
        string? parent;
        string? identity;

        switch (resourceType)
        {
            case "virtual-network":
                operation = "CreateVpc";
                parent = "vpc";
                identity = "vpcId";
                break;
            case "subnet":
                operation = "CreateSubnet";
                parent = "subnet";
                identity = "subnetId";
                break;
            case "route-table":
                operation = "CreateRouteTable";
                parent = "routeTable";
                identity = "routeTableId";
                break;
            default:
                return null;
        }

        if (eventPayload.ValueKind != JsonValueKind.Object)
            return null;

        if (Read(eventPayload, "eventSource") !=
            "ec2.amazonaws.com")
            return null;

        if (Read(eventPayload, "eventName") != operation)
            return null;

        if (Read(eventPayload, "awsRegion") != region)
            return null;

        if (Read(eventPayload, "recipientAccountId") != accountId)
            return null;

        if (eventPayload.TryGetProperty("errorCode", out var error)
            && error.ValueKind != JsonValueKind.Null
            && error.ValueKind != JsonValueKind.Undefined)
            return null;

        if (eventPayload.TryGetProperty("errorMessage", out var message)
            && message.ValueKind != JsonValueKind.Null
            && message.ValueKind != JsonValueKind.Undefined)
            return null;

        string? eventId = Read(eventPayload, "eventID");
        string? eventTime = Read(eventPayload, "eventTime");

        if (string.IsNullOrWhiteSpace(eventId) ||
            string.IsNullOrWhiteSpace(eventTime))
            return null;

        if (!DateTimeOffset.TryParse(
                eventTime,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal |
                DateTimeStyles.AdjustToUniversal,
                out var occurredAt))
            return null;

        if (!eventPayload.TryGetProperty(
                "responseElements", out var response) ||
            response.ValueKind != JsonValueKind.Object)
            return null;

        if (!response.TryGetProperty(parent, out var resource) ||
            resource.ValueKind != JsonValueKind.Object)
            return null;

        if (!string.Equals(
                Read(resource, identity),
                resourceId,
                StringComparison.Ordinal))
            return null;

        return new ResourceLifecycleEventEvidence(
            ResourceLifecycleEventKind.Created,
            occurredAt,
            ResourceLifecycleEvidenceQuality.ProviderReported,
            "aws.cloudtrail",
            eventId);
    }

    private static string? Read(
        JsonElement element,
        string property)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(property, out var value) ||
            value.ValueKind != JsonValueKind.String)
            return null;

        return value.GetString();
    }
}
