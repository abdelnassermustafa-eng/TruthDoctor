using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable declared reachability check specification.
/// Only ICMP echo and TCP connect have initial comparison semantics.
/// Does not perform DNS lookup, probe execution, or provider access.
/// </summary>
public sealed class OperationReachabilityCheckSpecification
{
    public string Destination { get; }

    public OperationReachabilityProtocol Protocol { get; }

    public OperationReachabilityContext Context { get; }

    public int? DestinationPort { get; }

    public OperationReachabilityCheckSpecification(
        string destination,
        OperationReachabilityProtocol protocol,
        OperationReachabilityContext context,
        int? destinationPort = null)
    {
        ArgumentNullException.ThrowIfNull(protocol);
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrWhiteSpace(destination))
        {
            throw new ArgumentException(
                "A nonblank destination is required.",
                nameof(destination));
        }

        var normalizedDestination = destination.Trim();

        foreach (var character in normalizedDestination)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "Destination cannot contain control characters.",
                    nameof(destination));
            }
        }

        if (destinationPort.HasValue &&
            (destinationPort.Value < 1 ||
             destinationPort.Value > 65535))
        {
            throw new ArgumentOutOfRangeException(
                nameof(destinationPort),
                destinationPort,
                "A supplied port must be between 1 and 65535.");
        }

        if (string.Equals(
            protocol.Identifier,
            "tcp.connect",
            StringComparison.OrdinalIgnoreCase))
        {
            if (!destinationPort.HasValue)
            {
                throw new ArgumentException(
                    "TCP connect requires a destination port.",
                    nameof(destinationPort));
            }
        }
        else if (string.Equals(
            protocol.Identifier,
            "icmp.echo",
            StringComparison.OrdinalIgnoreCase))
        {
            if (destinationPort.HasValue)
            {
                throw new ArgumentException(
                    "ICMP echo does not use a destination port.",
                    nameof(destinationPort));
            }
        }

        // Unknown protocol identifiers are permitted as declarations.
        // They cannot produce a matched comparison until explicitly
        // supported. Their other parameter semantics are not inferred.

        Destination = normalizedDestination;
        Protocol = protocol;
        Context = context;
        DestinationPort = destinationPort;
    }
}
