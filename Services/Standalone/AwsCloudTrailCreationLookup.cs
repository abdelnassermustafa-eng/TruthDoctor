using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Models.Platform;

namespace TruthDoctor.Services.Standalone;

/// <summary>
/// Bounded offline-testable CloudTrail event lookup.
/// No AWS process is created by this class.
/// The caller owns identity verification, trusted runner
/// construction, authorization and API rate limiting.
/// </summary>
public sealed class AwsCloudTrailCreationLookup
{
    private const int MaxPages = 3;
    private const int MaxEventsPerPage = 50;
    private const int MaxJsonCharacters = 1_000_000;
    private const int MaxTokenCharacters = 4096;

    private readonly Func<
        IReadOnlyList<string>,
        CancellationToken,
        Task<string>> _runner;

    public AwsCloudTrailCreationLookup(
        Func<
            IReadOnlyList<string>,
            CancellationToken,
            Task<string>> runner)
    {
        _runner = runner ??
            throw new ArgumentNullException(nameof(runner));
    }

    public async Task<ResourceLifecycleEventEvidence?>
        FindCreationAsync(
            string profile,
            string accountId,
            string region,
            string resourceType,
            string resourceId,
            DateTimeOffset endTime,
            bool authenticatedProviderSource,
            CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!authenticatedProviderSource)
            return null;

        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);

        if (accountId.Length != 12 ||
            !IsDigits(accountId) ||
            !SafeSelector(profile) ||
            !SafeSelector(region) ||
            !SafeSelector(resourceId))
        {
            throw new ArgumentException(
                "Invalid AWS lookup selector.");
        }

        string? operation = resourceType switch
        {
            "virtual-network" => "CreateVpc",
            "subnet" => "CreateSubnet",
            "route-table" => "CreateRouteTable",
            _ => null
        };

        if (operation is null)
            return null;

        if (endTime == default)
        {
            throw new ArgumentException(
                "Lookup end time is required.",
                nameof(endTime));
        }

        string start = endTime
            .AddDays(-89)
            .ToUniversalTime()
            .ToString(
                "yyyy-MM-ddTHH:mm:ssZ",
                CultureInfo.InvariantCulture);

        string end = endTime
            .ToUniversalTime()
            .ToString(
                "yyyy-MM-ddTHH:mm:ssZ",
                CultureInfo.InvariantCulture);

        string? nextToken = null;

        var seenTokens = new HashSet<string>(
            StringComparer.Ordinal);

        ResourceLifecycleEventEvidence? earliest = null;

        for (int page = 0; page < MaxPages; page++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var arguments = new List<string>
            {
                "--profile", profile,
                "--region", region,
                "--no-cli-pager",
                "cloudtrail", "lookup-events",
                "--output", "json",
                "--no-paginate",
                "--lookup-attributes",
                "AttributeKey=EventName,AttributeValue=" +
                    operation,
                "--start-time", start,
                "--end-time", end
            };

            if (nextToken is not null)
            {
                arguments.Add("--next-token");
                arguments.Add(nextToken);
            }

            string output = await _runner(
                arguments,
                cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(output) ||
                output.Length > MaxJsonCharacters)
            {
                throw new InvalidOperationException(
                    "CloudTrail response size invalid.");
            }

            using var document = JsonDocument.Parse(output);

            JsonElement root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty(
                    "Events", out var events) ||
                events.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException(
                    "CloudTrail events collection invalid.");
            }

            if (events.GetArrayLength() > MaxEventsPerPage)
            {
                throw new InvalidOperationException(
                    "CloudTrail page exceeds event limit.");
            }

            foreach (JsonElement item in events.EnumerateArray())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (item.ValueKind != JsonValueKind.Object ||
                    !item.TryGetProperty(
                        "CloudTrailEvent", out var embedded) ||
                    embedded.ValueKind != JsonValueKind.String ||
                    !item.TryGetProperty(
                        "EventId", out var outerId) ||
                    outerId.ValueKind != JsonValueKind.String ||
                    !item.TryGetProperty(
                        "EventName", out var outerName) ||
                    outerName.ValueKind != JsonValueKind.String)
                {
                    throw new InvalidOperationException(
                        "CloudTrail event envelope invalid.");
                }

                string inner = embedded.GetString() ?? "";

                if (inner.Length == 0 ||
                    inner.Length > 250_000)
                {
                    throw new InvalidOperationException(
                        "Embedded CloudTrail event invalid.");
                }

                using var innerDocument =
                    JsonDocument.Parse(inner);

                JsonElement payload =
                    innerDocument.RootElement;

                if (payload.ValueKind != JsonValueKind.Object ||
                    outerName.GetString() != operation ||
                    !payload.TryGetProperty(
                        "eventID", out var innerId) ||
                    innerId.ValueKind != JsonValueKind.String ||
                    outerId.GetString() != innerId.GetString())
                {
                    throw new InvalidOperationException(
                        "CloudTrail event envelope mismatch.");
                }

                var evidence =
                    AwsCloudTrailCreationEvidenceExtractor
                        .TryExtract(
                            payload,
                            resourceType,
                            resourceId,
                            accountId,
                            region,
                            authenticatedProviderSource);

                if (evidence is not null &&
                    (earliest is null ||
                     evidence.OccurredAt < earliest.OccurredAt))
                {
                    earliest = evidence;
                }
            }

            nextToken = null;

            if (root.TryGetProperty(
                    "NextToken", out var tokenElement) &&
                tokenElement.ValueKind != JsonValueKind.Null)
            {
                if (tokenElement.ValueKind !=
                    JsonValueKind.String)
                {
                    throw new InvalidOperationException(
                        "CloudTrail pagination token invalid.");
                }

                nextToken = tokenElement.GetString();
            }

            if (string.IsNullOrEmpty(nextToken))
                return earliest;

            if (nextToken.Length > MaxTokenCharacters ||
                !seenTokens.Add(nextToken))
            {
                throw new InvalidOperationException(
                    "CloudTrail pagination token unsafe.");
            }
        }

        // Never return a partial lookup result.
        throw new InvalidOperationException(
            "CloudTrail lookup exceeded page budget.");
    }

    private static bool IsDigits(string value)
    {
        foreach (char c in value)
        {
            if (!char.IsAsciiDigit(c))
                return false;
        }

        return true;
    }

    private static bool SafeSelector(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.Length > 128)
            return false;

        foreach (char c in value)
        {
            if (!(char.IsAsciiLetterOrDigit(c) ||
                  c is '-' or '_' or '.' or ':' or '/'))
                return false;
        }

        return true;
    }
}
