using System;
using System.Threading;

namespace TruthDoctor.Services.Standalone;

/// <summary>
/// Describes the reported origin of AWS credentials.
///
/// This enumeration never proves that credentials have been
/// resolved, exported, bound to a process or authenticated.
/// </summary>
public enum AwsCloudTrailCredentialSourceKind
{
    Unknown,
    SharedProfile,
    IdentityCenter,
    AssumedRole,
    CredentialProcess,
    Environment,
    StaticKeys
}

/// <summary>
/// AWS-only, non-secret credential-session metadata.
///
/// Do not add access keys, secret keys, session tokens,
/// credential JSON or configuration file contents here.
/// </summary>
public sealed record AwsCloudTrailCredentialContextDescriptor(
    AwsCloudTrailCredentialSourceKind SourceKind,
    string AccountId,
    AwsCloudTrailPrincipalIdentity Principal,
    DateTimeOffset ObservedAtUtc,
    DateTimeOffset? ExpiresAtUtc);

public enum AwsCloudTrailCredentialContextStatus
{
    CandidateForBinding,
    UnsupportedSource,
    IdentityMismatch,
    ObservationStale,
    ExpirationUnknown,
    Expired,
    InsufficientLifetime
}

/// <summary>
/// Metadata assessment only.
///
/// CandidateForBinding is not proof of transport binding.
/// The current contract cannot authorize AWS execution or
/// CloudTrail evidence publication.
/// </summary>
public sealed record AwsCloudTrailCredentialContextAssessment(
    AwsCloudTrailCredentialContextStatus Status)
{
    public bool EligibleForBinding =>
        Status == AwsCloudTrailCredentialContextStatus
            .CandidateForBinding;

    // No production binding implementation exists yet.
    // Keep authorization impossible at this layer.
    public bool AuthorizesCliExecution => false;

    public bool AuthorizesEvidencePublication => false;
}

/// <summary>
/// Fail-closed eligibility checks for a future short-lived
/// AWS credential execution context.
///
/// This contract never reads AWS files, launches AWS CLI,
/// resolves credentials or stores credential secrets.
/// </summary>
public static class AwsCloudTrailCredentialContextPolicy
{
    public static readonly TimeSpan MinimumRemainingLifetime =
        TimeSpan.FromMinutes(5);

    public static readonly TimeSpan MaximumObservationAge =
        TimeSpan.FromMinutes(2);

    public static readonly TimeSpan MaximumFutureClockSkew =
        TimeSpan.FromSeconds(30);

    public static AwsCloudTrailCredentialContextAssessment Evaluate(
        AwsCloudTrailCredentialContextDescriptor? descriptor,
        string expectedAccountId,
        AwsCloudTrailPrincipalIdentity? expectedPrincipal,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (descriptor is null ||
            expectedPrincipal is null ||
            descriptor.Principal is null ||
            string.IsNullOrWhiteSpace(expectedAccountId) ||
            expectedAccountId.Length != 12)
        {
            return new(
                AwsCloudTrailCredentialContextStatus
                    .IdentityMismatch);
        }

        foreach (char character in expectedAccountId)
        {
            if (!char.IsAsciiDigit(character))
            {
                return new(
                    AwsCloudTrailCredentialContextStatus
                        .IdentityMismatch);
            }
        }

        if (!StringComparer.Ordinal.Equals(
                descriptor.AccountId,
                expectedAccountId) ||
            !StringComparer.Ordinal.Equals(
                descriptor.Principal.AccountId,
                expectedAccountId) ||
            !expectedPrincipal.SamePrincipal(
                descriptor.Principal))
        {
            return new(
                AwsCloudTrailCredentialContextStatus
                    .IdentityMismatch);
        }

        cancellationToken.ThrowIfCancellationRequested();

        // A profile selector or environment variable alone
        // does not provide provenance for one credential set.
        if (descriptor.SourceKind is not (
                AwsCloudTrailCredentialSourceKind.IdentityCenter or
                AwsCloudTrailCredentialSourceKind.AssumedRole or
                AwsCloudTrailCredentialSourceKind.CredentialProcess))
        {
            return new(
                AwsCloudTrailCredentialContextStatus
                    .UnsupportedSource);
        }

        if (nowUtc == default ||
            descriptor.ObservedAtUtc == default ||
            descriptor.ObservedAtUtc >
                nowUtc + MaximumFutureClockSkew ||
            nowUtc - descriptor.ObservedAtUtc >
                MaximumObservationAge)
        {
            return new(
                AwsCloudTrailCredentialContextStatus
                    .ObservationStale);
        }

        if (descriptor.ExpiresAtUtc is null)
        {
            return new(
                AwsCloudTrailCredentialContextStatus
                    .ExpirationUnknown);
        }

        DateTimeOffset expires = descriptor.ExpiresAtUtc.Value;

        if (expires <= nowUtc)
        {
            return new(
                AwsCloudTrailCredentialContextStatus
                    .Expired);
        }

        if (expires - nowUtc < MinimumRemainingLifetime)
        {
            return new(
                AwsCloudTrailCredentialContextStatus
                    .InsufficientLifetime);
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Eligibility is not authorization. Actual credential
        // binding must be proven by a separate execution
        // transport before AWS calls or evidence publication.
        return new(
            AwsCloudTrailCredentialContextStatus
                .CandidateForBinding);
    }
}
