using System;
using System.Collections.Generic;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationPreconditionsTests
{
    private static OperationPrecondition Condition(
        string key,
        string value)
    {
        return new OperationPrecondition(key, value);
    }

    [Fact]
    public void StoresConditionsInDeclaredOrder()
    {
        var first = Condition("resource.state", "stopped");
        var second = Condition("context.account", "present");

        var requirements = new OperationPreconditions(
            new[] { first, second });

        Assert.Equal(2, requirements.Conditions.Count);
        Assert.Same(first, requirements.Conditions[0]);
        Assert.Same(second, requirements.Conditions[1]);
    }

    [Fact]
    public void RejectsNullCollection()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationPreconditions(null!));
    }

    [Fact]
    public void AllowsEmptyCollection()
    {
        var requirements = new OperationPreconditions(
            Array.Empty<OperationPrecondition>());

        Assert.Empty(requirements.Conditions);
    }

    [Fact]
    public void RejectsNullConditionEntry()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationPreconditions(
                new OperationPrecondition[]
                {
                    Condition("resource.state", "stopped"),
                    null!
                }));
    }

    [Fact]
    public void RejectsDuplicateKeysIgnoringCase()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationPreconditions(
                new[]
                {
                    Condition("resource.state", "stopped"),
                    Condition("RESOURCE.STATE", "running")
                }));
    }

    [Fact]
    public void RejectsDuplicateKeysAfterTrimming()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationPreconditions(
                new[]
                {
                    Condition(" resource.state ", "stopped"),
                    Condition("resource.state", "running")
                }));
    }

    [Fact]
    public void SnapshotsInputCollection()
    {
        var source = new List<OperationPrecondition>
        {
            Condition("resource.state", "stopped")
        };

        var requirements = new OperationPreconditions(source);

        source.Clear();
        source.Add(Condition("resource.state", "running"));

        Assert.Single(requirements.Conditions);
        Assert.Equal(
            "stopped",
            requirements.Conditions[0].ExpectedValue);
    }

    [Fact]
    public void ExposesReadOnlyCollection()
    {
        var requirements = new OperationPreconditions(
            new[] { Condition("resource.state", "stopped") });

        var list = Assert.IsAssignableFrom<
            IList<OperationPrecondition>>(requirements.Conditions);

        Assert.Throws<NotSupportedException>(() =>
            list[0] = Condition("resource.state", "running"));

        Assert.Throws<NotSupportedException>(() =>
            list.Add(Condition("context.account", "present")));
    }

    [Fact]
    public void PreservesIndependentConditionObjects()
    {
        var first = Condition("resource.state", "stopped");
        var second = Condition("context.account", "present");

        var requirements = new OperationPreconditions(
            new[] { first, second });

        Assert.Same(first, requirements.Conditions[0]);
        Assert.Same(second, requirements.Conditions[1]);
        Assert.NotSame(
            requirements.Conditions[0],
            requirements.Conditions[1]);
    }

    [Fact]
    public void ExposesExactlyOneReadOnlyProperty()
    {
        var type = typeof(OperationPreconditions);

        Assert.True(type.IsSealed);

        var property = Assert.Single(type.GetProperties(
            BindingFlags.Instance | BindingFlags.Public));

        Assert.Equal("Conditions", property.Name);
        Assert.Equal(
            typeof(IReadOnlyList<OperationPrecondition>),
            property.PropertyType);
        Assert.NotNull(property.GetMethod);
        Assert.Null(property.SetMethod);

        var constructor = Assert.Single(type.GetConstructors());
        var parameter = Assert.Single(constructor.GetParameters());

        Assert.Equal("conditions", parameter.Name);
        Assert.Equal(
            typeof(IEnumerable<OperationPrecondition>),
            parameter.ParameterType);
        Assert.False(parameter.IsOptional);
    }
}
