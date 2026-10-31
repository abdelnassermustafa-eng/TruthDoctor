using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

/// <summary>
/// Test-first contract for a declared, provider-neutral history event.
/// Construction must not authenticate historical truth or execute work.
/// </summary>
public sealed class OperationHistoryEventTests
{
    private static OperationCorrelationId Operation(
        string value = "operation-42")
    {
        return new OperationCorrelationId(value);
    }

    private static OperationHistoryEvent Create(
        string eventId = "event-001",
        OperationCorrelationId? operation = null,
        string eventKind = "ActionReported")
    {
        return new OperationHistoryEvent(
            eventId,
            operation ?? Operation(),
            eventKind);
    }

    [Fact]
    public void PreservesNormalizedEventId()
    {
        var result = Create(eventId: "  Event-ABC  ");

        Assert.Equal("Event-ABC", result.EventId);
    }

    [Fact]
    public void PreservesNormalizedEventKind()
    {
        var result = Create(eventKind: "  ActionObserved  ");

        Assert.Equal("ActionObserved", result.EventKind);
    }

    [Fact]
    public void PreservesOriginalCase()
    {
        var result = Create(
            eventId: "Event-MiXeD",
            eventKind: "ServiceRestartReported");

        Assert.Equal("Event-MiXeD", result.EventId);
        Assert.Equal(
            "ServiceRestartReported",
            result.EventKind);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void RejectsNullOrBlankEventId(string? value)
    {
        Assert.Throws<ArgumentException>(
            () => Create(eventId: value!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void RejectsNullOrBlankEventKind(string? value)
    {
        Assert.Throws<ArgumentException>(
            () => Create(eventKind: value!));
    }

    [Theory]
    [InlineData("event\nid")]
    [InlineData("event\rid")]
    [InlineData("event\tid")]
    [InlineData("event\0id")]
    public void RejectsControlCharactersInEventId(string value)
    {
        Assert.Throws<ArgumentException>(
            () => Create(eventId: value));
    }

    [Theory]
    [InlineData("Action\nReported")]
    [InlineData("Action\rReported")]
    [InlineData("Action\tReported")]
    [InlineData("Action\0Reported")]
    public void RejectsControlCharactersInEventKind(string value)
    {
        Assert.Throws<ArgumentException>(
            () => Create(eventKind: value));
    }

    [Fact]
    public void PreservesExplicitOperationCorrelation()
    {
        var operation = Operation("operation-99");
        var result = Create(operation: operation);

        Assert.Equal(operation, result.Operation);
        Assert.Equal("operation-99", result.Operation.Value);
    }

    [Fact]
    public void RejectsUninitializedOperationCorrelation()
    {
        Assert.Throws<ArgumentException>(
            () => Create(operation: default(OperationCorrelationId)));
    }

    [Fact]
    public void AcceptsEquivalentOperationIdentity()
    {
        var first = Operation("shared-operation");
        var second = Operation("shared-operation");

        Assert.Equal(first, second);
        Assert.Equal(
            first,
            Create(operation: second).Operation);
    }

    [Fact]
    public void IsPublicSealedProviderNeutralReferenceType()
    {
        var type = typeof(OperationHistoryEvent);

        Assert.True(type.IsPublic);
        Assert.True(type.IsClass);
        Assert.True(type.IsSealed);
        Assert.Equal(
            "TruthDoctor.Models.Platform",
            type.Namespace);
    }

    [Fact]
    public void ExposesExactlyThreeGetterOnlyProperties()
    {
        var type = typeof(OperationHistoryEvent);

        var properties = type.GetProperties(
            BindingFlags.Public |
            BindingFlags.Instance |
            BindingFlags.DeclaredOnly);

        Assert.Equal(3, properties.Length);

        var expected = new (string Name, Type Type)[]
        {
            ("EventId", typeof(string)),
            ("Operation", typeof(OperationCorrelationId)),
            ("EventKind", typeof(string))
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
    public void HasExactlyOneThreeArgumentConstructor()
    {
        var constructor = Assert.Single(
            typeof(OperationHistoryEvent).GetConstructors(
                BindingFlags.Public | BindingFlags.Instance));

        var parameters = constructor.GetParameters();

        var expectedNames = new[]
        {
            "eventId", "operation", "eventKind"
        };

        var expectedTypes = new[]
        {
            typeof(string),
            typeof(OperationCorrelationId),
            typeof(string)
        };

        Assert.Equal(3, parameters.Length);

        for (var index = 0; index < parameters.Length; index++)
        {
            Assert.Equal(
                expectedNames[index],
                parameters[index].Name);
            Assert.Equal(
                expectedTypes[index],
                parameters[index].ParameterType);
            Assert.False(parameters[index].IsOptional);
        }
    }

    [Fact]
    public void ExposesNoPublicOperationalMethods()
    {
        var methods = typeof(OperationHistoryEvent).GetMethods(
            BindingFlags.Public |
            BindingFlags.Instance |
            BindingFlags.Static |
            BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName);

        Assert.Empty(methods);
    }

    [Fact]
    public void DoesNotDeclareIntegrationInterfaces()
    {
        Assert.Empty(
            typeof(OperationHistoryEvent).GetInterfaces());
    }
}
