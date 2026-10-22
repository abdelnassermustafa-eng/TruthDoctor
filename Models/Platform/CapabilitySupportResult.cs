using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral declaration of capability support.
/// Does not evaluate preconditions, authorize, or execute operations.
/// </summary>
public sealed class CapabilitySupportResult
{
    public UniversalCapabilityIdentity Capability { get; }

    public CapabilitySupportStatus Status { get; }

    public string? Reason { get; }

    public CapabilitySupportResult(
        UniversalCapabilityIdentity capability,
        CapabilitySupportStatus status,
        string? reason = null)
    {
        if (string.IsNullOrWhiteSpace(capability.Value))
        {
            throw new ArgumentException(
                "A constructed capability identity is required.",
                nameof(capability));
        }

        if (status != CapabilitySupportStatus.Unknown &&
            status != CapabilitySupportStatus.Unsupported &&
            status != CapabilitySupportStatus.Supported)
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Unknown capability support status.");
        }

        Capability = capability;
        Status = status;
        Reason = NormalizeReason(reason);
    }

    private static string? NormalizeReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return null;
        }

        var normalized = reason.Trim();

        foreach (var character in normalized)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "Reason cannot contain control characters.",
                    nameof(reason));
            }
        }

        return normalized;
    }
}
