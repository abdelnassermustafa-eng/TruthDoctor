using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationRediscoveryObservationTests
{
    private static readonly DateTimeOffset ObservationTime =
        new(2026, 10, 29, 13, 0, 0, TimeSpan.Zero);

    private static OperationTarget Target() => new(
        "provider", "account-01", "us-east-1",
        "compute", "instance", "resource-42");

    private static OperationRediscoveryObservation Create(
        OperationRediscoveryDisposition disposition =
            OperationRediscoveryDisposition.Unknown,
        string? observedState = null,
        OperationCorrelationId? operation = null,
        OperationTarget? target = null,
        DateTimeOffset? observedAt = null)
    {
        return new OperationRediscoveryObservation(
            operation ?? new OperationCorrelationId("operation-42"),
            target ?? Target(),
            observedAt ?? ObservationTime,
            disposition,
            observedState);
    }

    [Fact]
    public void PreservesOperationTargetTimestampAndObservedState()
    {
        var operation = new OperationCorrelationId("operation-abc");
        var target = Target();
        var timestamp = new DateTimeOffset(
            2026, 10, 29, 6, 0, 0, TimeSpan.FromHours(-7));

        var observation = Create(
            OperationRediscoveryDisposition.Observed,
            "running", operation, target, timestamp);

        Assert.Equal(operation, observation.Operation);
        Assert.Same(target, observation.Target);
        Assert.Equal(timestamp, observation.ObservedAt);
        Assert.Equal(OperationRediscoveryDisposition.Observed,
            observation.Disposition);
        Assert.Equal("running", observation.ObservedState);
    }

    [Theory]
    [InlineData(OperationRediscoveryDisposition.Unknown)]
    [InlineData(OperationRediscoveryDisposition.Absent)]
    [InlineData(OperationRediscoveryDisposition.Unavailable)]
    public void NonObservedDispositionsHaveNoResourceState(
        OperationRediscoveryDisposition disposition)
    {
        var observation = Create(disposition);
        Assert.Equal(disposition, observation.Disposition);
        Assert.Null(observation.ObservedState);
    }

    [Fact]
    public void UnknownIsDefaultDisposition()
    {
        Assert.Equal(OperationRediscoveryDisposition.Unknown,
            default(OperationRediscoveryDisposition));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    [InlineData(999)]
    public void RejectsUndefinedDispositions(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Create((OperationRediscoveryDisposition)value));
    }

    [Fact]
    public void RejectsUninitializedOperationIdentity()
    {
        Assert.Throws<ArgumentException>(() =>
            Create(operation: default(OperationCorrelationId)));
    }

    [Fact]
    public void RejectsNullTarget()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationRediscoveryObservation(
                new OperationCorrelationId("operation-42"),
                null!,
                ObservationTime,
                OperationRediscoveryDisposition.Absent));
    }

    [Fact]
    public void RejectsDefaultObservationTimestamp()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OperationRediscoveryObservation(
                new OperationCorrelationId("operation-42"),
                Target(),
                default,
                OperationRediscoveryDisposition.Absent));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void ObservedRequiresNonblankState(string? state)
    {
        Assert.Throws<ArgumentException>(() =>
            Create(OperationRediscoveryDisposition.Observed, state));
    }

    [Theory]
    [InlineData("running\nnow")]
    [InlineData("running\tnow")]
    [InlineData("running\001now")]
    public void ObservedRejectsControlCharacters(string state)
    {
        Assert.Throws<ArgumentException>(() =>
            Create(OperationRediscoveryDisposition.Observed, state));
    }

    [Fact]
    public void ObservedStateTrimsWhitespaceAndPreservesCase()
    {
        var observation = Create(
            OperationRediscoveryDisposition.Observed,
            "  Running  ");
        Assert.Equal("Running", observation.ObservedState);
    }

    [Theory]
    [InlineData(OperationRediscoveryDisposition.Unknown)]
    [InlineData(OperationRediscoveryDisposition.Absent)]
    [InlineData(OperationRediscoveryDisposition.Unavailable)]
    public void NonObservedDispositionRejectsInventedState(
        OperationRediscoveryDisposition disposition)
    {
        Assert.Throws<ArgumentException>(() =>
            Create(disposition, "running"));
    }

    [Theory]
    [InlineData(OperationRediscoveryDisposition.Unknown)]
    [InlineData(OperationRediscoveryDisposition.Absent)]
    [InlineData(OperationRediscoveryDisposition.Unavailable)]
    public void NonObservedDispositionRejectsEvenBlankState(
        OperationRediscoveryDisposition disposition)
    {
        Assert.Throws<ArgumentException>(() =>
            Create(disposition, " "));
    }

    [Fact]
    public void ExposesOnlyFiveGetterOnlyProperties()
    {
        var type = typeof(OperationRediscoveryObservation);
        Assert.True(type.IsPublic);
        Assert.True(type.IsSealed);
        Assert.Equal("TruthDoctor.Models.Platform", type.Namespace);

        var properties = type.GetProperties(
            BindingFlags.Public | BindingFlags.Instance);
        Assert.Equal(5, properties.Length);
        Assert.Equal(new[]
        {
            "Disposition", "ObservedAt", "ObservedState",
            "Operation", "Target"
        }, properties.Select(p => p.Name).OrderBy(n => n));

        Assert.All(properties, property =>
        {
            Assert.NotNull(property.GetMethod);
            Assert.Null(property.SetMethod);
        });

        Assert.Equal(typeof(OperationCorrelationId),
            type.GetProperty("Operation")!.PropertyType);
        Assert.Equal(typeof(OperationTarget),
            type.GetProperty("Target")!.PropertyType);
        Assert.Equal(typeof(DateTimeOffset),
            type.GetProperty("ObservedAt")!.PropertyType);
        Assert.Equal(typeof(OperationRediscoveryDisposition),
            type.GetProperty("Disposition")!.PropertyType);
        Assert.Equal(typeof(string),
            type.GetProperty("ObservedState")!.PropertyType);

        var constructor = Assert.Single(type.GetConstructors());
        var parameters = constructor.GetParameters();
        Assert.Equal(new[]
        {
            typeof(OperationCorrelationId),
            typeof(OperationTarget),
            typeof(DateTimeOffset),
            typeof(OperationRediscoveryDisposition),
            typeof(string)
        }, parameters.Select(p => p.ParameterType));
        Assert.True(parameters[4].IsOptional);
        Assert.All(parameters.Take(4),
            parameter => Assert.False(parameter.IsOptional));
    }

    [Fact]
    public void ExposesNoExecutionOrVerificationSuccessAPI()
    {
        var type = typeof(OperationRediscoveryObservation);
        var properties = type.GetProperties(
            BindingFlags.Public | BindingFlags.Instance);
        foreach (var name in new[]
        {
            "IsSuccessful", "IsVerified", "VerificationResult",
            "ExecutionStatus", "RawResponse", "Payload",
            "PlatformState", "Resource", "AccessToken", "Secret"
        })
        {
            Assert.DoesNotContain(properties,
                property => property.Name == name);
        }

        var methods = type.GetMethods(
            BindingFlags.DeclaredOnly |
            BindingFlags.Public |
            BindingFlags.Instance |
            BindingFlags.Static);

        Assert.DoesNotContain(methods,
            method => method.Name is
                "Discover" or "Rediscover" or "Execute" or "Verify"
                or "Authorize" or "Approve" or "Dispatch");
    }
}
