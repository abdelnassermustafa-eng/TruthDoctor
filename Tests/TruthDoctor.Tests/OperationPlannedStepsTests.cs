using System;
using System.Collections.Generic;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationPlannedStepsTests
{
    private static UniversalCapabilityIdentity Start =>
        new("compute.instance.start");

    private static OperationPlannedStep Step(string id)
    {
        return new OperationPlannedStep(
            id, Start, "Start instance");
    }

    [Fact]
    public void StoresStepsInDeclaredOrder()
    {
        var first = Step("step-1");
        var second = Step("step-2");

        var plan = new OperationPlannedSteps(
            new[] { first, second });

        Assert.Equal(2, plan.Steps.Count);
        Assert.Same(first, plan.Steps[0]);
        Assert.Same(second, plan.Steps[1]);
    }

    [Fact]
    public void RejectsNullCollection()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationPlannedSteps(null!));
    }

    [Fact]
    public void RejectsEmptyCollection()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationPlannedSteps(
                Array.Empty<OperationPlannedStep>()));
    }

    [Fact]
    public void RejectsNullStepEntry()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationPlannedSteps(
                new OperationPlannedStep[]
                {
                    Step("step-1"),
                    null!
                }));
    }

    [Fact]
    public void RejectsDuplicateStepIdsIgnoringCase()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationPlannedSteps(
                new[]
                {
                    Step("Step-A"),
                    Step("step-a")
                }));
    }

    [Fact]
    public void RejectsDuplicateStepIdsAfterTrimming()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationPlannedSteps(
                new[]
                {
                    Step(" step-1 "),
                    Step("step-1")
                }));
    }

    [Fact]
    public void SnapshotsInputCollection()
    {
        var source = new List<OperationPlannedStep>
        {
            Step("step-1")
        };

        var plan = new OperationPlannedSteps(source);

        source.Clear();
        source.Add(Step("step-2"));

        Assert.Single(plan.Steps);
        Assert.Equal("step-1", plan.Steps[0].StepId);
    }

    [Fact]
    public void ExposesReadOnlyCollection()
    {
        var plan = new OperationPlannedSteps(
            new[] { Step("step-1") });

        var list = Assert.IsAssignableFrom<
            IList<OperationPlannedStep>>(plan.Steps);

        Assert.Throws<NotSupportedException>(() =>
            list[0] = Step("step-2"));

        Assert.Throws<NotSupportedException>(() =>
            list.Add(Step("step-3")));
    }

    [Fact]
    public void PreservesIndependentStepObjects()
    {
        var first = Step("step-1");
        var second = Step("step-2");

        var plan = new OperationPlannedSteps(
            new[] { first, second });

        Assert.Same(first, plan.Steps[0]);
        Assert.Same(second, plan.Steps[1]);
        Assert.NotSame(plan.Steps[0], plan.Steps[1]);
    }

    [Fact]
    public void ExposesExactlyOneReadOnlyPropertyAndConstructor()
    {
        var type = typeof(OperationPlannedSteps);

        Assert.True(type.IsSealed);

        var property = Assert.Single(type.GetProperties(
            BindingFlags.Instance | BindingFlags.Public));

        Assert.Equal("Steps", property.Name);
        Assert.Equal(
            typeof(IReadOnlyList<OperationPlannedStep>),
            property.PropertyType);
        Assert.NotNull(property.GetMethod);
        Assert.Null(property.SetMethod);

        var constructor = Assert.Single(type.GetConstructors());
        var parameter = Assert.Single(constructor.GetParameters());

        Assert.Equal("steps", parameter.Name);
        Assert.Equal(
            typeof(IEnumerable<OperationPlannedStep>),
            parameter.ParameterType);
        Assert.False(parameter.IsOptional);
    }
}
