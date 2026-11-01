using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationHistoryRetentionTests
{
    private static OperationHistoryEvent Event() =>
        new(
            "retention-event-01",
            new OperationCorrelationId("retention-operation-01"),
            "OperationRecorded");

    private static OperationHistoryRetention Create(
        OperationHistoryEvent historyEvent,
        OperationHistoryRetentionMode mode,
        DateTimeOffset? retainUntil) =>
        new(historyEvent, mode, retainUntil);

    private static DateTimeOffset Deadline() =>
        new(2034, 6, 15, 17, 45, 0, TimeSpan.FromHours(3));

    [Fact]
    public void RejectsNullHistoryEvent()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Create(null!, OperationHistoryRetentionMode.Indefinite, null));
    }

    [Fact]
    public void PreservesExactHistoryEventReference()
    {
        var historyEvent = Event();
        var retention = Create(
            historyEvent, OperationHistoryRetentionMode.Indefinite, null);

        Assert.Same(historyEvent, retention.Event);
    }

    [Fact]
    public void PreservesHistoryEventIdentityAndKind()
    {
        var historyEvent = Event();
        var retention = Create(
            historyEvent, OperationHistoryRetentionMode.Indefinite, null);

        Assert.Equal("retention-event-01", retention.Event.EventId);
        Assert.Equal(
            new OperationCorrelationId("retention-operation-01"),
            retention.Event.Operation);
        Assert.Equal("OperationRecorded", retention.Event.EventKind);
    }

    [Fact]
    public void PreservesExplicitIndefiniteMode()
    {
        var retention = Create(
            Event(), OperationHistoryRetentionMode.Indefinite, null);

        Assert.Equal(
            OperationHistoryRetentionMode.Indefinite, retention.Mode);
        Assert.Null(retention.RetainUntil);
        Assert.Equal(0, (int)OperationHistoryRetentionMode.Indefinite);
    }

    [Fact]
    public void IndefiniteRequiresNullRetentionDeadline()
    {
        Assert.Throws<ArgumentException>(() =>
            Create(
                Event(),
                OperationHistoryRetentionMode.Indefinite,
                Deadline()));
    }

    [Fact]
    public void FiniteModeRequiresExplicitDeadline()
    {
        Assert.Throws<ArgumentException>(() =>
            Create(Event(), OperationHistoryRetentionMode.Until, null));
    }

    [Fact]
    public void PreservesFiniteRetentionDeadline()
    {
        var deadline = Deadline();
        var retention = Create(
            Event(), OperationHistoryRetentionMode.Until, deadline);

        Assert.Equal(OperationHistoryRetentionMode.Until, retention.Mode);
        Assert.Equal(deadline, retention.RetainUntil);
        Assert.Equal(1, (int)OperationHistoryRetentionMode.Until);
    }

    [Fact]
    public void PreservesOriginalDeadlineOffset()
    {
        var deadline = new DateTimeOffset(
            2034, 6, 15, 17, 45, 0, TimeSpan.FromHours(-7));
        var retention = Create(
            Event(), OperationHistoryRetentionMode.Until, deadline);

        Assert.True(retention.RetainUntil.HasValue);
        Assert.Equal(deadline.Offset, retention.RetainUntil.Value.Offset);
        Assert.Equal(deadline.Ticks, retention.RetainUntil.Value.Ticks);
    }

    [Fact]
    public void AcceptsExplicitDefaultDeadline()
    {
        var retention = Create(
            Event(),
            OperationHistoryRetentionMode.Until,
            default(DateTimeOffset));

        Assert.True(retention.RetainUntil.HasValue);
        Assert.Equal(
            default(DateTimeOffset), retention.RetainUntil.Value);
    }

    [Fact]
    public void RejectsUndefinedRetentionMode()
    {
        Assert.Throws<ArgumentException>(() =>
            Create(Event(), (OperationHistoryRetentionMode)(-1), null));

        Assert.Throws<ArgumentException>(() =>
            Create(
                Event(), (OperationHistoryRetentionMode)99, Deadline()));
    }

    [Fact]
    public void DoesNotInferDeadlineFromHistoryTimestamps()
    {
        var historyEvent = Event();
        var timestamps = new OperationHistoryTimestampAttribution(
            historyEvent,
            new DateTimeOffset(
                2020, 1, 1, 9, 0, 0, TimeSpan.FromHours(-8)),
            new DateTimeOffset(
                2024, 1, 1, 9, 0, 0, TimeSpan.FromHours(2)));

        var indefinite = Create(
            timestamps.Event,
            OperationHistoryRetentionMode.Indefinite,
            null);

        var finite = Create(
            timestamps.Event,
            OperationHistoryRetentionMode.Until,
            Deadline());

        Assert.Same(historyEvent, indefinite.Event);
        Assert.Null(indefinite.RetainUntil);
        Assert.Same(historyEvent, finite.Event);
        Assert.Equal(Deadline(), finite.RetainUntil);
        Assert.NotEqual(
            timestamps.OccurredAt, finite.RetainUntil);
        Assert.NotEqual(
            timestamps.RecordedAt, finite.RetainUntil);
    }

    [Fact]
    public void DoesNotReadOrEvaluateCurrentClock()
    {
        var past = new DateTimeOffset(
            2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var future = new DateTimeOffset(
            2090, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var pastRetention = Create(
            Event(), OperationHistoryRetentionMode.Until, past);
        var futureRetention = Create(
            Event(), OperationHistoryRetentionMode.Until, future);

        Assert.Equal(OperationHistoryRetentionMode.Until, pastRetention.Mode);
        Assert.Equal(OperationHistoryRetentionMode.Until, futureRetention.Mode);
        Assert.Equal(past, pastRetention.RetainUntil);
        Assert.Equal(future, futureRetention.RetainUntil);
    }

    [Fact]
    public void IsPublicSealedProviderNeutralReferenceType()
    {
        var type = typeof(OperationHistoryRetention);
        var modeType = typeof(OperationHistoryRetentionMode);

        Assert.True(type.IsPublic);
        Assert.True(type.IsClass);
        Assert.True(type.IsSealed);
        Assert.False(type.IsAbstract);
        Assert.Equal(
            "TruthDoctor.Models.Platform", type.Namespace);

        Assert.True(modeType.IsPublic);
        Assert.True(modeType.IsEnum);
        Assert.Equal(
            new[] { "Indefinite", "Until" },
            Enum.GetNames(modeType));
        Assert.Equal(
            new[] { 0, 1 },
            Enum.GetValues<OperationHistoryRetentionMode>()
                .Select(value => (int)value).ToArray());
    }

    [Fact]
    public void ExposesExactlyThreeGetterOnlyProperties()
    {
        var properties = typeof(OperationHistoryRetention)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance |
                           BindingFlags.DeclaredOnly)
            .OrderBy(property => property.Name)
            .ToArray();

        Assert.Equal(
            new[] { "Event", "Mode", "RetainUntil" },
            properties.Select(property => property.Name).ToArray());

        Assert.Equal(
            typeof(OperationHistoryEvent),
            properties.Single(property => property.Name == "Event")
                .PropertyType);

        Assert.Equal(
            typeof(OperationHistoryRetentionMode),
            properties.Single(property => property.Name == "Mode")
                .PropertyType);

        Assert.Equal(
            typeof(DateTimeOffset?),
            properties.Single(property => property.Name == "RetainUntil")
                .PropertyType);

        Assert.All(properties, property =>
        {
            Assert.NotNull(property.GetMethod);
            Assert.True(property.GetMethod!.IsPublic);
            Assert.Null(property.SetMethod);
        });
    }

    [Fact]
    public void HasExactlyOneThreeArgumentConstructor()
    {
        var constructors = typeof(OperationHistoryRetention)
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        var constructor = Assert.Single(constructors);
        var parameters = constructor.GetParameters();

        Assert.Equal(3, parameters.Length);
        Assert.Equal(
            new[] { "historyEvent", "mode", "retainUntil" },
            parameters.Select(parameter => parameter.Name).ToArray());

        Assert.Equal(
            typeof(OperationHistoryEvent), parameters[0].ParameterType);
        Assert.Equal(
            typeof(OperationHistoryRetentionMode),
            parameters[1].ParameterType);
        Assert.Equal(
            typeof(DateTimeOffset?), parameters[2].ParameterType);
    }

    [Fact]
    public void ExposesNoOperationalMethodsOrIntegrationInterfaces()
    {
        var type = typeof(OperationHistoryRetention);
        var modeType = typeof(OperationHistoryRetentionMode);

        Assert.Empty(type.GetInterfaces());
        Assert.DoesNotContain(
            type.GetMethods(
                BindingFlags.Public | BindingFlags.Instance |
                BindingFlags.Static | BindingFlags.DeclaredOnly),
            method => !method.IsSpecialName);

        Assert.Empty(
            type.GetFields(
                BindingFlags.Public | BindingFlags.Instance |
                BindingFlags.Static | BindingFlags.DeclaredOnly));

        Assert.Equal(
            3,
            type.GetProperties(
                BindingFlags.Public | BindingFlags.Instance |
                BindingFlags.DeclaredOnly).Length);

        Assert.Equal(
            2, Enum.GetNames(modeType).Length);
    }
}
