using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationRoleDefinitionTests
{
    private static OperationRoleIdentity Role(string value = "operator")
    {
        return new OperationRoleIdentity(value);
    }

    private static OperationPermissionIdentity Permission(string value)
    {
        return new OperationPermissionIdentity(value);
    }

    [Fact]
    public void DefinitionIsPublicSealedClass()
    {
        var type = typeof(OperationRoleDefinition);

        Assert.True(type.IsPublic);
        Assert.True(type.IsClass);
        Assert.True(type.IsSealed);
    }

    [Fact]
    public void HasExactlyTwoGetterOnlyProperties()
    {
        var properties = typeof(OperationRoleDefinition)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance);

        Assert.Equal(2, properties.Length);

        var role = Assert.Single(
            properties,
            property => property.Name == "Role");

        Assert.Equal(typeof(OperationRoleIdentity), role.PropertyType);
        Assert.NotNull(role.GetMethod);
        Assert.Null(role.SetMethod);

        var permissions = Assert.Single(
            properties,
            property => property.Name == "Permissions");

        Assert.Equal(
            typeof(IReadOnlyList<OperationPermissionIdentity>),
            permissions.PropertyType);
        Assert.NotNull(permissions.GetMethod);
        Assert.Null(permissions.SetMethod);
    }

    [Fact]
    public void HasExactlyOneRequiredTwoArgumentConstructor()
    {
        var constructor = Assert.Single(
            typeof(OperationRoleDefinition).GetConstructors(
                BindingFlags.Public | BindingFlags.Instance));

        var parameters = constructor.GetParameters();

        Assert.Equal(2, parameters.Length);

        Assert.Equal("role", parameters[0].Name);
        Assert.Equal(
            typeof(OperationRoleIdentity),
            parameters[0].ParameterType);

        Assert.Equal("permissions", parameters[1].Name);
        Assert.Equal(
            typeof(IEnumerable<OperationPermissionIdentity>),
            parameters[1].ParameterType);

        Assert.All(parameters,
            parameter => Assert.False(parameter.IsOptional));
    }

    [Fact]
    public void StoresRoleAndPermissionsInDeclarationOrder()
    {
        var first = Permission("resources.read");
        var second = Permission("topology.read");
        var third = Permission("operations.submit");

        var definition = new OperationRoleDefinition(
            Role(),
            new[] { first, second, third });

        Assert.Equal(Role(), definition.Role);
        Assert.Equal(
            new[] { first, second, third },
            definition.Permissions);
    }

    [Fact]
    public void AllowsExplicitlyEmptyPermissionCollection()
    {
        var definition = new OperationRoleDefinition(
            Role("platform-admin"),
            Array.Empty<OperationPermissionIdentity>());

        Assert.Equal(Role("platform-admin"), definition.Role);
        Assert.Empty(definition.Permissions);
    }

    [Fact]
    public void RejectsDefaultRoleIdentity()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationRoleDefinition(
                default(OperationRoleIdentity),
                Array.Empty<OperationPermissionIdentity>()));
    }

    [Fact]
    public void RejectsNullPermissionCollection()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationRoleDefinition(Role(), null!));
    }

    [Fact]
    public void RejectsDefaultPermissionIdentity()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationRoleDefinition(
                Role(),
                new[]
                {
                    Permission("resources.read"),
                    default(OperationPermissionIdentity)
                }));
    }

    [Fact]
    public void RejectsDuplicatePermissionIdentities()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationRoleDefinition(
                Role(),
                new[]
                {
                    Permission("resources.read"),
                    Permission("topology.read"),
                    Permission("resources.read")
                }));
    }

    [Fact]
    public void DistinctPermissionsAreNotConflated()
    {
        var definition = new OperationRoleDefinition(
            Role(),
            new[]
            {
                Permission("resources.read"),
                Permission("resources.modify")
            });

        Assert.Equal(2, definition.Permissions.Count);
        Assert.NotEqual(
            definition.Permissions[0],
            definition.Permissions[1]);
    }

    [Fact]
    public void SnapshotsMutableInputCollection()
    {
        var source = new List<OperationPermissionIdentity>
        {
            Permission("resources.read")
        };

        var definition = new OperationRoleDefinition(Role(), source);

        source.Clear();
        source.Add(Permission("resources.modify"));

        Assert.Single(definition.Permissions);
        Assert.Equal(
            Permission("resources.read"),
            definition.Permissions[0]);
    }

    [Fact]
    public void ExposedPermissionCollectionCannotBeMutated()
    {
        var definition = new OperationRoleDefinition(
            Role(),
            new[] { Permission("resources.read") });

        var list = Assert.IsAssignableFrom<
            IList<OperationPermissionIdentity>>(
                definition.Permissions);

        Assert.Throws<NotSupportedException>(() =>
            list[0] = Permission("resources.modify"));

        Assert.Throws<NotSupportedException>(() =>
            list.Add(Permission("topology.read")));

        Assert.Throws<NotSupportedException>(() =>
            list.Clear());
    }

    [Fact]
    public void DifferentRoleDefinitionsRemainIndependent()
    {
        var viewer = new OperationRoleDefinition(
            Role("viewer"),
            new[] { Permission("resources.read") });

        var operatorRole = new OperationRoleDefinition(
            Role("operator"),
            new[] { Permission("operations.submit") });

        Assert.NotEqual(viewer.Role, operatorRole.Role);
        Assert.Equal(
            Permission("resources.read"),
            Assert.Single(viewer.Permissions));
        Assert.Equal(
            Permission("operations.submit"),
            Assert.Single(operatorRole.Permissions));
    }

    [Fact]
    public void AdministratorLikeNameDoesNotInventPermissions()
    {
        var definition = new OperationRoleDefinition(
            Role("platform-admin"),
            Array.Empty<OperationPermissionIdentity>());

        Assert.Empty(definition.Permissions);
    }

    [Fact]
    public void DoesNotExposeAuthorizationOrExecutionMethods()
    {
        var methods = typeof(OperationRoleDefinition)
            .GetMethods(
                BindingFlags.Public |
                BindingFlags.Static |
                BindingFlags.Instance);

        var forbidden = new[]
        {
            "Authenticate", "Login", "Authorize",
            "IsAuthorized", "HasPermission",
            "Grant", "Deny", "Approve", "Reject",
            "Schedule", "Execute", "Dispatch",
            "Save", "Load"
        };

        Assert.DoesNotContain(methods,
            method => forbidden.Contains(method.Name));
    }
}
