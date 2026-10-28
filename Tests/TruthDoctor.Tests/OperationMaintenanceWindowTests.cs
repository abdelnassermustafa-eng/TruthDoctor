using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationMaintenanceWindowTests
{
    [Fact]
    public void AcceptsStrictlyIncreasingInterval()
    {
        var start = new DateTimeOffset(
            2026, 10, 28, 22, 0, 0, TimeSpan.Zero);
        var end = start.AddHours(2);

        var window = new OperationMaintenanceWindow(start, end);

        Assert.Equal(start, window.Start);
        Assert.Equal(end, window.End);
    }

    [Fact]
    public void PreservesOriginalOffsets()
    {
        var start = new DateTimeOffset(
            2026, 10, 28, 22, 0, 0,
            TimeSpan.FromHours(-7));

        var end = new DateTimeOffset(
            2026, 10, 29, 2, 0, 0,
            TimeSpan.FromHours(-7));

        var window = new OperationMaintenanceWindow(start, end);

        Assert.Equal(start.Offset, window.Start.Offset);
        Assert.Equal(end.Offset, window.End.Offset);
        Assert.Equal(start, window.Start);
        Assert.Equal(end, window.End);
    }

    [Fact]
    public void RejectsEqualBoundaries()
    {
        var instant = new DateTimeOffset(
            2026, 10, 28, 22, 0, 0,
            TimeSpan.Zero);

        Assert.Throws<ArgumentException>(() =>
            new OperationMaintenanceWindow(instant, instant));
    }

    [Fact]
    public void RejectsReversedBoundaries()
    {
        var start = new DateTimeOffset(
            2026, 10, 28, 23, 0, 0,
            TimeSpan.Zero);
        var end = start.AddMinutes(-1);

        Assert.Throws<ArgumentException>(() =>
            new OperationMaintenanceWindow(start, end));
    }

    [Fact]
    public void RejectsDifferentOffsetsRepresentingSameInstant()
    {
        var start = new DateTimeOffset(
            2026, 10, 28, 22, 0, 0,
            TimeSpan.FromHours(-7));

        var end = start.ToOffset(TimeSpan.Zero);

        Assert.Throws<ArgumentException>(() =>
            new OperationMaintenanceWindow(start, end));
    }

    [Fact]
    public void AcceptsIncreasingInstantsAcrossOffsets()
    {
        var start = new DateTimeOffset(
            2026, 10, 28, 22, 0, 0,
            TimeSpan.FromHours(-7));

        var end = start.AddMinutes(30)
            .ToOffset(TimeSpan.Zero);

        var window = new OperationMaintenanceWindow(start, end);

        Assert.Equal(start.Offset, window.Start.Offset);
        Assert.Equal(end.Offset, window.End.Offset);
        Assert.True(window.End > window.Start);
    }

    [Fact]
    public void RejectsReversedInstantsAcrossOffsets()
    {
        var start = new DateTimeOffset(
            2026, 10, 28, 22, 0, 0,
            TimeSpan.FromHours(-7));

        var end = start.AddMinutes(-30)
            .ToOffset(TimeSpan.Zero);

        Assert.Throws<ArgumentException>(() =>
            new OperationMaintenanceWindow(start, end));
    }

    [Fact]
    public void IsPublicSealedProviderNeutralReferenceType()
    {
        var type = typeof(OperationMaintenanceWindow);

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
        var properties = typeof(OperationMaintenanceWindow)
            .GetProperties(
                BindingFlags.Public |
                BindingFlags.Instance);

        Assert.Equal(2, properties.Length);

        foreach (var name in new[] { "Start", "End" })
        {
            var property = Assert.Single(
                properties,
                p => p.Name == name);

            Assert.Equal(
                typeof(DateTimeOffset),
                property.PropertyType);

            Assert.NotNull(property.GetMethod);
            Assert.Null(property.SetMethod);
        }
    }

    [Fact]
    public void HasExactlyOnePublicConstructor()
    {
        var constructors = typeof(OperationMaintenanceWindow)
            .GetConstructors();

        var constructor = Assert.Single(constructors);
        var parameters = constructor.GetParameters();

        Assert.Equal(2, parameters.Length);
        Assert.All(parameters, parameter =>
            Assert.Equal(
                typeof(DateTimeOffset),
                parameter.ParameterType));
    }

    [Fact]
    public void DoesNotExposeOperationalInstanceMethods()
    {
        var methods = typeof(OperationMaintenanceWindow)
            .GetMethods(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .ToArray();

        Assert.Empty(methods);
    }
}
