using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationHistoryIntegrityTests
{
    private static OperationHistoryEvent Event(
        string id = "integrity-event-01",
        string kind = "OperationRecorded") =>
        new(
            id,
            new OperationCorrelationId("integrity-operation-01"),
            kind);

    private static OperationHistoryIntegrity Create(
        OperationHistoryEvent historyEvent,
        OperationHistoryIntegrityStatus status) =>
        new(historyEvent, status);

    [Fact]
    public void RejectsNullHistoryEvent()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Create(null!, OperationHistoryIntegrityStatus.NotAssessed));
    }

    [Fact]
    public void PreservesExactHistoryEventReference()
    {
        var historyEvent = Event();
        var integrity = Create(
            historyEvent, OperationHistoryIntegrityStatus.NotAssessed);

        Assert.Same(historyEvent, integrity.Event);
    }

    [Fact]
    public void PreservesHistoryEventIdentityAndKind()
    {
        var historyEvent = Event();
        var integrity = Create(
            historyEvent, OperationHistoryIntegrityStatus.ReportedChanged);

        Assert.Equal("integrity-event-01", integrity.Event.EventId);
        Assert.Equal(
            new OperationCorrelationId("integrity-operation-01"),
            integrity.Event.Operation);
        Assert.Equal("OperationRecorded", integrity.Event.EventKind);
    }

    [Fact]
    public void PreservesNotAssessedStatus()
    {
        var integrity = Create(
            Event(), OperationHistoryIntegrityStatus.NotAssessed);

        Assert.Equal(
            OperationHistoryIntegrityStatus.NotAssessed,
            integrity.Status);
        Assert.Equal(0, (int)integrity.Status);
    }

    [Fact]
    public void PreservesReportedUnchangedStatus()
    {
        var integrity = Create(
            Event(), OperationHistoryIntegrityStatus.ReportedUnchanged);

        Assert.Equal(
            OperationHistoryIntegrityStatus.ReportedUnchanged,
            integrity.Status);
        Assert.Equal(1, (int)integrity.Status);
    }

    [Fact]
    public void PreservesReportedChangedStatus()
    {
        var integrity = Create(
            Event(), OperationHistoryIntegrityStatus.ReportedChanged);

        Assert.Equal(
            OperationHistoryIntegrityStatus.ReportedChanged,
            integrity.Status);
        Assert.Equal(2, (int)integrity.Status);
    }

    [Fact]
    public void RejectsNegativeUndefinedStatus()
    {
        Assert.Throws<ArgumentException>(() =>
            Create(Event(), (OperationHistoryIntegrityStatus)(-1)));
    }

    [Fact]
    public void RejectsPositiveUndefinedStatus()
    {
        Assert.Throws<ArgumentException>(() =>
            Create(Event(), (OperationHistoryIntegrityStatus)99));
    }

    [Fact]
    public void DoesNotInferStatusFromEventIdentity()
    {
        var first = Event("event-A", "Started");
        var second = Event("event-B", "Completed");

        var unknown = Create(
            first, OperationHistoryIntegrityStatus.NotAssessed);
        var unchanged = Create(
            second, OperationHistoryIntegrityStatus.ReportedUnchanged);
        var changed = Create(
            first, OperationHistoryIntegrityStatus.ReportedChanged);

        Assert.Same(first, unknown.Event);
        Assert.Same(second, unchanged.Event);
        Assert.Same(first, changed.Event);

        Assert.Equal(
            OperationHistoryIntegrityStatus.NotAssessed, unknown.Status);
        Assert.Equal(
            OperationHistoryIntegrityStatus.ReportedUnchanged,
            unchanged.Status);
        Assert.Equal(
            OperationHistoryIntegrityStatus.ReportedChanged,
            changed.Status);
    }

    [Fact]
    public void DoesNotInferStatusFromVerificationEvidence()
    {
        var historyEvent = Event();
        var integrity = Create(
            historyEvent, OperationHistoryIntegrityStatus.NotAssessed);

        Assert.Same(historyEvent, integrity.Event);
        Assert.Equal(
            OperationHistoryIntegrityStatus.NotAssessed,
            integrity.Status);

        Assert.DoesNotContain(
            typeof(OperationHistoryIntegrity)
                .GetConstructors(BindingFlags.Public |
                                 BindingFlags.Instance)
                .SelectMany(ctor => ctor.GetParameters()),
            parameter =>
                parameter.ParameterType ==
                    typeof(OperationHistoryVerificationEvidenceAttribution) ||
                parameter.ParameterType ==
                    typeof(OperationVerificationEvidenceSet));
    }

    [Fact]
    public void DoesNotInferStatusFromRetention()
    {
        var historyEvent = Event();
        var retention = new OperationHistoryRetention(
            historyEvent,
            OperationHistoryRetentionMode.Indefinite,
            null);

        var integrity = Create(
            retention.Event,
            OperationHistoryIntegrityStatus.NotAssessed);

        Assert.Same(historyEvent, integrity.Event);
        Assert.Null(retention.RetainUntil);
        Assert.Equal(
            OperationHistoryIntegrityStatus.NotAssessed,
            integrity.Status);

        Assert.DoesNotContain(
            typeof(OperationHistoryIntegrity)
                .GetConstructors(BindingFlags.Public |
                                 BindingFlags.Instance)
                .SelectMany(ctor => ctor.GetParameters()),
            parameter =>
                parameter.ParameterType == typeof(OperationHistoryRetention));
    }

    [Fact]
    public void DoesNotPerformCryptographicVerification()
    {
        var historyEvent = Event();

        var unknown = Create(
            historyEvent, OperationHistoryIntegrityStatus.NotAssessed);
        var unchanged = Create(
            historyEvent, OperationHistoryIntegrityStatus.ReportedUnchanged);
        var changed = Create(
            historyEvent, OperationHistoryIntegrityStatus.ReportedChanged);

        Assert.Same(historyEvent, unknown.Event);
        Assert.Same(historyEvent, unchanged.Event);
        Assert.Same(historyEvent, changed.Event);

        Assert.Equal(
            OperationHistoryIntegrityStatus.NotAssessed, unknown.Status);
        Assert.Equal(
            OperationHistoryIntegrityStatus.ReportedUnchanged,
            unchanged.Status);
        Assert.Equal(
            OperationHistoryIntegrityStatus.ReportedChanged,
            changed.Status);

        Assert.DoesNotContain(
            typeof(OperationHistoryIntegrity)
                .GetProperties(BindingFlags.Public |
                               BindingFlags.Instance |
                               BindingFlags.DeclaredOnly),
            property =>
                property.Name.Contains("Hash", StringComparison.Ordinal) ||
                property.Name.Contains("Signature", StringComparison.Ordinal) ||
                property.Name.Contains("Verified", StringComparison.Ordinal) ||
                property.Name.Contains("Digest", StringComparison.Ordinal));
    }

    [Fact]
    public void IsPublicSealedProviderNeutralReferenceType()
    {
        var type = typeof(OperationHistoryIntegrity);
        var statusType = typeof(OperationHistoryIntegrityStatus);

        Assert.True(type.IsPublic);
        Assert.True(type.IsClass);
        Assert.True(type.IsSealed);
        Assert.False(type.IsAbstract);
        Assert.Equal("TruthDoctor.Models.Platform", type.Namespace);

        Assert.True(statusType.IsPublic);
        Assert.True(statusType.IsEnum);
        Assert.Equal(
            new[] {
                "NotAssessed", "ReportedUnchanged", "ReportedChanged"
            },
            Enum.GetNames(statusType));
        Assert.Equal(
            new[] { 0, 1, 2 },
            Enum.GetValues<OperationHistoryIntegrityStatus>()
                .Select(value => (int)value).ToArray());
    }

    [Fact]
    public void ExposesExactlyTwoGetterOnlyProperties()
    {
        var properties = typeof(OperationHistoryIntegrity)
            .GetProperties(BindingFlags.Public |
                           BindingFlags.Instance |
                           BindingFlags.DeclaredOnly)
            .OrderBy(property => property.Name)
            .ToArray();

        Assert.Equal(
            new[] { "Event", "Status" },
            properties.Select(property => property.Name).ToArray());

        Assert.Equal(
            typeof(OperationHistoryEvent),
            properties.Single(property => property.Name == "Event")
                .PropertyType);
        Assert.Equal(
            typeof(OperationHistoryIntegrityStatus),
            properties.Single(property => property.Name == "Status")
                .PropertyType);

        Assert.All(properties, property =>
        {
            Assert.NotNull(property.GetMethod);
            Assert.True(property.GetMethod!.IsPublic);
            Assert.Null(property.SetMethod);
        });
    }

    [Fact]
    public void HasExactlyOneTwoArgumentConstructor()
    {
        var ctor = Assert.Single(
            typeof(OperationHistoryIntegrity).GetConstructors(
                BindingFlags.Public | BindingFlags.Instance));

        var parameters = ctor.GetParameters();

        Assert.Equal(2, parameters.Length);
        Assert.Equal(
            new[] { "historyEvent", "status" },
            parameters.Select(parameter => parameter.Name).ToArray());

        Assert.Equal(
            typeof(OperationHistoryEvent),
            parameters[0].ParameterType);
        Assert.Equal(
            typeof(OperationHistoryIntegrityStatus),
            parameters[1].ParameterType);
    }

    [Fact]
    public void ExposesNoOperationalMethodsOrIntegrationInterfaces()
    {
        var type = typeof(OperationHistoryIntegrity);

        Assert.Empty(type.GetInterfaces());

        Assert.DoesNotContain(
            type.GetMethods(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.Static |
                BindingFlags.DeclaredOnly),
            method => !method.IsSpecialName);

        Assert.Empty(
            type.GetFields(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.Static |
                BindingFlags.DeclaredOnly));

        Assert.Equal(
            2,
            type.GetProperties(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly).Length);

        Assert.Single(
            type.GetConstructors(
                BindingFlags.Public | BindingFlags.Instance));
    }
}
