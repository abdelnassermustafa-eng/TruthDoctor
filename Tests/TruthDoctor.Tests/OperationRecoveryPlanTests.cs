using System;
using System.Collections.Generic;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationRecoveryPlanTests
{
    private static OperationStepRecovery Unknown(string stepId) =>
        new(stepId, OperationRecoveryStatus.Unknown,
            OperationRecoveryKind.None);

    [Fact]
    public void AllowsEmptyRecoveryPlan()
    {
        var plan = new OperationRecoveryPlan(
            Array.Empty<OperationStepRecovery>());

        Assert.Empty(plan.Recoveries);
    }

    [Fact]
    public void RejectsNullCollection()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationRecoveryPlan(null!));
    }

    [Fact]
    public void RejectsNullEntry()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationRecoveryPlan(
                new OperationStepRecovery[]
                {
                    Unknown("step-a"),
                    null!
                }));
    }

    [Fact]
    public void RejectsExactDuplicateStepId()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationRecoveryPlan(new[]
            {
                Unknown("step-a"),
                Unknown("step-a")
            }));
    }

    [Fact]
    public void RejectsCaseInsensitiveDuplicateStepId()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationRecoveryPlan(new[]
            {
                Unknown("Step-A"),
                Unknown("step-a")
            }));
    }

    [Fact]
    public void RejectsDuplicateAfterStepIdNormalization()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationRecoveryPlan(new[]
            {
                Unknown(" step-a "),
                Unknown("step-a")
            }));
    }

    [Fact]
    public void PreservesDeclarationOrderAndOriginalInstances()
    {
        var first = Unknown("step-c");
        var second = Unknown("step-a");

        var plan = new OperationRecoveryPlan(
            new[] { first, second });

        Assert.Equal(2, plan.Recoveries.Count);
        Assert.Same(first, plan.Recoveries[0]);
        Assert.Same(second, plan.Recoveries[1]);
    }

    [Fact]
    public void SnapshotsMutableSourceList()
    {
        var first = Unknown("step-a");
        var input = new List<OperationStepRecovery> { first };

        var plan = new OperationRecoveryPlan(input);

        input.Clear();
        input.Add(Unknown("step-b"));

        Assert.Single(plan.Recoveries);
        Assert.Same(first, plan.Recoveries[0]);
    }

    [Fact]
    public void ExposedCollectionCannotBeMutated()
    {
        var plan = new OperationRecoveryPlan(
            new[] { Unknown("step-a") });

        var list = Assert.IsAssignableFrom<
            IList<OperationStepRecovery>>(plan.Recoveries);

        Assert.Throws<NotSupportedException>(() =>
            list[0] = Unknown("step-b"));
        Assert.Throws<NotSupportedException>(() =>
            list.Add(Unknown("step-c")));
    }

    [Fact]
    public void RecoveryStatusDoesNotAlterCollectionOrdering()
    {
        var planned = new OperationStepRecovery(
            "step-b", OperationRecoveryStatus.Planned,
            OperationRecoveryKind.Rollback,
            new UniversalCapabilityIdentity("compute.instance.stop"));

        var unknown = Unknown("step-a");

        var plan = new OperationRecoveryPlan(
            new[] { planned, unknown });

        Assert.Same(planned, plan.Recoveries[0]);
        Assert.Same(unknown, plan.Recoveries[1]);
    }

    [Fact]
    public void ExposesExactImmutablePublicApi()
    {
        var type = typeof(OperationRecoveryPlan);
        Assert.True(type.IsSealed);

        var property = Assert.Single(type.GetProperties(
            BindingFlags.Instance | BindingFlags.Public));

        Assert.Equal("Recoveries", property.Name);
        Assert.Equal(
            typeof(IReadOnlyList<OperationStepRecovery>),
            property.PropertyType);
        Assert.NotNull(property.GetMethod);
        Assert.Null(property.SetMethod);

        var ctor = Assert.Single(type.GetConstructors());
        var parameter = Assert.Single(ctor.GetParameters());

        Assert.Equal("recoveries", parameter.Name);
        Assert.Equal(
            typeof(IEnumerable<OperationStepRecovery>),
            parameter.ParameterType);
    }
}
