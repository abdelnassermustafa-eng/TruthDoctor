using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral reversibility metadata.
/// Does not execute rollback or guarantee state restoration.
/// </summary>
public sealed class CapabilityReversibilityMetadata
{
    public UniversalCapabilityIdentity Capability { get; }

    public CapabilityReversibilityStatus Status { get; }

    public UniversalCapabilityIdentity? RollbackCapability { get; }

    public string? Limitations { get; }

    public CapabilityReversibilityMetadata(
        UniversalCapabilityIdentity capability,
        CapabilityReversibilityStatus status,
        UniversalCapabilityIdentity? rollbackCapability = null,
        string? limitations = null)
    {
        if (string.IsNullOrWhiteSpace(capability.Value))
        {
            throw new ArgumentException(
                "A constructed capability identity is required.",
                nameof(capability));
        }

        if (status != CapabilityReversibilityStatus.Unknown &&
            status != CapabilityReversibilityStatus.Unsupported &&
            status != CapabilityReversibilityStatus.Supported)
        {
            throw new ArgumentOutOfRangeException(
                nameof(status), status,
                "Unknown reversibility status.");
        }

        if (rollbackCapability.HasValue)
        {
            var reversal = rollbackCapability.Value;

            if (string.IsNullOrWhiteSpace(reversal.Value))
            {
                throw new ArgumentException(
                    "A constructed rollback identity is required.",
                    nameof(rollbackCapability));
            }

            if (status != CapabilityReversibilityStatus.Supported)
            {
                throw new ArgumentException(
                    "Only supported reversal can reference a rollback capability.",
                    nameof(rollbackCapability));
            }

            if (reversal == capability)
            {
                throw new ArgumentException(
                    "A capability cannot reference itself as rollback.",
                    nameof(rollbackCapability));
            }
        }

        Capability = capability;
        Status = status;
        RollbackCapability = rollbackCapability;
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
