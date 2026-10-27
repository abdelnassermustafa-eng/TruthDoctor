using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral declaration of an explicitly supplied
/// authorization condition key and expected value.
/// This declaration does not evaluate a condition, inspect runtime
/// context, authenticate or authorize an actor, grant permissions,
/// approve requests, establish provider support, or execute operations.
/// </summary>
public sealed class OperationAuthorizationCondition
{
    public string Key { get; }

    public string ExpectedValue { get; }

    public OperationAuthorizationCondition(
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
                "A nonblank authorization condition value is required.",
                parameterName);
        }

        var normalized = value.Trim();

        foreach (var character in normalized)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "Authorization condition values cannot contain " +
                    "control characters.",
                    parameterName);
            }
        }

        return normalized;
    }
}
