using System;
using System.Collections.Generic;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable declaration of directed planned-step dependencies.
/// Declaration order does not determine execution or scheduling order.
/// </summary>
public sealed class OperationStepDependencies
{
    public IReadOnlyList<OperationStepDependency> Dependencies { get; }

    public OperationStepDependencies(
        IEnumerable<OperationStepDependency> dependencies)
    {
        ArgumentNullException.ThrowIfNull(dependencies);

        var seen = new Dictionary<string, HashSet<string>>(
            StringComparer.OrdinalIgnoreCase);

        var snapshot = new List<OperationStepDependency>();

        foreach (var dependency in dependencies)
        {
            if (dependency is null)
            {
                throw new ArgumentException(
                    "Dependencies cannot contain null entries.",
                    nameof(dependencies));
            }

            if (!seen.TryGetValue(dependency.StepId, out var prerequisites))
            {
                prerequisites = new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

                seen.Add(dependency.StepId, prerequisites);
            }

            if (!prerequisites.Add(dependency.PrerequisiteStepId))
            {
                throw new ArgumentException(
                    "Duplicate directed dependency edges are not allowed.",
                    nameof(dependencies));
            }

            snapshot.Add(dependency);
        }

        Dependencies = snapshot.AsReadOnly();
    }
}
