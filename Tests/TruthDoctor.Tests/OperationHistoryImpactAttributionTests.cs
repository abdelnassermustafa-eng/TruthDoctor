using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

/// <summary>
/// Test-first historical association of a caller-declared impact estimate.
/// Estimates are not observed changes or proof of execution success.
/// </summary>
public sealed class OperationHistoryImpactAttributionTests
{
    private static OperationHistoryEvent Event(
        string operationId = "operation-impact-001")
    {
        return new OperationHistoryEvent(
            "history-impact-001",
            new OperationCorrelationId(operationId),
            "ImpactEstimated");
    }

    private static OperationStepImpact Step(
        string stepId = "step-001",
        OperationImpactSeverity severity = OperationImpactSeverity.Unknown,
        OperationImpactConfidence confidence = OperationImpactConfidence.Unknown,
        int? affectedCount = null,
        bool? disruption = null,
        string? rationale = null)
    {
        return new OperationStepImpact(
            stepId, severity, confidence,
            affectedCount, disruption, rationale);
    }

    private static OperationEstimatedImpact Impact(
        params OperationStepImpact[] steps)
    {
        return new OperationEstimatedImpact(steps);
    }

    private static OperationHistoryImpactAttribution Create()
    {
        return new OperationHistoryImpactAttribution(
            Event(), Impact());
    }

    [Fact]
    public void PreservesExactHistoryEventReference()
    {
        var historyEvent = Event();
        var result = new OperationHistoryImpactAttribution(
            historyEvent, Impact());

        Assert.Same(historyEvent, result.Event);
    }

    [Fact]
    public void PreservesExactEstimatedImpactReference()
    {
        var estimatedImpact = Impact(Step());
        var result = new OperationHistoryImpactAttribution(
            Event(), estimatedImpact);

        Assert.Same(estimatedImpact, result.EstimatedImpact);
    }

    [Fact]
    public void PreservesEventIdentityAndOperation()
    {
        var historyEvent = Event("operation-distinct-42");
        var result = new OperationHistoryImpactAttribution(
            historyEvent, Impact());

        Assert.Equal("history-impact-001", result.Event.EventId);
        Assert.Equal(historyEvent.Operation, result.Event.Operation);
        Assert.Equal("ImpactEstimated", result.Event.EventKind);
    }

    [Fact]
    public void PreservesEmptyEstimateWithoutInventingEffect()
    {
        var estimatedImpact = Impact();
        var result = new OperationHistoryImpactAttribution(
            Event(), estimatedImpact);

        Assert.Same(estimatedImpact, result.EstimatedImpact);
        Assert.Empty(result.EstimatedImpact.Impacts);
    }

    [Fact]
    public void PreservesSingleStepImpactReference()
    {
        var step = Step("step-only");
        var estimatedImpact = Impact(step);
        var result = new OperationHistoryImpactAttribution(
            Event(), estimatedImpact);

        Assert.Same(step, Assert.Single(result.EstimatedImpact.Impacts));
    }

    [Fact]
    public void PreservesMultipleStepDeclarationOrder()
    {
        var first = Step("step-alpha");
        var second = Step("step-beta");
        var third = Step("step-gamma");
        var estimatedImpact = Impact(first, second, third);

        var result = new OperationHistoryImpactAttribution(
            Event(), estimatedImpact);

        Assert.Equal(3, result.EstimatedImpact.Impacts.Count);
        Assert.Same(first, result.EstimatedImpact.Impacts[0]);
        Assert.Same(second, result.EstimatedImpact.Impacts[1]);
        Assert.Same(third, result.EstimatedImpact.Impacts[2]);
    }

    [Fact]
    public void PreservesUnknownAndExplicitNoneSeverity()
    {
        var unknown = Step(
            "step-unknown",
            OperationImpactSeverity.Unknown);
        var none = Step(
            "step-none",
            OperationImpactSeverity.None);
        var result = new OperationHistoryImpactAttribution(
            Event(), Impact(unknown, none));

        Assert.Equal(
            OperationImpactSeverity.Unknown,
            result.EstimatedImpact.Impacts[0].Severity);
        Assert.Equal(
            OperationImpactSeverity.None,
            result.EstimatedImpact.Impacts[1].Severity);
    }

    [Fact]
    public void PreservesEstimatedDimensionsWithoutRecalculation()
    {
        var step = Step(
            "step-high",
            OperationImpactSeverity.High,
            OperationImpactConfidence.Medium,
            affectedCount: 3,
            disruption: true,
            rationale: "  Planned maintenance  ");

        var result = new OperationHistoryImpactAttribution(
            Event(), Impact(step));

        var retained = Assert.Single(result.EstimatedImpact.Impacts);

        Assert.Same(step, retained);
        Assert.Equal(OperationImpactSeverity.High, retained.Severity);
        Assert.Equal(OperationImpactConfidence.Medium, retained.Confidence);
        Assert.Equal(3, retained.EstimatedAffectedResourceCount);
        Assert.True(retained.AvailabilityDisruptionExpected);
        Assert.Equal("Planned maintenance", retained.Rationale);
    }

    [Fact]
    public void RejectsNullHistoryEvent()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationHistoryImpactAttribution(
                null!, Impact()));
    }

    [Fact]
    public void RejectsNullEstimatedImpact()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationHistoryImpactAttribution(
                Event(), null!));
    }

    [Fact]
    public void DoesNotInferExecutionOrActualResourceChange()
    {
        var result = Create();
        Assert.Empty(result.EstimatedImpact.Impacts);

        var properties = typeof(OperationHistoryImpactAttribution)
            .GetProperties(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly);

        foreach (var name in new[]
        {
            "Executed", "Succeeded", "ActualImpact",
            "ObservedEffect", "StateChanged", "Verified",
            "Evidence", "BeforeState", "AfterState"
        })
        {
            Assert.DoesNotContain(
                properties, property => property.Name == name);
        }
    }

    [Fact]
    public void IsPublicSealedProviderNeutralReferenceType()
    {
        var type = typeof(OperationHistoryImpactAttribution);

        Assert.True(type.IsPublic);
        Assert.True(type.IsClass);
        Assert.True(type.IsSealed);
        Assert.Equal("TruthDoctor.Models.Platform", type.Namespace);
    }

    [Fact]
    public void ExposesExactlyTwoGetterOnlyProperties()
    {
        var properties = typeof(OperationHistoryImpactAttribution)
            .GetProperties(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly);

        Assert.Equal(2, properties.Length);

        var expected = new (string Name, Type Type)[]
        {
            ("Event", typeof(OperationHistoryEvent)),
            ("EstimatedImpact", typeof(OperationEstimatedImpact))
        };

        Assert.Equal(
            expected.Select(item => item.Name).OrderBy(name => name),
            properties.Select(item => item.Name).OrderBy(name => name));

        foreach (var (name, expectedType) in expected)
        {
            var property = Assert.Single(
                properties, item => item.Name == name);

            Assert.Equal(expectedType, property.PropertyType);
            Assert.NotNull(property.GetMethod);
            Assert.Null(property.SetMethod);
        }
    }

    [Fact]
    public void HasExactlyOneTwoArgumentConstructor()
    {
        var constructor = Assert.Single(
            typeof(OperationHistoryImpactAttribution)
                .GetConstructors(
                    BindingFlags.Public |
                    BindingFlags.Instance));

        var parameters = constructor.GetParameters();

        Assert.Equal(2, parameters.Length);
        Assert.Equal("historyEvent", parameters[0].Name);
        Assert.Equal(
            typeof(OperationHistoryEvent),
            parameters[0].ParameterType);
        Assert.Equal("estimatedImpact", parameters[1].Name);
        Assert.Equal(
            typeof(OperationEstimatedImpact),
            parameters[1].ParameterType);
        Assert.All(
            parameters,
            parameter => Assert.False(parameter.IsOptional));
    }

    [Fact]
    public void ExposesNoPublicOperationalMethodsOrInterfaces()
    {
        var type = typeof(OperationHistoryImpactAttribution);

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
