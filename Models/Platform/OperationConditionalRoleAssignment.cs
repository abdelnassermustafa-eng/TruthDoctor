using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral declaration associating an existing
/// operation role assignment with explicit authorization conditions.
/// Neither this association nor an empty conditions collection grants
/// effective permissions or establishes authorization.
/// This declaration does not authenticate actors, evaluate conditions,
/// inspect runtime context, approve requests, or execute operations.
/// </summary>
public sealed class OperationConditionalRoleAssignment
{
    public OperationRoleAssignment Assignment { get; }

    public OperationAuthorizationConditions Conditions { get; }

    public OperationConditionalRoleAssignment(
        OperationRoleAssignment assignment,
        OperationAuthorizationConditions conditions)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        ArgumentNullException.ThrowIfNull(conditions);

        Assignment = assignment;
        Conditions = conditions;
    }
}
