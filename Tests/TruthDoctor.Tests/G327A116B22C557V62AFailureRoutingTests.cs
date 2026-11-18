using System;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class
    G327A116B22C557V62AFailureRoutingTests
{
    private static string Diagnostic(string code)
        => $"An error occurred ({code}) when calling the " +
           "LookupEvents operation: synthetic-private-marker";

    [Fact]
    public void CloudTrailAccessDeniedUsesTypedTransport()
    {
        Exception error = AwsCliFailureRouter.CreateException(
            new[] { "cloudtrail", "lookup-events" },
            Diagnostic("AccessDenied"));

        var typed = Assert.IsType<
            AwsCloudTrailCliFailureException>(error);

        Assert.Equal(
            AwsCloudTrailEnrichmentFailureKind.PermissionDenied,
            typed.FailureKind);
    }

    [Fact]
    public void StsAccessDeniedUsesTypedTransport()
    {
        Exception error = AwsCliFailureRouter.CreateException(
            new[] { "sts", "get-caller-identity" },
            Diagnostic("AccessDeniedException"));

        var typed = Assert.IsType<
            AwsCloudTrailCliFailureException>(error);

        Assert.Equal(
            AwsCloudTrailEnrichmentFailureKind.PermissionDenied,
            typed.FailureKind);
    }

    [Fact]
    public void CloudTrailExpiredTokenUsesTypedTransport()
    {
        Exception error = AwsCliFailureRouter.CreateException(
            new[] { "cloudtrail", "lookup-events" },
            Diagnostic("ExpiredToken"));

        var typed = Assert.IsType<
            AwsCloudTrailCliFailureException>(error);

        Assert.Equal(
            AwsCloudTrailEnrichmentFailureKind.CredentialRejected,
            typed.FailureKind);
    }

    [Fact]
    public void UnknownCloudTrailFailureFailsClosed()
    {
        Exception error = AwsCliFailureRouter.CreateException(
            new[] { "cloudtrail", "lookup-events" },
            Diagnostic("UnexpectedServiceCode"));

        var typed = Assert.IsType<
            AwsCloudTrailCliFailureException>(error);

        Assert.Equal(
            AwsCloudTrailEnrichmentFailureKind.ProviderUnavailable,
            typed.FailureKind);
    }

    [Fact]
    public void OrdinaryDiscoveryDenialPreservesLegacyContract()
    {
        Exception error = AwsCliFailureRouter.CreateException(
            new[] { "ec2", "describe-vpcs" },
            Diagnostic("AccessDenied"));

        Assert.IsType<
            AwsDiscoveryAccessDeniedException>(error);
    }

    [Fact]
    public void OrdinaryDiscoveryFailurePreservesGenericContract()
    {
        Exception error = AwsCliFailureRouter.CreateException(
            new[] { "ec2", "describe-subnets" },
            Diagnostic("ExpiredToken"));

        Assert.IsType<InvalidOperationException>(error);

        Assert.Equal(
            "AWS CLI read-only discovery failed.",
            error.Message);
    }

    [Fact]
    public void TypedFailureDoesNotRetainSensitiveDiagnostics()
    {
        const string marker = "synthetic-private-marker";

        Exception error = AwsCliFailureRouter.CreateException(
            new[] { "sts", "get-caller-identity" },
            Diagnostic("ExpiredToken"));

        var typed = Assert.IsType<
            AwsCloudTrailCliFailureException>(error);

        Assert.DoesNotContain(marker, typed.Message);
        Assert.Null(typed.InnerException);
    }

    [Fact]
    public void EmptyDiagnosticsAreSanitizedAndFailClosed()
    {
        Exception enrichment =
            AwsCliFailureRouter.CreateException(
                new[] { "cloudtrail", "lookup-events" },
                null);

        var typed = Assert.IsType<
            AwsCloudTrailCliFailureException>(enrichment);

        Assert.Equal(
            AwsCloudTrailEnrichmentFailureKind.ProviderUnavailable,
            typed.FailureKind);

        Exception ordinary =
            AwsCliFailureRouter.CreateException(
                new[] { "ec2", "describe-vpcs" },
                null);

        Assert.IsType<InvalidOperationException>(ordinary);
    }
}
