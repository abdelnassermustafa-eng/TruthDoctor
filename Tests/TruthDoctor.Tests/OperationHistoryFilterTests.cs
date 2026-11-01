using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

/// <summary>
/// Test-first contract for exact, provider-neutral history filtering.
/// Filters only caller-supplied events. It never queries providers,
/// persists events, authenticates history, or authorizes execution.
/// </summary>
public sealed class OperationHistoryFilterTests
{
    private static OperationHistoryEvent Event(
        string eventId = "event-01",
        string operation = "operation-01",
        string eventKind = "ActionReported")
        => new(eventId, new OperationCorrelationId(operation), eventKind);

    private static IReadOnlyList<OperationHistoryEvent> Apply(
        IEnumerable<OperationHistoryEvent> events,
        string? eventId = null,
        OperationCorrelationId? operation = null,
        string? eventKind = null)
        => OperationHistoryFilter.Filter(
            events, eventId, operation, eventKind);

    [Fact]
    public void RejectsNullEventCollection()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Apply(null!));
    }

    [Fact]
    public void RejectsNullEventEntry()
    {
        var first = Event();
        var items = new OperationHistoryEvent[]
        {
            first,
            null!
        };

        Assert.Throws<ArgumentException>(() => Apply(items));
        Assert.Throws<ArgumentException>(() =>
            Apply(items, eventId: "nonmatching"));
    }

    [Fact]
    public void NoCriteriaPreservesAllEventsInOrder()
    {
        var first = Event("event-01");
        var second = Event("event-02");
        var third = Event("event-03");

        var result = Apply(new[] { first, second, third });

        Assert.Equal(3, result.Count);
        Assert.Same(first, result[0]);
        Assert.Same(second, result[1]);
        Assert.Same(third, result[2]);
    }

    [Fact]
    public void EmptyInputReturnsEmptyResult()
    {
        Assert.Empty(Apply(Array.Empty<OperationHistoryEvent>()));
        Assert.Empty(Apply(
            Array.Empty<OperationHistoryEvent>(),
            eventId: "event-01"));

        Assert.Empty(Apply(
            new[] { Event("event-02") },
            eventId: "event-99"));
    }

    [Fact]
    public void ExactEventIdSelectsMatchingEvents()
    {
        var match = Event("target-01");
        var other = Event("target-02");

        var result = Apply(
            new[] { other, match }, eventId: "target-01");

        Assert.Same(match, Assert.Single(result));
    }

    [Fact]
    public void ExactEventKindSelectsMatchingEvents()
    {
        var match = Event(eventKind: "ActionReported");
        var other = Event(eventKind: "ApprovalReported");

        var result = Apply(
            new[] { other, match },
            eventKind: "ActionReported");

        Assert.Same(match, Assert.Single(result));
    }

    [Fact]
    public void ExactOperationSelectsMatchingEvents()
    {
        var first = Event("event-01", "shared");
        var second = Event("event-02", "other");
        var third = Event("event-03", "shared");

        var result = Apply(
            new[] { first, second, third },
            operation: new OperationCorrelationId("shared"));

        Assert.Equal(2, result.Count);
        Assert.Same(first, result[0]);
        Assert.Same(third, result[1]);
    }

    [Fact]
    public void CaseSensitiveEventIdMatching()
    {
        var upper = Event("Event-ABC");
        var lower = Event("event-abc");

        var result = Apply(
            new[] { upper, lower },
            eventId: "Event-ABC");

        Assert.Same(upper, Assert.Single(result));
    }

    [Fact]
    public void CaseSensitiveEventKindMatching()
    {
        var upper = Event(eventKind: "ActionReported");
        var lower = Event(eventKind: "actionreported");

        var result = Apply(
            new[] { upper, lower },
            eventKind: "ActionReported");

        Assert.Same(upper, Assert.Single(result));
    }

    [Fact]
    public void CaseSensitiveOperationMatching()
    {
        var upper = Event("event-01", "Corr-ABC");
        var lower = Event("event-02", "corr-abc");

        var result = Apply(
            new[] { upper, lower },
            operation: new OperationCorrelationId("Corr-ABC"));

        Assert.Same(upper, Assert.Single(result));
    }

    [Fact]
    public void CombinedCriteriaUseAndSemantics()
    {
        var match = Event("event-01", "corr-01", "Action");
        var wrongId = Event("event-02", "corr-01", "Action");
        var wrongOperation = Event("event-01", "corr-02", "Action");
        var wrongKind = Event("event-01", "corr-01", "Approval");

        var result = Apply(
            new[] { wrongId, wrongOperation, match, wrongKind },
            eventId: "event-01",
            operation: new OperationCorrelationId("corr-01"),
            eventKind: "Action");

        Assert.Same(match, Assert.Single(result));
    }

    [Fact]
    public void RejectsBlankAndControlCharacterTextCriteria()
    {
        var values = new string?[]
        {
            "", " ", "\t", "\u2003", "event\nid",
            "event\rid", "event\tid", "event\0id",
            "event\u007fid"
        };

        var events = new[] { Event() };

        foreach (var value in values)
        {
            Assert.Throws<ArgumentException>(() =>
                Apply(events, eventId: value));

            Assert.Throws<ArgumentException>(() =>
                Apply(events, eventKind: value));
        }
    }

    [Fact]
    public void RejectsDefaultOperationCriterion()
    {
        var invalid = default(OperationCorrelationId);

        Assert.Throws<ArgumentException>(() =>
            Apply(
                new[] { Event() },
                operation: invalid));
    }

    [Fact]
    public void PreservesDuplicateEventReferences()
    {
        var shared = Event("event-01");
        var other = Event("event-02");

        var result = Apply(
            new[] { shared, other, shared },
            eventId: "event-01");

        Assert.Equal(2, result.Count);
        Assert.Same(shared, result[0]);
        Assert.Same(shared, result[1]);
    }

    [Fact]
    public void ResultIsReadOnlySnapshot()
    {
        var first = Event("event-01");
        var second = Event("event-02");

        var source = new List<OperationHistoryEvent> { first };
        var result = Apply(source);

        source.Add(second);
        source.Clear();

        Assert.Same(first, Assert.Single(result));

        var writable = Assert.IsAssignableFrom<
            IList<OperationHistoryEvent>>(result);

        Assert.True(writable.IsReadOnly);

        Assert.Throws<NotSupportedException>(() =>
            writable.Add(second));
    }

    [Fact]
    public void HasExactlyOneFilterMethodAndNoOperationalIntegration()
    {
        var type = typeof(OperationHistoryFilter);

        Assert.True(type.IsPublic);
        Assert.True(type.IsClass);
        Assert.True(type.IsAbstract);
        Assert.True(type.IsSealed);
        Assert.Equal("TruthDoctor.Models.Platform", type.Namespace);

        var methods = type.GetMethods(
            BindingFlags.Public |
            BindingFlags.Static |
            BindingFlags.DeclaredOnly);

        var method = Assert.Single(methods);
        Assert.Equal("Filter", method.Name);
        Assert.Equal(
            typeof(IReadOnlyList<OperationHistoryEvent>),
            method.ReturnType);

        var parameters = method.GetParameters();
        Assert.Equal(4, parameters.Length);

        Assert.Equal("events", parameters[0].Name);
        Assert.Equal(
            typeof(IEnumerable<OperationHistoryEvent>),
            parameters[0].ParameterType);
        Assert.False(parameters[0].IsOptional);

        Assert.Equal("eventId", parameters[1].Name);
        Assert.Equal(typeof(string), parameters[1].ParameterType);
        Assert.True(parameters[1].IsOptional);
        Assert.Null(parameters[1].DefaultValue);

        Assert.Equal("operation", parameters[2].Name);
        Assert.Equal(
            typeof(OperationCorrelationId?),
            parameters[2].ParameterType);
        Assert.True(parameters[2].IsOptional);
        Assert.Null(parameters[2].DefaultValue);

        Assert.Equal("eventKind", parameters[3].Name);
        Assert.Equal(typeof(string), parameters[3].ParameterType);
        Assert.True(parameters[3].IsOptional);
        Assert.Null(parameters[3].DefaultValue);

        Assert.Empty(type.GetProperties(
            BindingFlags.Public | BindingFlags.Static |
            BindingFlags.DeclaredOnly));

        Assert.Empty(type.GetInterfaces());

        Assert.DoesNotContain(
            type.GetMethods(
                BindingFlags.Public |
                BindingFlags.Static |
                BindingFlags.DeclaredOnly),
            methodInfo => methodInfo.Name is
                "Execute" or "Save" or "Load" or "QueryProvider");
    }
}
