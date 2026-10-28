using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationMaintenanceWindowAuditEventTests
{
    private static OperationCorrelationId Operation(
        string value = "operation-42")
    {
        return new OperationCorrelationId(value);
    }

    private static OperationActorIdentity Reporter(
        OperationActorKind kind = OperationActorKind.Human)
    {
        return new OperationActorIdentity(kind, "operator-17");
    }

    private static OperationMaintenanceWindowLifecycle Lifecycle(
        OperationCorrelationId? operation = null)
    {
        var identity = operation ?? Operation();

        var window = new OperationMaintenanceWindow(
            new DateTimeOffset(
                2026, 10, 28, 1, 0, 0, TimeSpan.FromHours(-7)),
            new DateTimeOffset(
                2026, 10, 28, 3, 0, 0, TimeSpan.FromHours(-7)));

        return new OperationMaintenanceWindowLifecycle(
            identity,
            window,
            OperationMaintenanceWindowStatus.Active,
            new DateTimeOffset(
                2026, 10, 28, 1, 15, 0, TimeSpan.FromHours(-7)));
    }

    private static DateTimeOffset Occurred()
    {
        return new DateTimeOffset(
            2026, 10, 28, 1, 20, 0, TimeSpan.FromHours(-7));
    }

    private static DateTimeOffset Recorded()
    {
        return new DateTimeOffset(
            2026, 10, 28, 1, 45, 0, TimeSpan.FromHours(-7));
    }

    private static OperationMaintenanceWindowAuditEvent Create(
        string eventId = "event-001",
        string eventKind = "WindowReportedActive",
        string reason = "Maintenance activity reported")
    {
        return new OperationMaintenanceWindowAuditEvent(
            eventId,
            Operation(),
            Reporter(),
            Lifecycle(),
            eventKind,
            Occurred(),
            Recorded(),
            reason);
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
        var result = Create(eventKind: "  WindowClosedEarly  ");

        Assert.Equal("WindowClosedEarly", result.EventKind);
    }

    [Fact]
    public void PreservesNormalizedReason()
    {
        var result = Create(reason: "  Work finished early  ");

        Assert.Equal("Work finished early", result.Reason);
    }

    [Fact]
    public void PreservesIdentifierKindAndReasonCase()
    {
        var result = Create(
            "Event-MiXeD",
            "EmergencyChangeReported",
            "Reviewed By Operator");

        Assert.Equal("Event-MiXeD", result.EventId);
        Assert.Equal(
            "EmergencyChangeReported", result.EventKind);
        Assert.Equal("Reviewed By Operator", result.Reason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void RejectsNullOrBlankEventId(string? eventId)
    {
        Assert.Throws<ArgumentException>(() =>
            Create(eventId: eventId!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void RejectsNullOrBlankEventKind(string? eventKind)
    {
        Assert.Throws<ArgumentException>(() =>
            Create(eventKind: eventKind!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void RejectsNullOrBlankReason(string? reason)
    {
        Assert.Throws<ArgumentException>(() =>
            Create(reason: reason!));
    }

    [Theory]
    [InlineData("event\nname")]
    [InlineData("event\rname")]
    [InlineData("event\tname")]
    [InlineData("event\0name")]
    public void RejectsControlCharactersInEventId(string eventId)
    {
        Assert.Throws<ArgumentException>(() =>
            Create(eventId: eventId));
    }

    [Theory]
    [InlineData("window\nclosed")]
    [InlineData("window\rclosed")]
    [InlineData("window\tclosed")]
    [InlineData("window\0closed")]
    public void RejectsControlCharactersInEventKind(string eventKind)
    {
        Assert.Throws<ArgumentException>(() =>
            Create(eventKind: eventKind));
    }

    [Theory]
    [InlineData("reported\nclosure")]
    [InlineData("reported\rclosure")]
    [InlineData("reported\tclosure")]
    [InlineData("reported\0closure")]
    public void RejectsControlCharactersInReason(string reason)
    {
        Assert.Throws<ArgumentException>(() =>
            Create(reason: reason));
    }

    [Fact]
    public void PreservesExactOperationCorrelation()
    {
        var operation = Operation("operation-99");
        var lifecycle = Lifecycle(operation);

        var result = new OperationMaintenanceWindowAuditEvent(
            "event-99",
            operation,
            Reporter(),
            lifecycle,
            "WindowReportedActive",
            Occurred(),
            Recorded(),
            "Observation received");

        Assert.Equal(operation, result.Operation);
        Assert.Equal("operation-99", result.Operation.Value);
    }

    [Fact]
    public void RejectsUninitializedOperation()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationMaintenanceWindowAuditEvent(
                "event-1",
                default,
                Reporter(),
                Lifecycle(),
                "WindowReportedActive",
                Occurred(),
                Recorded(),
                "Observation received"));
    }

    [Fact]
    public void RejectsUninitializedReporter()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationMaintenanceWindowAuditEvent(
                "event-1",
                Operation(),
                default,
                Lifecycle(),
                "WindowReportedActive",
                Occurred(),
                Recorded(),
                "Observation received"));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    [InlineData(999)]
    public void RejectsUndefinedReporterKind(int invalidKind)
    {
        var reporter = new OperationActorIdentity(
            OperationActorKind.Human, "operator");

        var invalid = default(OperationActorIdentity);

        // The test uses reflection to construct an invalid value
        // without altering the existing actor-identity contract.
        object boxed = invalid;
        var kindField = typeof(OperationActorIdentity)
            .GetFields(BindingFlags.Instance |
                       BindingFlags.NonPublic)
            .Single(field => field.FieldType == typeof(OperationActorKind));
        var valueField = typeof(OperationActorIdentity)
            .GetFields(BindingFlags.Instance |
                       BindingFlags.NonPublic)
            .Single(field => field.FieldType == typeof(string));

        kindField.SetValue(boxed, (OperationActorKind)invalidKind);
        valueField.SetValue(boxed, reporter.Value);

        invalid = (OperationActorIdentity)boxed;

        Assert.Throws<ArgumentException>(() =>
            new OperationMaintenanceWindowAuditEvent(
                "event-1",
                Operation(),
                invalid,
                Lifecycle(),
                "WindowReportedActive",
                Occurred(),
                Recorded(),
                "Observation received"));
    }

    [Fact]
    public void AcceptsHumanReporter()
    {
        var result = new OperationMaintenanceWindowAuditEvent(
            "event-1",
            Operation(),
            Reporter(OperationActorKind.Human),
            Lifecycle(),
            "WindowReportedActive",
            Occurred(),
            Recorded(),
            "Human observation");

        Assert.Equal(OperationActorKind.Human, result.Reporter.Kind);
        Assert.Equal("operator-17", result.Reporter.Value);
    }

    [Fact]
    public void AcceptsServiceReporter()
    {
        var result = new OperationMaintenanceWindowAuditEvent(
            "event-2",
            Operation(),
            Reporter(OperationActorKind.Service),
            Lifecycle(),
            "WindowObservationReceived",
            Occurred(),
            Recorded(),
            "Service observation");

        Assert.Equal(OperationActorKind.Service, result.Reporter.Kind);
        Assert.Equal("operator-17", result.Reporter.Value);
    }

    [Fact]
    public void RejectsNullLifecycle()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationMaintenanceWindowAuditEvent(
                "event-1",
                Operation(),
                Reporter(),
                null!,
                "WindowReportedActive",
                Occurred(),
                Recorded(),
                "Observation received"));
    }

    [Fact]
    public void RejectsOperationLifecycleMismatch()
    {
        var unrelated = Lifecycle(Operation("unrelated-operation"));

        Assert.Throws<ArgumentException>(() =>
            new OperationMaintenanceWindowAuditEvent(
                "event-1",
                Operation(),
                Reporter(),
                unrelated,
                "WindowReportedActive",
                Occurred(),
                Recorded(),
                "Observation received"));
    }

    [Fact]
    public void MatchesOperationByValueNotObjectConstruction()
    {
        var first = Operation("shared-operation");
        var second = Operation("shared-operation");
        var lifecycle = Lifecycle(second);

        var result = new OperationMaintenanceWindowAuditEvent(
            "event-1",
            first,
            Reporter(),
            lifecycle,
            "WindowReportedActive",
            Occurred(),
            Recorded(),
            "Observation received");

        Assert.Equal(first, result.Operation);
        Assert.Same(lifecycle, result.Lifecycle);
    }

    [Fact]
    public void PreservesExactLifecycleReference()
    {
        var lifecycle = Lifecycle();

        var result = new OperationMaintenanceWindowAuditEvent(
            "event-1",
            Operation(),
            Reporter(),
            lifecycle,
            "WindowReportedActive",
            Occurred(),
            Recorded(),
            "Observation received");

        Assert.Same(lifecycle, result.Lifecycle);
    }

    [Fact]
    public void PreservesOccurredAtInstantAndOffset()
    {
        var occurred = Occurred().ToOffset(TimeSpan.FromHours(3));

        var result = new OperationMaintenanceWindowAuditEvent(
            "event-1",
            Operation(),
            Reporter(),
            Lifecycle(),
            "WindowReportedActive",
            occurred,
            Recorded(),
            "Observation received");

        Assert.Equal(occurred, result.OccurredAt);
        Assert.Equal(occurred.Offset, result.OccurredAt.Offset);
    }

    [Fact]
    public void PreservesRecordedAtInstantAndOffset()
    {
        var recorded = Recorded().ToOffset(TimeSpan.FromHours(-4));

        var result = new OperationMaintenanceWindowAuditEvent(
            "event-1",
            Operation(),
            Reporter(),
            Lifecycle(),
            "WindowReportedActive",
            Occurred(),
            recorded,
            "Observation received");

        Assert.Equal(recorded, result.RecordedAt);
        Assert.Equal(recorded.Offset, result.RecordedAt.Offset);
    }

    [Fact]
    public void AcceptsDelayedRecording()
    {
        var occurred = Occurred();
        var recorded = occurred.AddDays(3);

        var result = new OperationMaintenanceWindowAuditEvent(
            "event-1",
            Operation(),
            Reporter(),
            Lifecycle(),
            "WindowClosedEarly",
            occurred,
            recorded,
            "Historical report");

        Assert.Equal(occurred, result.OccurredAt);
        Assert.Equal(recorded, result.RecordedAt);
    }

    [Fact]
    public void DoesNotRequireOccurredAtBeforeRecordedAt()
    {
        var occurred = Occurred();
        var recorded = occurred.AddMinutes(-15);

        var result = new OperationMaintenanceWindowAuditEvent(
            "event-1",
            Operation(),
            Reporter(),
            Lifecycle(),
            "WindowObservationReceived",
            occurred,
            recorded,
            "Caller-supplied chronology");

        Assert.Equal(occurred, result.OccurredAt);
        Assert.Equal(recorded, result.RecordedAt);
    }

    [Fact]
    public void PreservesExplicitDefaultTimestamps()
    {
        var result = new OperationMaintenanceWindowAuditEvent(
            "event-1",
            Operation(),
            Reporter(),
            Lifecycle(),
            "WindowObservationReceived",
            default,
            default,
            "Imported declaration");

        Assert.Equal(default(DateTimeOffset), result.OccurredAt);
        Assert.Equal(default(DateTimeOffset), result.RecordedAt);
    }

    [Theory]
    [InlineData("AmendmentRequested")]
    [InlineData("WindowClosedEarly")]
    [InlineData("WindowCancelled")]
    [InlineData("OverrunReported")]
    [InlineData("EmergencyInterventionReported")]
    public void EventKindsRemainDescriptiveStrings(string eventKind)
    {
        var result = Create(eventKind: eventKind);

        Assert.Equal(eventKind, result.EventKind);
    }

    [Fact]
    public void DoesNotInferStatusOrModifyLifecycle()
    {
        var lifecycle = Lifecycle();
        var status = lifecycle.Status;
        var window = lifecycle.Window;

        var result = new OperationMaintenanceWindowAuditEvent(
            "event-1",
            Operation(),
            Reporter(),
            lifecycle,
            "WindowClosedEarly",
            Occurred(),
            Recorded(),
            "Early closure reported");

        Assert.Same(lifecycle, result.Lifecycle);
        Assert.Equal(status, lifecycle.Status);
        Assert.Same(window, lifecycle.Window);
    }

    [Fact]
    public void IsPublicSealedProviderNeutralReferenceType()
    {
        var type = typeof(OperationMaintenanceWindowAuditEvent);

        Assert.True(type.IsPublic);
        Assert.True(type.IsClass);
        Assert.True(type.IsSealed);
        Assert.Equal("TruthDoctor.Models.Platform", type.Namespace);
    }

    [Fact]
    public void ExposesExactlyEightGetterOnlyProperties()
    {
        var type = typeof(OperationMaintenanceWindowAuditEvent);

        var properties = type.GetProperties(
            BindingFlags.Public |
            BindingFlags.Instance |
            BindingFlags.DeclaredOnly);

        Assert.Equal(8, properties.Length);

        var expected = new (string Name, Type PropertyType)[]
        {
            ("EventId", typeof(string)),
            ("Operation", typeof(OperationCorrelationId)),
            ("Reporter", typeof(OperationActorIdentity)),
            ("Lifecycle", typeof(OperationMaintenanceWindowLifecycle)),
            ("EventKind", typeof(string)),
            ("OccurredAt", typeof(DateTimeOffset)),
            ("RecordedAt", typeof(DateTimeOffset)),
            ("Reason", typeof(string))
        };

        foreach (var (name, propertyType) in expected)
        {
            var property = type.GetProperty(name);
            Assert.NotNull(property);
            Assert.Equal(propertyType, property.PropertyType);
            Assert.NotNull(property.GetMethod);
            Assert.Null(property.SetMethod);
        }

        Assert.All(properties, property =>
            Assert.Null(property.SetMethod));
    }

    [Fact]
    public void HasExactlyOneEightArgumentConstructor()
    {
        var constructor = Assert.Single(
            typeof(OperationMaintenanceWindowAuditEvent)
                .GetConstructors(
                    BindingFlags.Public | BindingFlags.Instance));

        var parameters = constructor.GetParameters();

        var expectedNames = new[]
        {
            "eventId", "operation", "reporter", "lifecycle",
            "eventKind", "occurredAt", "recordedAt", "reason"
        };

        var expectedTypes = new[]
        {
            typeof(string),
            typeof(OperationCorrelationId),
            typeof(OperationActorIdentity),
            typeof(OperationMaintenanceWindowLifecycle),
            typeof(string),
            typeof(DateTimeOffset),
            typeof(DateTimeOffset),
            typeof(string)
        };

        Assert.Equal(8, parameters.Length);

        for (var index = 0; index < parameters.Length; index++)
        {
            Assert.Equal(expectedNames[index], parameters[index].Name);
            Assert.Equal(expectedTypes[index], parameters[index].ParameterType);
            Assert.False(parameters[index].IsOptional);
        }
    }

    [Fact]
    public void ExposesNoPublicOperationalMethods()
    {
        var type = typeof(OperationMaintenanceWindowAuditEvent);

        var methods = type.GetMethods(
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
            typeof(OperationMaintenanceWindowAuditEvent).GetInterfaces());
    }
}
