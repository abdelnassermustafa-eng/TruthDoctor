using System;
using System.Collections.Generic;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Declares a provider's advertised operational capabilities.
/// Advertisement does not establish authorization or execution.
/// </summary>
public sealed class ProviderCapabilityAdvertisement
{
    public string ProviderId { get; }

    public IReadOnlyList<CapabilityToResourceContract> Capabilities
    {
        get;
    }

    public ProviderCapabilityAdvertisement(
        string providerId,
        IEnumerable<CapabilityToResourceContract> capabilities)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        ArgumentNullException.ThrowIfNull(capabilities);

        var normalizedProviderId = providerId.Trim();

        var unique = new HashSet<UniversalCapabilityIdentity>();

        var mappings = new List<CapabilityToResourceContract>();

        foreach (var mapping in capabilities)
        {
            if (mapping is null)
            {
                throw new ArgumentException(
                    "Capability mappings cannot contain null entries.",
                    nameof(capabilities));
            }

            if (!unique.Add(mapping.Capability))
            {
                throw new ArgumentException(
                    "Duplicate capability identities are not allowed.",
                    nameof(capabilities));
            }

            mappings.Add(mapping);
        }

        ProviderId = normalizedProviderId;
        Capabilities = mappings.AsReadOnly();
    }

    public bool Advertises(
        UniversalCapabilityIdentity capability,
        string? resourceType)
    {
        if (string.IsNullOrWhiteSpace(capability.Value) ||
            string.IsNullOrWhiteSpace(resourceType))
        {
            return false;
        }

        foreach (var mapping in Capabilities)
        {
            if (mapping.Capability == capability &&
                mapping.AppliesTo(resourceType))
            {
                return true;
            }
        }

        return false;
    }
}
