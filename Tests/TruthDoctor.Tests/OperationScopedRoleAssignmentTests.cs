using System;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationScopedRoleAssignmentTests
{
    private static OperationRoleAssignment CreateAssignment(
        OperationActorKind kind = OperationActorKind.Human,
        string actorId = "operator-1",
        string roleId = "network-operator")
    {
        return new OperationRoleAssignment(
            new OperationActorIdentity(kind, actorId),
            new OperationRoleIdentity(roleId));
    }

    private static OperationResourceScope CreateScope(
        string providerId = "aws",
        string accountId = "account-1",
        string location = "us-east-1",
        string domainId = "compute",
        string resourceType = "instance",
        string nativeId = "i-AbC123")
    {
        return new OperationResourceScope(
            new OperationTarget(
                providerId,
                accountId,
                location,
                domainId,
                resourceType,
                nativeId));
    }

    [Fact]
    public void StoresExactAssignmentAndScopeReferences()
    {
        var assignment = CreateAssignment();
        var scope = CreateScope();

        var scoped = new OperationScopedRoleAssignment(
            assignment, scope);

        Assert.Same(assignment, scoped.Assignment);
        Assert.Same(scope, scoped.Scope);
    }

    [Fact]
    public void RejectsNullAssignment()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationScopedRoleAssignment(
                null!, CreateScope()));
    }

    [Fact]
    public void RejectsNullScope()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationScopedRoleAssignment(
                CreateAssignment(), null!));
    }

    [Fact]
    public void PreservesHumanActorIdentity()
    {
        var scoped = new OperationScopedRoleAssignment(
            CreateAssignment(
                OperationActorKind.Human, "actor-A"),
            CreateScope());

        Assert.Equal(
            OperationActorKind.Human,
            scoped.Assignment.Actor.Kind);
        Assert.Equal("actor-A", scoped.Assignment.Actor.Value);
    }

    [Fact]
    public void PreservesServiceActorIdentity()
    {
        var scoped = new OperationScopedRoleAssignment(
            CreateAssignment(
                OperationActorKind.Service, "actor-A"),
            CreateScope());

        Assert.Equal(
            OperationActorKind.Service,
            scoped.Assignment.Actor.Kind);
        Assert.Equal("actor-A", scoped.Assignment.Actor.Value);
    }

    [Fact]
    public void PreservesRoleIdentity()
    {
        var scoped = new OperationScopedRoleAssignment(
            CreateAssignment(roleId: "security-operator"),
            CreateScope());

        Assert.Equal(
            new OperationRoleIdentity("security-operator"),
            scoped.Assignment.Role);
    }

    [Fact]
    public void PreservesExactScopeTarget()
    {
        var scope = CreateScope(nativeId: "Resource-AbC");

        var scoped = new OperationScopedRoleAssignment(
            CreateAssignment(), scope);

        Assert.Same(scope.Target, scoped.Scope.Target);
        Assert.True(
            scoped.Scope.Matches(
                new OperationTarget(
                    "aws", "account-1", "us-east-1",
                    "compute", "instance", "Resource-AbC")));
        Assert.False(
            scoped.Scope.Matches(
                new OperationTarget(
                    "aws", "account-1", "us-east-1",
                    "compute", "instance", "Resource-abc")));
    }

    [Fact]
    public void DifferentActorsRemainDistinct()
    {
        var first = new OperationScopedRoleAssignment(
            CreateAssignment(actorId: "actor-A"),
            CreateScope());

        var second = new OperationScopedRoleAssignment(
            CreateAssignment(actorId: "actor-B"),
            CreateScope());

        Assert.NotEqual(
            first.Assignment.Actor,
            second.Assignment.Actor);
    }

    [Fact]
    public void DifferentRolesRemainDistinct()
    {
        var first = new OperationScopedRoleAssignment(
            CreateAssignment(roleId: "viewer"),
            CreateScope());

        var second = new OperationScopedRoleAssignment(
            CreateAssignment(roleId: "operator"),
            CreateScope());

        Assert.NotEqual(
            first.Assignment.Role,
            second.Assignment.Role);
    }

    [Fact]
    public void DifferentResourceScopesRemainDistinct()
    {
        var first = new OperationScopedRoleAssignment(
            CreateAssignment(),
            CreateScope(accountId: "account-A"));

        var second = new OperationScopedRoleAssignment(
            CreateAssignment(),
            CreateScope(accountId: "account-B"));

        Assert.False(first.Scope.Matches(second.Scope.Target));
        Assert.False(second.Scope.Matches(first.Scope.Target));
    }

    [Fact]
    public void ExposesExactlyTwoReadOnlyProperties()
    {
        var type = typeof(OperationScopedRoleAssignment);

        Assert.True(type.IsSealed);

        var assignmentProperty = type.GetProperty("Assignment");
        var scopeProperty = type.GetProperty("Scope");

        Assert.NotNull(assignmentProperty);
        Assert.NotNull(scopeProperty);
        Assert.Equal(
            typeof(OperationRoleAssignment),
            assignmentProperty!.PropertyType);
        Assert.Equal(
            typeof(OperationResourceScope),
            scopeProperty!.PropertyType);
        Assert.Null(assignmentProperty.SetMethod);
        Assert.Null(scopeProperty.SetMethod);

        Assert.Equal(
            2,
            type.GetProperties(
                BindingFlags.Public | BindingFlags.Instance).Length);
    }

    [Fact]
    public void HasOneTwoArgumentConstructor()
    {
        var constructor = Assert.Single(
            typeof(OperationScopedRoleAssignment)
                .GetConstructors());

        var parameters = constructor.GetParameters();

        Assert.Equal(2, parameters.Length);
        Assert.Equal(
            typeof(OperationRoleAssignment),
            parameters[0].ParameterType);
        Assert.Equal(
            typeof(OperationResourceScope),
            parameters[1].ParameterType);
    }

    [Fact]
    public void DoesNotExposeAuthorizationOrExecutionMethods()
    {
        var methods = typeof(OperationScopedRoleAssignment)
            .GetMethods(
                BindingFlags.Instance |
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.DeclaredOnly);

        Assert.DoesNotContain(methods, method =>
            method.Name.Contains(
                "Authorize", StringComparison.OrdinalIgnoreCase) ||
            method.Name.Contains(
                "Execute", StringComparison.OrdinalIgnoreCase) ||
            method.Name.Contains(
                "Grant", StringComparison.OrdinalIgnoreCase) ||
            method.Name.Contains(
                "Approve", StringComparison.OrdinalIgnoreCase) ||
            method.Name.Contains(
                "IsAllowed", StringComparison.OrdinalIgnoreCase));
    }
}
