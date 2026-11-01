using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

/// <summary>
/// Test-first historical correlation attribution contract.
/// This association does not authenticate historical correspondence,
/// generate identifiers, authorize operations, or execute work.
/// </summary>
public sealed class OperationHistoryCorrelationAttributionTests
{
    private static OperationCorrelationId Correlation(
        string value = "operation-01")
        => new(value);

    private static OperationHistoryEvent Event(
        string operationId = "operation-01",
        string eventId = "history-event-01",
        string eventKind = "ActionReported")
        => new(
            eventId,
            Correlation(operationId),
            eventKind);

    private static OperationHistoryCorrelationAttribution Create(
        OperationHistoryEvent? historyEvent = null,
        OperationCorrelationId? correlation = null)
        => new(
            historyEvent ?? Event(),
            correlation ?? Correlation());

    [Fact]
    public void PreservesExactHistoryEventReference()
    {
        var historyEvent = Event();

        var attribution = new OperationHistoryCorrelationAttribution(
            historyEvent, Correlation());

        Assert.Same(historyEvent, attribution.Event);
    }

    [Fact]
    public void PreservesSuppliedCorrelationValue()
    {
        var supplied = Correlation("declared-correlation-123");
        var attribution = Create(correlation: supplied);

        Assert.Equal(supplied, attribution.Correlation);
        Assert.Equal(
            "declared-correlation-123",
            attribution.Correlation.Value);
    }

    [Fact]
    public void PreservesHistoryEventIdAndKind()
    {
        var historyEvent = Event(
            "operation-42",
            "Event-ABC",
            "VerificationReported");

        var attribution = Create(historyEvent);

        Assert.Same(historyEvent, attribution.Event);
        Assert.Equal("Event-ABC", attribution.Event.EventId);
        Assert.Equal(
            "VerificationReported",
            attribution.Event.EventKind);
        Assert.Equal(
            "operation-42",
            attribution.Event.Operation.Value);
    }

    [Fact]
    public void PreservesEquivalentNormalizedCorrelation()
    {
        var supplied = Correlation("  Corr-ABC-123  ");
        var equivalent = Correlation("Corr-ABC-123");

        var attribution = Create(correlation: supplied);

        Assert.Equal(equivalent, attribution.Correlation);
        Assert.Equal(
            "Corr-ABC-123",
            attribution.Correlation.Value);
        Assert.Equal(
            equivalent.GetHashCode(),
            attribution.Correlation.GetHashCode());
    }

    [Fact]
    public void PreservesCaseSensitiveDistinctCorrelation()
    {
        var upper = Correlation("Corr-ABC");
        var lower = Correlation("corr-ABC");

        var attribution = Create(correlation: upper);

        Assert.Equal(upper, attribution.Correlation);
        Assert.NotEqual(lower, attribution.Correlation);
        Assert.Equal("Corr-ABC", attribution.Correlation.Value);
    }

    [Fact]
    public void PreservesOpaqueProviderNeutralCorrelation()
    {
        var supplied = Correlation("providerA/request:42.x_y");
        var attribution = Create(correlation: supplied);

        Assert.Equal(supplied, attribution.Correlation);
        Assert.Equal(
            "providerA/request:42.x_y",
            attribution.Correlation.Value);
    }

    [Fact]
    public void PreservesDifferentEventAndDeclaredCorrelation()
    {
        var historyEvent = Event("event-operation");
        var supplied = Correlation("different-declared-operation");

        var attribution = new OperationHistoryCorrelationAttribution(
            historyEvent, supplied);

        Assert.Same(historyEvent, attribution.Event);
        Assert.Equal(supplied, attribution.Correlation);
        Assert.NotEqual(
            attribution.Event.Operation,
            attribution.Correlation);
    }

    [Fact]
    public void PreservesMatchingEventAndDeclaredCorrelation()
    {
        var historyEvent = Event("shared-operation");
        var supplied = Correlation("shared-operation");

        var attribution = new OperationHistoryCorrelationAttribution(
            historyEvent, supplied);

        Assert.Same(historyEvent, attribution.Event);
        Assert.Equal(
            attribution.Event.Operation,
            attribution.Correlation);
    }

    [Fact]
    public void RejectsNullHistoryEvent()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationHistoryCorrelationAttribution(
                null!, Correlation()));
    }

    [Fact]
    public void RejectsDefaultCorrelation()
    {
        var historyEvent = Event();

        Assert.Throws<ArgumentException>(() =>
            new OperationHistoryCorrelationAttribution(
                historyEvent,
                default(OperationCorrelationId)));
    }

    [Fact]
    public void DoesNotGenerateOrNormalizeNewIdentity()
    {
        var historyEvent = Event("existing-operation");
        var supplied = Correlation(" Existing-Correlation ");

        var attribution = new OperationHistoryCorrelationAttribution(
            historyEvent, supplied);

        Assert.Equal(supplied, attribution.Correlation);
        Assert.Equal(
            "Existing-Correlation",
            attribution.Correlation.Value);
        Assert.NotEqual(
            attribution.Event.Operation,
            attribution.Correlation);

        var methods = typeof(OperationHistoryCorrelationAttribution)
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
    public void DoesNotInferAuthenticationOrExecution()
    {
        var attribution = Create(
            historyEvent: Event("history-only"),
            correlation: Correlation("caller-declared"));

        Assert.NotEqual(
            attribution.Event.Operation,
            attribution.Correlation);

        var properties = typeof(OperationHistoryCorrelationAttribution)
            .GetProperties(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly);

        var forbidden = new[]
        {
            "Authenticated",
            "AuthenticatedCorrelation",
            "Verified",
            "VerifiedSuccess",
            "ExecutionSucceeded",
            "Authorized",
            "Approved",
            "CanExecute",
            "Matched",
            "IsValid"
        };

        var names = properties
            .Select(property => property.Name)
            .ToArray();

        foreach (var name in forbidden)
        {
            Assert.DoesNotContain(name, names);
        }
    }

    [Fact]
    public void IsPublicSealedWithTwoGetterOnlyProperties()
    {
        var type = typeof(OperationHistoryCorrelationAttribution);

        Assert.True(type.IsPublic);
        Assert.True(type.IsClass);
        Assert.True(type.IsSealed);
        Assert.Equal("TruthDoctor.Models.Platform", type.Namespace);

        var properties = type.GetProperties(
            BindingFlags.Public |
            BindingFlags.Instance |
            BindingFlags.DeclaredOnly);

        Assert.Equal(2, properties.Length);

        var eventProperty = Assert.Single(
            properties,
            property => property.Name == "Event");

        var correlationProperty = Assert.Single(
            properties,
            property => property.Name == "Correlation");

        Assert.Equal(
            typeof(OperationHistoryEvent),
            eventProperty.PropertyType);
        Assert.Equal(
            typeof(OperationCorrelationId),
            correlationProperty.PropertyType);

        Assert.NotNull(eventProperty.GetMethod);
        Assert.NotNull(correlationProperty.GetMethod);
        Assert.Null(eventProperty.SetMethod);
        Assert.Null(correlationProperty.SetMethod);
    }

    [Fact]
    public void HasOneConstructorAndNoOperationalMethods()
    {
        var type = typeof(OperationHistoryCorrelationAttribution);

        var constructor = Assert.Single(
            type.GetConstructors(
                BindingFlags.Public |
                BindingFlags.Instance));

        var parameters = constructor.GetParameters();

        Assert.Equal(2, parameters.Length);
        Assert.Equal("historyEvent", parameters[0].Name);
        Assert.Equal(
            typeof(OperationHistoryEvent),
            parameters[0].ParameterType);
        Assert.Equal("correlation", parameters[1].Name);
        Assert.Equal(
            typeof(OperationCorrelationId),
            parameters[1].ParameterType);

        Assert.All(
            parameters,
            parameter => Assert.False(parameter.IsOptional));

        var methods = type.GetMethods(
            BindingFlags.Public |
            BindingFlags.Instance |
            BindingFlags.Static |
            BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .ToArray();

        Assert.Empty(methods);
        Assert.Empty(type.GetInterfaces());
    }
}
