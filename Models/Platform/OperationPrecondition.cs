using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral declaration of an operation prerequisite.
/// Does not evaluate resource state or determine authorization.
/// </summary>
public sealed class OperationPrecondition
{
    public string Key { get; }

    public string ExpectedValue { get; }

    public OperationPrecondition(
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
                "A nonblank precondition value is required.",
                parameterName);
        }

        var normalized = value.Trim();

        foreach (var character in normalized)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "Precondition values cannot contain control characters.",
                    parameterName);
            }
        }

        return normalized;
    }
}
