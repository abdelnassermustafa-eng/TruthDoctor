using System;
using System.Collections.Generic;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral declaration of the permissions assigned
/// to a role definition. This declaration does not authenticate or
/// authorize an actor, grant access, approve requests, establish
/// provider support, or execute operations.
/// </summary>
public sealed class OperationRoleDefinition
{
    public OperationRoleIdentity Role { get; }

    public IReadOnlyList<OperationPermissionIdentity> Permissions { get; }

    public OperationRoleDefinition(
        OperationRoleIdentity role,
        IEnumerable<OperationPermissionIdentity> permissions)
    {
        if (string.IsNullOrEmpty(role.Value))
        {
            throw new ArgumentException(
                "An initialized operation role identity is required.",
                nameof(role));
        }

        ArgumentNullException.ThrowIfNull(permissions);

        var seen = new HashSet<OperationPermissionIdentity>();
        var snapshot = new List<OperationPermissionIdentity>();

        foreach (var permission in permissions)
        {
            if (string.IsNullOrEmpty(permission.Value))
            {
                throw new ArgumentException(
                    "Permission collections cannot contain " +
                    "uninitialized permission identities.",
                    nameof(permissions));
            }

            if (!seen.Add(permission))
            {
                throw new ArgumentException(
                    "Duplicate permission identities are not allowed.",
                    nameof(permissions));
            }

            snapshot.Add(permission);
        }

        Role = role;
        Permissions = snapshot.AsReadOnly();
    }
}
