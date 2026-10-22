using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral verification capability metadata.
/// Does not observe resources, execute verification, or capture evidence.
/// </summary>
public sealed class CapabilityVerificationMetadata
{
    public UniversalCapabilityIdentity Capability { get; }

    public CapabilityVerificationStatus Status { get; }

    public UniversalCapabilityIdentity? VerificationCapability { get; }

    public string? Limitations { get; }

    public CapabilityVerificationMetadata(
        UniversalCapabilityIdentity capability,
        CapabilityVerificationStatus status,
        UniversalCapabilityIdentity? verificationCapability = null,
        string? limitations = null)
    {
        if (string.IsNullOrWhiteSpace(capability.Value))
        {
            throw new ArgumentException(
                "A constructed capability identity is required.",
                nameof(capability));
        }

        if (status != CapabilityVerificationStatus.Unknown &&
            status != CapabilityVerificationStatus.Unsupported &&
            status != CapabilityVerificationStatus.Supported)
        {
            throw new ArgumentOutOfRangeException(
                nameof(status), status,
                "Unknown verification status.");
        }

        if (verificationCapability.HasValue)
        {
            var verification = verificationCapability.Value;

            if (string.IsNullOrWhiteSpace(verification.Value))
            {
                throw new ArgumentException(
                    "A constructed verification identity is required.",
                    nameof(verificationCapability));
            }

            if (status != CapabilityVerificationStatus.Supported)
            {
                throw new ArgumentException(
                    "Only supported verification can reference a verification capability.",
                    nameof(verificationCapability));
            }

            if (verification == capability)
            {
                throw new ArgumentException(
                    "A capability cannot reference itself as verification.",
                    nameof(verificationCapability));
            }
        }

        Capability = capability;
        Status = status;
        VerificationCapability = verificationCapability;
        Limitations = NormalizeLimitations(limitations);
    }

    private static string? NormalizeLimitations(string? limitations)
    {
        if (string.IsNullOrWhiteSpace(limitations))
        {
            return null;
        }

        var normalized = limitations.Trim();

        foreach (var character in normalized)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "Limitations cannot contain control characters.",
                    nameof(limitations));
            }
        }

        return normalized;
    }
}
