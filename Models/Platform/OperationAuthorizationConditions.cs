using System;
using System.Collections.Generic;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, ordered declaration of explicit authorization conditions.
/// A collection of conditions, including an empty collection, does not
/// establish permission, eligibility, or effective authorization.
/// This declaration does not evaluate conditions, inspect runtime state,
/// authenticate actors, grant privileges, approve, or execute operations.
/// </summary>
public sealed class OperationAuthorizationConditions
{
    public IReadOnlyList<OperationAuthorizationCondition> Conditions { get; }

    public OperationAuthorizationConditions(
        IEnumerable<OperationAuthorizationCondition> conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);

        var seen = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        var snapshot = new List<OperationAuthorizationCondition>();

        foreach (var condition in conditions)
        {
            if (condition is null)
            {
                throw new ArgumentException(
                    "Authorization conditions cannot contain null entries.",
                    nameof(conditions));
            }

            if (!seen.Add(condition.Key))
            {
                throw new ArgumentException(
                    "Duplicate authorization condition keys are not allowed.",
                    nameof(conditions));
            }

            snapshot.Add(condition);
        }

        Conditions = snapshot.AsReadOnly();
    }
}
