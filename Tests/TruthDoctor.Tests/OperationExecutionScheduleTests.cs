using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationExecutionScheduleTests
{
    private static readonly DateTimeOffset WindowStart =
        new(2026, 10, 28, 9, 0, 0, TimeSpan.FromHours(-7));

    private static readonly DateTimeOffset WindowEnd =
        new(2026, 10, 28, 11, 0, 0, TimeSpan.FromHours(-7));

    private static OperationMaintenanceWindow Window() =>
        new(WindowStart, WindowEnd);

    private static OperationCorrelationId Operation() =>
        new("operation-42");

    private static OperationExecutionSchedule Create(
        string scheduleId = "schedule-001",
        string reason = "Planned maintenance")
    {
        return new OperationExecutionSchedule(
            scheduleId,
            Operation(),
            Window(),
            WindowStart.AddMinutes(30),
            reason);
    }

    [Fact]
    public void PreservesNormalizedScheduleId()
    {
        var schedule = Create(scheduleId: "  Schedule-AbC  ");
        Assert.Equal("Schedule-AbC", schedule.ScheduleId);
    }

    [Fact]
    public void PreservesNormalizedReason()
    {
        var schedule = Create(reason: "  Planned Network Upgrade  ");
        Assert.Equal("Planned Network Upgrade", schedule.Reason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void RejectsNullOrBlankScheduleId(string? value)
    {
        Assert.Throws<ArgumentException>(() =>
            Create(scheduleId: value!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void RejectsNullOrBlankReason(string? value)
    {
        Assert.Throws<ArgumentException>(() =>
            Create(reason: value!));
    }

    [Theory]
    [InlineData("schedule\nid")]
    [InlineData("schedule\rid")]
    [InlineData("schedule\tid")]
    [InlineData("schedule\0id")]
    public void RejectsEmbeddedScheduleIdControls(string value)
    {
        Assert.Throws<ArgumentException>(() =>
            Create(scheduleId: value));
    }

    [Theory]
    [InlineData("planned\nwork")]
    [InlineData("planned\rwork")]
    [InlineData("planned\twork")]
    [InlineData("planned\0work")]
    public void RejectsEmbeddedReasonControls(string value)
    {
        Assert.Throws<ArgumentException>(() =>
            Create(reason: value));
    }

    [Fact]
    public void PreservesOperationCorrelation()
    {
        var operation = new OperationCorrelationId("operation-ABC");

        var schedule = new OperationExecutionSchedule(
            "schedule-1",
            operation,
            Window(),
            WindowStart,
            "Planned");

        Assert.Equal(operation, schedule.Operation);
        Assert.Equal("operation-ABC", schedule.Operation.Value);
    }

    [Fact]
    public void RejectsDefaultOperationCorrelation()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationExecutionSchedule(
                "schedule-1",
                default,
                Window(),
                WindowStart,
                "Planned"));
    }

    [Fact]
    public void RejectsNullWindow()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationExecutionSchedule(
                "schedule-1",
                Operation(),
                null!,
                WindowStart,
                "Planned"));
    }

    [Fact]
    public void PreservesExactWindowReference()
    {
        var window = Window();

        var schedule = new OperationExecutionSchedule(
            "schedule-1",
            Operation(),
            window,
            WindowStart,
            "Planned");

        Assert.Same(window, schedule.Window);
    }

    [Fact]
    public void AcceptsInclusiveStartBoundary()
    {
        var schedule = new OperationExecutionSchedule(
            "schedule-1",
            Operation(),
            Window(),
            WindowStart,
            "Planned");

        Assert.Equal(WindowStart, schedule.ScheduledAt);
        Assert.Equal(WindowStart.Offset, schedule.ScheduledAt.Offset);
    }

    [Fact]
    public void AcceptsInstantImmediatelyBeforeExclusiveEnd()
    {
        var scheduledAt = WindowEnd.AddTicks(-1);

        var schedule = new OperationExecutionSchedule(
            "schedule-1",
            Operation(),
            Window(),
            scheduledAt,
            "Planned");

        Assert.Equal(scheduledAt, schedule.ScheduledAt);
    }

    [Fact]
    public void RejectsInstantBeforeWindowStart()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OperationExecutionSchedule(
                "schedule-1",
                Operation(),
                Window(),
                WindowStart.AddTicks(-1),
                "Planned"));
    }

    [Fact]
    public void RejectsExclusiveEndBoundary()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OperationExecutionSchedule(
                "schedule-1",
                Operation(),
                Window(),
                WindowEnd,
                "Planned"));
    }

    [Fact]
    public void RejectsInstantAfterWindowEnd()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OperationExecutionSchedule(
                "schedule-1",
                Operation(),
                Window(),
                WindowEnd.AddTicks(1),
                "Planned"));
    }

    [Fact]
    public void AcceptsEquivalentStartInstantWithDifferentOffset()
    {
        var scheduledAt = WindowStart.ToOffset(TimeSpan.FromHours(2));

        var schedule = new OperationExecutionSchedule(
            "schedule-1",
            Operation(),
            Window(),
            scheduledAt,
            "Planned");

        Assert.Equal(WindowStart, schedule.ScheduledAt);
        Assert.Equal(scheduledAt.Offset, schedule.ScheduledAt.Offset);
    }

    [Fact]
    public void RejectsEquivalentEndInstantWithDifferentOffset()
    {
        var scheduledAt = WindowEnd.ToOffset(TimeSpan.Zero);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OperationExecutionSchedule(
                "schedule-1",
                Operation(),
                Window(),
                scheduledAt,
                "Planned"));
    }

    [Fact]
    public void RejectsEarlierInstantEvenWhenWallClockAppearsLater()
    {
        var scheduledAt = WindowStart.AddMinutes(-30)
            .ToOffset(TimeSpan.FromHours(-4));

        Assert.True(scheduledAt.Hour > WindowStart.Hour);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OperationExecutionSchedule(
                "schedule-1",
                Operation(),
                Window(),
                scheduledAt,
                "Planned"));
    }

    [Fact]
    public void PreservesScheduledAtOffsetWithinWindow()
    {
        var scheduledAt = WindowStart.AddMinutes(45)
            .ToOffset(TimeSpan.FromHours(4));

        var schedule = new OperationExecutionSchedule(
            "schedule-1",
            Operation(),
            Window(),
            scheduledAt,
            "Planned");

        Assert.Equal(scheduledAt, schedule.ScheduledAt);
        Assert.Equal(scheduledAt.Offset, schedule.ScheduledAt.Offset);
    }

    [Fact]
    public void RejectsDefaultScheduledAtOutsideWindow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OperationExecutionSchedule(
                "schedule-1",
                Operation(),
                Window(),
                default,
                "Planned"));
    }

    [Fact]
    public void DoesNotModifyMaintenanceWindow()
    {
        var window = Window();
        var originalStart = window.Start;
        var originalEnd = window.End;

        var schedule = new OperationExecutionSchedule(
            "schedule-1",
            Operation(),
            window,
            originalStart.AddMinutes(15),
            "Planned");

        Assert.Same(window, schedule.Window);
        Assert.Equal(originalStart, window.Start);
        Assert.Equal(originalEnd, window.End);
    }

    [Fact]
    public void IsPublicSealedProviderNeutralReferenceType()
    {
        var type = typeof(OperationExecutionSchedule);

        Assert.True(type.IsPublic);
        Assert.True(type.IsClass);
        Assert.True(type.IsSealed);
        Assert.Equal("TruthDoctor.Models.Platform", type.Namespace);
    }

    [Fact]
    public void ExposesExactlyFiveGetterOnlyProperties()
    {
        var type = typeof(OperationExecutionSchedule);

        var properties = type.GetProperties(
            BindingFlags.Public |
            BindingFlags.Instance |
            BindingFlags.DeclaredOnly);

        Assert.Equal(5, properties.Length);

        var expected = new (string Name, Type PropertyType)[]
        {
            ("ScheduleId", typeof(string)),
            ("Operation", typeof(OperationCorrelationId)),
            ("Window", typeof(OperationMaintenanceWindow)),
            ("ScheduledAt", typeof(DateTimeOffset)),
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
    }

    [Fact]
    public void HasExactlyOneFiveArgumentConstructor()
    {
        var constructor = Assert.Single(
            typeof(OperationExecutionSchedule).GetConstructors(
                BindingFlags.Public | BindingFlags.Instance));

        var parameters = constructor.GetParameters();

        var expectedNames = new[]
        {
            "scheduleId", "operation", "window",
            "scheduledAt", "reason"
        };

        var expectedTypes = new[]
        {
            typeof(string),
            typeof(OperationCorrelationId),
            typeof(OperationMaintenanceWindow),
            typeof(DateTimeOffset),
            typeof(string)
        };

        Assert.Equal(5, parameters.Length);

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
        var methods = typeof(OperationExecutionSchedule)
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
            typeof(OperationExecutionSchedule).GetInterfaces());
    }
}
