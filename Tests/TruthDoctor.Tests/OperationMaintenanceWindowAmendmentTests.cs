using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationMaintenanceWindowAmendmentTests
{
    private static OperationCorrelationId Operation() =>
        new("maintenance-operation-001");

    private static OperationActorIdentity Requester() =>
        new(Enum.GetValues<OperationActorKind>().First(),
            "maintenance-operator-001");

    private static OperationMaintenanceWindow Window(
        int startHour = 20,
        int endHour = 22) =>
        new(
            new DateTimeOffset(
                2026, 10, 28, startHour, 0, 0,
                TimeSpan.Zero),
            new DateTimeOffset(
                2026, 10, 28, endHour, 0, 0,
                TimeSpan.Zero));

    private static OperationMaintenanceWindowAmendment Create(
        OperationMaintenanceWindow original,
        OperationMaintenanceWindow proposed,
        string reason = "Extend for verification") =>
        new(Operation(), Requester(),
            original, proposed, reason);

    [Fact]
    public void AcceptsExtensionRequest()
    {
        var original = Window();
        var proposed = Window(endHour: 23);

        var amendment = Create(original, proposed);

        Assert.Same(original, amendment.OriginalWindow);
        Assert.Same(proposed, amendment.ProposedWindow);
        Assert.True(proposed.End > original.End);
    }

    [Fact]
    public void AcceptsShorteningRequest()
    {
        var original = Window();
        var proposed = Window(endHour: 21);

        var amendment = Create(
            original, proposed, "Work completed early");

        Assert.Same(original, amendment.OriginalWindow);
        Assert.Same(proposed, amendment.ProposedWindow);
    }

    [Fact]
    public void AcceptsReschedulingRequest()
    {
        var original = Window();
        var proposed = Window(21, 23);

        var amendment = Create(
            original, proposed, "Move maintenance start");

        Assert.Same(proposed, amendment.ProposedWindow);
        Assert.NotEqual(
            original.Start, proposed.Start);
    }

    [Fact]
    public void PreservesOperationAndRequester()
    {
        var operation = Operation();
        var requester = Requester();

        var amendment =
            new OperationMaintenanceWindowAmendment(
                operation, requester,
                Window(), Window(endHour: 23),
                "Capacity adjustment");

        Assert.Equal(operation, amendment.Operation);
        Assert.Equal(requester, amendment.Requester);
    }

    [Fact]
    public void PreservesOriginalAndProposedReferences()
    {
        var original = Window();
        var proposed = Window(endHour: 23);

        var amendment = Create(original, proposed);

        Assert.Same(original, amendment.OriginalWindow);
        Assert.Same(proposed, amendment.ProposedWindow);
        Assert.Equal(22, original.End.Hour);
        Assert.Equal(23, proposed.End.Hour);
    }

    [Fact]
    public void PreservesReasonWithOuterWhitespaceRemoved()
    {
        var amendment = new OperationMaintenanceWindowAmendment(
            Operation(), Requester(),
            Window(), Window(endHour: 23),
            "  Additional  verification  ");

        Assert.Equal(
            "Additional  verification",
            amendment.Reason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void RejectsNullOrBlankReason(string? reason)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationMaintenanceWindowAmendment(
                Operation(), Requester(),
                Window(), Window(endHour: 23),
                reason!));
    }

    [Theory]
    [InlineData("Unexpected\nnewline")]
    [InlineData("Embedded\ttab")]
    [InlineData("Control\u0001character")]
    public void RejectsControlCharactersInReason(string reason)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationMaintenanceWindowAmendment(
                Operation(), Requester(),
                Window(), Window(endHour: 23),
                reason));
    }

    [Fact]
    public void RejectsDefaultOperationIdentity()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationMaintenanceWindowAmendment(
                default, Requester(),
                Window(), Window(endHour: 23),
                "Request extension"));
    }

    [Fact]
    public void RejectsDefaultRequesterIdentity()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationMaintenanceWindowAmendment(
                Operation(), default,
                Window(), Window(endHour: 23),
                "Request extension"));
    }

    [Fact]
    public void RejectsNullOriginalWindow()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationMaintenanceWindowAmendment(
                Operation(), Requester(),
                null!, Window(endHour: 23),
                "Request extension"));
    }

    [Fact]
    public void RejectsNullProposedWindow()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationMaintenanceWindowAmendment(
                Operation(), Requester(),
                Window(), null!,
                "Request extension"));
    }

    [Fact]
    public void RejectsUnchangedWindowReference()
    {
        var original = Window();

        Assert.Throws<ArgumentException>(() =>
            Create(original, original));
    }

    [Fact]
    public void RejectsEquivalentBoundariesInDifferentObjects()
    {
        var original = Window();
        var proposed = Window();

        Assert.Throws<ArgumentException>(() =>
            Create(original, proposed));
    }

    [Fact]
    public void RejectsEquivalentInstantsAcrossOffsets()
    {
        var original = Window();

        var proposed = new OperationMaintenanceWindow(
            original.Start.ToOffset(
                TimeSpan.FromHours(-7)),
            original.End.ToOffset(
                TimeSpan.FromHours(-7)));

        Assert.Throws<ArgumentException>(() =>
            Create(original, proposed));
    }

    [Fact]
    public void AcceptsRealChangeAcrossOffsets()
    {
        var original = Window();

        var proposed = new OperationMaintenanceWindow(
            original.Start.ToOffset(
                TimeSpan.FromHours(-7)),
            original.End.AddMinutes(30)
                .ToOffset(TimeSpan.FromHours(-7)));

        var amendment = Create(original, proposed);

        Assert.Same(original, amendment.OriginalWindow);
        Assert.Same(proposed, amendment.ProposedWindow);
    }

    [Fact]
    public void IsSealedProviderNeutralReferenceType()
    {
        var type = typeof(OperationMaintenanceWindowAmendment);

        Assert.True(type.IsPublic);
        Assert.True(type.IsClass);
        Assert.True(type.IsSealed);
        Assert.Equal(
            "TruthDoctor.Models.Platform",
            type.Namespace);
    }

    [Fact]
    public void ExposesExactlyFiveGetterOnlyProperties()
    {
        var properties =
            typeof(OperationMaintenanceWindowAmendment)
                .GetProperties(
                    BindingFlags.Public |
                    BindingFlags.Instance);

        Assert.Equal(5, properties.Length);

        AssertProperty<OperationCorrelationId>(
            properties, "Operation");
        AssertProperty<OperationActorIdentity>(
            properties, "Requester");
        AssertProperty<OperationMaintenanceWindow>(
            properties, "OriginalWindow");
        AssertProperty<OperationMaintenanceWindow>(
            properties, "ProposedWindow");
        AssertProperty<string>(
            properties, "Reason");
    }

    private static void AssertProperty<T>(
        PropertyInfo[] properties, string name)
    {
        var property =
            Assert.Single(properties, p => p.Name == name);

        Assert.Equal(typeof(T), property.PropertyType);
        Assert.NotNull(property.GetMethod);
        Assert.Null(property.SetMethod);
    }

    [Fact]
    public void HasExactlyOneFiveArgumentConstructor()
    {
        var constructor = Assert.Single(
            typeof(OperationMaintenanceWindowAmendment)
                .GetConstructors());

        var parameters = constructor.GetParameters();

        Assert.Equal(5, parameters.Length);

        Assert.Equal(
            typeof(OperationCorrelationId),
            parameters[0].ParameterType);
        Assert.Equal(
            typeof(OperationActorIdentity),
            parameters[1].ParameterType);
        Assert.Equal(
            typeof(OperationMaintenanceWindow),
            parameters[2].ParameterType);
        Assert.Equal(
            typeof(OperationMaintenanceWindow),
            parameters[3].ParameterType);
        Assert.Equal(
            typeof(string),
            parameters[4].ParameterType);
    }

    [Fact]
    public void ExposesNoOperationalInstanceMethods()
    {
        var methods =
            typeof(OperationMaintenanceWindowAmendment)
                .GetMethods(
                    BindingFlags.Public |
                    BindingFlags.Instance |
                    BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName)
                .ToArray();

        Assert.Empty(methods);
    }
}
