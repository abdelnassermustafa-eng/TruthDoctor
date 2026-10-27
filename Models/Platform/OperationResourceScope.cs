using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral declaration of an exact operation
/// resource scope. A matching scope does not grant permissions,
/// authenticate or authorize an actor, approve a request,
/// establish provider support, or execute operations.
/// </summary>
public sealed class OperationResourceScope
{
    public OperationTarget Target { get; }

    public OperationResourceScope(OperationTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);

        Target = target;
    }

    /// <summary>
    /// Determines whether a candidate has exactly the same six
    /// resource identity fields as the declared scope target.
    /// This is identity matching, not an authorization decision.
    /// </summary>
    public bool Matches(OperationTarget? candidate)
    {
        if (candidate is null)
        {
            return false;
        }

        return
            string.Equals(
                Target.ProviderId,
                candidate.ProviderId,
                StringComparison.Ordinal) &&
            string.Equals(
                Target.AccountId,
                candidate.AccountId,
                StringComparison.Ordinal) &&
            string.Equals(
                Target.Location,
                candidate.Location,
                StringComparison.Ordinal) &&
            string.Equals(
                Target.DomainId,
                candidate.DomainId,
                StringComparison.Ordinal) &&
            string.Equals(
                Target.ResourceType,
                candidate.ResourceType,
                StringComparison.Ordinal) &&
            string.Equals(
                Target.NativeId,
                candidate.NativeId,
                StringComparison.Ordinal);
    }
}
