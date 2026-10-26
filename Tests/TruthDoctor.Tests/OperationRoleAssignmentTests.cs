using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationRoleAssignmentTests
{
    private static OperationActorIdentity Actor(
        OperationActorKind kind = OperationActorKind.Human,
        string value = "actor-123")
    {
        return new OperationActorIdentity(kind, value);
    }

    private static OperationRoleIdentity Role(string value = "operator")
    {
        return new OperationRoleIdentity(value);
    }

    [Fact]
    public void AssignmentIsPublicSealedClass()
    {
        var type = typeof(OperationRoleAssignment);

        Assert.True(type.IsPublic);
        Assert.True(type.IsClass);
        Assert.True(type.IsSealed);
    }

    [Fact]
    public void HasExactlyTwoGetterOnlyProperties()
    {
        var properties = typeof(OperationRoleAssignment)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance);

        Assert.Equal(2, properties.Length);

        var actor = Assert.Single(
            properties,
            property => property.Name == "Actor");

        Assert.Equal(typeof(OperationActorIdentity), actor.PropertyType);
        Assert.NotNull(actor.GetMethod);
        Assert.Null(actor.SetMethod);

        var role = Assert.Single(
            properties,
            property => property.Name == "Role");

        Assert.Equal(typeof(OperationRoleIdentity), role.PropertyType);
        Assert.NotNull(role.GetMethod);
        Assert.Null(role.SetMethod);
    }

    [Fact]
    public void HasExactlyOneRequiredTwoArgumentConstructor()
    {
        var constructor = Assert.Single(
            typeof(OperationRoleAssignment).GetConstructors(
                BindingFlags.Public | BindingFlags.Instance));

        var parameters = constructor.GetParameters();

        Assert.Equal(2, parameters.Length);

        Assert.Equal("actor", parameters[0].Name);
        Assert.Equal(
            typeof(OperationActorIdentity),
            parameters[0].ParameterType);

        Assert.Equal("role", parameters[1].Name);
        Assert.Equal(
            typeof(OperationRoleIdentity),
            parameters[1].ParameterType);

        Assert.All(parameters,
            parameter => Assert.False(parameter.IsOptional));
    }

    [Fact]
    public void PreservesHumanActorAndRole()
    {
        var actor = Actor(OperationActorKind.Human, "user-123");
        var role = Role("viewer");

        var assignment = new OperationRoleAssignment(actor, role);

        Assert.Equal(actor, assignment.Actor);
        Assert.Equal(role, assignment.Role);
        Assert.Equal(OperationActorKind.Human, assignment.Actor.Kind);
    }

    [Fact]
    public void PreservesServiceActorAndRole()
    {
        var actor = Actor(
            OperationActorKind.Service,
            "automation-agent-7");

        var role = Role("operator");

        var assignment = new OperationRoleAssignment(actor, role);

        Assert.Equal(actor, assignment.Actor);
        Assert.Equal(role, assignment.Role);
        Assert.Equal(OperationActorKind.Service, assignment.Actor.Kind);
    }

    [Fact]
    public void PreservesActorIdentityNormalization()
    {
        var actor = Actor(
            OperationActorKind.Human,
            "  User-ABC-123  ");

        var assignment = new OperationRoleAssignment(
            actor,
            Role());

        Assert.Equal("User-ABC-123", assignment.Actor.Value);
    }

    [Fact]
    public void RejectsDefaultActorIdentity()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationRoleAssignment(
                default(OperationActorIdentity),
                Role()));
    }

    [Fact]
    public void RejectsDefaultRoleIdentity()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationRoleAssignment(
                Actor(),
                default(OperationRoleIdentity)));
    }

    [Fact]
    public void RejectsBothDefaultIdentities()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationRoleAssignment(
                default(OperationActorIdentity),
                default(OperationRoleIdentity)));
    }

    [Fact]
    public void HumanAndServiceActorsRemainDistinct()
    {
        var human = new OperationRoleAssignment(
            Actor(OperationActorKind.Human, "shared-id"),
            Role());

        var service = new OperationRoleAssignment(
            Actor(OperationActorKind.Service, "shared-id"),
            Role());

        Assert.NotEqual(human.Actor, service.Actor);
        Assert.Equal(human.Role, service.Role);
    }

    [Fact]
    public void ActorIdentifierCaseRemainsSignificant()
    {
        var first = new OperationRoleAssignment(
            Actor(OperationActorKind.Human, "Actor-123"),
            Role());

        var second = new OperationRoleAssignment(
            Actor(OperationActorKind.Human, "actor-123"),
            Role());

        Assert.NotEqual(first.Actor, second.Actor);
    }

    [Fact]
    public void DifferentRolesRemainDistinct()
    {
        var viewer = new OperationRoleAssignment(
            Actor(),
            Role("viewer"));

        var operatorRole = new OperationRoleAssignment(
            Actor(),
            Role("operator"));

        Assert.Equal(viewer.Actor, operatorRole.Actor);
        Assert.NotEqual(viewer.Role, operatorRole.Role);
    }

    [Fact]
    public void AdministratorLikeRoleDoesNotExposePrivileges()
    {
        var assignment = new OperationRoleAssignment(
            Actor(),
            Role("platform-admin"));

        Assert.Equal("platform-admin", assignment.Role.Value);

        var forbidden = new[]
        {
            "Permissions", "Privileges", "Grants",
            "IsAdmin", "IsAdministrator",
            "IsAuthorized", "CanExecute"
        };

        var properties = typeof(OperationRoleAssignment)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance);

        Assert.DoesNotContain(
            properties,
            property => forbidden.Contains(property.Name));
    }

    [Fact]
    public void DoesNotExposeAuthorizationOrExecutionMethods()
    {
        var methods = typeof(OperationRoleAssignment)
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

        Assert.DoesNotContain(
            methods,
            method => forbidden.Contains(method.Name));
    }
}
