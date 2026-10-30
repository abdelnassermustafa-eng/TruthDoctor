using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable provider-neutral service-check mechanism identifier.
/// Declaration of a mechanism does not establish its support.
/// </summary>
public sealed class OperationServiceProtocol
{
    public string Identifier { get; }

    public OperationServiceProtocol(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException(
                "A nonblank service mechanism is required.",
                nameof(identifier));
        }

        var normalized = identifier.Trim();

        foreach (var character in normalized)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "Service mechanism cannot contain control characters.",
                    nameof(identifier));
            }
        }

        Identifier = normalized;
    }
}
