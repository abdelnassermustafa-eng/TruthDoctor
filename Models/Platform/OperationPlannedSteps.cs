using System;
using System.Collections.Generic;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, ordered declaration of planned operation steps.
/// Declaration order does not establish execution dependency order.
/// </summary>
public sealed class OperationPlannedSteps
{
    public IReadOnlyList<OperationPlannedStep> Steps { get; }

    public OperationPlannedSteps(
        IEnumerable<OperationPlannedStep> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);

        var seen = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        var snapshot = new List<OperationPlannedStep>();

        foreach (var step in steps)
        {
            if (step is null)
            {
                throw new ArgumentException(
                    "Planned steps cannot contain null entries.",
                    nameof(steps));
            }

            if (!seen.Add(step.StepId))
            {
                throw new ArgumentException(
                    "Duplicate planned step IDs are not allowed.",
                    nameof(steps));
            }

            snapshot.Add(step);
        }

        if (snapshot.Count == 0)
        {
            throw new ArgumentException(
                "At least one planned step is required.",
                nameof(steps));
        }

        Steps = snapshot.AsReadOnly();
    }
}
