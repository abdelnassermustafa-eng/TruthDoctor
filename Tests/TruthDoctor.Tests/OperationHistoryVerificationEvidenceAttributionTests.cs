using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

/// <summary>
/// Tests historical association of caller-declared verification evidence.
/// Attribution does not authenticate evidence, validate freshness,
/// certify execution success, or grant execution authorization.
/// </summary>
public sealed class OperationHistoryVerificationEvidenceAttributionTests
{
    private static readonly DateTimeOffset Observed =
        new(2026, 10, 30, 9, 0, 0, TimeSpan.FromHours(-7));

    private static readonly DateTimeOffset Recorded =
        new(2026, 10, 30, 9, 1, 0, TimeSpan.FromHours(-7));

    private static OperationTarget Target(
        string resourceId = "service-01")
        => new(
            "provider-01", "account-01", "region-01",
            "domain-01", "service", resourceId);

    private static OperationResourceScope Scope(
        string resourceId = "service-01")
        => new(Target(resourceId));

    private static OperationHistoryEvent Event(
        string operationId = "operation-01")
        => new(
            "history-event-01",
            new OperationCorrelationId(operationId),
            "VerificationEvidenceRecorded");

    private static OperationVerificationResult Result(
        string operationId = "operation-01",
        string resourceId = "service-01",
        string checkId = "check-01",
        int observedStatus = 200)
    {
        var operation = new OperationCorrelationId(operationId);
        var target = Target(resourceId);
        var scope = new OperationResourceScope(target);
        var condition = new OperationOutcomeCondition(
            "service.http.status", "200");

        var specification = new OperationServiceCheckSpecification(
            new OperationServiceProtocol("http.response-status"),
            new OperationServiceEndpoint(
                "https://example.com/health"),
            "GET",
            new OperationServiceResponseCriteria(200));

        var observation = new OperationServiceObservation(
            operation, target, specification, Observed,
            OperationServiceObservationDisposition.Responded,
            observedStatus);

        var comparison = OperationServiceComparison.Evaluate(
            condition, scope, specification, observation);

        return OperationVerificationResult.FromService(
            checkId, comparison);
    }

    private static OperationVerificationEvidenceRecord Evidence(
        string evidenceId = "evidence-01",
        string sourceId = "source-01",
        DateTimeOffset? recordedAt = null,
        OperationVerificationResult? result = null)
        => new(
            evidenceId,
            sourceId,
            recordedAt ?? Recorded,
            result ?? Result());

    private static OperationVerificationEvidenceSet EvidenceSet(
        params OperationVerificationEvidenceRecord[] records)
        => new(
            new OperationCorrelationId("operation-01"),
            Scope(),
            records);

    private static OperationHistoryVerificationEvidenceAttribution Create(
        OperationVerificationEvidenceSet? evidenceSet = null)
        => new(
            Event(),
            evidenceSet ?? EvidenceSet());

    [Fact]
    public void PreservesExactHistoryEventReference()
    {
        var historyEvent = Event();
        var evidenceSet = EvidenceSet();

        var attribution =
            new OperationHistoryVerificationEvidenceAttribution(
                historyEvent, evidenceSet);

        Assert.Same(historyEvent, attribution.Event);
    }

    [Fact]
    public void PreservesExactEvidenceSetReference()
    {
        var evidenceSet = EvidenceSet(Evidence());

        var attribution =
            new OperationHistoryVerificationEvidenceAttribution(
                Event(), evidenceSet);

        Assert.Same(evidenceSet, attribution.EvidenceSet);
    }

    [Fact]
    public void PreservesHistoryEventIdentityAndKind()
    {
        var historyEvent = Event("other-operation");

        var attribution =
            new OperationHistoryVerificationEvidenceAttribution(
                historyEvent, EvidenceSet());

        Assert.Equal("history-event-01", attribution.Event.EventId);
        Assert.Equal(
            "other-operation", attribution.Event.Operation.Value);
        Assert.Equal(
            "VerificationEvidenceRecorded",
            attribution.Event.EventKind);
        Assert.Same(historyEvent, attribution.Event);
    }

    [Fact]
    public void PreservesEvidenceOperationAndScope()
    {
        var evidenceSet = EvidenceSet(Evidence());
        var attribution = Create(evidenceSet);

        Assert.Equal(
            "operation-01",
            attribution.EvidenceSet.Operation.Value);
        Assert.Same(
            evidenceSet.Scope,
            attribution.EvidenceSet.Scope);
        Assert.Equal(
            "service-01",
            attribution.EvidenceSet.Scope.Target.NativeId);
    }

    [Fact]
    public void PreservesEmptyEvidenceSet()
    {
        var evidenceSet = EvidenceSet();
        var attribution = Create(evidenceSet);

        Assert.Same(evidenceSet, attribution.EvidenceSet);
        Assert.Empty(attribution.EvidenceSet.Records);
    }

    [Fact]
    public void PreservesSingleEvidenceRecordReference()
    {
        var record = Evidence();
        var attribution = Create(EvidenceSet(record));

        Assert.Same(
            record,
            Assert.Single(attribution.EvidenceSet.Records));
    }

    [Fact]
    public void PreservesMultipleEvidenceRecordOrder()
    {
        var first = Evidence("evidence-01");
        var second = Evidence("evidence-02");
        var third = Evidence("evidence-03");

        var attribution = Create(
            EvidenceSet(first, second, third));

        Assert.Equal(3, attribution.EvidenceSet.Records.Count);
        Assert.Same(first, attribution.EvidenceSet.Records[0]);
        Assert.Same(second, attribution.EvidenceSet.Records[1]);
        Assert.Same(third, attribution.EvidenceSet.Records[2]);
    }

    [Fact]
    public void PreservesEvidenceIdentifiersAndSources()
    {
        var record = Evidence(
            evidenceId: " EV-01 ",
            sourceId: " SRC-01 ");

        var attribution = Create(EvidenceSet(record));
        var retained = Assert.Single(
            attribution.EvidenceSet.Records);

        Assert.Same(record, retained);
        Assert.Equal("EV-01", retained.EvidenceId);
        Assert.Equal("SRC-01", retained.SourceId);
        Assert.Equal(
            OperationVerificationFamily.Service,
            retained.Family);
        Assert.Equal("check-01", retained.CheckId);
    }

    [Fact]
    public void PreservesRecordingAndObservationTimes()
    {
        var suppliedRecorded = new DateTimeOffset(
            2026, 10, 31, 13, 45, 0,
            TimeSpan.FromHours(3));

        var record = Evidence(
            recordedAt: suppliedRecorded);

        var attribution = Create(EvidenceSet(record));
        var retained = Assert.Single(
            attribution.EvidenceSet.Records);

        Assert.Same(record, retained);
        Assert.Equal(suppliedRecorded, retained.RecordedAt);
        Assert.Equal(
            suppliedRecorded.Offset,
            retained.RecordedAt.Offset);
        Assert.Equal(Observed, retained.ObservedAt);
        Assert.Equal(
            Observed.Offset,
            retained.ObservedAt.Offset);
    }

    [Fact]
    public void PreservesOriginalVerificationResultReference()
    {
        var result = Result(observedStatus: 503);
        var record = Evidence(result: result);
        var attribution = Create(EvidenceSet(record));

        var retained = Assert.Single(
            attribution.EvidenceSet.Records);

        Assert.Same(result, retained.Result);
        Assert.NotEqual("Matched", retained.Result.DispositionName);
        Assert.Equal(
            OperationVerificationFamily.Service,
            retained.Result.Family);
    }

    [Fact]
    public void RejectsNullHistoryEvent()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationHistoryVerificationEvidenceAttribution(
                null!, EvidenceSet()));
    }

    [Fact]
    public void RejectsNullEvidenceSet()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationHistoryVerificationEvidenceAttribution(
                Event(), null!));
    }

    [Fact]
    public void DoesNotEnforceNewHistoryEvidenceCorrelation()
    {
        var historyEvent = Event("different-history-operation");
        var evidenceSet = EvidenceSet(Evidence());

        var attribution =
            new OperationHistoryVerificationEvidenceAttribution(
                historyEvent, evidenceSet);

        Assert.Same(historyEvent, attribution.Event);
        Assert.Same(evidenceSet, attribution.EvidenceSet);
        Assert.NotEqual(
            attribution.Event.Operation,
            attribution.EvidenceSet.Operation);
    }

    [Fact]
    public void DoesNotInferVerificationOrExecutionSuccess()
    {
        var mismatched = Result(observedStatus: 503);
        var record = Evidence(result: mismatched);
        var attribution = Create(EvidenceSet(record));

        Assert.Same(
            mismatched,
            attribution.EvidenceSet.Records[0].Result);

        Assert.NotEqual(
            "Matched",
            attribution.EvidenceSet.Records[0]
                .Result.DispositionName);

        var names = typeof(
            OperationHistoryVerificationEvidenceAttribution)
            .GetProperties(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly)
            .Select(property => property.Name)
            .ToArray();

        foreach (var forbidden in new[]
        {
            "VerifiedSuccess",
            "ExecutionSucceeded",
            "Authorized",
            "AuthenticatedProvenance",
            "IsFresh",
            "Outcome",
            "Disposition",
            "CanExecute"
        })
        {
            Assert.DoesNotContain(forbidden, names);
        }
    }

    [Fact]
    public void IsPublicSealedWithExactlyTwoGetterOnlyProperties()
    {
        var type = typeof(
            OperationHistoryVerificationEvidenceAttribution);

        Assert.True(type.IsPublic);
        Assert.True(type.IsClass);
        Assert.True(type.IsSealed);
        Assert.Equal(
            "TruthDoctor.Models.Platform",
            type.Namespace);

        var properties = type.GetProperties(
            BindingFlags.Public |
            BindingFlags.Instance |
            BindingFlags.DeclaredOnly);

        Assert.Equal(2, properties.Length);

        var eventProperty = Assert.Single(
            properties, property => property.Name == "Event");
        var evidenceProperty = Assert.Single(
            properties, property => property.Name == "EvidenceSet");

        Assert.Equal(
            typeof(OperationHistoryEvent),
            eventProperty.PropertyType);
        Assert.Equal(
            typeof(OperationVerificationEvidenceSet),
            evidenceProperty.PropertyType);

        Assert.NotNull(eventProperty.GetMethod);
        Assert.NotNull(evidenceProperty.GetMethod);
        Assert.Null(eventProperty.SetMethod);
        Assert.Null(evidenceProperty.SetMethod);
    }

    [Fact]
    public void HasOneConstructorAndNoOperationalMethods()
    {
        var type = typeof(
            OperationHistoryVerificationEvidenceAttribution);

        var constructor = Assert.Single(
            type.GetConstructors(
                BindingFlags.Public |
                BindingFlags.Instance));

        var parameters = constructor.GetParameters();

        Assert.Equal(2, parameters.Length);
        Assert.Equal("historyEvent", parameters[0].Name);
        Assert.Equal(
            typeof(OperationHistoryEvent),
            parameters[0].ParameterType);
        Assert.Equal("evidenceSet", parameters[1].Name);
        Assert.Equal(
            typeof(OperationVerificationEvidenceSet),
            parameters[1].ParameterType);

        Assert.All(
            parameters,
            parameter => Assert.False(parameter.IsOptional));

        var methods = type.GetMethods(
            BindingFlags.Public |
            BindingFlags.Instance |
            BindingFlags.Static |
            BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .ToArray();

        Assert.Empty(methods);
        Assert.Empty(type.GetInterfaces());
    }
}
