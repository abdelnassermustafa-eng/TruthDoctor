using System;
using System.Collections.Generic;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Validates planned-step dependencies and produces a deterministic,
/// prerequisite-first planning order.
/// Does not authorize, schedule, dispatch, or execute operations.
/// </summary>
public static class OperationDependencyOrdering
{
    public static IReadOnlyList<OperationPlannedStep> Order(
        OperationPlannedSteps steps,
        OperationStepDependencies dependencies)
    {
        ArgumentNullException.ThrowIfNull(steps);
        ArgumentNullException.ThrowIfNull(dependencies);

        var count = steps.Steps.Count;

        var positions = new Dictionary<string, int>(
            StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < count; index++)
        {
            positions.Add(steps.Steps[index].StepId, index);
        }

        var remainingPrerequisites = new int[count];
        var dependents = new List<int>[count];

        for (var index = 0; index < count; index++)
        {
            dependents[index] = new List<int>();
        }

        foreach (var dependency in dependencies.Dependencies)
        {
            if (!positions.TryGetValue(
                dependency.StepId, out var dependentIndex))
            {
                throw new ArgumentException(
                    "A dependency references an unknown dependent step.",
                    nameof(dependencies));
            }

            if (!positions.TryGetValue(
                dependency.PrerequisiteStepId,
                out var prerequisiteIndex))
            {
                throw new ArgumentException(
                    "A dependency references an unknown prerequisite step.",
                    nameof(dependencies));
            }

            remainingPrerequisites[dependentIndex]++;
            dependents[prerequisiteIndex].Add(dependentIndex);
        }

        var available = new SortedSet<int>();

        for (var index = 0; index < count; index++)
        {
            if (remainingPrerequisites[index] == 0)
            {
                available.Add(index);
            }
        }

        var ordered = new List<OperationPlannedStep>(count);

        while (available.Count > 0)
        {
            var next = available.Min;
            available.Remove(next);

            ordered.Add(steps.Steps[next]);

            foreach (var dependentIndex in dependents[next])
            {
                remainingPrerequisites[dependentIndex]--;

                if (remainingPrerequisites[dependentIndex] == 0)
                {
                    available.Add(dependentIndex);
                }
            }
        }

        if (ordered.Count != count)
        {
            throw new ArgumentException(
                "The planned-step dependencies contain a cycle.",
                nameof(dependencies));
        }

        return ordered.AsReadOnly();
    }
}
