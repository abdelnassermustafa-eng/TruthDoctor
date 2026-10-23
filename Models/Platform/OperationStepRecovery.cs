using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral recovery declaration for one planned step.
/// Does not authorize, schedule, or execute rollback or compensation.
/// </summary>
public sealed class OperationStepRecovery
{
    public string StepId { get; }

    public OperationRecoveryStatus Status { get; }

    public OperationRecoveryKind Kind { get; }

    public UniversalCapabilityIdentity? RecoveryCapability { get; }

    public string? Limitations { get; }

    public OperationStepRecovery(
        string stepId,
        OperationRecoveryStatus status,
        OperationRecoveryKind kind,
        UniversalCapabilityIdentity? recoveryCapability = null,
        string? limitations = null)
    {
        StepId = NormalizeRequired(stepId, nameof(stepId));

        if (status != OperationRecoveryStatus.Unknown &&
            status != OperationRecoveryStatus.Unsupported &&
            status != OperationRecoveryStatus.Planned)
        {
            throw new ArgumentOutOfRangeException(
                nameof(status), status,
                "Unknown operation recovery status.");
        }

        if (kind != OperationRecoveryKind.None &&
            kind != OperationRecoveryKind.Rollback &&
            kind != OperationRecoveryKind.Compensation)
        {
            throw new ArgumentOutOfRangeException(
                nameof(kind), kind,
                "Unknown operation recovery kind.");
        }

        if (status == OperationRecoveryStatus.Planned)
        {
            if (kind == OperationRecoveryKind.None)
            {
                throw new ArgumentException(
                    "A planned recovery requires rollback or compensation.",
                    nameof(kind));
            }

            if (!recoveryCapability.HasValue ||
                string.IsNullOrWhiteSpace(recoveryCapability.Value.Value))
            {
                throw new ArgumentException(
                    "A planned recovery requires a constructed capability identity.",
                    nameof(recoveryCapability));
            }
        }
        else
        {
            if (kind != OperationRecoveryKind.None)
            {
                throw new ArgumentException(
                    "An unplanned recovery must have kind None.",
                    nameof(kind));
            }

            if (recoveryCapability.HasValue)
            {
                throw new ArgumentException(
                    "Only a planned recovery may declare a recovery capability.",
                    nameof(recoveryCapability));
            }
        }

        Status = status;
        Kind = kind;
        RecoveryCapability = recoveryCapability;
        Limitations = NormalizeOptional(limitations, nameof(limitations));
    }

    private static string NormalizeRequired(
        string value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A nonblank recovery step ID is required.",
                parameterName);
        }

        var normalized = value.Trim();

        foreach (var character in normalized)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "Recovery step IDs cannot contain control characters.",
                    parameterName);
            }
        }

        return normalized;
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
                    "Recovery limitations cannot contain control characters.",
                    parameterName);
            }
        }

        return normalized;
    }
}
