using System;
using System.Collections.Generic;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, declaration-ordered evidence requirements for an operation.
/// Does not collect, verify, authorize, or schedule any action.
/// </summary>
public sealed class OperationEvidenceRequirements
{
    public IReadOnlyList<OperationEvidenceRequirement> Requirements { get; }

    public OperationEvidenceRequirements(
        IEnumerable<OperationEvidenceRequirement> requirements)
    {
        ArgumentNullException.ThrowIfNull(requirements);

        var seen = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        var snapshot = new List<OperationEvidenceRequirement>();

        foreach (var requirement in requirements)
        {
            if (requirement is null)
            {
                throw new ArgumentException(
                    "Evidence requirements cannot contain null entries.",
                    nameof(requirements));
            }

            if (!seen.Add(requirement.RequirementId))
            {
                throw new ArgumentException(
                    "Duplicate evidence requirement IDs are not allowed.",
                    nameof(requirements));
            }

            snapshot.Add(requirement);
        }

        Requirements = snapshot.AsReadOnly();
    }
}
