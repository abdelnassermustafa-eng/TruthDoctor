using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

/// <summary>
/// Test-first history association of a caller-supplied rediscovery
/// observation designated as after-state context.
/// This designation does not prove post-execution chronology,
/// source authenticity, execution success, or resource change.
/// </summary>
public sealed class OperationHistoryAfterStateAttributionTests
{
    private static OperationHistoryEvent Event(
        string operationId = "operation-after-001")
    {
        return new OperationHistoryEvent(
            "history-after-001",
            new OperationCorrelationId(operationId),
            "AfterStateReported");
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
        string operationId = "operation-after-001")
    {
        return new OperationRediscoveryObservation(
            new OperationCorrelationId(operationId),
            Target(),
            observedAt ?? new DateTimeOffset(
                2026, 1, 2, 12, 0, 0, TimeSpan.Zero),
            disposition,
            observedState);
    }

    private static OperationHistoryAfterStateAttribution Create()
    {
        return new OperationHistoryAfterStateAttribution(
            Event(), Observation());
    }

    [Fact]
    public void PreservesExactHistoryEventReference()
    {
        var historyEvent = Event();
        var result = new OperationHistoryAfterStateAttribution(
            historyEvent, Observation());

        Assert.Same(historyEvent, result.Event);
        Assert.Equal("history-after-001", result.Event.EventId);
    }

    [Fact]
    public void PreservesExactRediscoveryObservationReference()
    {
        var observation = Observation();
        var result = new OperationHistoryAfterStateAttribution(
            Event(), observation);

        Assert.Same(observation, result.Observation);
    }

    [Fact]
    public void PreservesOperationTargetAndTimestamp()
    {
        var timestamp = new DateTimeOffset(
            2026, 1, 2, 10, 30, 0, TimeSpan.Zero);
        var observation = Observation(observedAt: timestamp);
        var result = new OperationHistoryAfterStateAttribution(
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
            "  Stopped  ");
        var result = new OperationHistoryAfterStateAttribution(
            Event(), observation);

        Assert.Same(observation, result.Observation);
        Assert.Equal(
            OperationRediscoveryDisposition.Observed,
            result.Observation.Disposition);
        Assert.Equal("Stopped", result.Observation.ObservedState);
    }

    [Fact]
    public void PreservesUnknownDispositionWithoutState()
    {
        var observation = Observation(
            OperationRediscoveryDisposition.Unknown,
            null);
        var result = new OperationHistoryAfterStateAttribution(
            Event(), observation);

        Assert.Same(observation, result.Observation);
        Assert.Equal(
            OperationRediscoveryDisposition.Unknown,
            result.Observation.Disposition);
        Assert.Null(result.Observation.ObservedState);
    }

    [Fact]
    public void PreservesAbsentAndUnavailableDispositions()
    {
        foreach (var disposition in new[]
        {
            OperationRediscoveryDisposition.Absent,
            OperationRediscoveryDisposition.Unavailable
        })
        {
            var observation = Observation(disposition, null);
            var result = new OperationHistoryAfterStateAttribution(
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
            new OperationHistoryAfterStateAttribution(
                null!, Observation()));
    }

    [Fact]
    public void RejectsNullObservation()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationHistoryAfterStateAttribution(
                Event(), null!));
    }

    [Fact]
    public void DoesNotInferPostExecutionChronology()
    {
        var earlier = new DateTimeOffset(
            2001, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var observation = Observation(
            observedAt: earlier,
            operationId: "operation-independent");

        var result = new OperationHistoryAfterStateAttribution(
            Event("operation-after-001"), observation);

        Assert.Same(observation, result.Observation);
        Assert.Equal(earlier, result.Observation.ObservedAt);
        Assert.NotEqual(
            result.Event.Operation,
            result.Observation.Operation);
    }

    [Fact]
    public void DoesNotInferExecutionOrSuccessfulChange()
    {
        var properties = typeof(OperationHistoryAfterStateAttribution)
            .GetProperties(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly);

        Assert.DoesNotContain(properties, property =>
            property.Name is
                "Executed" or "Succeeded" or "StateChanged" or
                "Verified" or "BeforeState" or "Impact" or
                "Effect" or "Evidence");
    }

    [Fact]
    public void IsPublicSealedProviderNeutralReferenceType()
    {
        var type = typeof(OperationHistoryAfterStateAttribution);

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
        var properties = typeof(OperationHistoryAfterStateAttribution)
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
            typeof(OperationHistoryAfterStateAttribution)
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
        var methods = typeof(OperationHistoryAfterStateAttribution)
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
            typeof(OperationHistoryAfterStateAttribution)
                .GetInterfaces());
    }
}
