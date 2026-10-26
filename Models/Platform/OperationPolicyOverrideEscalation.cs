using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral declaration of policy override and escalation
/// requests. Values are explicitly supplied, not inferred or evaluated.
/// Does not grant exceptions, approve, authorize, schedule, or execute.
/// Does not calculate or modify any operation risk classification.
/// </summary>
public sealed class OperationPolicyOverrideEscalation
{
    public string? OverridePolicyId { get; }

    public string? OverrideRequestReason { get; }

    public string? EscalationRequestReason { get; }

    public OperationPolicyOverrideEscalation(
        string? overridePolicyId = null,
        string? overrideRequestReason = null,
        string? escalationRequestReason = null)
    {
        OverridePolicyId = NormalizeOptional(
            overridePolicyId, nameof(overridePolicyId));

        OverrideRequestReason = NormalizeOptional(
            overrideRequestReason, nameof(overrideRequestReason));

        EscalationRequestReason = NormalizeOptional(
            escalationRequestReason, nameof(escalationRequestReason));
    }

    private static string? NormalizeOptional(
        string? value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();

        foreach (var character in normalized)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "Policy declaration text cannot contain control characters.",
                    parameterName);
            }
        }

        return normalized;
    }
}
