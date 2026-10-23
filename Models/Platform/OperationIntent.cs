using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral declaration of an intended operation.
/// Does not resolve targets, authorize, plan, or execute operations.
/// </summary>
public sealed class OperationIntent
{
    public UniversalCapabilityIdentity Capability { get; }

    public string Description { get; }

    public string? Reason { get; }

    public OperationIntent(
        UniversalCapabilityIdentity capability,
        string description,
        string? reason = null)
    {
        if (string.IsNullOrWhiteSpace(capability.Value))
        {
            throw new ArgumentException(
                "A constructed capability identity is required.",
                nameof(capability));
        }

        Capability = capability;
        Description = NormalizeRequired(
            description, nameof(description));
        Reason = NormalizeOptional(reason, nameof(reason));
    }

    private static string NormalizeRequired(
        string value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A nonblank operation description is required.",
                parameterName);
        }

        return ValidateText(value.Trim(), parameterName);
    }

    private static string? NormalizeOptional(
        string? value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return ValidateText(value.Trim(), parameterName);
    }

    private static string ValidateText(
        string value,
        string parameterName)
    {
        foreach (var character in value)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "Operation text cannot contain control characters.",
                    parameterName);
            }
        }

        return value;
    }
}
