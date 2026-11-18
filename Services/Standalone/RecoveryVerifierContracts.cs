using System;
using System.Threading;
using System.Threading.Tasks;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(
    "TruthDoctor.Tests")]

namespace TruthDoctor.Services.Standalone;

/// <summary>
/// Identifies the original provider cleanup ownership record.
/// This type does not allocate or verify a session identity.
/// </summary>
internal readonly record struct RecoverySessionIdentity(
    string ConnectionId,
    string ProviderId,
    long RegistrationGeneration,
    Guid CleanupOwnershipId);

/// <summary>
/// Outcome returned by a trusted provider-specific verifier.
/// A verdict alone cannot release coordinator ownership.
/// </summary>
internal enum RecoveryVerificationVerdict
{
    VerifiedSafe,
    Rejected,
    Inconclusive
}

/// <summary>
/// Contract for a provider-specific recovery safety verifier.
/// The coordinator must select a trusted implementation and
/// independently validate ownership before any release.
/// </summary>
internal interface IProviderRecoveryVerifier
{
    string ProviderId { get; }

    Task<RecoveryVerificationVerdict> VerifyAsync(
        RecoverySessionIdentity session,
        CancellationToken cancellationToken);
}
