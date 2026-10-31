using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

/// <summary>
/// Test-first contract for a caller-declared history action.
/// Action attribution never authenticates, authorizes or proves execution.
/// </summary>
public sealed class OperationHistoryActionAttributionTests
{
    private static OperationHistoryEvent Event(
        string eventKind = "OperationReported")
    {
        return new OperationHistoryEvent(
            "history-action-001",
            new OperationCorrelationId("operation-action-42"),
            eventKind);
    }

    private static OperationHistoryActionAttribution Create(
        string action = "Restart instance")
    {
        return new OperationHistoryActionAttribution(
            Event(), action);
    }

    [Fact]
    public void PreservesExactHistoryEventReference()
    {
        var historyEvent = Event();
        var result = new OperationHistoryActionAttribution(
            historyEvent, "Restart instance");

        Assert.Same(historyEvent, result.Event);
        Assert.Equal("history-action-001", result.Event.EventId);
    }

    [Fact]
    public void PreservesActionDescription()
    {
        var result = Create("Restart instance");

        Assert.Equal("Restart instance", result.Action);
    }

    [Fact]
    public void TrimsActionAndPreservesCase()
    {
        var result = Create("  ReStart INSTANCE  ");

        Assert.Equal("ReStart INSTANCE", result.Action);
    }

    [Fact]
    public void PreservesInternalActionWhitespace()
    {
        var result = Create("Restart  production   instance");

        Assert.Equal(
            "Restart  production   instance",
            result.Action);
    }

    [Fact]
    public void RejectsNullHistoryEvent()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationHistoryActionAttribution(
                null!, "Restart instance"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void RejectsNullOrBlankAction(string? action)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationHistoryActionAttribution(
                Event(), action!));
    }

    [Theory]
    [InlineData("Restart\ninstance")]
    [InlineData("Restart\rinstance")]
    [InlineData("Restart\tinstance")]
    [InlineData("Restart\0instance")]
    [InlineData("Restart\u007finstance")]
    public void RejectsEmbeddedActionControlCharacters(
        string action)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationHistoryActionAttribution(
                Event(), action));
    }

    [Theory]
    [InlineData("Restart instance")]
    [InlineData("Scale compute group")]
    [InlineData("Rotate credential")]
    [InlineData("Custom.Provider.Action/Version2")]
    public void AcceptsProviderNeutralActionDescriptions(
        string action)
    {
        var result = Create(action);

        Assert.Equal(action, result.Action);
    }

    [Fact]
    public void KeepsActionDistinctFromEventKind()
    {
        var historyEvent = Event("ChangeRequested");
        var result = new OperationHistoryActionAttribution(
            historyEvent, "Restart instance");

        Assert.Equal("ChangeRequested", result.Event.EventKind);
        Assert.Equal("Restart instance", result.Action);
        Assert.NotEqual(result.Event.EventKind, result.Action);
    }

    [Fact]
    public void DoesNotInferExecutionOrSuccess()
    {
        var result = Create("Restart instance");
        var properties = result.GetType().GetProperties(
            BindingFlags.Public |
            BindingFlags.Instance |
            BindingFlags.DeclaredOnly);

        Assert.Equal("Restart instance", result.Action);
        Assert.DoesNotContain(
            properties,
            property => property.Name is
                "Executed" or "Succeeded" or "Approved" or
                "Authorized" or "Attempted" or "Outcome");
    }

    [Fact]
    public void IsPublicSealedProviderNeutralReferenceType()
    {
        var type = typeof(OperationHistoryActionAttribution);

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
        var properties = typeof(OperationHistoryActionAttribution)
            .GetProperties(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly);

        Assert.Equal(2, properties.Length);

        var expected = new (string Name, Type Type)[]
        {
            ("Event", typeof(OperationHistoryEvent)),
            ("Action", typeof(string))
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
            typeof(OperationHistoryActionAttribution)
                .GetConstructors(
                    BindingFlags.Public |
                    BindingFlags.Instance));

        var parameters = constructor.GetParameters();

        Assert.Equal(2, parameters.Length);

        Assert.Equal("historyEvent", parameters[0].Name);
        Assert.Equal(
            typeof(OperationHistoryEvent),
            parameters[0].ParameterType);

        Assert.Equal("action", parameters[1].Name);
        Assert.Equal(typeof(string), parameters[1].ParameterType);

        Assert.All(
            parameters,
            parameter => Assert.False(parameter.IsOptional));
    }

    [Fact]
    public void ExposesNoPublicOperationalMethods()
    {
        var methods = typeof(OperationHistoryActionAttribution)
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
            typeof(OperationHistoryActionAttribution)
                .GetInterfaces());
    }
}
