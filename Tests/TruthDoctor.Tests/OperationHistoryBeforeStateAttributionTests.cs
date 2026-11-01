using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

/// <summary>
/// Test-first association of a history event with a caller-supplied
/// rediscovery observation labeled as before-state context.
/// The association does not prove chronology, provenance, or change.
/// </summary>
public sealed class OperationHistoryBeforeStateAttributionTests
{
    private static OperationHistoryEvent Event(
        string operationId = "operation-before-001")
    {
        return new OperationHistoryEvent(
            "history-before-001",
            new OperationCorrelationId(operationId),
            "BeforeStateReported");
    }

    private static OperationTarget Target() =>
        new(
            "aws",
            "account-001",
            "us-east-1",
            "domain-001",
            "compute.instance",
            "instance-001");

    private static OperationRediscoveryObservation Observation(
        OperationRediscoveryDisposition disposition =
            OperationRediscoveryDisposition.Observed,
        string? observedState = "running",
        DateTimeOffset? observedAt = null,
        string operationId = "operation-before-001")
    {
        return new OperationRediscoveryObservation(
            new OperationCorrelationId(operationId),
            Target(),
            observedAt ?? new DateTimeOffset(
                2026, 1, 1, 12, 0, 0, TimeSpan.Zero),
            disposition,
            observedState);
    }

    private static OperationHistoryBeforeStateAttribution Create()
    {
        return new OperationHistoryBeforeStateAttribution(
            Event(), Observation());
    }

    [Fact]
    public void PreservesExactHistoryEventReference()
    {
        var historyEvent = Event();
        var result = new OperationHistoryBeforeStateAttribution(
            historyEvent, Observation());

        Assert.Same(historyEvent, result.Event);
        Assert.Equal("history-before-001", result.Event.EventId);
    }

    [Fact]
    public void PreservesExactRediscoveryObservationReference()
    {
        var observation = Observation();
        var result = new OperationHistoryBeforeStateAttribution(
            Event(), observation);

        Assert.Same(observation, result.Observation);
    }

    [Fact]
    public void PreservesOperationTargetAndTimestamp()
    {
        var timestamp = new DateTimeOffset(
            2026, 1, 1, 10, 30, 0, TimeSpan.Zero);
        var observation = Observation(observedAt: timestamp);
        var result = new OperationHistoryBeforeStateAttribution(
            Event(), observation);

        Assert.Same(observation.Target, result.Observation.Target);
        Assert.Equal(
            observation.Operation, result.Observation.Operation);
        Assert.Equal(timestamp, result.Observation.ObservedAt);
    }

    [Fact]
    public void PreservesObservedDispositionAndState()
    {
        var observation = Observation(
            OperationRediscoveryDisposition.Observed,
            "  Running  ");
        var result = new OperationHistoryBeforeStateAttribution(
            Event(), observation);

        Assert.Same(observation, result.Observation);
        Assert.Equal(
            OperationRediscoveryDisposition.Observed,
            result.Observation.Disposition);
        Assert.Equal("Running", result.Observation.ObservedState);
    }

    [Fact]
    public void PreservesUnknownDispositionWithoutState()
    {
        var observation = Observation(
            OperationRediscoveryDisposition.Unknown,
            null);
        var result = new OperationHistoryBeforeStateAttribution(
            Event(), observation);

        Assert.Same(observation, result.Observation);
        Assert.Equal(
            OperationRediscoveryDisposition.Unknown,
            result.Observation.Disposition);
        Assert.Null(result.Observation.ObservedState);
    }

    [Fact]
    public void PreservesUnavailableOrNonObservedDisposition()
    {
        foreach (var disposition in new[]
        {
            OperationRediscoveryDisposition.Absent,
            OperationRediscoveryDisposition.Unavailable
        })
        {
            var observation = Observation(disposition, null);
            var result = new OperationHistoryBeforeStateAttribution(
                Event(), observation);

            Assert.Same(observation, result.Observation);
            Assert.Equal(disposition, result.Observation.Disposition);
            Assert.Null(result.Observation.ObservedState);
        }
    }

    [Fact]
    public void RejectsNullHistoryEvent()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationHistoryBeforeStateAttribution(
                null!, Observation()));
    }

    [Fact]
    public void RejectsNullObservation()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationHistoryBeforeStateAttribution(
                Event(), null!));
    }

    [Fact]
    public void DoesNotInferTemporalOrdering()
    {
        var future = new DateTimeOffset(
            2099, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var observation = Observation(observedAt: future);
        var result = new OperationHistoryBeforeStateAttribution(
            Event("operation-other"), observation);

        Assert.Same(observation, result.Observation);
        Assert.Equal(future, result.Observation.ObservedAt);
    }

    [Fact]
    public void DoesNotInferExecutionOrSuccessfulChange()
    {
        var type = typeof(OperationHistoryBeforeStateAttribution);
        var properties = type.GetProperties(
            BindingFlags.Public |
            BindingFlags.Instance |
            BindingFlags.DeclaredOnly);

        Assert.DoesNotContain(properties, item =>
            item.Name is
                "Executed" or "Succeeded" or "StateChanged" or
                "Verified" or "AfterState" or "Impact" or
                "Effect" or "Evidence");
    }

    [Fact]
    public void IsPublicSealedProviderNeutralReferenceType()
    {
        var type = typeof(OperationHistoryBeforeStateAttribution);

        Assert.True(type.IsPublic);
        Assert.True(type.IsClass);
        Assert.True(type.IsSealed);
        Assert.Equal(
            "TruthDoctor.Models.Platform",
            type.Namespace);
    }

    [Fact]
    public void ExposesExactlyTwoGetterOnlyProperties()
    {
        var properties = typeof(
            OperationHistoryBeforeStateAttribution)
            .GetProperties(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly);

        Assert.Equal(2, properties.Length);

        var expected = new (string Name, Type Type)[]
        {
            ("Event", typeof(OperationHistoryEvent)),
            ("Observation", typeof(OperationRediscoveryObservation))
        };

        foreach (var (name, propertyType) in expected)
        {
            var property = Assert.Single(
                properties, item => item.Name == name);

            Assert.Equal(propertyType, property.PropertyType);
            Assert.NotNull(property.GetMethod);
            Assert.Null(property.SetMethod);
        }
    }

    [Fact]
    public void HasExactlyOneTwoArgumentConstructor()
    {
        var constructor = Assert.Single(
            typeof(OperationHistoryBeforeStateAttribution)
                .GetConstructors(
                    BindingFlags.Public |
                    BindingFlags.Instance));

        var parameters = constructor.GetParameters();

        Assert.Equal(2, parameters.Length);
        Assert.Equal("historyEvent", parameters[0].Name);
        Assert.Equal(
            typeof(OperationHistoryEvent),
            parameters[0].ParameterType);
        Assert.Equal("observation", parameters[1].Name);
        Assert.Equal(
            typeof(OperationRediscoveryObservation),
            parameters[1].ParameterType);

        Assert.All(
            parameters,
            parameter => Assert.False(parameter.IsOptional));
    }

    [Fact]
    public void ExposesNoPublicOperationalMethods()
    {
        var methods = typeof(OperationHistoryBeforeStateAttribution)
            .GetMethods(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.Static |
                BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .ToArray();

        Assert.Empty(methods);
    }

    [Fact]
    public void DoesNotDeclareIntegrationInterfaces()
    {
        Assert.Empty(
            typeof(OperationHistoryBeforeStateAttribution)
                .GetInterfaces());
    }
}
