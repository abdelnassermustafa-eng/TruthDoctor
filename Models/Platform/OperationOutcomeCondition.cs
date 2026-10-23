using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral declaration of an expected outcome condition.
/// Does not inspect resource state or establish successful verification.
/// </summary>
public sealed class OperationOutcomeCondition
{
    public string Key { get; }

    public string ExpectedValue { get; }

    public OperationOutcomeCondition(
        string key,
        string expectedValue)
    {
        Key = NormalizeRequired(key, nameof(key));
        ExpectedValue = NormalizeRequired(
            expectedValue, nameof(expectedValue));
    }

    private static string NormalizeRequired(
        string value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A nonblank outcome condition value is required.",
                parameterName);
        }

        var normalized = value.Trim();

        foreach (var character in normalized)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "Outcome condition values cannot contain " +
                    "control characters.",
                    parameterName);
            }
        }

        return normalized;
    }
}
