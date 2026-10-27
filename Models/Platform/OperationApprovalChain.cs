using System;
using System.Collections.Generic;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral declaration of an ordered approval chain
/// associated with an existing native approval request.
/// Stage order describes the intended approval sequence; it does not
/// establish completed approvals, effective authorization, or readiness
/// to execute. This declaration does not select approvers, verify role
/// membership, enforce separation of duties, approve or deny requests,
/// schedule work, or execute operations.
/// </summary>
public sealed class OperationApprovalChain
{
    public OperationNativeApprovalRequest Request { get; }

    public IReadOnlyList<OperationApprovalStage> Stages { get; }

    public OperationApprovalChain(
        OperationNativeApprovalRequest request,
        IEnumerable<OperationApprovalStage> stages)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(stages);

        var seen = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        var snapshot = new List<OperationApprovalStage>();

        foreach (var stage in stages)
        {
            if (stage is null)
            {
                throw new ArgumentException(
                    "Approval chains cannot contain null stages.",
                    nameof(stages));
            }

            if (!seen.Add(stage.StageId))
            {
                throw new ArgumentException(
                    "Duplicate approval stage IDs are not allowed.",
                    nameof(stages));
            }

            snapshot.Add(stage);
        }

        if (snapshot.Count == 0)
        {
            throw new ArgumentException(
                "At least one approval stage is required.",
                nameof(stages));
        }

        Request = request;
        Stages = snapshot.AsReadOnly();
    }
}
