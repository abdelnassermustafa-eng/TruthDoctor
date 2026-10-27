using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral declaration of one approval stage.
/// The stage identifies an approver role requirement, not an assigned
/// or authenticated approver. It does not verify role membership,
/// approve or deny requests, authorize actions, enforce separation
/// of duties, schedule work, or execute operations.
/// </summary>
public sealed class OperationApprovalStage
{
    public string StageId { get; }

    public OperationRoleIdentity ApproverRole { get; }

    public OperationApprovalStage(
        string stageId,
        OperationRoleIdentity approverRole)
    {
        if (string.IsNullOrWhiteSpace(stageId))
        {
            throw new ArgumentException(
                "A nonblank approval stage ID is required.",
                nameof(stageId));
        }

        var normalizedStageId = stageId.Trim();

        foreach (var character in normalizedStageId)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "Approval stage IDs cannot contain control characters.",
                    nameof(stageId));
            }
        }

        if (string.IsNullOrEmpty(approverRole.Value))
        {
            throw new ArgumentException(
                "An initialized approver role identity is required.",
                nameof(approverRole));
        }

        StageId = normalizedStageId;
        ApproverRole = approverRole;
    }
}
