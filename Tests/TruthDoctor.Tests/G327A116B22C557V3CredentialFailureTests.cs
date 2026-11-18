using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A116B22C557V3CredentialFailureTests
{
    private const string Account = "123456789012";

    private static string Role(string session = "Initial")
        => $"arn:aws:sts::{Account}:assumed-role/Engineering/{session}";

    private static string Sts(string arn)
        => JsonSerializer.Serialize(
            new { Account, Arn = arn });

    private static AwsCloudTrailPrincipalIdentity Expected()
    {
        Assert.True(
            AwsCloudTrailPrincipalIdentity.TryParse(
                Sts(Role()), out var identity));

        return Assert.IsType<
            AwsCloudTrailPrincipalIdentity>(identity);
    }

    private static Exception AwsError(
        string code, string operation)
        => new InvalidOperationException(
            $"An error occurred ({code}) when calling the " +
            $"{operation} operation: synthetic test failure");

    private static async Task<(
        AwsCloudTrailEnrichmentResult Result,
        int StsCalls,
        int LookupCalls)> Execute(
            Func<int, string> identity,
            Func<int, string> lookup,
            CancellationToken cancellationToken = default)
    {
        int stsCount = 0;
        int lookupCount = 0;

        Task<string> Runner(
            IReadOnlyList<string> arguments,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            string command = string.Join(" ", arguments);

            if (command.Contains(
                "get-caller-identity",
                StringComparison.Ordinal))
            {
                int call =
                    Interlocked.Increment(ref stsCount);

                return Task.FromResult(identity(call));
            }

            if (command.Contains(
                "lookup-events",
                StringComparison.Ordinal))
            {
                int call =
                    Interlocked.Increment(ref lookupCount);

                return Task.FromResult(lookup(call));
            }

            throw new InvalidOperationException(
                "Unexpected synthetic AWS CLI command.");
        }

        var orchestrator =
            new AwsCloudTrailAuthenticatedOrchestrator(
                Runner, new AwsCloudTrailRegionalRateGate());

        var result = await orchestrator.FindCreationAsync(
            "test-profile",
            Account,
            "us-east-1",
            "virtual-network",
            "vpc-123",
            DateTimeOffset.UtcNow,
            cancellationToken,
            expectedPrincipal: Expected());

        return (result, stsCount, lookupCount);
    }

    [Fact]
    public async Task ExpiredCredentialsBeforeLookupAreUnavailable()
    {
        var actual = await Execute(
            _ => throw AwsError(
                "ExpiredToken", "GetCallerIdentity"),
            _ => """{"Events":[]}""");

        Assert.Equal(
            AwsCloudTrailEnrichmentStatus.Unavailable,
            actual.Result.Status);

        Assert.Equal(
            AwsCloudTrailEnrichmentFailureKind.CredentialRejected,
            actual.Result.FailureKind);

        Assert.Null(actual.Result.Evidence);
        Assert.Equal(0, actual.LookupCalls);
    }

    [Fact]
    public async Task InvalidClientTokenIsCredentialRejection()
    {
        var actual = await Execute(
            _ => throw AwsError(
                "InvalidClientTokenId", "GetCallerIdentity"),
            _ => """{"Events":[]}""");

        Assert.Equal(
            AwsCloudTrailEnrichmentFailureKind.CredentialRejected,
            actual.Result.FailureKind);

        Assert.Equal(
            AwsCloudTrailEnrichmentStatus.Unavailable,
            actual.Result.Status);
    }

    [Fact]
    public async Task LookupPermissionDenialIsDistinct()
    {
        var actual = await Execute(
            _ => Sts(Role()),
            _ => throw AwsError(
                "AccessDeniedException", "LookupEvents"));

        Assert.Equal(
            AwsCloudTrailEnrichmentFailureKind.PermissionDenied,
            actual.Result.FailureKind);

        Assert.Equal(
            AwsCloudTrailEnrichmentStatus.Unavailable,
            actual.Result.Status);

        Assert.Null(actual.Result.Evidence);
        Assert.Equal(1, actual.LookupCalls);
    }

    [Fact]
    public async Task ExpiredCredentialsOnLaterPageRejectLookup()
    {
        var actual = await Execute(
            _ => Sts(Role()),
            page => page == 1
                ? """{"Events":[],"NextToken":"page2"}"""
                : throw AwsError(
                    "ExpiredToken", "LookupEvents"));

        Assert.Equal(
            AwsCloudTrailEnrichmentFailureKind.CredentialRejected,
            actual.Result.FailureKind);

        Assert.Equal(
            AwsCloudTrailEnrichmentStatus.Unavailable,
            actual.Result.Status);

        Assert.Null(actual.Result.Evidence);
        Assert.Equal(2, actual.LookupCalls);
    }

    [Fact]
    public async Task PostLookupPermissionFailureRejectsResult()
    {
        var actual = await Execute(
            call => call == 1
                ? Sts(Role())
                : throw AwsError(
                    "AccessDenied", "GetCallerIdentity"),
            _ => """{"Events":[]}""");

        Assert.Equal(
            AwsCloudTrailEnrichmentFailureKind.PermissionDenied,
            actual.Result.FailureKind);

        Assert.Equal(
            AwsCloudTrailEnrichmentStatus.Unavailable,
            actual.Result.Status);

        Assert.Equal(2, actual.StsCalls);
        Assert.Equal(1, actual.LookupCalls);
    }

    [Fact]
    public async Task UnknownFailureDoesNotLeakDiagnostics()
    {
        var actual = await Execute(
            _ => Sts(Role()),
            _ => throw new InvalidOperationException(
                "private diagnostic should not be exposed"));

        Assert.Equal(
            AwsCloudTrailEnrichmentFailureKind.ProviderUnavailable,
            actual.Result.FailureKind);

        Assert.Equal(
            AwsCloudTrailEnrichmentStatus.Unavailable,
            actual.Result.Status);

        Assert.Null(actual.Result.Evidence);
    }

    [Fact]
    public async Task PrincipalMismatchRemainsIdentityUnverified()
    {
        var actual = await Execute(
            _ => Sts(
                $"arn:aws:sts::{Account}:" +
                "assumed-role/Administration/Changed"),
            _ => """{"Events":[]}""");

        Assert.Equal(
            AwsCloudTrailEnrichmentFailureKind.IdentityUnverified,
            actual.Result.FailureKind);

        Assert.Equal(
            AwsCloudTrailEnrichmentStatus.Unavailable,
            actual.Result.Status);

        Assert.Equal(0, actual.LookupCalls);
    }

    [Fact]
    public async Task ValidSessionRenewalRemainsNoMatchingEvidence()
    {
        var actual = await Execute(
            call => Sts(
                Role(call == 1 ? "Renewed" : "AnotherSession")),
            _ => """{"Events":[]}""");

        Assert.Equal(
            AwsCloudTrailEnrichmentStatus.NoMatchingEvidence,
            actual.Result.Status);

        Assert.Equal(
            AwsCloudTrailEnrichmentFailureKind.None,
            actual.Result.FailureKind);

        Assert.Equal(2, actual.StsCalls);
        Assert.Equal(1, actual.LookupCalls);
    }

    [Fact]
    public async Task ExternalCancellationPropagatesWithoutEvidence()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () =>
                await Execute(
                    _ => Sts(Role()),
                    _ => """{"Events":[]}""",
                    cancellation.Token));
    }
}
