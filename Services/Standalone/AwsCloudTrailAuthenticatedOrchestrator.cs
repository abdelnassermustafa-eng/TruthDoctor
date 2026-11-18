using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Models.Platform;

namespace TruthDoctor.Services.Standalone;

public enum AwsCloudTrailEnrichmentStatus
{
    EvidenceFound,
    NoMatchingEvidence,
    Unavailable
}

/// <summary>
/// AWS-only safe failure categories. Raw CLI diagnostics are
/// deliberately excluded from published results.
/// </summary>
public enum AwsCloudTrailEnrichmentFailureKind
{
    None,
    IdentityUnverified,
    CredentialRejected,
    PermissionDenied,
    ProviderUnavailable
}

public sealed record AwsCloudTrailEnrichmentResult(
    AwsCloudTrailEnrichmentStatus Status,
    ResourceLifecycleEventEvidence? Evidence,
    AwsCloudTrailEnrichmentFailureKind FailureKind =
        AwsCloudTrailEnrichmentFailureKind.None);

/// <summary>
/// Offline-testable orchestration over an injected AWS CLI runner.
///
/// STS identity must match the expected account before lookup.
/// The same injected runner is used for STS and CloudTrail.
/// Every CloudTrail page passes through a shared regional gate.
///
/// This class does not construct the AWS process or activate
/// desktop discovery. Production callers must ensure runner
/// ownership, credential stability and authorization.
/// </summary>
public sealed class AwsCloudTrailAuthenticatedOrchestrator
{
    private static readonly AwsCloudTrailRegionalRateGate
        ApplicationRateGate = new();

    private readonly Func<
        IReadOnlyList<string>,
        CancellationToken,
        Task<string>> _runner;

    private readonly AwsCloudTrailRegionalRateGate _gate;

    public AwsCloudTrailAuthenticatedOrchestrator(
        Func<IReadOnlyList<string>,
            CancellationToken, Task<string>> runner,
        AwsCloudTrailRegionalRateGate? gate = null)
    {
        _runner = runner ??
            throw new ArgumentNullException(nameof(runner));

        _gate = gate ?? ApplicationRateGate;
    }

    public async Task<AwsCloudTrailEnrichmentResult>
        FindCreationAsync(
            string profile,
            string expectedAccountId,
            string region,
            string resourceType,
            string resourceId,
            DateTimeOffset endTime,
            CancellationToken cancellationToken = default,
            AwsCloudTrailPrincipalIdentity? expectedPrincipal = null)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!SafeSelector(profile) ||
            !SafeSelector(region) ||
            !SafeSelector(resourceId) ||
            expectedAccountId is null ||
            expectedAccountId.Length != 12 ||
            !OnlyDigits(expectedAccountId))
        {
            throw new ArgumentException(
                "AWS creation lookup identity is invalid.");
        }

        if (resourceType is not
            ("virtual-network" or "subnet" or "route-table"))
        {
            throw new ArgumentException(
                "Unsupported AWS creation lookup resource type.",
                nameof(resourceType));
        }

        if (endTime == default)
        {
            throw new ArgumentException(
                "A lookup end time is required.",
                nameof(endTime));
        }

        try
        {
            // Authentication is checked through the same runner
            // used for the subsequent CloudTrail requests.
            IReadOnlyList<string> stsArguments =
            [
                "--profile", profile,
                "--region", region,
                "--no-cli-pager",
                "sts", "get-caller-identity",
                "--output", "json"
            ];

            string stsJson = await ExecuteBoundedAsync(
                stsArguments, cancellationToken)
                .ConfigureAwait(false);

            if (!MatchesIdentity(stsJson, expectedAccountId))
                return Unavailable(
                    AwsCloudTrailEnrichmentFailureKind
                        .IdentityUnverified);

            if (expectedPrincipal is not null &&
                (!AwsCloudTrailPrincipalIdentity.TryParse(
                    stsJson, out var currentPrincipal) ||
                 !expectedPrincipal.SamePrincipal(currentPrincipal)))
            {
                return Unavailable(
                    AwsCloudTrailEnrichmentFailureKind
                        .IdentityUnverified);
            }

            var lookup = new AwsCloudTrailCreationLookup(
                async (args, token) =>
                    await _gate.ExecuteAsync(
                        expectedAccountId,
                        region,
                        gatedToken => ExecuteBoundedAsync(
                            args, gatedToken),
                        token).ConfigureAwait(false));

            ResourceLifecycleEventEvidence? evidence =
                await lookup.FindCreationAsync(
                    profile,
                    expectedAccountId,
                    region,
                    resourceType,
                    resourceId,
                    endTime,
                    authenticatedProviderSource: true,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            // Recheck principal identity after the complete
            // CloudTrail lookup, including its pagination.
            //
            // This does not pin individual AWS CLI credentials.
            if (expectedPrincipal is not null)
            {
                string postLookupIdentity =
                    await ExecuteBoundedAsync(
                        stsArguments,
                        cancellationToken).ConfigureAwait(false);

                if (!AwsCloudTrailPrincipalIdentity.TryParse(
                        postLookupIdentity,
                        out var afterPrincipal) ||
                    !expectedPrincipal.SamePrincipal(afterPrincipal))
                {
                    return Unavailable(
                    AwsCloudTrailEnrichmentFailureKind
                        .IdentityUnverified);
                }
            }

            if (evidence is null)
            {
                return new AwsCloudTrailEnrichmentResult(
                    AwsCloudTrailEnrichmentStatus.NoMatchingEvidence,
                    null);
            }

            return new AwsCloudTrailEnrichmentResult(
                AwsCloudTrailEnrichmentStatus.EvidenceFound,
                evidence);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception error)
        {
            // Classify only recognized AWS CLI service codes.
            // Never return raw stderr, exception text or events.
            return Unavailable(ClassifyFailure(error));
        }
    }

    private async Task<string> ExecuteBoundedAsync(
        IReadOnlyList<string> args,
        CancellationToken cancellationToken)
    {
        using var timeout =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);

        timeout.CancelAfter(TimeSpan.FromSeconds(90));

        return await _runner(args, timeout.Token)
            .ConfigureAwait(false);
    }

    private static bool MatchesIdentity(
        string json,
        string expectedAccountId)
    {
        if (string.IsNullOrWhiteSpace(json) ||
            json.Length > 100_000)
            return false;

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("Account", out var account) ||
            account.ValueKind != JsonValueKind.String ||
            !root.TryGetProperty("Arn", out var arn) ||
            arn.ValueKind != JsonValueKind.String)
            return false;

        if (!StringComparer.Ordinal.Equals(
                account.GetString(), expectedAccountId))
            return false;

        string arnValue = arn.GetString() ?? "";
        string[] parts = arnValue.Split(':');

        // arn:partition:service:region:account:resource
        return parts.Length >= 6 &&
            parts[0] == "arn" &&
            !string.IsNullOrWhiteSpace(parts[1]) &&
            parts[2] is "sts" or "iam" &&
            parts[4] == expectedAccountId &&
            !string.IsNullOrWhiteSpace(parts[5]);
    }

    private static AwsCloudTrailEnrichmentResult Unavailable(
        AwsCloudTrailEnrichmentFailureKind failure =
            AwsCloudTrailEnrichmentFailureKind.ProviderUnavailable)
        => new(
            AwsCloudTrailEnrichmentStatus.Unavailable,
            null,
            failure);

    private static AwsCloudTrailEnrichmentFailureKind
        ClassifyFailure(Exception error)
    {
        // Prefer sanitized production transport failures.
        if (error is AwsCloudTrailCliFailureException typed)
            return typed.FailureKind;

        // Preserve the existing discovery access-denied contract.
        if (error is AwsDiscoveryAccessDeniedException)
            return AwsCloudTrailEnrichmentFailureKind
                .PermissionDenied;

        // AWS CLI typically formats service errors as:
        // An error occurred (ServiceCode) when calling the ...
        //
        // Parse only a bounded service code, never free-text
        // descriptions that might contain secrets or account data.
        string message = error.Message;

        if (message.Length > 200_000)
            return AwsCloudTrailEnrichmentFailureKind
                .ProviderUnavailable;

        const string prefix = "An error occurred (";
        const string suffix = ") when calling the ";

        int start = message.IndexOf(
            prefix, StringComparison.Ordinal);

        if (start < 0)
            return AwsCloudTrailEnrichmentFailureKind
                .ProviderUnavailable;

        start += prefix.Length;

        int end = message.IndexOf(
            suffix, start, StringComparison.Ordinal);

        if (end < 0 || end <= start || end - start > 64)
            return AwsCloudTrailEnrichmentFailureKind
                .ProviderUnavailable;

        string code = message[start..end];

        foreach (char character in code)
        {
            if (!char.IsAsciiLetterOrDigit(character))
                return AwsCloudTrailEnrichmentFailureKind
                    .ProviderUnavailable;
        }

        return code switch
        {
            "ExpiredToken" or
            "ExpiredTokenException" or
            "InvalidClientTokenId" or
            "UnrecognizedClientException" or
            "InvalidAccessKeyId" or
            "SignatureDoesNotMatch" or
            "IncompleteSignature" or
            "RequestExpired"
                => AwsCloudTrailEnrichmentFailureKind
                    .CredentialRejected,

            "AccessDenied" or
            "AccessDeniedException" or
            "UnauthorizedOperation"
                => AwsCloudTrailEnrichmentFailureKind
                    .PermissionDenied,

            _ => AwsCloudTrailEnrichmentFailureKind
                .ProviderUnavailable
        };
    }

    private static bool OnlyDigits(string value)
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
