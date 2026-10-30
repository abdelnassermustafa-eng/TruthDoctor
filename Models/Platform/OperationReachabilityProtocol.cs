using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable provider-neutral check-mechanism identifier.
/// Existence of an identifier does not establish support.
/// </summary>
public sealed class OperationReachabilityProtocol
{
    public string Identifier { get; }

    public OperationReachabilityProtocol(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException(
                "A nonblank protocol identifier is required.",
                nameof(identifier));
        }

        var normalized = identifier.Trim();

        foreach (var character in normalized)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "Protocol identifiers cannot contain " +
                    "control characters.",
                    nameof(identifier));
            }
        }

        Identifier = normalized;
    }
}
