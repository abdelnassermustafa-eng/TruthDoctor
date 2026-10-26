using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral identity of a human or service actor.
/// Does not authenticate, authorize, approve, schedule, or execute.
/// </summary>
public readonly record struct OperationActorIdentity
{
    public OperationActorKind Kind { get; }

    public string Value { get; }

    public OperationActorIdentity(
        OperationActorKind kind,
        string value)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentException(
                "A defined operation actor kind is required.",
                nameof(kind));
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A nonblank operation actor identifier is required.",
                nameof(value));
        }

        var normalized = value.Trim();

        foreach (var character in normalized)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "Operation actor identifiers cannot contain " +
                    "control characters.",
                    nameof(value));
            }
        }

        Kind = kind;
        Value = normalized;
    }

    public override string ToString()
    {
        return Value ?? string.Empty;
    }
}
