using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

/// <summary>
/// Contract tests for declarative history reporter attribution.
/// Attribution does not authenticate, authorize, or prove execution.
/// </summary>
public sealed class OperationHistoryActorAttributionTests
{
    private static OperationHistoryEvent Event(
        string id = "history-001")
    {
        return new OperationHistoryEvent(
            id,
            new OperationCorrelationId("operation-42"),
            "ActionReported");
    }

    private static OperationActorIdentity Reporter(
        OperationActorKind kind = OperationActorKind.Human,
        string value = "operator-17")
    {
        return new OperationActorIdentity(kind, value);
    }

    private static OperationHistoryActorAttribution Create(
        OperationHistoryEvent? historyEvent = null,
        OperationActorIdentity? reporter = null)
    {
        return new OperationHistoryActorAttribution(
            historyEvent ?? Event(),
            reporter ?? Reporter());
    }

    [Fact]
    public void PreservesExactHistoryEventReference()
    {
        var historyEvent = Event("history-99");
        var result = Create(historyEvent: historyEvent);

        Assert.Same(historyEvent, result.Event);
        Assert.Equal("history-99", result.Event.EventId);
        Assert.Equal(
            new OperationCorrelationId("operation-42"),
            result.Event.Operation);
    }

    [Fact]
    public void PreservesHumanReporterIdentity()
    {
        var reporter = Reporter(
            OperationActorKind.Human, "human-123");

        var result = Create(reporter: reporter);

        Assert.Equal(reporter, result.Reporter);
        Assert.Equal(
            OperationActorKind.Human,
            result.Reporter.Kind);
        Assert.Equal("human-123", result.Reporter.Value);
    }

    [Fact]
    public void PreservesServiceReporterIdentity()
    {
        var reporter = Reporter(
            OperationActorKind.Service, "service-abc");

        var result = Create(reporter: reporter);

        Assert.Equal(reporter, result.Reporter);
        Assert.Equal(
            OperationActorKind.Service,
            result.Reporter.Kind);
        Assert.Equal("service-abc", result.Reporter.Value);
    }

    [Fact]
    public void RejectsNullHistoryEvent()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationHistoryActorAttribution(
                null!, Reporter()));
    }

    [Fact]
    public void RejectsUninitializedReporter()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationHistoryActorAttribution(
                Event(), default(OperationActorIdentity)));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    [InlineData(999)]
    public void RejectsUndefinedReporterKind(int rawKind)
    {
        object boxed = default(OperationActorIdentity);

        var fields = typeof(OperationActorIdentity).GetFields(
            BindingFlags.Instance | BindingFlags.NonPublic);

        var kindField = Assert.Single(
            fields,
            field => field.FieldType == typeof(OperationActorKind));

        var valueField = Assert.Single(
            fields,
            field => field.FieldType == typeof(string));

        kindField.SetValue(
            boxed, (OperationActorKind)rawKind);
        valueField.SetValue(boxed, "reporter-123");

        var invalid = (OperationActorIdentity)boxed;

        Assert.Throws<ArgumentException>(() =>
            new OperationHistoryActorAttribution(
                Event(), invalid));
    }

    [Fact]
    public void PreservesActorCaseAndOpaqueIdentifier()
    {
        var reporter = Reporter(
            OperationActorKind.Service,
            "  Service/Group:ABC_7  ");

        var result = Create(reporter: reporter);

        Assert.Equal(
            "Service/Group:ABC_7",
            result.Reporter.Value);
        Assert.Equal(reporter, result.Reporter);
    }

    [Fact]
    public void SeparatesReporterFromExecutionClaims()
    {
        var historyEvent = Event();
        var reporter = Reporter(
            OperationActorKind.Human, "reporter-only");

        var result = Create(historyEvent, reporter);

        Assert.Same(historyEvent, result.Event);
        Assert.Equal(reporter, result.Reporter);
        Assert.Equal("ActionReported", result.Event.EventKind);

        var names = typeof(OperationHistoryActorAttribution)
            .GetProperties(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly)
            .Select(property => property.Name)
            .ToArray();

        Assert.DoesNotContain("Executor", names);
        Assert.DoesNotContain("Initiator", names);
        Assert.DoesNotContain("ExecutionSucceeded", names);
        Assert.DoesNotContain("Authenticated", names);
        Assert.DoesNotContain("Authorized", names);
    }

    [Fact]
    public void IsPublicSealedProviderNeutralReferenceType()
    {
        var type = typeof(OperationHistoryActorAttribution);

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
        var type = typeof(OperationHistoryActorAttribution);

        var properties = type.GetProperties(
            BindingFlags.Public |
            BindingFlags.Instance |
            BindingFlags.DeclaredOnly);

        Assert.Equal(2, properties.Length);

        var expected = new (string Name, Type Type)[]
        {
            ("Event", typeof(OperationHistoryEvent)),
            ("Reporter", typeof(OperationActorIdentity))
        };

        foreach (var (name, propertyType) in expected)
        {
            var property = type.GetProperty(name);

            Assert.NotNull(property);
            Assert.Equal(propertyType, property.PropertyType);
            Assert.NotNull(property.GetMethod);
            Assert.Null(property.SetMethod);
        }
    }

    [Fact]
    public void HasExactlyOneTwoArgumentConstructor()
    {
        var constructor = Assert.Single(
            typeof(OperationHistoryActorAttribution)
                .GetConstructors(
                    BindingFlags.Public | BindingFlags.Instance));

        var parameters = constructor.GetParameters();

        Assert.Equal(2, parameters.Length);

        Assert.Equal("historyEvent", parameters[0].Name);
        Assert.Equal(
            typeof(OperationHistoryEvent),
            parameters[0].ParameterType);
        Assert.False(parameters[0].IsOptional);

        Assert.Equal("reporter", parameters[1].Name);
        Assert.Equal(
            typeof(OperationActorIdentity),
            parameters[1].ParameterType);
        Assert.False(parameters[1].IsOptional);
    }

    [Fact]
    public void ExposesNoPublicOperationalMethods()
    {
        var methods = typeof(OperationHistoryActorAttribution)
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
            typeof(OperationHistoryActorAttribution)
                .GetInterfaces());
    }
}
