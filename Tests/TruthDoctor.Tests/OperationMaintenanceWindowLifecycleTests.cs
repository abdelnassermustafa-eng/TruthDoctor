using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationMaintenanceWindowLifecycleTests
{
    private static OperationCorrelationId Operation()
    {
        return new OperationCorrelationId("maintenance-42");
    }

    private static OperationMaintenanceWindow Window()
    {
        return new OperationMaintenanceWindow(
            new DateTimeOffset(2026, 10, 28, 1, 0, 0, TimeSpan.FromHours(-7)),
            new DateTimeOffset(2026, 10, 28, 3, 0, 0, TimeSpan.FromHours(-7)));
    }

    private static DateTimeOffset Observed()
    {
        return new DateTimeOffset(
            2026, 10, 28, 1, 30, 0, TimeSpan.FromHours(-7));
    }

    [Theory]
    [InlineData(OperationMaintenanceWindowStatus.Unknown)]
    [InlineData(OperationMaintenanceWindowStatus.Planned)]
    [InlineData(OperationMaintenanceWindowStatus.Active)]
    [InlineData(OperationMaintenanceWindowStatus.ClosedEarly)]
    [InlineData(OperationMaintenanceWindowStatus.Closed)]
    [InlineData(OperationMaintenanceWindowStatus.Expired)]
    [InlineData(OperationMaintenanceWindowStatus.Cancelled)]
    public void AcceptsEveryDefinedReportedStatus(
        OperationMaintenanceWindowStatus status)
    {
        var lifecycle = new OperationMaintenanceWindowLifecycle(
            Operation(), Window(), status, Observed());

        Assert.Equal(status, lifecycle.Status);
    }

    [Fact]
    public void PreservesOperationIdentity()
    {
        var operation = Operation();

        var lifecycle = new OperationMaintenanceWindowLifecycle(
            operation, Window(),
            OperationMaintenanceWindowStatus.Planned, Observed());

        Assert.Equal(operation, lifecycle.Operation);
        Assert.Equal("maintenance-42", lifecycle.Operation.Value);
    }

    [Fact]
    public void PreservesExactWindowReference()
    {
        var window = Window();

        var lifecycle = new OperationMaintenanceWindowLifecycle(
            Operation(), window,
            OperationMaintenanceWindowStatus.Active, Observed());

        Assert.Same(window, lifecycle.Window);
    }

    [Fact]
    public void PreservesExplicitObservationTimestampAndOffset()
    {
        var observed = Observed();

        var lifecycle = new OperationMaintenanceWindowLifecycle(
            Operation(), Window(),
            OperationMaintenanceWindowStatus.Active, observed);

        Assert.Equal(observed, lifecycle.ObservedAt);
        Assert.Equal(observed.Offset, lifecycle.ObservedAt.Offset);
    }

    [Fact]
    public void PreservesDifferentOffsetsForSameObservationInstant()
    {
        var observed = Observed().ToOffset(TimeSpan.FromHours(2));

        var lifecycle = new OperationMaintenanceWindowLifecycle(
            Operation(), Window(),
            OperationMaintenanceWindowStatus.Closed, observed);

        Assert.Equal(observed, lifecycle.ObservedAt);
        Assert.Equal(TimeSpan.FromHours(2), lifecycle.ObservedAt.Offset);
    }

    [Fact]
    public void DoesNotInferStatusFromObservationBeforeWindowStarts()
    {
        var window = Window();
        var observed = window.Start.AddHours(-3);

        var lifecycle = new OperationMaintenanceWindowLifecycle(
            Operation(), window,
            OperationMaintenanceWindowStatus.Active, observed);

        Assert.Equal(OperationMaintenanceWindowStatus.Active,
            lifecycle.Status);
        Assert.Equal(observed, lifecycle.ObservedAt);
    }

    [Fact]
    public void DoesNotInferExpirationAfterWindowEnds()
    {
        var window = Window();
        var observed = window.End.AddHours(5);

        var lifecycle = new OperationMaintenanceWindowLifecycle(
            Operation(), window,
            OperationMaintenanceWindowStatus.Planned, observed);

        Assert.Equal(OperationMaintenanceWindowStatus.Planned,
            lifecycle.Status);
    }

    [Fact]
    public void DoesNotInferEarlyClosureFromTimestamp()
    {
        var window = Window();

        var lifecycle = new OperationMaintenanceWindowLifecycle(
            Operation(), window,
            OperationMaintenanceWindowStatus.Closed,
            window.Start.AddMinutes(15));

        Assert.Equal(OperationMaintenanceWindowStatus.Closed,
            lifecycle.Status);
    }

    [Fact]
    public void RejectsDefaultOperationCorrelationId()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationMaintenanceWindowLifecycle(
                default, Window(),
                OperationMaintenanceWindowStatus.Planned, Observed()));
    }

    [Fact]
    public void RejectsNullWindow()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationMaintenanceWindowLifecycle(
                Operation(), null!,
                OperationMaintenanceWindowStatus.Planned, Observed()));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    [InlineData(999)]
    public void RejectsUndefinedStatuses(int invalidStatus)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationMaintenanceWindowLifecycle(
                Operation(), Window(),
                (OperationMaintenanceWindowStatus)invalidStatus,
                Observed()));
    }

    [Fact]
    public void PreservesExplicitDefaultObservationTimestamp()
    {
        var lifecycle = new OperationMaintenanceWindowLifecycle(
            Operation(), Window(),
            OperationMaintenanceWindowStatus.Unknown,
            default(DateTimeOffset));

        Assert.Equal(default(DateTimeOffset), lifecycle.ObservedAt);
    }

    [Fact]
    public void IsPublicSealedProviderNeutralReferenceType()
    {
        var type = typeof(OperationMaintenanceWindowLifecycle);

        Assert.True(type.IsPublic);
        Assert.True(type.IsClass);
        Assert.True(type.IsSealed);
        Assert.Equal("TruthDoctor.Models.Platform", type.Namespace);
    }

    [Fact]
    public void ExposesExactlyFourGetterOnlyProperties()
    {
        var type = typeof(OperationMaintenanceWindowLifecycle);
        var properties = type.GetProperties(
            BindingFlags.Public | BindingFlags.Instance |
            BindingFlags.DeclaredOnly);

        Assert.Equal(4, properties.Length);

        Assert.Equal(typeof(OperationCorrelationId),
            type.GetProperty("Operation")!.PropertyType);
        Assert.Equal(typeof(OperationMaintenanceWindow),
            type.GetProperty("Window")!.PropertyType);
        Assert.Equal(typeof(OperationMaintenanceWindowStatus),
            type.GetProperty("Status")!.PropertyType);
        Assert.Equal(typeof(DateTimeOffset),
            type.GetProperty("ObservedAt")!.PropertyType);

        Assert.All(properties, property =>
        {
            Assert.NotNull(property.GetMethod);
            Assert.Null(property.SetMethod);
        });
    }

    [Fact]
    public void HasExactlyOneFourArgumentConstructor()
    {
        var constructors =
            typeof(OperationMaintenanceWindowLifecycle)
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        var constructor = Assert.Single(constructors);
        var parameters = constructor.GetParameters();

        Assert.Equal(4, parameters.Length);
        Assert.Equal(typeof(OperationCorrelationId),
            parameters[0].ParameterType);
        Assert.Equal(typeof(OperationMaintenanceWindow),
            parameters[1].ParameterType);
        Assert.Equal(typeof(OperationMaintenanceWindowStatus),
            parameters[2].ParameterType);
        Assert.Equal(typeof(DateTimeOffset),
            parameters[3].ParameterType);
    }

    [Fact]
    public void ExposesNoOperationalInstanceMethods()
    {
        var methods =
            typeof(OperationMaintenanceWindowLifecycle)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance |
                            BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName)
                .ToArray();

        Assert.Empty(methods);
    }

    [Fact]
    public void ExposesNoPublicStaticOperationalMethods()
    {
        var methods =
            typeof(OperationMaintenanceWindowLifecycle)
                .GetMethods(BindingFlags.Public | BindingFlags.Static |
                            BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName)
                .ToArray();

        Assert.Empty(methods);
    }
}
