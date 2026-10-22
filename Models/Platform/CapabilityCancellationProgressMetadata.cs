using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral cancellation and progress support metadata.
/// Does not cancel operations, execute work, or report live progress.
/// </summary>
public sealed class CapabilityCancellationProgressMetadata
{
    public UniversalCapabilityIdentity Capability { get; }

    public CapabilityCancellationStatus CancellationStatus { get; }

    public CapabilityProgressStatus ProgressStatus { get; }

    public string? CancellationLimitations { get; }

    public string? ProgressLimitations { get; }

    public CapabilityCancellationProgressMetadata(
        UniversalCapabilityIdentity capability,
        CapabilityCancellationStatus cancellationStatus,
        CapabilityProgressStatus progressStatus,
        string? cancellationLimitations = null,
        string? progressLimitations = null)
    {
        if (string.IsNullOrWhiteSpace(capability.Value))
        {
            throw new ArgumentException(
                "A constructed capability identity is required.",
                nameof(capability));
        }

        if (cancellationStatus != CapabilityCancellationStatus.Unknown &&
            cancellationStatus != CapabilityCancellationStatus.Unsupported &&
            cancellationStatus != CapabilityCancellationStatus.Supported)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cancellationStatus),
                cancellationStatus,
                "Unknown cancellation support status.");
        }

        if (progressStatus != CapabilityProgressStatus.Unknown &&
            progressStatus != CapabilityProgressStatus.Unsupported &&
            progressStatus != CapabilityProgressStatus.Supported)
        {
            throw new ArgumentOutOfRangeException(
                nameof(progressStatus),
                progressStatus,
                "Unknown progress support status.");
        }

        Capability = capability;
        CancellationStatus = cancellationStatus;
        ProgressStatus = progressStatus;
        CancellationLimitations = NormalizeLimitations(
            cancellationLimitations,
            nameof(cancellationLimitations));
        ProgressLimitations = NormalizeLimitations(
            progressLimitations,
            nameof(progressLimitations));
    }

    private static string? NormalizeLimitations(
        string? limitations,
        string parameterName)
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
                    parameterName);
            }
        }

        return normalized;
    }
}
