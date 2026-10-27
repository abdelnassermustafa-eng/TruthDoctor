using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral declaration associating an existing
/// operation role assignment with an exact operation resource scope.
/// This declaration does not authenticate an actor, verify that a role
/// definition exists, grant effective permissions, authorize actions,
/// evaluate policies, approve requests, or execute operations.
/// </summary>
public sealed class OperationScopedRoleAssignment
{
    public OperationRoleAssignment Assignment { get; }

    public OperationResourceScope Scope { get; }

    public OperationScopedRoleAssignment(
        OperationRoleAssignment assignment,
        OperationResourceScope scope)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        ArgumentNullException.ThrowIfNull(scope);

        Assignment = assignment;
        Scope = scope;
    }
}
