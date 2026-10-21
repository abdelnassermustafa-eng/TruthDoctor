using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// A canonical, provider-neutral operational capability identity.
/// Identity alone does not establish provider support or execution.
/// </summary>
public readonly record struct UniversalCapabilityIdentity
{
    public string Value { get; }

    public UniversalCapabilityIdentity(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (!IsValid(value))
        {
            throw new ArgumentException(
                "Capability identity must contain at least two " +
                "lowercase dot-separated segments. Each segment " +
                "must begin with a letter and may contain lowercase " +
                "letters, digits, or internal hyphens.",
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
        var segments = value.Split('.');

        if (segments.Length < 2)
        {
            return false;
        }

        foreach (var segment in segments)
        {
            if (segment.Length == 0 ||
                !IsLowercaseLetter(segment[0]) ||
                segment[^1] == '-')
            {
                return false;
            }

            foreach (var character in segment)
            {
                if (!IsLowercaseLetter(character) &&
                    !IsAsciiDigit(character) &&
                    character != '-')
                {
                    return false;
                }
            }
        }

        return true;
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
