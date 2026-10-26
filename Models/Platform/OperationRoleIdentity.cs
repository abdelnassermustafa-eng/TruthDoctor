using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral identity of an operation role.
/// A role name alone does not grant permissions, establish
/// administrative privileges, authorize an actor, approve
/// a request, or permit operation execution.
/// </summary>
public readonly record struct OperationRoleIdentity
{
    public string Value { get; }

    public OperationRoleIdentity(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (!IsValid(value))
        {
            throw new ArgumentException(
                "Role identity must start with a lowercase letter " +
                "and contain only lowercase letters, ASCII digits, " +
                "or single internal hyphens. Wildcards, uppercase " +
                "letters, whitespace, and other separators are not permitted.",
                nameof(value));
        }

        Value = value;
    }

    public override string ToString()
    {
        return Value ?? string.Empty;
    }

    private static bool IsValid(string value)
    {
        if (value.Length == 0 || !IsLowercaseLetter(value[0]))
        {
            return false;
        }

        var previousWasHyphen = false;

        foreach (var character in value)
        {
            if (character == '-')
            {
                if (previousWasHyphen)
                {
                    return false;
                }

                previousWasHyphen = true;
                continue;
            }

            if (!IsLowercaseLetter(character) &&
                !IsAsciiDigit(character))
            {
                return false;
            }

            previousWasHyphen = false;
        }

        return !previousWasHyphen;
    }

    private static bool IsLowercaseLetter(char character)
    {
        return character >= 'a' && character <= 'z';
    }

    private static bool IsAsciiDigit(char character)
    {
        return character >= '0' && character <= '9';
    }
}
