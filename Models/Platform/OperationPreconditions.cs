using System;
using System.Collections.Generic;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, ordered declaration of operation prerequisites.
/// An empty declaration does not establish readiness or authorization.
/// </summary>
public sealed class OperationPreconditions
{
    public IReadOnlyList<OperationPrecondition> Conditions { get; }

    public OperationPreconditions(
        IEnumerable<OperationPrecondition> conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);

        var seen = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        var snapshot = new List<OperationPrecondition>();

        foreach (var condition in conditions)
        {
            if (condition is null)
            {
                throw new ArgumentException(
                    "Preconditions cannot contain null entries.",
                    nameof(conditions));
            }

            if (!seen.Add(condition.Key))
            {
                throw new ArgumentException(
                    "Duplicate precondition keys are not allowed.",
                    nameof(conditions));
            }

            snapshot.Add(condition);
        }

        Conditions = snapshot.AsReadOnly();
    }
}
