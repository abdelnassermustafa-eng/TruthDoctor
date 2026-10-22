using System;
using System.Collections.Generic;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Declares ordered preconditions for a universal capability.
/// No runtime eligibility or authorization evaluation is performed.
/// </summary>
public sealed class CapabilityPreconditionsContract
{
    public UniversalCapabilityIdentity Capability { get; }

    public IReadOnlyList<CapabilityPrecondition> Preconditions
    {
        get;
    }

    public CapabilityPreconditionsContract(
        UniversalCapabilityIdentity capability,
        IEnumerable<CapabilityPrecondition> preconditions)
    {
        if (string.IsNullOrWhiteSpace(capability.Value))
        {
            throw new ArgumentException(
                "A constructed capability identity is required.",
                nameof(capability));
        }

        ArgumentNullException.ThrowIfNull(preconditions);

        var seen = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        var snapshot = new List<CapabilityPrecondition>();

        foreach (var precondition in preconditions)
        {
            if (precondition is null)
            {
                throw new ArgumentException(
                    "Preconditions cannot contain null entries.",
                    nameof(preconditions));
            }

            if (!seen.Add(precondition.Key))
            {
                throw new ArgumentException(
                    "Duplicate precondition keys are not allowed.",
                    nameof(preconditions));
            }

            snapshot.Add(precondition);
        }

        Capability = capability;
        Preconditions = snapshot.AsReadOnly();
    }
}
