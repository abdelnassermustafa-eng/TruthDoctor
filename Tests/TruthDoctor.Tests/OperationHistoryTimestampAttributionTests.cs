using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

/// <summary>
/// Contract tests for caller-declared history event timestamps.
/// Timestamp attribution does not authenticate historical truth.
/// </summary>
public sealed class OperationHistoryTimestampAttributionTests
{
    private static OperationHistoryEvent Event()
    {
        return new OperationHistoryEvent(
            "history-001",
            new OperationCorrelationId("operation-42"),
            "EventReported");
    }

    private static DateTimeOffset Occurred()
    {
        return new DateTimeOffset(
            2026, 10, 28, 1, 20, 0,
            TimeSpan.FromHours(-7));
    }

    private static DateTimeOffset Recorded()
    {
        return new DateTimeOffset(
            2026, 10, 28, 1, 45, 0,
            TimeSpan.FromHours(-7));
    }

    private static OperationHistoryTimestampAttribution Create(
        OperationHistoryEvent? historyEvent = null,
        DateTimeOffset? occurredAt = null,
        DateTimeOffset? recordedAt = null)
    {
        return new OperationHistoryTimestampAttribution(
            historyEvent ?? Event(),
            occurredAt ?? Occurred(),
            recordedAt ?? Recorded());
    }

    [Fact]
    public void PreservesExactHistoryEventReference()
    {
        var historyEvent = Event();
        var result = Create(historyEvent: historyEvent);

        Assert.Same(historyEvent, result.Event);
        Assert.Equal("history-001", result.Event.EventId);
        Assert.Equal(
            new OperationCorrelationId("operation-42"),
            result.Event.Operation);
    }

    [Fact]
    public void PreservesOccurredAtInstantAndOffset()
    {
        var occurred = Occurred().ToOffset(
            TimeSpan.FromHours(3));

        var result = Create(occurredAt: occurred);

        Assert.Equal(occurred, result.OccurredAt);
        Assert.Equal(occurred.Offset, result.OccurredAt.Offset);
        Assert.Equal(occurred.Ticks, result.OccurredAt.Ticks);
    }

    [Fact]
    public void PreservesRecordedAtInstantAndOffset()
    {
        var recorded = Recorded().ToOffset(
            TimeSpan.FromHours(-4));

        var result = Create(recordedAt: recorded);

        Assert.Equal(recorded, result.RecordedAt);
        Assert.Equal(recorded.Offset, result.RecordedAt.Offset);
        Assert.Equal(recorded.Ticks, result.RecordedAt.Ticks);
    }

    [Fact]
    public void AcceptsDelayedRecording()
    {
        var occurred = Occurred();
        var recorded = occurred.AddDays(3);

        var result = Create(
            occurredAt: occurred,
            recordedAt: recorded);

        Assert.Equal(occurred, result.OccurredAt);
        Assert.Equal(recorded, result.RecordedAt);
        Assert.True(result.RecordedAt > result.OccurredAt);
    }

    [Fact]
    public void AllowsRecordedAtBeforeOccurredAt()
    {
        var occurred = Occurred();
        var recorded = occurred.AddMinutes(-15);

        var result = Create(
            occurredAt: occurred,
            recordedAt: recorded);

        Assert.Equal(occurred, result.OccurredAt);
        Assert.Equal(recorded, result.RecordedAt);
        Assert.True(result.RecordedAt < result.OccurredAt);
    }

    [Fact]
    public void AcceptsIdenticalTimestamps()
    {
        var instant = Occurred();

        var result = Create(
            occurredAt: instant,
            recordedAt: instant);

        Assert.Equal(instant, result.OccurredAt);
        Assert.Equal(instant, result.RecordedAt);
    }

    [Fact]
    public void PreservesExplicitDefaultTimestamps()
    {
        var result = new OperationHistoryTimestampAttribution(
            Event(),
            default(DateTimeOffset),
            default(DateTimeOffset));

        Assert.Equal(
            default(DateTimeOffset),
            result.OccurredAt);
        Assert.Equal(
            default(DateTimeOffset),
            result.RecordedAt);
    }

    [Fact]
    public void RejectsNullHistoryEvent()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationHistoryTimestampAttribution(
                null!,
                Occurred(),
                Recorded()));
    }

    [Fact]
    public void DoesNotReadOrReplaceCallerSuppliedTime()
    {
        var occurred = new DateTimeOffset(
            2001, 2, 3, 4, 5, 6,
            TimeSpan.FromHours(9));

        var recorded = new DateTimeOffset(
            2040, 7, 8, 9, 10, 11,
            TimeSpan.FromHours(-3));

        var result = Create(
            occurredAt: occurred,
            recordedAt: recorded);

        Assert.Equal(occurred, result.OccurredAt);
        Assert.Equal(recorded, result.RecordedAt);
        Assert.Equal(
            occurred.Offset, result.OccurredAt.Offset);
        Assert.Equal(
            recorded.Offset, result.RecordedAt.Offset);
    }

    [Fact]
    public void DoesNotNormalizeDifferentOffsets()
    {
        var occurred = new DateTimeOffset(
            2026, 10, 28, 1, 20, 0,
            TimeSpan.FromHours(5));

        var recorded = new DateTimeOffset(
            2026, 10, 28, 1, 45, 0,
            TimeSpan.FromHours(-6));

        var result = Create(
            occurredAt: occurred,
            recordedAt: recorded);

        Assert.Equal(
            TimeSpan.FromHours(5),
            result.OccurredAt.Offset);
        Assert.Equal(
            TimeSpan.FromHours(-6),
            result.RecordedAt.Offset);

        Assert.Equal(occurred.Ticks, result.OccurredAt.Ticks);
        Assert.Equal(recorded.Ticks, result.RecordedAt.Ticks);
    }

    [Fact]
    public void IsPublicSealedProviderNeutralReferenceType()
    {
        var type = typeof(OperationHistoryTimestampAttribution);

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
        var properties = typeof(OperationHistoryTimestampAttribution)
            .GetProperties(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly);

        Assert.Equal(3, properties.Length);

        var expected = new (string Name, Type Type)[]
        {
            ("Event", typeof(OperationHistoryEvent)),
            ("OccurredAt", typeof(DateTimeOffset)),
            ("RecordedAt", typeof(DateTimeOffset))
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
    public void HasExactlyOneThreeArgumentConstructor()
    {
        var constructor = Assert.Single(
            typeof(OperationHistoryTimestampAttribution)
                .GetConstructors(
                    BindingFlags.Public |
                    BindingFlags.Instance));

        var parameters = constructor.GetParameters();

        Assert.Equal(3, parameters.Length);

        Assert.Equal("historyEvent", parameters[0].Name);
        Assert.Equal(
            typeof(OperationHistoryEvent),
            parameters[0].ParameterType);

        Assert.Equal("occurredAt", parameters[1].Name);
        Assert.Equal(
            typeof(DateTimeOffset),
            parameters[1].ParameterType);

        Assert.Equal("recordedAt", parameters[2].Name);
        Assert.Equal(
            typeof(DateTimeOffset),
            parameters[2].ParameterType);

        Assert.All(
            parameters,
            parameter => Assert.False(parameter.IsOptional));
    }

    [Fact]
    public void ExposesNoPublicOperationalMethods()
    {
        var methods = typeof(OperationHistoryTimestampAttribution)
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
            typeof(OperationHistoryTimestampAttribution)
                .GetInterfaces());
    }
}
