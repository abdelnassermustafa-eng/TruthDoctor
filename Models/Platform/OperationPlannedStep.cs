using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral declaration of one planned operation step.
/// Does not authorize, schedule, or execute the declared action.
/// </summary>
public sealed class OperationPlannedStep
{
    public string StepId { get; }

    public UniversalCapabilityIdentity Capability { get; }

    public string Description { get; }

    public OperationTarget? Target { get; }

    public OperationPlannedStep(
        string stepId,
        UniversalCapabilityIdentity capability,
        string description,
        OperationTarget? target = null)
    {
        if (string.IsNullOrWhiteSpace(capability.Value))
        {
            throw new ArgumentException(
                "A constructed capability identity is required.",
                nameof(capability));
        }

        StepId = NormalizeRequired(stepId, nameof(stepId));
        Capability = capability;
        Description = NormalizeRequired(
            description, nameof(description));
        Target = target;
    }

    private static string NormalizeRequired(
        string value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A nonblank planned step value is required.",
                parameterName);
        }

        var normalized = value.Trim();

        foreach (var character in normalized)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "Planned step values cannot contain control characters.",
                    parameterName);
            }
        }

        return normalized;
    }
}
