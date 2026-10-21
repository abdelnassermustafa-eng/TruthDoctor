using System;
using System.Collections.Generic;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Declares which resource types a universal capability applies to.
/// Applicability does not establish provider support or execution.
/// </summary>
public sealed class CapabilityToResourceContract
{
    public UniversalCapabilityIdentity Capability { get; }

    public IReadOnlyList<string> ResourceTypes { get; }

    public CapabilityToResourceContract(
        UniversalCapabilityIdentity capability,
        IEnumerable<string> resourceTypes)
    {
        if (string.IsNullOrWhiteSpace(capability.Value))
        {
            throw new ArgumentException(
                "A constructed capability identity is required.",
                nameof(capability));
        }

        ArgumentNullException.ThrowIfNull(resourceTypes);

        var unique = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        var normalized = new List<string>();

        foreach (var resourceType in resourceTypes)
        {
            if (string.IsNullOrWhiteSpace(resourceType))
            {
                throw new ArgumentException(
                    "Resource types cannot be null or blank.",
                    nameof(resourceTypes));
            }

            var value = resourceType.Trim();

            foreach (var character in value)
            {
                if (char.IsControl(character))
                {
                    throw new ArgumentException(
                        "Resource types cannot contain control characters.",
                        nameof(resourceTypes));
                }
            }

            if (unique.Add(value))
            {
                normalized.Add(value);
            }
        }

        if (normalized.Count == 0)
        {
            throw new ArgumentException(
                "At least one resource type is required.",
                nameof(resourceTypes));
        }

        Capability = capability;
        ResourceTypes = normalized.AsReadOnly();
    }

    public bool AppliesTo(string? resourceType)
    {
        if (string.IsNullOrWhiteSpace(resourceType))
        {
            return false;
        }

        var candidate = resourceType.Trim();

        foreach (var supported in ResourceTypes)
        {
            if (string.Equals(
                supported,
                candidate,
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
