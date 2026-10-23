using System;
using System.Collections.Generic;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, declaration-ordered collection of per-step impact estimates.
/// Does not infer missing estimates, aggregate risk, or determine execution.
/// </summary>
public sealed class OperationEstimatedImpact
{
    public IReadOnlyList<OperationStepImpact> Impacts { get; }

    public OperationEstimatedImpact(
        IEnumerable<OperationStepImpact> impacts)
    {
        ArgumentNullException.ThrowIfNull(impacts);

        var seen = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        var snapshot = new List<OperationStepImpact>();

        foreach (var impact in impacts)
        {
            if (impact is null)
            {
                throw new ArgumentException(
                    "Impact collections cannot contain null entries.",
                    nameof(impacts));
            }

            if (!seen.Add(impact.StepId))
            {
                throw new ArgumentException(
                    "Duplicate impact step IDs are not allowed.",
                    nameof(impacts));
            }

            snapshot.Add(impact);
        }

        Impacts = snapshot.AsReadOnly();
    }
}
