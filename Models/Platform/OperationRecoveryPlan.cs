using System;
using System.Collections.Generic;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, declaration-ordered collection of per-step recovery plans.
/// Does not imply coverage, execution order, or authorization.
/// </summary>
public sealed class OperationRecoveryPlan
{
    public IReadOnlyList<OperationStepRecovery> Recoveries { get; }

    public OperationRecoveryPlan(
        IEnumerable<OperationStepRecovery> recoveries)
    {
        ArgumentNullException.ThrowIfNull(recoveries);

        var seen = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        var snapshot = new List<OperationStepRecovery>();

        foreach (var recovery in recoveries)
        {
            if (recovery is null)
            {
                throw new ArgumentException(
                    "Recovery plans cannot contain null entries.",
                    nameof(recoveries));
            }

            if (!seen.Add(recovery.StepId))
            {
                throw new ArgumentException(
                    "Duplicate recovery step IDs are not allowed.",
                    nameof(recoveries));
            }

            snapshot.Add(recovery);
        }

        Recoveries = snapshot.AsReadOnly();
    }
}
