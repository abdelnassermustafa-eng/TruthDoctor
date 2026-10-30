using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable caller-declared network vantage-point context.
/// These identifiers do not prove actual device configuration.
/// </summary>
public sealed class OperationReachabilityContext
{
    public string? InterfaceId { get; }

    public string? Vrf { get; }

    public int? VlanId { get; }

    public string? AddressFamily { get; }

    public OperationReachabilityContext(
        string? interfaceId = null,
        string? vrf = null,
        int? vlanId = null,
        string? addressFamily = null)
    {
        if (vlanId.HasValue &&
            (vlanId.Value < 1 || vlanId.Value > 4094))
        {
            throw new ArgumentOutOfRangeException(
                nameof(vlanId),
                vlanId,
                "VLAN identifier must be between 1 and 4094.");
        }

        InterfaceId = NormalizeOptional(
            interfaceId, nameof(interfaceId));

        Vrf = NormalizeOptional(vrf, nameof(vrf));
        VlanId = vlanId;

        AddressFamily = NormalizeOptional(
            addressFamily, nameof(addressFamily));
    }

    private static string? NormalizeOptional(
        string? value,
        string parameterName)
    {
        if (value is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A supplied network context identifier " +
                "cannot be blank.",
                parameterName);
        }

        var normalized = value.Trim();

        foreach (var character in normalized)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "Network context identifiers cannot " +
                    "contain control characters.",
                    parameterName);
            }
        }

        return normalized;
    }
}
