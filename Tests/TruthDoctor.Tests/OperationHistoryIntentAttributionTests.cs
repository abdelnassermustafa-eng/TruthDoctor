using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

/// <summary>
/// Test-first history association of caller-declared operation intent.
/// Declaring intent does not independently verify motive, authorization,
/// approval, execution, resource state, or a successful outcome.
/// </summary>
public sealed class OperationHistoryIntentAttributionTests
{
    private static OperationHistoryEvent Event(
        string operationId = "operation-intent-42")
    {
        return new OperationHistoryEvent(
            "history-intent-001",
            new OperationCorrelationId(operationId),
            "IntentReported");
    }

    private static OperationIntent Intent(
        string capability = "compute.instance.start",
        string description = "Start instance",
        string? reason = "Scheduled maintenance")
    {
        return new OperationIntent(
            new UniversalCapabilityIdentity(capability),
            description,
            reason);
    }

    private static OperationHistoryIntentAttribution Create()
    {
        return new OperationHistoryIntentAttribution(
            Event(), Intent());
    }

    [Fact]
    public void PreservesExactHistoryEventReference()
    {
        var historyEvent = Event();
        var result = new OperationHistoryIntentAttribution(
            historyEvent, Intent());

        Assert.Same(historyEvent, result.Event);
        Assert.Equal("history-intent-001", result.Event.EventId);
    }

    [Fact]
    public void PreservesExactOperationIntentReference()
    {
        var intent = Intent();
        var result = new OperationHistoryIntentAttribution(
            Event(), intent);

        Assert.Same(intent, result.Intent);
    }

    [Fact]
    public void PreservesCapabilityDescriptionAndReason()
    {
        var result = Create();

        Assert.Equal(
            new UniversalCapabilityIdentity("compute.instance.start"),
            result.Intent.Capability);
        Assert.Equal("Start instance", result.Intent.Description);
        Assert.Equal("Scheduled maintenance", result.Intent.Reason);
    }

    [Fact]
    public void PreservesOmittedOptionalReason()
    {
        var intent = Intent(reason: null);
        var result = new OperationHistoryIntentAttribution(
            Event(), intent);

        Assert.Same(intent, result.Intent);
        Assert.Null(result.Intent.Reason);
    }

    [Fact]
    public void PreservesNormalizedDescriptionAndReason()
    {
        var intent = Intent(
            description: "  Start PROD Instance  ",
            reason: "  Maintenance Window  ");

        var result = new OperationHistoryIntentAttribution(
            Event(), intent);

        Assert.Same(intent, result.Intent);
        Assert.Equal(
            "Start PROD Instance",
            result.Intent.Description);
        Assert.Equal(
            "Maintenance Window",
            result.Intent.Reason);
    }

    [Fact]
    public void PreservesDistinctCapabilityIdentities()
    {
        var firstIntent = Intent(
            capability: "compute.instance.start");
        var secondIntent = Intent(
            capability: "compute.instance.stop");

        var first = new OperationHistoryIntentAttribution(
            Event("operation-first"), firstIntent);
        var second = new OperationHistoryIntentAttribution(
            Event("operation-second"), secondIntent);

        Assert.Same(firstIntent, first.Intent);
        Assert.Same(secondIntent, second.Intent);
        Assert.NotEqual(
            first.Intent.Capability,
            second.Intent.Capability);
    }

    [Fact]
    public void RejectsNullHistoryEvent()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationHistoryIntentAttribution(
                null!, Intent()));
    }

    [Fact]
    public void RejectsNullOperationIntent()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationHistoryIntentAttribution(
                Event(), null!));
    }

    [Fact]
    public void DoesNotRequireOperationPlanReview()
    {
        var intent = Intent(
            reason: null);
        var result = new OperationHistoryIntentAttribution(
            Event(), intent);

        Assert.Same(intent, result.Intent);
        Assert.Null(result.Intent.Reason);
    }

    [Fact]
    public void DoesNotInferAuthorizationOrSuccessfulOutcome()
    {
        var properties = typeof(OperationHistoryIntentAttribution)
            .GetProperties(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly);

        Assert.DoesNotContain(
            properties,
            property => property.Name is
                "Approved" or "Authorized" or "Executed" or
                "Succeeded" or "Verified" or "ActualOutcome" or
                "Approval" or "ExecutionResult");
    }

    [Fact]
    public void IsPublicSealedProviderNeutralReferenceType()
    {
        var type = typeof(OperationHistoryIntentAttribution);

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
        var properties = typeof(OperationHistoryIntentAttribution)
            .GetProperties(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly);

        Assert.Equal(2, properties.Length);

        var expected = new (string Name, Type Type)[]
        {
            ("Event", typeof(OperationHistoryEvent)),
            ("Intent", typeof(OperationIntent))
        };

        foreach (var (name, propertyType) in expected)
        {
            var property = Assert.Single(
                properties,
                item => item.Name == name);

            Assert.Equal(propertyType, property.PropertyType);
            Assert.NotNull(property.GetMethod);
            Assert.Null(property.SetMethod);
        }
    }

    [Fact]
    public void HasExactlyOneTwoArgumentConstructor()
    {
        var constructor = Assert.Single(
            typeof(OperationHistoryIntentAttribution)
                .GetConstructors(
                    BindingFlags.Public |
                    BindingFlags.Instance));

        var parameters = constructor.GetParameters();

        Assert.Equal(2, parameters.Length);
        Assert.Equal("historyEvent", parameters[0].Name);
        Assert.Equal(
            typeof(OperationHistoryEvent),
            parameters[0].ParameterType);
        Assert.Equal("intent", parameters[1].Name);
        Assert.Equal(
            typeof(OperationIntent),
            parameters[1].ParameterType);

        Assert.All(
            parameters,
            parameter => Assert.False(parameter.IsOptional));
    }

    [Fact]
    public void ExposesNoPublicOperationalMethods()
    {
        var methods = typeof(OperationHistoryIntentAttribution)
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
            typeof(OperationHistoryIntentAttribution)
                .GetInterfaces());
    }
}
