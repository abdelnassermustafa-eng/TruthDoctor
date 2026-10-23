using System;
using System.Collections.Generic;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationDesiredOutcomeTests
{
    private static OperationOutcomeCondition Condition(
        string key,
        string value)
    {
        return new OperationOutcomeCondition(key, value);
    }

    [Fact]
    public void StoresConditionsInDeclaredOrder()
    {
        var first = Condition("resource.state", "running");
        var second = Condition("resource.size", "large");

        var outcome = new OperationDesiredOutcome(
            new[] { first, second });

        Assert.Equal(2, outcome.Conditions.Count);
        Assert.Same(first, outcome.Conditions[0]);
        Assert.Same(second, outcome.Conditions[1]);
    }

    [Fact]
    public void RejectsNullCollection()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationDesiredOutcome(null!));
    }

    [Fact]
    public void RejectsEmptyCollection()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationDesiredOutcome(
                Array.Empty<OperationOutcomeCondition>()));
    }

    [Fact]
    public void RejectsNullConditionEntry()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationDesiredOutcome(
                new OperationOutcomeCondition[]
                {
                    Condition("resource.state", "running"),
                    null!
                }));
    }

    [Fact]
    public void RejectsDuplicateKeysIgnoringCase()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationDesiredOutcome(
                new[]
                {
                    Condition("resource.state", "running"),
                    Condition("RESOURCE.STATE", "stopped")
                }));
    }

    [Fact]
    public void RejectsDuplicateKeysAfterTrimming()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationDesiredOutcome(
                new[]
                {
                    Condition(" resource.state ", "running"),
                    Condition("resource.state", "stopped")
                }));
    }

    [Fact]
    public void SnapshotsTheInputCollection()
    {
        var source = new List<OperationOutcomeCondition>
        {
            Condition("resource.state", "running")
        };

        var outcome = new OperationDesiredOutcome(source);

        source.Clear();
        source.Add(Condition("resource.state", "stopped"));

        Assert.Single(outcome.Conditions);
        Assert.Equal(
            "running",
            outcome.Conditions[0].ExpectedValue);
    }

    [Fact]
    public void ExposesReadOnlyCollection()
    {
        var outcome = new OperationDesiredOutcome(
            new[] { Condition("resource.state", "running") });

        var list = Assert.IsAssignableFrom<
            IList<OperationOutcomeCondition>>(outcome.Conditions);

        Assert.Throws<NotSupportedException>(() =>
            list[0] = Condition("resource.state", "stopped"));

        Assert.Throws<NotSupportedException>(() =>
            list.Add(Condition("resource.size", "large")));
    }

    [Fact]
    public void PreservesIndependentConditionObjects()
    {
        var first = Condition("resource.state", "running");
        var second = Condition("resource.size", "large");

        var outcome = new OperationDesiredOutcome(
            new[] { first, second });

        Assert.Same(first, outcome.Conditions[0]);
        Assert.Same(second, outcome.Conditions[1]);
        Assert.NotSame(
            outcome.Conditions[0],
            outcome.Conditions[1]);
    }

    [Fact]
    public void ExposesExactlyOneReadOnlyProperty()
    {
        var type = typeof(OperationDesiredOutcome);

        Assert.True(type.IsSealed);

        var property = Assert.Single(type.GetProperties(
            BindingFlags.Instance | BindingFlags.Public));

        Assert.Equal("Conditions", property.Name);
        Assert.Equal(
            typeof(IReadOnlyList<OperationOutcomeCondition>),
            property.PropertyType);
        Assert.NotNull(property.GetMethod);
        Assert.Null(property.SetMethod);

        var constructor = Assert.Single(type.GetConstructors());
        var parameter = Assert.Single(constructor.GetParameters());

        Assert.Equal("conditions", parameter.Name);
        Assert.Equal(
            typeof(IEnumerable<OperationOutcomeCondition>),
            parameter.ParameterType);
        Assert.False(parameter.IsOptional);
    }
}
