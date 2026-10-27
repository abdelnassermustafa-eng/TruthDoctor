using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral declaration requesting native approval
/// for one logical operation from an explicitly identified actor.
/// Creating this declaration does not approve a request, establish
/// effective authorization, evaluate policy, schedule, or execute.
/// Approval decisions and approval chains are separate contracts.
/// </summary>
public sealed class OperationNativeApprovalRequest
{
    public OperationCorrelationId Operation { get; }

    public OperationActorIdentity Requester { get; }

    public string Reason { get; }

    public OperationNativeApprovalRequest(
        OperationCorrelationId operation,
        OperationActorIdentity requester,
        string reason)
    {
        if (string.IsNullOrEmpty(operation.Value))
        {
            throw new ArgumentException(
                "An initialized operation correlation ID is required.",
                nameof(operation));
        }

        if (string.IsNullOrEmpty(requester.Value) ||
            !Enum.IsDefined(requester.Kind))
        {
            throw new ArgumentException(
                "An initialized operation requester identity is required.",
                nameof(requester));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException(
                "A nonblank approval request reason is required.",
                nameof(reason));
        }

        var normalizedReason = reason.Trim();

        foreach (var character in normalizedReason)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "Approval request reasons cannot contain control characters.",
                    nameof(reason));
            }
        }

        Operation = operation;
        Requester = requester;
        Reason = normalizedReason;
    }
}
