using System;
using System.Collections.Generic;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, ordered declaration of requested outcome conditions.
/// Does not evaluate, authorize, plan, or execute an operation.
/// </summary>
public sealed class OperationDesiredOutcome
{
    public IReadOnlyList<OperationOutcomeCondition> Conditions { get; }

    public OperationDesiredOutcome(
        IEnumerable<OperationOutcomeCondition> conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);

        var seen = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        var snapshot = new List<OperationOutcomeCondition>();

        foreach (var condition in conditions)
        {
            if (condition is null)
            {
                throw new ArgumentException(
                    "Outcome conditions cannot contain null entries.",
                    nameof(conditions));
            }

            if (!seen.Add(condition.Key))
            {
                throw new ArgumentException(
                    "Duplicate outcome condition keys are not allowed.",
                    nameof(conditions));
            }

            snapshot.Add(condition);
        }

        if (snapshot.Count == 0)
        {
            throw new ArgumentException(
                "At least one outcome condition is required.",
                nameof(conditions));
        }

        Conditions = snapshot.AsReadOnly();
    }
}
