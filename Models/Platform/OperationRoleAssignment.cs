using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral declaration associating an operation
/// actor identity with an operation role identity.
/// This assignment does not authenticate the actor, verify that a role
/// definition exists, grant effective permissions, authorize actions,
/// approve requests, or execute operations.
/// </summary>
public sealed class OperationRoleAssignment
{
    public OperationActorIdentity Actor { get; }

    public OperationRoleIdentity Role { get; }

    public OperationRoleAssignment(
        OperationActorIdentity actor,
        OperationRoleIdentity role)
    {
        if (string.IsNullOrEmpty(actor.Value) ||
            !Enum.IsDefined(actor.Kind))
        {
            throw new ArgumentException(
                "An initialized operation actor identity is required.",
                nameof(actor));
        }

        if (string.IsNullOrEmpty(role.Value))
        {
            throw new ArgumentException(
                "An initialized operation role identity is required.",
                nameof(role));
        }

        Actor = actor;
        Role = role;
    }
}
