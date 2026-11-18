using System;
using System.Text.Json;
using System.Threading;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class
    G327A116B22C557V62BCredentialContextTests
{
    private const string Account = "123456789012";

    private static readonly DateTimeOffset Now =
        new(2026, 11, 16, 12, 0, 0, TimeSpan.Zero);

    private static AwsCloudTrailPrincipalIdentity Principal(
        string role = "Engineering",
        string session = "Initial")
    {
        string json = JsonSerializer.Serialize(new
        {
            Account,
            Arn = $"arn:aws:sts::{Account}:" +
                  $"assumed-role/{role}/{session}"
        });

        Assert.True(
            AwsCloudTrailPrincipalIdentity.TryParse(
                json, out var identity));

        return Assert.IsType<
            AwsCloudTrailPrincipalIdentity>(identity);
    }

    private static AwsCloudTrailCredentialContextDescriptor
        Descriptor(
            AwsCloudTrailCredentialSourceKind source =
                AwsCloudTrailCredentialSourceKind.AssumedRole,
            DateTimeOffset? expiry = null,
            DateTimeOffset? observed = null,
            AwsCloudTrailPrincipalIdentity? principal = null,
            string account = Account)
        => new(
            source,
            account,
            principal ?? Principal(),
            observed ?? Now,
            expiry ?? Now.AddMinutes(20));

    private static AwsCloudTrailCredentialContextAssessment
        Assess(
            AwsCloudTrailCredentialContextDescriptor descriptor,
            AwsCloudTrailPrincipalIdentity? expected = null)
        => AwsCloudTrailCredentialContextPolicy.Evaluate(
            descriptor,
            Account,
            expected ?? Principal(),
            Now);

    [Fact]
    public void ValidAssumedRoleIsOnlyCandidateForBinding()
    {
        var result = Assess(Descriptor());

        Assert.Equal(
            AwsCloudTrailCredentialContextStatus
                .CandidateForBinding,
            result.Status);

        Assert.True(result.EligibleForBinding);
        Assert.False(result.AuthorizesCliExecution);
        Assert.False(result.AuthorizesEvidencePublication);
    }

    [Fact]
    public void IdentityCenterMetadataCanBeCandidate()
    {
        var result = Assess(Descriptor(
            AwsCloudTrailCredentialSourceKind.IdentityCenter));

        Assert.Equal(
            AwsCloudTrailCredentialContextStatus
                .CandidateForBinding,
            result.Status);

        Assert.False(result.AuthorizesEvidencePublication);
    }

    [Fact]
    public void SameRoleRenewalPreservesNormalizedIdentity()
    {
        var result = Assess(
            Descriptor(
                principal: Principal(
                    "Engineering", "RenewedSession")),
            expected: Principal(
                "Engineering", "OriginalSession"));

        Assert.Equal(
            AwsCloudTrailCredentialContextStatus
                .CandidateForBinding,
            result.Status);
    }

    [Fact]
    public void SharedProfileAloneIsNotBindingProof()
    {
        var result = Assess(Descriptor(
            AwsCloudTrailCredentialSourceKind.SharedProfile));

        Assert.Equal(
            AwsCloudTrailCredentialContextStatus
                .UnsupportedSource,
            result.Status);

        Assert.False(result.AuthorizesCliExecution);
    }

    [Fact]
    public void StaticKeyMetadataIsNotAutomaticallyEligible()
    {
        var result = Assess(Descriptor(
            AwsCloudTrailCredentialSourceKind.StaticKeys));

        Assert.Equal(
            AwsCloudTrailCredentialContextStatus
                .UnsupportedSource,
            result.Status);
    }

    [Fact]
    public void DifferentPrincipalFailsClosed()
    {
        var result = Assess(
            Descriptor(
                principal: Principal("Administration")));

        Assert.Equal(
            AwsCloudTrailCredentialContextStatus
                .IdentityMismatch,
            result.Status);

        Assert.False(result.AuthorizesEvidencePublication);
    }

    [Fact]
    public void DifferentAccountFailsClosed()
    {
        var result = Assess(
            Descriptor(account: "999999999999"));

        Assert.Equal(
            AwsCloudTrailCredentialContextStatus
                .IdentityMismatch,
            result.Status);
    }

    [Fact]
    public void UnknownExpirationFailsClosed()
    {
        var descriptor = new
            AwsCloudTrailCredentialContextDescriptor(
                AwsCloudTrailCredentialSourceKind.AssumedRole,
                Account,
                Principal(),
                Now,
                null);

        var result = Assess(descriptor);

        Assert.Equal(
            AwsCloudTrailCredentialContextStatus
                .ExpirationUnknown,
            result.Status);
    }

    [Fact]
    public void ExpiredCredentialsFailClosed()
    {
        var result = Assess(
            Descriptor(expiry: Now.AddMinutes(-1)));

        Assert.Equal(
            AwsCloudTrailCredentialContextStatus
                .Expired,
            result.Status);
    }

    [Fact]
    public void InsufficientRemainingLifetimeFailsClosed()
    {
        var result = Assess(
            Descriptor(expiry: Now.AddMinutes(2)));

        Assert.Equal(
            AwsCloudTrailCredentialContextStatus
                .InsufficientLifetime,
            result.Status);
    }

    [Fact]
    public void StaleObservationAndCancellationFailClosed()
    {
        var result = Assess(
            Descriptor(
                observed: Now.AddMinutes(-3)));

        Assert.Equal(
            AwsCloudTrailCredentialContextStatus
                .ObservationStale,
            result.Status);

        using var cancellation =
            new CancellationTokenSource();

        cancellation.Cancel();

        Assert.ThrowsAny<OperationCanceledException>(
            () => AwsCloudTrailCredentialContextPolicy.Evaluate(
                Descriptor(),
                Account,
                Principal(),
                Now,
                cancellation.Token));
    }
}
