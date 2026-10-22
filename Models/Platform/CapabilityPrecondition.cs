using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// An immutable, symbolic requirement for a capability.
/// This declaration does not evaluate runtime resource state.
/// </summary>
public sealed class CapabilityPrecondition
{
    public string Key { get; }

    public string ExpectedValue { get; }

    public CapabilityPrecondition(
        string key,
        string expectedValue)
    {
        Key = Normalize(key, nameof(key));
        ExpectedValue = Normalize(
            expectedValue, nameof(expectedValue));
    }

    private static string Normalize(
        string value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A nonblank symbolic value is required.",
                parameterName);
        }

        var normalized = value.Trim();

        foreach (var character in normalized)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "Symbolic values cannot contain control characters.",
                    parameterName);
            }
        }

        return normalized;
    }
}
