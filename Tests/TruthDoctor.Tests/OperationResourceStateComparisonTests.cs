using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationResourceStateComparisonTests
{
    private static readonly DateTimeOffset ObservedAt =
        new(2026, 10, 29, 13, 0, 0, TimeSpan.Zero);

    private static OperationTarget Target(
        int changedDimension = -1)
    {
        var values = new[]
        {
            "provider",
            "account-01",
            "us-east-1",
            "compute",
            "instance",
            "resource-42"
        };

        if (changedDimension >= 0)
        {
            values[changedDimension] += "-other";
        }

        return new OperationTarget(
            values[0], values[1], values[2],
            values[3], values[4], values[5]);
    }

    private static OperationRediscoveryObservation Observation(
        OperationRediscoveryDisposition disposition =
            OperationRediscoveryDisposition.Observed,
        string? state = "running",
        OperationTarget? target = null)
    {
        return new OperationRediscoveryObservation(
            new OperationCorrelationId("operation-42"),
            target ?? Target(),
            ObservedAt,
            disposition,
            disposition == OperationRediscoveryDisposition.Observed
                ? state : null);
    }

    private static OperationResourceStateComparison Compare(
        string key = "resource.state",
        string expected = "running",
        OperationRediscoveryDisposition disposition =
            OperationRediscoveryDisposition.Observed,
        string? observed = "running",
        OperationTarget? observationTarget = null)
    {
        return OperationResourceStateComparison.Evaluate(
            new OperationOutcomeCondition(key, expected),
            new OperationResourceScope(Target()),
            Observation(disposition, observed, observationTarget));
    }

    [Fact]
    public void MatchingStatePreservesExactInputs()
    {
        var condition =
            new OperationOutcomeCondition("resource.state", "running");
        var scope = new OperationResourceScope(Target());
        var observation = Observation();

        var result = OperationResourceStateComparison.Evaluate(
            condition, scope, observation);

        Assert.Same(condition, result.Condition);
        Assert.Same(scope, result.Scope);
        Assert.Same(observation, result.Observation);
        Assert.Equal(
            OperationResourceStateComparisonDisposition.Matched,
            result.Disposition);
    }

    [Fact]
    public void SupportedKeyIgnoresKeyCase()
    {
        Assert.Equal(
            OperationResourceStateComparisonDisposition.Matched,
            Compare(key: "RESOURCE.STATE").Disposition);
    }

    [Fact]
    public void ObservedStateComparisonIsCaseSensitive()
    {
        Assert.Equal(
            OperationResourceStateComparisonDisposition.Mismatched,
            Compare(observed: "Running").Disposition);
    }

    [Fact]
    public void DifferentStateDoesNotMatch()
    {
        Assert.Equal(
            OperationResourceStateComparisonDisposition.Mismatched,
            Compare(observed: "stopped").Disposition);
    }

    [Fact]
    public void TargetMismatchTakesPrecedenceOverStateMatch()
    {
        Assert.Equal(
            OperationResourceStateComparisonDisposition.TargetMismatch,
            Compare(observationTarget: Target(5)).Disposition);
    }

    [Fact]
    public void UnsupportedConditionPrecedesDiscoveryClassification()
    {
        Assert.Equal(
            OperationResourceStateComparisonDisposition.UnsupportedCondition,
            Compare(
                key: "resource.size",
                disposition: OperationRediscoveryDisposition.Unavailable)
                .Disposition);
    }

    [Fact]
    public void AbsentIsDistinctFromMismatched()
    {
        Assert.Equal(
            OperationResourceStateComparisonDisposition.Absent,
            Compare(
                disposition: OperationRediscoveryDisposition.Absent)
                .Disposition);
    }

    [Fact]
    public void UnavailableIsNotAStateMatch()
    {
        Assert.Equal(
            OperationResourceStateComparisonDisposition.Unavailable,
            Compare(
                disposition: OperationRediscoveryDisposition.Unavailable)
                .Disposition);
    }

    [Fact]
    public void UnknownDiscoveryIsNotAStateMatch()
    {
        Assert.Equal(
            OperationResourceStateComparisonDisposition.NoObservation,
            Compare(
                disposition: OperationRediscoveryDisposition.Unknown)
                .Disposition);
    }

    [Fact]
    public void UnknownComparisonIsDefaultEnumValue()
    {
        Assert.Equal(
            OperationResourceStateComparisonDisposition.Unknown,
            default(OperationResourceStateComparisonDisposition));
    }

    [Fact]
    public void NormalizedExpectedAndObservedStatesMatch()
    {
        Assert.Equal(
            OperationResourceStateComparisonDisposition.Matched,
            Compare(expected: " running ", observed: " running ")
                .Disposition);
    }

    [Fact]
    public void PublicShapeIsImmutableAndNarrow()
    {
        var type = typeof(OperationResourceStateComparison);

        Assert.True(type.IsPublic);
        Assert.True(type.IsSealed);
        Assert.Equal("TruthDoctor.Models.Platform", type.Namespace);
        Assert.Empty(type.GetConstructors());

        var properties = type.GetProperties(
            BindingFlags.Public | BindingFlags.Instance);

        Assert.Equal(4, properties.Length);
        Assert.Equal(new[]
        {
            "Condition", "Disposition", "Observation", "Scope"
        }, properties.Select(p => p.Name).OrderBy(n => n));

        Assert.All(properties, property =>
        {
            Assert.NotNull(property.GetMethod);
            Assert.Null(property.SetMethod);
        });

        Assert.Equal(typeof(OperationOutcomeCondition),
            type.GetProperty("Condition")!.PropertyType);
        Assert.Equal(typeof(OperationResourceScope),
            type.GetProperty("Scope")!.PropertyType);
        Assert.Equal(typeof(OperationRediscoveryObservation),
            type.GetProperty("Observation")!.PropertyType);
        Assert.Equal(typeof(OperationResourceStateComparisonDisposition),
            type.GetProperty("Disposition")!.PropertyType);

        var declaredMethods = type.GetMethods(
            BindingFlags.DeclaredOnly |
            BindingFlags.Public |
            BindingFlags.Static |
            BindingFlags.Instance);

        var method = Assert.Single(declaredMethods, method => !method.IsSpecialName);
        Assert.Equal("Evaluate", method.Name);
        Assert.True(method.IsStatic);
        Assert.Equal(type, method.ReturnType);

        Assert.Equal(new[]
        {
            typeof(OperationOutcomeCondition),
            typeof(OperationResourceScope),
            typeof(OperationRediscoveryObservation)
        }, method.GetParameters().Select(p => p.ParameterType));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void RejectsMismatchInEveryTargetIdentityDimension(
        int dimension)
    {
        Assert.Equal(
            OperationResourceStateComparisonDisposition.TargetMismatch,
            Compare(observationTarget: Target(dimension)).Disposition);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void RejectsNullInputs(int which)
    {
        var condition =
            new OperationOutcomeCondition("resource.state", "running");
        var scope = new OperationResourceScope(Target());
        var observation = Observation();

        Assert.Throws<ArgumentNullException>(() =>
            OperationResourceStateComparison.Evaluate(
                which == 0 ? null! : condition,
                which == 1 ? null! : scope,
                which == 2 ? null! : observation));
    }
}
