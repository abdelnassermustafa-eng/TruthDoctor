using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationAuthorizationConditionsTests
{
    private static OperationAuthorizationCondition Condition(
        string key,
        string expectedValue)
    {
        return new OperationAuthorizationCondition(key, expectedValue);
    }

    [Fact]
    public void StoresConditionsInDeclaredOrder()
    {
        var first = Condition("environment.name", "Production");
        var second = Condition("risk.level", "R3");

        var conditions = new OperationAuthorizationConditions(
            new[] { first, second });

        Assert.Equal(2, conditions.Conditions.Count);
        Assert.Same(first, conditions.Conditions[0]);
        Assert.Same(second, conditions.Conditions[1]);
    }

    [Fact]
    public void RejectsNullCollection()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationAuthorizationConditions(null!));
    }

    [Fact]
    public void AllowsEmptyCollectionWithoutImplyingAuthorization()
    {
        var conditions = new OperationAuthorizationConditions(
            Array.Empty<OperationAuthorizationCondition>());

        Assert.Empty(conditions.Conditions);
    }

    [Fact]
    public void RejectsNullConditionEntry()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationAuthorizationConditions(
                new OperationAuthorizationCondition[]
                {
                    Condition("environment.name", "Production"),
                    null!
                }));
    }

    [Fact]
    public void RejectsDuplicateKeysIgnoringCase()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationAuthorizationConditions(
                new[]
                {
                    Condition("environment.name", "Production"),
                    Condition("ENVIRONMENT.NAME", "Staging")
                }));
    }

    [Fact]
    public void RejectsDuplicateKeysAfterTrimming()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationAuthorizationConditions(
                new[]
                {
                    Condition(" environment.name ", "Production"),
                    Condition("environment.name", "Staging")
                }));
    }

    [Fact]
    public void AllowsDistinctKeysWithSameExpectedValue()
    {
        var conditions = new OperationAuthorizationConditions(
            new[]
            {
                Condition("environment.name", "Production"),
                Condition("environment.tier", "Production")
            });

        Assert.Equal(2, conditions.Conditions.Count);
    }

    [Fact]
    public void PreservesOriginalKeysAndExpectedValues()
    {
        var condition = Condition(
            "Environment.Name",
            "ProDUCTION");

        var conditions = new OperationAuthorizationConditions(
            new[] { condition });

        Assert.Equal(
            "Environment.Name",
            conditions.Conditions[0].Key);

        Assert.Equal(
            "ProDUCTION",
            conditions.Conditions[0].ExpectedValue);
    }

    [Fact]
    public void SnapshotsMutableInputCollection()
    {
        var first = Condition("environment.name", "Production");
        var second = Condition("risk.level", "R3");

        var input = new List<OperationAuthorizationCondition>
        {
            first
        };

        var conditions = new OperationAuthorizationConditions(input);

        input.Add(second);
        input.Clear();

        Assert.Single(conditions.Conditions);
        Assert.Same(first, conditions.Conditions[0]);
    }

    [Fact]
    public void ExposesReadOnlyCollection()
    {
        var conditions = new OperationAuthorizationConditions(
            new[]
            {
                Condition("environment.name", "Production")
            });

        var list = Assert.IsAssignableFrom<
            IList<OperationAuthorizationCondition>>(
                conditions.Conditions);

        Assert.True(list.IsReadOnly);

        Assert.Throws<NotSupportedException>(() =>
            list.Add(Condition("risk.level", "R3")));

        Assert.Throws<NotSupportedException>(() =>
            list[0] = Condition("environment.name", "Staging"));
    }

    [Fact]
    public void PreservesIndependentConditionObjects()
    {
        var first = Condition("environment.name", "Production");
        var second = Condition("environment.name", "Production");

        var firstCollection = new OperationAuthorizationConditions(
            new[] { first });

        var secondCollection = new OperationAuthorizationConditions(
            new[] { second });

        Assert.Same(first, firstCollection.Conditions[0]);
        Assert.Same(second, secondCollection.Conditions[0]);
        Assert.NotSame(
            firstCollection.Conditions[0],
            secondCollection.Conditions[0]);
    }

    [Fact]
    public void ExposesExactlyOneGetterOnlyProperty()
    {
        var type = typeof(OperationAuthorizationConditions);

        Assert.True(type.IsPublic);
        Assert.True(type.IsSealed);

        var properties = type.GetProperties(
            BindingFlags.Public | BindingFlags.Instance);

        var property = Assert.Single(properties);

        Assert.Equal("Conditions", property.Name);
        Assert.Equal(
            typeof(IReadOnlyList<OperationAuthorizationCondition>),
            property.PropertyType);

        Assert.True(property.CanRead);
        Assert.False(property.CanWrite);
    }

    [Fact]
    public void HasOneEnumerableConstructor()
    {
        var constructors = typeof(OperationAuthorizationConditions)
            .GetConstructors(
                BindingFlags.Public | BindingFlags.Instance);

        var constructor = Assert.Single(constructors);
        var parameter = Assert.Single(constructor.GetParameters());

        Assert.Equal("conditions", parameter.Name);
        Assert.Equal(
            typeof(IEnumerable<OperationAuthorizationCondition>),
            parameter.ParameterType);
    }

    [Fact]
    public void DoesNotExposeAuthorizationOrExecutionMethods()
    {
        var methods = typeof(OperationAuthorizationConditions)
            .GetMethods(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName);

        Assert.Empty(methods);
    }
}
