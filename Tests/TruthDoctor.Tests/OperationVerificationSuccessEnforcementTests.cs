using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationVerificationSuccessEnforcementTests
{
    [Fact]
    public void EnforcementTypeMustBePublicAndSealed()
    {
        var type = typeof(OperationVerificationSuccessEnforcement);

        Assert.True(type.IsPublic);
        Assert.True(type.IsSealed);
    }

    [Fact]
    public void EvaluationAcceptsOneExistingVerificationOutcome()
    {
        var method = typeof(OperationVerificationSuccessEnforcement)
            .GetMethod(
                "Evaluate",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(OperationVerificationOutcome) },
                modifiers: null);

        Assert.NotNull(method);
        Assert.Equal(
            typeof(OperationVerificationEnforcementDecision),
            method.ReturnType);
    }

    [Fact]
    public void EnforcementDispositionContainsNoExecutionSuccessState()
    {
        var values = Enum.GetNames(
            typeof(OperationVerificationEnforcementDisposition));

        Assert.Equal(
            new[]
            {
                "NotEstablished",
                "Blocked",
                "LocallyConsistentOnly"
            },
            values);

        Assert.Equal(
            0,
            (int)OperationVerificationEnforcementDisposition.NotEstablished);
    }

    [Fact]
    public void EnforcementDecisionIsImmutable()
    {
        var type = typeof(OperationVerificationEnforcementDecision);

        Assert.True(type.IsPublic);
        Assert.True(type.IsSealed);

        var properties = type.GetProperties(
            BindingFlags.Instance | BindingFlags.Public);

        Assert.NotEmpty(properties);

        foreach (var property in properties)
        {
            Assert.NotNull(property.GetMethod);
            Assert.Null(property.SetMethod);
        }

        Assert.Contains(
            properties,
            property =>
                property.Name == "Disposition" &&
                property.PropertyType ==
                    typeof(OperationVerificationEnforcementDisposition));

        Assert.DoesNotContain(
            properties,
            property =>
                property.Name.Contains(
                    "Success",
                    StringComparison.OrdinalIgnoreCase) ||
                property.Name.Contains(
                    "Authenticated",
                    StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void NullVerificationOutcomeIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            OperationVerificationSuccessEnforcement.Evaluate(null!));
    }

    // Real service-verification fixtures from the published contracts.
    private static readonly OperationCorrelationId TestOperation =
        new("enforcement-operation-01");

    private static readonly DateTimeOffset TestTimestamp =
        new(2026, 10, 30, 0, 0, 0, TimeSpan.Zero);

    private static OperationResourceScope TestScope()
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

    private static OperationOutcomeCondition TestCondition()
    {
        return new OperationOutcomeCondition(
            "service.http.status", "200");
    }

    private static OperationServiceCheckSpecification TestSpecification()
    {
        return new OperationServiceCheckSpecification(
            new OperationServiceProtocol("http.response-status"),
            new OperationServiceEndpoint(
                "https://example.com/health"),
            "GET",
            new OperationServiceResponseCriteria(200));
    }

    private static OperationVerificationRequirement TestRequirement()
    {
        return OperationVerificationRequirement.ForService(
            "http-health",
            TestCondition(),
            TestScope(),
            TestSpecification());
    }

    private static OperationVerificationResult TestResult(
        int observedStatus = 200,
        OperationServiceObservationDisposition disposition =
            OperationServiceObservationDisposition.Responded)
    {
        var specification = TestSpecification();

        var observation = new OperationServiceObservation(
            TestOperation,
            TestScope().Target,
            specification,
            TestTimestamp,
            disposition,
            disposition == OperationServiceObservationDisposition.Responded
                ? observedStatus
                : null);

        var comparison = OperationServiceComparison.Evaluate(
            TestCondition(),
            TestScope(),
            specification,
            observation);

        return OperationVerificationResult.FromService(
            "http-health", comparison);
    }

    private static OperationVerificationOutcome TestOutcome(
        OperationVerificationRequirement[] requirements,
        OperationVerificationResult[] results)
    {
        return OperationVerificationOutcome.Evaluate(
            TestOperation, TestScope(), requirements, results);
    }

    [Fact]
    public void OutcomeWithoutRequirementsNeverClaimsExecutionSuccess()
    {
        var outcome = TestOutcome(
            Array.Empty<OperationVerificationRequirement>(),
            Array.Empty<OperationVerificationResult>());

        Assert.Equal(
            OperationVerificationOutcomeDisposition.NoRequirements,
            outcome.Disposition);

        var decision =
            OperationVerificationSuccessEnforcement.Evaluate(outcome);

        Assert.Equal(
            OperationVerificationEnforcementDisposition.NotEstablished,
            decision.Disposition);
    }

    [Fact]
    public void MissingRequiredCheckNeverClaimsExecutionSuccess()
    {
        var outcome = TestOutcome(
            new[] { TestRequirement() },
            Array.Empty<OperationVerificationResult>());

        Assert.Equal(
            OperationVerificationOutcomeDisposition.Incomplete,
            outcome.Disposition);

        var decision =
            OperationVerificationSuccessEnforcement.Evaluate(outcome);

        Assert.Equal(
            OperationVerificationEnforcementDisposition.NotEstablished,
            decision.Disposition);
    }

    [Fact]
    public void MismatchedObservationBlocksSuccessPromotion()
    {
        var outcome = TestOutcome(
            new[] { TestRequirement() },
            new[] { TestResult(observedStatus: 500) });

        Assert.Equal(
            OperationVerificationOutcomeDisposition.Inconsistent,
            outcome.Disposition);

        var decision =
            OperationVerificationSuccessEnforcement.Evaluate(outcome);

        Assert.Equal(
            OperationVerificationEnforcementDisposition.Blocked,
            decision.Disposition);
    }

    [Fact]
    public void UnknownObservationNeverClaimsExecutionSuccess()
    {
        var outcome = TestOutcome(
            new[] { TestRequirement() },
            new[] {
                TestResult(
                    disposition:
                        OperationServiceObservationDisposition.Unknown)
            });

        Assert.Equal(
            OperationVerificationOutcomeDisposition.Unresolved,
            outcome.Disposition);

        var decision =
            OperationVerificationSuccessEnforcement.Evaluate(outcome);

        Assert.Equal(
            OperationVerificationEnforcementDisposition.NotEstablished,
            decision.Disposition);
    }

    [Fact]
    public void ConflictingResultsBlockSuccessPromotion()
    {
        var result = TestResult();

        var outcome = TestOutcome(
            new[] { TestRequirement() },
            new[] { result, result });

        Assert.Equal(
            OperationVerificationOutcomeDisposition.Conflict,
            outcome.Disposition);

        var decision =
            OperationVerificationSuccessEnforcement.Evaluate(outcome);

        Assert.Equal(
            OperationVerificationEnforcementDisposition.Blocked,
            decision.Disposition);
    }

    [Fact]
    public void MatchingVerificationIsOnlyLocallyConsistent()
    {
        var outcome = TestOutcome(
            new[] { TestRequirement() },
            new[] { TestResult() });

        Assert.Equal(
            OperationVerificationOutcomeDisposition.LocallyConsistent,
            outcome.Disposition);

        var decision =
            OperationVerificationSuccessEnforcement.Evaluate(outcome);

        Assert.Equal(
            OperationVerificationEnforcementDisposition.LocallyConsistentOnly,
            decision.Disposition);
    }


    // E07-E12 and additional identity/acknowledgement negative paths.
    private static OperationVerificationOutcome MatchingOutcome()
    {
        return TestOutcome(
            new[] { TestRequirement() },
            new[] { TestResult() });
    }

    private static OperationVerificationEvidenceSet EvidenceSet(
        OperationCorrelationId? operation = null,
        string nativeId = "service-01",
        bool includeRecord = false,
        string sourceId = "caller-declared-source",
        DateTimeOffset? recordedAt = null,
        OperationVerificationOutcome? outcome = null)
    {
        var records = includeRecord
            ? new[]
            {
                new OperationVerificationEvidenceRecord(
                    "evidence-01",
                    sourceId,
                    recordedAt ?? TestTimestamp,
                    outcome?.Results.Single() ??
                        throw new ArgumentException(
                            "Populated evidence requires an outcome.",
                            nameof(outcome)))
            }
            : Array.Empty<OperationVerificationEvidenceRecord>();

        var scope = new OperationResourceScope(
            new OperationTarget(
                "provider-a",
                "account-a",
                "us-east-1",
                "application",
                "service",
                nativeId));

        return new OperationVerificationEvidenceSet(
            operation ?? TestOperation,
            scope,
            records);
    }

    private static OperationProviderResponseEvidence ProviderResponse(
        OperationCorrelationId? operation = null,
        OperationProviderResponseDisposition disposition =
            OperationProviderResponseDisposition.Acknowledged)
    {
        return new OperationProviderResponseEvidence(
            operation ?? TestOperation,
            "step-01",
            "provider-a",
            TestTimestamp,
            disposition);
    }

    [Fact]
    public void EmptyEvidenceSetCannotEstablishExecutionSuccess()
    {
        var decision = OperationVerificationSuccessEnforcement.Evaluate(
            MatchingOutcome(), EvidenceSet());

        Assert.Equal(
            OperationVerificationEnforcementDisposition.LocallyConsistentOnly,
            decision.Disposition);
    }

    [Fact]
    public void PopulatedMatchingEvidenceStillDoesNotProveExecution()
    {
        var outcome = MatchingOutcome();
        var decision = OperationVerificationSuccessEnforcement.Evaluate(
            outcome,
            EvidenceSet(includeRecord: true, outcome: outcome));

        Assert.Equal(
            OperationVerificationEnforcementDisposition.LocallyConsistentOnly,
            decision.Disposition);
    }

    [Fact]
    public void CrossOperationEvidenceIsRejectedByEnforcement()
    {
        var foreign = EvidenceSet(
            operation: new OperationCorrelationId("other-operation"));

        Assert.Throws<ArgumentException>(() =>
            OperationVerificationSuccessEnforcement.Evaluate(
                MatchingOutcome(), foreign));
    }

    [Fact]
    public void CrossResourceEvidenceIsRejectedByEnforcement()
    {
        var foreign = EvidenceSet(nativeId: "other-service");

        Assert.Throws<ArgumentException>(() =>
            OperationVerificationSuccessEnforcement.Evaluate(
                MatchingOutcome(), foreign));
    }

    [Fact]
    public void AcknowledgedProviderResponseCannotProveExecution()
    {
        var outcome = MatchingOutcome();
        var decision = OperationVerificationSuccessEnforcement.Evaluate(
            outcome,
            EvidenceSet(includeRecord: true, outcome: outcome),
            ProviderResponse());

        Assert.Equal(
            OperationVerificationEnforcementDisposition.LocallyConsistentOnly,
            decision.Disposition);
    }

    [Fact]
    public void CrossOperationProviderResponseIsRejected()
    {
        var foreign = ProviderResponse(
            operation: new OperationCorrelationId("other-operation"));

        Assert.Throws<ArgumentException>(() =>
            OperationVerificationSuccessEnforcement.Evaluate(
                MatchingOutcome(), EvidenceSet(), foreign));
    }

    [Fact]
    public void CallerDeclaredSourceAndTimestampDoNotAuthenticateEvidence()
    {
        var later = TestTimestamp.AddDays(10);
        var outcome = MatchingOutcome();
        var evidence = EvidenceSet(
            includeRecord: true,
            sourceId: "trusted-looking-source",
            recordedAt: later,
            outcome: outcome);

        var decision = OperationVerificationSuccessEnforcement.Evaluate(
            outcome, evidence);

        Assert.Equal(
            OperationVerificationEnforcementDisposition.LocallyConsistentOnly,
            decision.Disposition);
    }

    [Fact]
    public void ProviderAcknowledgementCannotOverrideConflict()
    {
        var result = TestResult();
        var conflict = TestOutcome(
            new[] { TestRequirement() },
            new[] { result, result });

        var decision = OperationVerificationSuccessEnforcement.Evaluate(
            conflict,
            EvidenceSet(),
            ProviderResponse());

        Assert.Equal(
            OperationVerificationEnforcementDisposition.Blocked,
            decision.Disposition);
    }

    [Fact]
    public void ProviderAcknowledgementCannotOverrideIncomplete()
    {
        var incomplete = TestOutcome(
            new[] { TestRequirement() },
            Array.Empty<OperationVerificationResult>());

        var decision = OperationVerificationSuccessEnforcement.Evaluate(
            incomplete,
            EvidenceSet(),
            ProviderResponse());

        Assert.Equal(
            OperationVerificationEnforcementDisposition.NotEstablished,
            decision.Disposition);
    }


    [Fact]
    public void EnforcementDispositionHasOnlyThreeExplicitValues()
    {
        var type = typeof(OperationVerificationEnforcementDisposition);
        Assert.True(type.IsEnum);

        Assert.Equal(
            new[]
            {
                "NotEstablished",
                "Blocked",
                "LocallyConsistentOnly"
            },
            Enum.GetNames(type));

        Assert.Equal(
            0,
            (int)OperationVerificationEnforcementDisposition.NotEstablished);
        Assert.Equal(
            1,
            (int)OperationVerificationEnforcementDisposition.Blocked);
        Assert.Equal(
            2,
            (int)OperationVerificationEnforcementDisposition.LocallyConsistentOnly);

        Assert.False(Enum.IsDefined(type, -1));
        Assert.False(Enum.IsDefined(type, 3));
        Assert.False(Enum.IsDefined(type, 999));
    }

    [Fact]
    public void DefaultEnforcementDispositionIsNotEstablished()
    {
        var disposition =
            default(OperationVerificationEnforcementDisposition);

        Assert.Equal(
            OperationVerificationEnforcementDisposition.NotEstablished,
            disposition);

        Assert.NotEqual(
            OperationVerificationEnforcementDisposition.LocallyConsistentOnly,
            disposition);
    }

    [Fact]
    public void EvaluationExposesExactlyThreeSupportedOverloads()
    {
        var type = typeof(OperationVerificationSuccessEnforcement);

        var methods = type.GetMethods(
                BindingFlags.Public |
                BindingFlags.Static |
                BindingFlags.DeclaredOnly)
            .Where(method => method.Name == "Evaluate")
            .ToArray();

        Assert.Equal(3, methods.Length);

        var signatures = new[]
        {
            new[] { typeof(OperationVerificationOutcome) },
            new[]
            {
                typeof(OperationVerificationOutcome),
                typeof(OperationVerificationEvidenceSet)
            },
            new[]
            {
                typeof(OperationVerificationOutcome),
                typeof(OperationVerificationEvidenceSet),
                typeof(OperationProviderResponseEvidence)
            }
        };

        foreach (var signature in signatures)
        {
            var method = type.GetMethod(
                "Evaluate",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: signature,
                modifiers: null);

            Assert.NotNull(method);
            Assert.Equal(
                typeof(OperationVerificationEnforcementDecision),
                method.ReturnType);
        }
    }

    [Fact]
    public void PublicEnforcementApiCannotClaimAuthenticatedSuccess()
    {
        var types = new[]
        {
            typeof(OperationVerificationSuccessEnforcement),
            typeof(OperationVerificationEnforcementDecision)
        };

        foreach (var type in types)
        {
            var properties = type.GetProperties(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.Static);

            foreach (var property in properties)
            {
                Assert.DoesNotContain(
                    "Success",
                    property.Name,
                    StringComparison.OrdinalIgnoreCase);

                Assert.DoesNotContain(
                    "Authenticated",
                    property.Name,
                    StringComparison.OrdinalIgnoreCase);
            }

            var methods = type.GetMethods(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.Static |
                BindingFlags.DeclaredOnly);

            foreach (var method in methods)
            {
                Assert.DoesNotContain(
                    "VerifiedSuccess",
                    method.Name,
                    StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain(
                    "ExecutionSucceeded",
                    method.Name,
                    StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain(
                    "AuthenticatedProvenance",
                    method.Name,
                    StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void NullOutcomeIsRejectedByEveryEvaluationOverload()
    {
        Assert.Throws<ArgumentNullException>(() =>
            OperationVerificationSuccessEnforcement.Evaluate(null!));

        Assert.Throws<ArgumentNullException>(() =>
            OperationVerificationSuccessEnforcement.Evaluate(
                null!, EvidenceSet()));

        Assert.Throws<ArgumentNullException>(() =>
            OperationVerificationSuccessEnforcement.Evaluate(
                null!, EvidenceSet(), ProviderResponse()));
    }

    [Fact]
    public void EvidenceForUnrelatedCheckIsRejectedByEnforcement()
    {
        var original = TestResult();

        var unrelated = OperationVerificationResult.FromService(
            "unrelated-check",
            (OperationServiceComparison)original.OriginalComparison);

        var record = new OperationVerificationEvidenceRecord(
            "evidence-unrelated",
            "caller-source",
            TestTimestamp,
            unrelated);

        var evidence = new OperationVerificationEvidenceSet(
            TestOperation,
            TestScope(),
            new[] { record });

        Assert.Throws<ArgumentException>(() =>
            OperationVerificationSuccessEnforcement.Evaluate(
                MatchingOutcome(), evidence));
    }


    [Fact]
    public void ExplicitNullEvidenceSetIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            OperationVerificationSuccessEnforcement.Evaluate(
                MatchingOutcome(),
                (OperationVerificationEvidenceSet)null!));

        Assert.Throws<ArgumentNullException>(() =>
            OperationVerificationSuccessEnforcement.Evaluate(
                MatchingOutcome(),
                (OperationVerificationEvidenceSet)null!,
                ProviderResponse()));
    }

    [Fact]
    public void ExplicitNullProviderResponseIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            OperationVerificationSuccessEnforcement.Evaluate(
                MatchingOutcome(),
                EvidenceSet(),
                (OperationProviderResponseEvidence)null!));
    }

    [Fact]
    public void EvidenceResultAbsentFromOutcomeIsRejected()
    {
        var unrelated = OperationVerificationResult.FromService(
            "different-check",
            (OperationServiceComparison)TestResult().OriginalComparison);

        var record = new OperationVerificationEvidenceRecord(
            "evidence-outside-outcome",
            "source-declared",
            TestTimestamp,
            unrelated);

        var set = new OperationVerificationEvidenceSet(
            TestOperation,
            TestScope(),
            new[] { record });

        Assert.Throws<ArgumentException>(() =>
            OperationVerificationSuccessEnforcement.Evaluate(
                MatchingOutcome(), set));
    }

    [Fact]
    public void ReusedCheckIdWithDifferentResultObjectIsRejected()
    {
        var actualResult = TestResult();
        var outcome = TestOutcome(
            new[] { TestRequirement() },
            new[] { actualResult });

        var independentResult = TestResult();
        Assert.NotSame(actualResult, independentResult);
        Assert.Equal(actualResult.CheckId, independentResult.CheckId);

        var record = new OperationVerificationEvidenceRecord(
            "evidence-same-check-different-result",
            "source-declared",
            TestTimestamp,
            independentResult);

        var set = new OperationVerificationEvidenceSet(
            TestOperation,
            TestScope(),
            new[] { record });

        Assert.Throws<ArgumentException>(() =>
            OperationVerificationSuccessEnforcement.Evaluate(outcome, set));
    }

    [Fact]
    public void AllProviderResponseDispositionsRemainNonProof()
    {
        foreach (var disposition in new[]
        {
            OperationProviderResponseDisposition.Unknown,
            OperationProviderResponseDisposition.Acknowledged,
            OperationProviderResponseDisposition.Rejected
        })
        {
            var outcome = MatchingOutcome();
            var decision = OperationVerificationSuccessEnforcement.Evaluate(
                outcome,
                EvidenceSet(includeRecord: true, outcome: outcome),
                ProviderResponse(disposition: disposition));

            Assert.Equal(
                OperationVerificationEnforcementDisposition.LocallyConsistentOnly,
                decision.Disposition);
        }
    }

    [Fact]
    public void EnforcementDecisionHasNoPublicConstructors()
    {
        var type = typeof(OperationVerificationEnforcementDecision);
        Assert.True(type.IsSealed);

        Assert.Empty(type.GetConstructors(
            BindingFlags.Public | BindingFlags.Instance));

        Assert.Empty(type.GetConstructors(
            BindingFlags.Public | BindingFlags.Static));
    }

    [Fact]
    public void InvalidOutcomeDispositionCannotBecomeSuccess()
    {
        var outcome = MatchingOutcome();

        // This is a corruption test, not a public construction pattern.
        var field = typeof(OperationVerificationOutcome).GetField(
            "<Disposition>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(field);

        field.SetValue(
            outcome,
            (OperationVerificationOutcomeDisposition)999);

        Assert.Equal(
            (OperationVerificationOutcomeDisposition)999,
            outcome.Disposition);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            OperationVerificationSuccessEnforcement.Evaluate(outcome));
    }

}
