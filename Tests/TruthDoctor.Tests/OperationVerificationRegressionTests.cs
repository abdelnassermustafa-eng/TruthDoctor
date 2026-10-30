using System;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

/// <summary>
/// Cross-contract regression tests for conservative verification.
/// None of these tests authenticates actual operation execution.
/// </summary>
public sealed class OperationVerificationRegressionTests
{
    private static readonly OperationCorrelationId Operation =
        new("regression-operation-01");

    private static readonly DateTimeOffset Timestamp =
        new(2026, 10, 30, 0, 0, 0, TimeSpan.Zero);

    private static OperationResourceScope Scope()
    {
        return new OperationResourceScope(
            new OperationTarget(
                "provider-a",
                "account-a",
                "us-east-1",
                "application",
                "service",
                "service-01"));
    }

    private static OperationOutcomeCondition Condition()
    {
        return new OperationOutcomeCondition(
            "service.http.status", "200");
    }

    private static OperationServiceCheckSpecification Specification()
    {
        return new OperationServiceCheckSpecification(
            new OperationServiceProtocol("http.response-status"),
            new OperationServiceEndpoint(
                "https://example.com/health"),
            "GET",
            new OperationServiceResponseCriteria(200));
    }

    private static OperationVerificationRequirement Requirement(
        string checkId = "http-health")
    {
        return OperationVerificationRequirement.ForService(
            checkId,
            Condition(),
            Scope(),
            Specification());
    }

    private static OperationVerificationResult Result(
        string checkId = "http-health",
        int observedStatus = 200,
        OperationServiceObservationDisposition disposition =
            OperationServiceObservationDisposition.Responded)
    {
        var specification = Specification();

        var observation = new OperationServiceObservation(
            Operation,
            Scope().Target,
            specification,
            Timestamp,
            disposition,
            disposition ==
                OperationServiceObservationDisposition.Responded
                ? observedStatus
                : null);

        var comparison = OperationServiceComparison.Evaluate(
            Condition(),
            Scope(),
            specification,
            observation);

        return OperationVerificationResult.FromService(
            checkId, comparison);
    }

    private static OperationVerificationOutcome Outcome(
        OperationVerificationRequirement[] requirements,
        OperationVerificationResult[] results)
    {
        return OperationVerificationOutcome.Evaluate(
            Operation,
            Scope(),
            requirements,
            results);
    }

    private static OperationVerificationEvidenceRecord Record(
        string evidenceId,
        OperationVerificationResult result,
        string sourceId = "caller-source",
        DateTimeOffset? recordedAt = null)
    {
        return new OperationVerificationEvidenceRecord(
            evidenceId,
            sourceId,
            recordedAt ?? Timestamp,
            result);
    }

    private static OperationVerificationEvidenceSet Evidence(
        params OperationVerificationEvidenceRecord[] records)
    {
        return new OperationVerificationEvidenceSet(
            Operation,
            Scope(),
            records);
    }

    private static OperationProviderResponseEvidence Acknowledgement()
    {
        return new OperationProviderResponseEvidence(
            Operation,
            "step-01",
            "provider-a",
            Timestamp,
            OperationProviderResponseDisposition.Acknowledged);
    }

    [Fact]
    public void MixedVerificationFailuresPreserveOutcomePrecedence()
    {
        var first = Requirement("check-a");
        var second = Requirement("check-b");
        var mismatched = Result("check-a", observedStatus: 500);
        var unknown = Result(
            "check-b",
            disposition: OperationServiceObservationDisposition.Unknown);

        var conflict = Outcome(
            new[] { first, second },
            new[] { mismatched, mismatched });

        Assert.Equal(
            OperationVerificationOutcomeDisposition.Conflict,
            conflict.Disposition);
        Assert.Equal(
            OperationVerificationEnforcementDisposition.Blocked,
            OperationVerificationSuccessEnforcement
                .Evaluate(conflict).Disposition);

        var incomplete = Outcome(
            new[] { first, second },
            new[] { mismatched });

        Assert.Equal(
            OperationVerificationOutcomeDisposition.Incomplete,
            incomplete.Disposition);
        Assert.Equal(
            OperationVerificationEnforcementDisposition.NotEstablished,
            OperationVerificationSuccessEnforcement
                .Evaluate(incomplete).Disposition);

        var inconsistent = Outcome(
            new[] { first, second },
            new[] { mismatched, unknown });

        Assert.Equal(
            OperationVerificationOutcomeDisposition.Inconsistent,
            inconsistent.Disposition);
        Assert.Equal(
            OperationVerificationEnforcementDisposition.Blocked,
            OperationVerificationSuccessEnforcement
                .Evaluate(inconsistent).Disposition);
    }

    [Fact]
    public void LaterInvalidEvidenceRecordRejectsEntireSet()
    {
        var actual = Result();
        var outcome = Outcome(
            new[] { Requirement() },
            new[] { actual });

        Assert.Equal(
            OperationVerificationOutcomeDisposition.LocallyConsistent,
            outcome.Disposition);

        var independent = Result();
        Assert.NotSame(actual, independent);
        Assert.Equal(actual.CheckId, independent.CheckId);

        var evidence = Evidence(
            Record("valid-first", actual),
            Record("invalid-second", independent));

        Assert.Equal(2, evidence.Records.Count);
        Assert.Same(actual, evidence.Records[0].Result);
        Assert.Same(independent, evidence.Records[1].Result);

        Assert.Throws<ArgumentException>(() =>
            OperationVerificationSuccessEnforcement.Evaluate(
                outcome, evidence));
    }

    [Fact]
    public void AcknowledgedProviderCannotOverrideInconsistentOutcome()
    {
        var mismatched = Result(observedStatus: 500);
        var outcome = Outcome(
            new[] { Requirement() },
            new[] { mismatched });

        Assert.Equal(
            OperationVerificationOutcomeDisposition.Inconsistent,
            outcome.Disposition);

        var decision = OperationVerificationSuccessEnforcement.Evaluate(
            outcome,
            Evidence(Record("mismatched-observation", mismatched)),
            Acknowledgement());

        Assert.Equal(
            OperationVerificationEnforcementDisposition.Blocked,
            decision.Disposition);
    }

    [Fact]
    public void EvidenceMetadataVariationsCannotPromoteExecutionSuccess()
    {
        var actual = Result();
        var outcome = Outcome(
            new[] { Requirement() },
            new[] { actual });

        var evidence = Evidence(
            Record(
                "source-early",
                actual,
                "provider-audit-looking-source",
                Timestamp.AddDays(-30)),
            Record(
                "source-late",
                actual,
                "caller-declared-source",
                Timestamp.AddDays(30)));

        Assert.Equal(2, evidence.Records.Count);
        Assert.NotEqual(
            evidence.Records[0].SourceId,
            evidence.Records[1].SourceId);
        Assert.NotEqual(
            evidence.Records[0].RecordedAt,
            evidence.Records[1].RecordedAt);

        var decision = OperationVerificationSuccessEnforcement.Evaluate(
            outcome, evidence, Acknowledgement());

        Assert.Equal(
            OperationVerificationEnforcementDisposition.LocallyConsistentOnly,
            decision.Disposition);
    }

    [Fact]
    public void InvalidEvidenceCannotBeMaskedByProviderResponse()
    {
        var actual = Result();
        var outcome = Outcome(
            new[] { Requirement() },
            new[] { actual });

        var independent = Result();
        Assert.NotSame(actual, independent);

        var evidence = Evidence(
            Record("accepted-reference-first", actual),
            Record("unrelated-reference-last", independent));

        var acknowledgement = Acknowledgement();

        Assert.Throws<ArgumentException>(() =>
            OperationVerificationSuccessEnforcement.Evaluate(
                outcome, evidence, acknowledgement));

        Assert.Equal(
            OperationVerificationOutcomeDisposition.LocallyConsistent,
            outcome.Disposition);
    }
}
