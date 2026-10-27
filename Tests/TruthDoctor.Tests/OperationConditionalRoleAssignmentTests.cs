using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationConditionalRoleAssignmentTests
{
    private static OperationRoleAssignment Assignment(
        string role = "operator")
    {
        return new OperationRoleAssignment(
            new OperationActorIdentity(
                OperationActorKind.Human,
                "test-actor"),
            new OperationRoleIdentity(role));
    }

    private static OperationAuthorizationConditions Conditions()
    {
        return new OperationAuthorizationConditions(
            new[]
            {
                new OperationAuthorizationCondition(
                    "environment.name", "Production")
            });
    }

    [Fact]
    public void PreservesExactAssignmentReference()
    {
        var assignment = Assignment();
        var conditions = Conditions();

        var conditional = new OperationConditionalRoleAssignment(
            assignment, conditions);

        Assert.Same(assignment, conditional.Assignment);
    }

    [Fact]
    public void PreservesExactConditionsReference()
    {
        var assignment = Assignment();
        var conditions = Conditions();

        var conditional = new OperationConditionalRoleAssignment(
            assignment, conditions);

        Assert.Same(conditions, conditional.Conditions);
    }

    [Fact]
    public void RejectsNullAssignment()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationConditionalRoleAssignment(
                null!, Conditions()));
    }

    [Fact]
    public void RejectsNullConditions()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationConditionalRoleAssignment(
                Assignment(), null!));
    }

    [Fact]
    public void AllowsEmptyConditionsWithoutImplyingAuthorization()
    {
        var empty = new OperationAuthorizationConditions(
            Array.Empty<OperationAuthorizationCondition>());

        var conditional = new OperationConditionalRoleAssignment(
            Assignment(), empty);

        Assert.Same(empty, conditional.Conditions);
        Assert.Empty(conditional.Conditions.Conditions);
    }

    [Fact]
    public void PreservesConditionDeclarationReferences()
    {
        var condition = new OperationAuthorizationCondition(
            "risk.level", "R3");

        var conditions = new OperationAuthorizationConditions(
            new[] { condition });

        var conditional = new OperationConditionalRoleAssignment(
            Assignment(), conditions);

        Assert.Same(
            condition,
            Assert.Single(conditional.Conditions.Conditions));
    }

    [Fact]
    public void DifferentAssignmentsRemainDistinct()
    {
        var conditions = Conditions();

        var first = new OperationConditionalRoleAssignment(
            Assignment("viewer"), conditions);

        var second = new OperationConditionalRoleAssignment(
            Assignment("operator"), conditions);

        Assert.NotSame(first.Assignment, second.Assignment);
        Assert.Same(conditions, first.Conditions);
        Assert.Same(conditions, second.Conditions);
    }

    [Fact]
    public void DifferentConditionsRemainDistinct()
    {
        var assignment = Assignment();

        var firstConditions = Conditions();
        var secondConditions = Conditions();

        var first = new OperationConditionalRoleAssignment(
            assignment, firstConditions);

        var second = new OperationConditionalRoleAssignment(
            assignment, secondConditions);

        Assert.Same(assignment, first.Assignment);
        Assert.Same(assignment, second.Assignment);
        Assert.NotSame(first.Conditions, second.Conditions);
    }

    [Fact]
    public void ExposesExactlyTwoGetterOnlyProperties()
    {
        var type = typeof(OperationConditionalRoleAssignment);

        Assert.True(type.IsPublic);
        Assert.True(type.IsSealed);

        var properties = type.GetProperties(
            BindingFlags.Public |
            BindingFlags.Instance |
            BindingFlags.DeclaredOnly);

        Assert.Equal(2, properties.Length);

        var assignment = Assert.Single(
            properties, p => p.Name == "Assignment");

        var conditions = Assert.Single(
            properties, p => p.Name == "Conditions");

        Assert.Equal(
            typeof(OperationRoleAssignment),
            assignment.PropertyType);

        Assert.Equal(
            typeof(OperationAuthorizationConditions),
            conditions.PropertyType);

        Assert.True(assignment.CanRead);
        Assert.False(assignment.CanWrite);
        Assert.True(conditions.CanRead);
        Assert.False(conditions.CanWrite);
    }

    [Fact]
    public void HasOneTwoArgumentConstructor()
    {
        var constructors = typeof(OperationConditionalRoleAssignment)
            .GetConstructors(
                BindingFlags.Public |
                BindingFlags.Instance);

        var constructor = Assert.Single(constructors);
        var parameters = constructor.GetParameters();

        Assert.Equal(2, parameters.Length);
        Assert.Equal("assignment", parameters[0].Name);
        Assert.Equal(
            typeof(OperationRoleAssignment),
            parameters[0].ParameterType);
        Assert.Equal("conditions", parameters[1].Name);
        Assert.Equal(
            typeof(OperationAuthorizationConditions),
            parameters[1].ParameterType);
    }

    [Fact]
    public void DoesNotExposeAuthorizationOrExecutionMethods()
    {
        var methods = typeof(OperationConditionalRoleAssignment)
            .GetMethods(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName);

        Assert.Empty(methods);
    }
}
