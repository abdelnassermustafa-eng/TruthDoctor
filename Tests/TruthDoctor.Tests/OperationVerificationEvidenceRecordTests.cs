using System;
using System.Linq;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationVerificationEvidenceRecordTests
{
    private static readonly DateTimeOffset Observed =
        new(2026, 10, 30, 9, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset Recorded =
        new(2026, 10, 30, 9, 1, 0, TimeSpan.Zero);

    private static OperationVerificationResult Result(
        string operationId = "operation-01",
        string resourceId = "service-01",
        string checkId = "check-01",
        int observedStatus = 200)
    {
        var operation = new OperationCorrelationId(operationId);
        var target = new OperationTarget(
            "provider-01", "account-01", "region-01",
            "domain-01", "service", resourceId);
        var scope = new OperationResourceScope(target);
        var condition = new OperationOutcomeCondition(
            "service.http.status", "200");
        var specification = new OperationServiceCheckSpecification(
            new OperationServiceProtocol("http.response-status"),
            new OperationServiceEndpoint("https://example.com/health"),
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

    private static OperationVerificationEvidenceRecord Create(
        string evidenceId = "evidence-01",
        string sourceId = "source-01",
        DateTimeOffset? recordedAt = null,
        OperationVerificationResult? result = null)
        => new(
            evidenceId, sourceId, recordedAt ?? Recorded,
            result ?? Result());

    [Fact]
    public void EvidenceRecordTypeMustBePublic()
        => Assert.True(typeof(OperationVerificationEvidenceRecord).IsPublic);

    [Fact]
    public void EvidenceRecordTypeMustBeSealed()
        => Assert.True(typeof(OperationVerificationEvidenceRecord).IsSealed);

    [Fact]
    public void ExactPublicConstructorSignature()
    {
        var constructors =
            typeof(OperationVerificationEvidenceRecord).GetConstructors();

        Assert.Single(constructors);
        Assert.NotNull(
            typeof(OperationVerificationEvidenceRecord).GetConstructor(
                new[]
                {
                    typeof(string), typeof(string),
                    typeof(DateTimeOffset),
                    typeof(OperationVerificationResult)
                }));
    }

    [Fact]
    public void AllPublicPropertiesAreGetterOnly()
    {
        var properties = typeof(OperationVerificationEvidenceRecord)
            .GetProperties();

        Assert.NotEmpty(properties);
        Assert.All(properties, property =>
            Assert.False(property.SetMethod?.IsPublic ?? false));
    }

    [Fact]
    public void IdentifiersAndResultArePreserved()
    {
        var result = Result();
        var record = Create(result: result);

        Assert.Equal("evidence-01", record.EvidenceId);
        Assert.Equal("source-01", record.SourceId);
        Assert.Same(result, record.Result);
    }

    [Fact]
    public void OperationAndCheckMetadataAreDerived()
    {
        var record = Create();

        Assert.Equal("operation-01", record.Operation.Value);
        Assert.Equal(OperationVerificationFamily.Service, record.Family);
        Assert.Equal("check-01", record.CheckId);
        Assert.Equal("service-01", record.Scope.Target.NativeId);
    }

    [Fact]
    public void ObservationTimeIsIndependentOfRecordingTime()
    {
        var record = Create();

        Assert.Equal(Observed, record.ObservedAt);
        Assert.Equal(Recorded, record.RecordedAt);
        Assert.NotEqual(record.ObservedAt, record.RecordedAt);
    }

    [Fact]
    public void EvidenceIdIsTrimmedAndCasePreserved()
    {
        Assert.Equal("EV-01", Create(evidenceId: " EV-01 ").EvidenceId);
    }

    [Fact]
    public void SourceIdIsTrimmedAndCasePreserved()
    {
        Assert.Equal("SRC-01", Create(sourceId: " SRC-01 ").SourceId);
    }

    [Fact]
    public void BlankEvidenceIdIsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            Create(evidenceId: " \t "));
    }

    [Fact]
    public void BlankSourceIdIsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            Create(sourceId: " \t "));
    }

    [Fact]
    public void ControlCharacterInEvidenceIdIsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            Create(evidenceId: "evidence\n01"));
    }

    [Fact]
    public void ControlCharacterInSourceIdIsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            Create(sourceId: "source\n01"));
    }

    [Fact]
    public void DefaultRecordingTimestampIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OperationVerificationEvidenceRecord(
                "evidence-01", "source-01", default, Result()));
    }

    [Fact]
    public void NullVerificationResultIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationVerificationEvidenceRecord(
                "evidence-01", "source-01", Recorded, null!));
    }

    [Fact]
    public void MismatchedComparisonCanStillBeEvidence()
    {
        var result = Result(observedStatus: 503);
        var record = Create(result: result);

        Assert.Same(result, record.Result);
        Assert.NotEqual("Matched", result.DispositionName);
    }

    [Fact]
    public void NoPublicPropertyClaimsVerifiedExecution()
    {
        var names = typeof(OperationVerificationEvidenceRecord)
            .GetProperties().Select(property => property.Name).ToArray();

        Assert.DoesNotContain("VerifiedSuccess", names);
        Assert.DoesNotContain("ExecutionSucceeded", names);
        Assert.DoesNotContain("AuthenticatedProvenance", names);
        Assert.DoesNotContain("IsFresh", names);
    }

    [Fact]
    public void ResourceStateEvidenceDerivesObservationMetadata()
    {
        var operation = new OperationCorrelationId("state-operation");
        var target = new OperationTarget(
            "provider-01", "account-01", "region-01",
            "domain-01", "service", "state-resource");
        var scope = new OperationResourceScope(target);
        var condition = new OperationOutcomeCondition(
            "resource.state", "running");
        var observation = new OperationRediscoveryObservation(
            operation, target, Observed,
            OperationRediscoveryDisposition.Observed, "running");
        var comparison = OperationResourceStateComparison.Evaluate(
            condition, scope, observation);
        var result = OperationVerificationResult.FromResourceState(
            "state-check", comparison);

        var record = new OperationVerificationEvidenceRecord(
            "state-evidence", "source-01", Recorded, result);

        Assert.Equal(OperationVerificationFamily.ResourceState,
            record.Family);
        Assert.Equal(operation, record.Operation);
        Assert.Equal("state-check", record.CheckId);
        Assert.Equal(Observed, record.ObservedAt);
        Assert.Equal(Recorded, record.RecordedAt);
        Assert.True(record.Scope.Matches(target));
        Assert.Same(result, record.Result);
    }

    [Fact]
    public void HealthEvidenceDerivesObservationMetadata()
    {
        var operation = new OperationCorrelationId("health-operation");
        var target = new OperationTarget(
            "provider-01", "account-01", "region-01",
            "domain-01", "service", "health-resource");
        var scope = new OperationResourceScope(target);
        var condition = new OperationOutcomeCondition(
            "resource.health", "healthy");
        var observation = new OperationHealthObservation(
            operation, target, Observed,
            OperationHealthObservationDisposition.Observed,
            OperationHealthStatus.Healthy);
        var comparison = OperationHealthComparison.Evaluate(
            condition, scope, observation);
        var result = OperationVerificationResult.FromHealth(
            "health-check", comparison);

        var record = new OperationVerificationEvidenceRecord(
            "health-evidence", "source-01", Recorded, result);

        Assert.Equal(OperationVerificationFamily.Health, record.Family);
        Assert.Equal(operation, record.Operation);
        Assert.Equal("health-check", record.CheckId);
        Assert.Equal(Observed, record.ObservedAt);
        Assert.Equal(Recorded, record.RecordedAt);
        Assert.True(record.Scope.Matches(target));
        Assert.Same(result, record.Result);
    }

    [Fact]
    public void DependencyEvidenceDerivesObservationMetadata()
    {
        var operation = new OperationCorrelationId(
            "dependency-operation");
        var target = new OperationTarget(
            "provider-01", "account-01", "region-01",
            "domain-01", "service", "dependency-resource");
        var dependency = new OperationTarget(
            "provider-01", "account-01", "region-01",
            "domain-01", "service", "dependency-target");
        var scope = new OperationResourceScope(target);
        var condition = new OperationOutcomeCondition(
            "dependency.relationship", "present");
        var observation = new OperationDependencyObservation(
            operation, target, dependency, "depends-on",
            Observed, OperationDependencyObservationDisposition.Observed);
        var comparison = OperationDependencyComparison.Evaluate(
            condition, scope, dependency, "depends-on", observation);
        var result = OperationVerificationResult.FromDependency(
            "dependency-check", comparison);

        var record = new OperationVerificationEvidenceRecord(
            "dependency-evidence", "source-01", Recorded, result);

        Assert.Equal(OperationVerificationFamily.Dependency,
            record.Family);
        Assert.Equal(operation, record.Operation);
        Assert.Equal("dependency-check", record.CheckId);
        Assert.Equal(Observed, record.ObservedAt);
        Assert.Equal(Recorded, record.RecordedAt);
        Assert.True(record.Scope.Matches(target));
        Assert.Same(result, record.Result);
    }

    [Fact]
    public void ReachabilityEvidenceDerivesObservationMetadata()
    {
        var operation = new OperationCorrelationId(
            "reachability-operation");
        var target = new OperationTarget(
            "provider-01", "account-01", "region-01",
            "domain-01", "service", "reachability-resource");
        var scope = new OperationResourceScope(target);
        var condition = new OperationOutcomeCondition(
            "network.probe", "satisfied");
        var specification = new OperationReachabilityCheckSpecification(
            "192.0.2.10",
            new OperationReachabilityProtocol("tcp.connect"),
            new OperationReachabilityContext(vrf: "default"),
            443);
        var observation = new OperationReachabilityObservation(
            operation, target, specification, Observed,
            OperationReachabilityObservationDisposition.Satisfied);
        var comparison = OperationReachabilityComparison.Evaluate(
            condition, scope, specification, observation);
        var result = OperationVerificationResult.FromReachability(
            "reachability-check", comparison);

        var record = new OperationVerificationEvidenceRecord(
            "reachability-evidence", "source-01", Recorded, result);

        Assert.Equal(OperationVerificationFamily.Reachability,
            record.Family);
        Assert.Equal(operation, record.Operation);
        Assert.Equal("reachability-check", record.CheckId);
        Assert.Equal(Observed, record.ObservedAt);
        Assert.Equal(Recorded, record.RecordedAt);
        Assert.True(record.Scope.Matches(target));
        Assert.Same(result, record.Result);
    }
}
