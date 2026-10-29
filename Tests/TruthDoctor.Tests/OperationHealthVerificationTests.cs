using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationHealthVerificationTests
{
    private static readonly DateTimeOffset ObservedAt =
        new(2026, 10, 29, 13, 0, 0, TimeSpan.Zero);

    private static OperationTarget Target(int change = -1)
    {
        var values = new[]
        {
            "provider", "account-01", "us-east-1",
            "compute", "instance", "resource-42"
        };

        if (change >= 0)
        {
            values[change] += "-other";
        }

        return new OperationTarget(
            values[0], values[1], values[2],
            values[3], values[4], values[5]);
    }

    private static OperationHealthObservation Observation(
        OperationHealthObservationDisposition disposition =
            OperationHealthObservationDisposition.Observed,
        OperationHealthStatus? health = OperationHealthStatus.Healthy,
        OperationTarget? target = null)
    {
        return new OperationHealthObservation(
            new OperationCorrelationId("operation-42"),
            target ?? Target(),
            ObservedAt,
            disposition,
            health);
    }

    private static OperationHealthComparison Compare(
        string key = "resource.health",
        string expected = "healthy",
        OperationHealthObservationDisposition disposition =
            OperationHealthObservationDisposition.Observed,
        OperationHealthStatus? health = OperationHealthStatus.Healthy,
        OperationTarget? target = null)
    {
        return OperationHealthComparison.Evaluate(
            new OperationOutcomeCondition(key, expected),
            new OperationResourceScope(Target()),
            Observation(disposition, health, target));
    }

    [Fact]
    public void HealthStatusHasExplicitNumericValues()
    {
        Assert.Equal(0, (int)OperationHealthStatus.Unknown);
        Assert.Equal(1, (int)OperationHealthStatus.Healthy);
        Assert.Equal(2, (int)OperationHealthStatus.Degraded);
        Assert.Equal(3, (int)OperationHealthStatus.Unhealthy);
        Assert.Equal(4, Enum.GetValues<OperationHealthStatus>().Length);
    }

    [Fact]
    public void ObservationDispositionHasExplicitNumericValues()
    {
        Assert.Equal(0, (int)OperationHealthObservationDisposition.Unknown);
        Assert.Equal(1, (int)OperationHealthObservationDisposition.Observed);
        Assert.Equal(2, (int)OperationHealthObservationDisposition.Unavailable);
        Assert.Equal(3, (int)OperationHealthObservationDisposition.NotAssessed);
        Assert.Equal(
            4, Enum.GetValues<OperationHealthObservationDisposition>().Length);
    }

    [Fact]
    public void ComparisonDispositionHasExplicitNumericValues()
    {
        Assert.Equal(0, (int)OperationHealthComparisonDisposition.Unknown);
        Assert.Equal(1, (int)OperationHealthComparisonDisposition.Matched);
        Assert.Equal(2, (int)OperationHealthComparisonDisposition.Mismatched);
        Assert.Equal(3, (int)OperationHealthComparisonDisposition.TargetMismatch);
        Assert.Equal(4, (int)OperationHealthComparisonDisposition.UnsupportedCondition);
        Assert.Equal(5, (int)OperationHealthComparisonDisposition.Unavailable);
        Assert.Equal(6, (int)OperationHealthComparisonDisposition.NoObservation);
        Assert.Equal(7, (int)OperationHealthComparisonDisposition.NotAssessed);
        Assert.Equal(
            8, Enum.GetValues<OperationHealthComparisonDisposition>().Length);
    }

    [Fact]
    public void ObservedHealthPreservesExactIdentityAndTimestamp()
    {
        var operation = new OperationCorrelationId("operation-42");
        var target = Target();

        var observed = new OperationHealthObservation(
            operation, target, ObservedAt,
            OperationHealthObservationDisposition.Observed,
            OperationHealthStatus.Degraded);

        Assert.Equal(operation, observed.Operation);
        Assert.Same(target, observed.Target);
        Assert.Equal(ObservedAt, observed.ObservedAt);
        Assert.Equal(OperationHealthObservationDisposition.Observed,
            observed.Disposition);
        Assert.Equal(OperationHealthStatus.Degraded, observed.Health);
    }

    [Theory]
    [InlineData(OperationHealthStatus.Healthy)]
    [InlineData(OperationHealthStatus.Degraded)]
    [InlineData(OperationHealthStatus.Unhealthy)]
    public void ObservedAcceptsOnlyKnownReportableHealth(
        OperationHealthStatus health)
    {
        var result = Observation(
            OperationHealthObservationDisposition.Observed, health);

        Assert.Equal(health, result.Health);
    }

    [Theory]
    [InlineData(OperationHealthObservationDisposition.Unknown)]
    [InlineData(OperationHealthObservationDisposition.Unavailable)]
    [InlineData(OperationHealthObservationDisposition.NotAssessed)]
    public void NonObservedDispositionsRequireNullHealth(
        OperationHealthObservationDisposition disposition)
    {
        var result = Observation(disposition, null);

        Assert.Null(result.Health);
        Assert.Equal(disposition, result.Disposition);
    }

    [Theory]
    [InlineData(OperationHealthObservationDisposition.Unknown)]
    [InlineData(OperationHealthObservationDisposition.Unavailable)]
    [InlineData(OperationHealthObservationDisposition.NotAssessed)]
    public void NonObservedDispositionsRejectReportedHealth(
        OperationHealthObservationDisposition disposition)
    {
        Assert.Throws<ArgumentException>(() =>
            Observation(disposition, OperationHealthStatus.Healthy));
    }

    [Fact]
    public void ObservedRejectsNullHealth()
    {
        Assert.Throws<ArgumentException>(() =>
            Observation(OperationHealthObservationDisposition.Observed, null));
    }

    [Theory]
    [InlineData(OperationHealthStatus.Unknown)]
    [InlineData((OperationHealthStatus)99)]
    public void ObservedRejectsUnknownOrUndefinedHealth(
        OperationHealthStatus health)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Observation(OperationHealthObservationDisposition.Observed, health));
    }

    [Fact]
    public void ObservationRejectsUndefinedDisposition()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Observation((OperationHealthObservationDisposition)99, null));
    }

    [Fact]
    public void ObservationRejectsDefaultOperationCorrelation()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationHealthObservation(
                default, Target(), ObservedAt,
                OperationHealthObservationDisposition.Observed,
                OperationHealthStatus.Healthy));
    }

    [Fact]
    public void ObservationRejectsNullTarget()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationHealthObservation(
                new OperationCorrelationId("operation-42"),
                null!, ObservedAt,
                OperationHealthObservationDisposition.Observed,
                OperationHealthStatus.Healthy));
    }

    [Fact]
    public void ObservationRejectsDefaultTimestamp()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OperationHealthObservation(
                new OperationCorrelationId("operation-42"),
                Target(), default,
                OperationHealthObservationDisposition.Observed,
                OperationHealthStatus.Healthy));
    }

    [Theory]
    [InlineData("healthy", OperationHealthStatus.Healthy)]
    [InlineData("degraded", OperationHealthStatus.Degraded)]
    [InlineData("unhealthy", OperationHealthStatus.Unhealthy)]
    public void MatchingKnownHealthClassifiesAsMatched(
        string expected, OperationHealthStatus observed)
    {
        Assert.Equal(
            OperationHealthComparisonDisposition.Matched,
            Compare(expected: expected, health: observed).Disposition);
    }

    [Fact]
    public void ExpectedHealthTokensIgnoreCase()
    {
        Assert.Equal(
            OperationHealthComparisonDisposition.Matched,
            Compare(key: "RESOURCE.HEALTH", expected: "HEALTHY")
                .Disposition);
    }

    [Fact]
    public void DifferentKnownHealthClassifiesAsMismatched()
    {
        Assert.Equal(
            OperationHealthComparisonDisposition.Mismatched,
            Compare(expected: "healthy",
                health: OperationHealthStatus.Unhealthy).Disposition);
    }

    [Fact]
    public void UnsupportedKeyDoesNotMatch()
    {
        Assert.Equal(
            OperationHealthComparisonDisposition.UnsupportedCondition,
            Compare(key: "resource.state").Disposition);
    }

    [Fact]
    public void UnsupportedExpectedHealthValueDoesNotMatch()
    {
        Assert.Equal(
            OperationHealthComparisonDisposition.UnsupportedCondition,
            Compare(expected: "running").Disposition);
    }

    [Theory]
    [InlineData(OperationHealthObservationDisposition.Unknown,
        OperationHealthComparisonDisposition.NoObservation)]
    [InlineData(OperationHealthObservationDisposition.Unavailable,
        OperationHealthComparisonDisposition.Unavailable)]
    [InlineData(OperationHealthObservationDisposition.NotAssessed,
        OperationHealthComparisonDisposition.NotAssessed)]
    public void NonObservedAvailabilityNeverMatches(
        OperationHealthObservationDisposition disposition,
        OperationHealthComparisonDisposition expected)
    {
        Assert.Equal(expected,
            Compare(disposition: disposition, health: null).Disposition);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void EveryTargetIdentityDimensionIsStrict(int dimension)
    {
        Assert.Equal(
            OperationHealthComparisonDisposition.TargetMismatch,
            Compare(target: Target(dimension)).Disposition);
    }

    [Fact]
    public void TargetMismatchPrecedesUnsupportedCondition()
    {
        Assert.Equal(
            OperationHealthComparisonDisposition.TargetMismatch,
            Compare(key: "unsupported",
                target: Target(5)).Disposition);
    }

    [Fact]
    public void UnsupportedConditionPrecedesUnavailableObservation()
    {
        Assert.Equal(
            OperationHealthComparisonDisposition.UnsupportedCondition,
            Compare(expected: "invalid",
                disposition: OperationHealthObservationDisposition.Unavailable,
                health: null).Disposition);
    }

    [Fact]
    public void ComparisonPreservesExactInputs()
    {
        var condition =
            new OperationOutcomeCondition("resource.health", "healthy");
        var scope = new OperationResourceScope(Target());
        var observation = Observation();

        var result = OperationHealthComparison.Evaluate(
            condition, scope, observation);

        Assert.Same(condition, result.Condition);
        Assert.Same(scope, result.Scope);
        Assert.Same(observation, result.Observation);
        Assert.Equal(OperationHealthComparisonDisposition.Matched,
            result.Disposition);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void ComparisonRejectsNullInputs(int which)
    {
        var condition =
            new OperationOutcomeCondition("resource.health", "healthy");
        var scope = new OperationResourceScope(Target());
        var observation = Observation();

        Assert.Throws<ArgumentNullException>(() =>
            OperationHealthComparison.Evaluate(
                which == 0 ? null! : condition,
                which == 1 ? null! : scope,
                which == 2 ? null! : observation));
    }

    [Fact]
    public void ObservationPublicShapeIsImmutable()
    {
        var type = typeof(OperationHealthObservation);
        Assert.True(type.IsPublic);
        Assert.True(type.IsSealed);

        var properties = type.GetProperties(
            BindingFlags.Public | BindingFlags.Instance);

        Assert.Equal(new[]
        {
            "Disposition", "Health", "ObservedAt", "Operation", "Target"
        }, properties.Select(p => p.Name).OrderBy(n => n));

        Assert.All(properties, property =>
        {
            Assert.NotNull(property.GetMethod);
            Assert.Null(property.SetMethod);
        });
    }

    [Fact]
    public void ComparisonPublicShapeIsNarrowAndImmutable()
    {
        var type = typeof(OperationHealthComparison);
        Assert.True(type.IsPublic);
        Assert.True(type.IsSealed);
        Assert.Empty(type.GetConstructors());

        var properties = type.GetProperties(
            BindingFlags.Public | BindingFlags.Instance);

        Assert.Equal(new[]
        {
            "Condition", "Disposition", "Observation", "Scope"
        }, properties.Select(p => p.Name).OrderBy(n => n));

        Assert.All(properties, property =>
        {
            Assert.NotNull(property.GetMethod);
            Assert.Null(property.SetMethod);
        });

        var methods = type.GetMethods(
            BindingFlags.Public | BindingFlags.DeclaredOnly |
            BindingFlags.Static | BindingFlags.Instance);

        var method = Assert.Single(
            methods, candidate => !candidate.IsSpecialName);

        Assert.Equal("Evaluate", method.Name);
        Assert.True(method.IsStatic);
        Assert.Equal(type, method.ReturnType);
        Assert.Equal(new[]
        {
            typeof(OperationOutcomeCondition),
            typeof(OperationResourceScope),
            typeof(OperationHealthObservation)
        }, method.GetParameters().Select(p => p.ParameterType));
    }

    [Fact]
    public void ComparisonDoesNotExposeVerifiedSuccess()
    {
        var type = typeof(OperationHealthComparison);

        Assert.Null(type.GetProperty("IsVerified"));
        Assert.Null(type.GetProperty("IsSuccessful"));
        Assert.Null(type.GetProperty("VerifiedSuccess"));
        Assert.Null(type.GetProperty("ExecutionSucceeded"));
        Assert.Null(type.GetProperty("Evidence"));
    }
}
