using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral identifier for correlating declarations
/// belonging to one logical operation. Does not generate, propagate,
/// authorize, schedule, or execute an operation.
/// </summary>
public readonly record struct OperationCorrelationId
{
    public string Value { get; }

    public OperationCorrelationId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A nonblank operation correlation ID is required.",
                nameof(value));
        }

        var normalized = value.Trim();

        foreach (var character in normalized)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "Operation correlation IDs cannot contain control characters.",
                    nameof(value));
            }
        }

        Value = normalized;
    }

    public override string ToString()
    {
        return Value ?? string.Empty;
    }
}
