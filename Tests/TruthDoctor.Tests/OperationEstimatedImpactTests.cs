using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationEstimatedImpactTests
{
    private static OperationStepImpact Step(
        string stepId,
        OperationImpactSeverity severity = OperationImpactSeverity.Unknown)
    {
        return new OperationStepImpact(
            stepId,
            severity,
            OperationImpactConfidence.Unknown);
    }

    [Fact]
    public void TypeIsPublicAndSealed()
    {
        Assert.True(typeof(OperationEstimatedImpact).IsPublic);
        Assert.True(typeof(OperationEstimatedImpact).IsSealed);
    }

    [Fact]
    public void PublicApiContainsOneGetterOnlyProperty()
    {
        var properties = typeof(OperationEstimatedImpact).GetProperties(
            BindingFlags.Public | BindingFlags.Instance);

        var property = Assert.Single(properties);

        Assert.Equal(nameof(OperationEstimatedImpact.Impacts), property.Name);
        Assert.Equal(
            typeof(IReadOnlyList<OperationStepImpact>),
            property.PropertyType);
        Assert.NotNull(property.GetMethod);
        Assert.Null(property.SetMethod);
    }

    [Fact]
    public void ExactlyOnePublicConstructorAcceptsEnumerable()
    {
        var constructor = Assert.Single(
            typeof(OperationEstimatedImpact).GetConstructors());

        var parameter = Assert.Single(constructor.GetParameters());

        Assert.Equal("impacts", parameter.Name);
        Assert.Equal(
            typeof(IEnumerable<OperationStepImpact>),
            parameter.ParameterType);
        Assert.False(parameter.IsOptional);
    }

    [Fact]
    public void NullCollectionIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationEstimatedImpact(null!));
    }

    [Fact]
    public void NullEntryIsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationEstimatedImpact(
                new OperationStepImpact[] { Step("a"), null! }));
    }

    [Fact]
    public void EmptyCollectionIsAllowed()
    {
        var plan = new OperationEstimatedImpact(
            Array.Empty<OperationStepImpact>());

        Assert.Empty(plan.Impacts);
    }

    [Fact]
    public void SingleImpactIsPreservedByReference()
    {
        var impact = Step("a");
        var plan = new OperationEstimatedImpact(
            new[] { impact });

        Assert.Same(impact, Assert.Single(plan.Impacts));
    }

    [Fact]
    public void DeclarationOrderIsPreserved()
    {
        var a = Step("a");
        var b = Step("b");
        var c = Step("c");

        var plan = new OperationEstimatedImpact(
            new[] { c, a, b });

        Assert.Same(c, plan.Impacts[0]);
        Assert.Same(a, plan.Impacts[1]);
        Assert.Same(b, plan.Impacts[2]);
    }

    [Fact]
    public void DuplicateExactStepIdsAreRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationEstimatedImpact(
                new[] { Step("a"), Step("a") }));
    }

    [Fact]
    public void DuplicateStepIdsAreRejectedIgnoringCase()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationEstimatedImpact(
                new[] { Step("Step-A"), Step("step-a") }));
    }

    [Fact]
    public void DuplicateStepIdsAreRejectedAfterTrimming()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationEstimatedImpact(
                new[] { Step(" Step-A "), Step("step-a") }));
    }

    [Fact]
    public void ChangingInputListDoesNotChangeSnapshot()
    {
        var original = Step("a");
        var input = new List<OperationStepImpact> { original };

        var plan = new OperationEstimatedImpact(input);

        input.Clear();
        input.Add(Step("b"));

        Assert.Same(original, Assert.Single(plan.Impacts));
    }

    [Fact]
    public void OutputCollectionCannotBeMutatedThroughListInterface()
    {
        var impact = Step("a");
        var plan = new OperationEstimatedImpact(
            new[] { impact });

        var list = Assert.IsAssignableFrom<IList<OperationStepImpact>>(
            plan.Impacts);

        Assert.True(list.IsReadOnly);

        Assert.Throws<NotSupportedException>(() =>
            list.Add(Step("b")));

        Assert.Throws<NotSupportedException>(() =>
            list[0] = Step("c"));

        Assert.Same(impact, Assert.Single(plan.Impacts));
    }

    [Fact]
    public void EmptyPlanDoesNotCreateSyntheticZeroImpact()
    {
        var plan = new OperationEstimatedImpact(
            Array.Empty<OperationStepImpact>());

        Assert.Empty(plan.Impacts);
    }

    [Fact]
    public void DifferentSeveritiesCanCoexistWithoutAggregation()
    {
        var unknown = Step("a");
        var high = Step("b", OperationImpactSeverity.High);
        var none = Step("c", OperationImpactSeverity.None);

        var plan = new OperationEstimatedImpact(
            new[] { unknown, high, none });

        Assert.Equal(
            new[]
            {
                OperationImpactSeverity.Unknown,
                OperationImpactSeverity.High,
                OperationImpactSeverity.None
            },
            plan.Impacts.Select(x => x.Severity));
    }
}
