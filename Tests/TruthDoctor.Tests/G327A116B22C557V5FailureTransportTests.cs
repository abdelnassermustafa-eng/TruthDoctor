using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A116B22C557V5FailureTransportTests
{
    private const string Account = "123456789012";

    private static string Diagnostic(
        string code)
        => $"An error occurred ({code}) when calling the " +
           "LookupEvents operation: sensitive synthetic text";

    [Fact]
    public void ExpiredCredentialsAreClassified()
    {
        foreach (string code in new[]
        {
            "ExpiredToken",
            "ExpiredTokenException",
            "InvalidClientTokenId",
            "UnrecognizedClientException"
        })
        {
            Assert.Equal(
                AwsCloudTrailEnrichmentFailureKind.CredentialRejected,
                AwsCloudTrailCliFailureTransport
                    .ClassifyDiagnostic(Diagnostic(code)));
        }
    }

    [Fact]
    public void AccessDeniedCodesAreClassified()
    {
        foreach (string code in new[]
        {
            "AccessDenied",
            "AccessDeniedException",
            "UnauthorizedOperation"
        })
        {
            Assert.Equal(
                AwsCloudTrailEnrichmentFailureKind.PermissionDenied,
                AwsCloudTrailCliFailureTransport
                    .ClassifyDiagnostic(Diagnostic(code)));
        }
    }

    [Fact]
    public void UnknownCodesRemainUnavailable()
    {
        Assert.Equal(
            AwsCloudTrailEnrichmentFailureKind.ProviderUnavailable,
            AwsCloudTrailCliFailureTransport.ClassifyDiagnostic(
                Diagnostic("UnknownServiceFailure")));
    }

    [Fact]
    public void MalformedCodesFailClosed()
    {
        Assert.Equal(
            AwsCloudTrailEnrichmentFailureKind.ProviderUnavailable,
            AwsCloudTrailCliFailureTransport.ClassifyDiagnostic(
                Diagnostic("AccessDenied-Spoofed")));

        Assert.Equal(
            AwsCloudTrailEnrichmentFailureKind.ProviderUnavailable,
            AwsCloudTrailCliFailureTransport.ClassifyDiagnostic(
                "unrecognized error"));
    }

    [Fact]
    public void OversizedDiagnosticsFailClosed()
    {
        Assert.Equal(
            AwsCloudTrailEnrichmentFailureKind.ProviderUnavailable,
            AwsCloudTrailCliFailureTransport.ClassifyDiagnostic(
                new string('X', 65537)));
    }

    [Fact]
    public void TransportExceptionNeverContainsRawDiagnostic()
    {
        const string secret = "synthetic-sensitive-marker";

        var failure =
            AwsCloudTrailCliFailureTransport.FromDiagnostic(
                Diagnostic("ExpiredToken") + secret);

        Assert.Equal(
            AwsCloudTrailEnrichmentFailureKind.CredentialRejected,
            failure.FailureKind);

        Assert.DoesNotContain(secret, failure.Message);
        Assert.Null(failure.InnerException);
    }

    [Fact]
    public async Task OrchestratorConsumesTypedTransportFailure()
    {
        Task<string> Runner(
            IReadOnlyList<string> args,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            throw AwsCloudTrailCliFailureTransport
                .FromDiagnostic(Diagnostic("ExpiredToken"));
        }

        var orchestrator =
            new AwsCloudTrailAuthenticatedOrchestrator(Runner);

        var result = await orchestrator.FindCreationAsync(
            "test-profile",
            Account,
            "us-east-1",
            "virtual-network",
            "vpc-123",
            DateTimeOffset.UtcNow);

        Assert.Equal(
            AwsCloudTrailEnrichmentStatus.Unavailable,
            result.Status);

        Assert.Equal(
            AwsCloudTrailEnrichmentFailureKind.CredentialRejected,
            result.FailureKind);

        Assert.Null(result.Evidence);
    }

    [Fact]
    public async Task ExistingDiscoveryDenialRemainsClassified()
    {
        Task<string> Runner(
            IReadOnlyList<string> args,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            throw new AwsDiscoveryAccessDeniedException();
        }

        var orchestrator =
            new AwsCloudTrailAuthenticatedOrchestrator(Runner);

        var result = await orchestrator.FindCreationAsync(
            "test-profile",
            Account,
            "us-east-1",
            "virtual-network",
            "vpc-123",
            DateTimeOffset.UtcNow);

        Assert.Equal(
            AwsCloudTrailEnrichmentStatus.Unavailable,
            result.Status);

        Assert.Equal(
            AwsCloudTrailEnrichmentFailureKind.PermissionDenied,
            result.FailureKind);

        Assert.Null(result.Evidence);
    }
}
