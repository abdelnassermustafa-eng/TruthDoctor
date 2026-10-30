using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

/// <summary>
/// Test-first contract for local aggregate verification.
/// Never establishes authenticated operation success.
/// </summary>
public sealed class OperationVerificationOutcomeTests
{
    private static readonly OperationCorrelationId Operation =
        new("aggregate-operation-01");

    private static readonly DateTimeOffset Timestamp =
        new(2026, 10, 30, 0, 0, 0, TimeSpan.Zero);

    private static OperationTarget Target(string nativeId = "service-01")
    {
        return new OperationTarget(
            "provider-a", "account-a", "us-east-1",
            "application", "service", nativeId);
    }

    private static OperationResourceScope Scope()
    {
        return new OperationResourceScope(Target());
    }

    private static OperationOutcomeCondition Condition(int status = 200)
    {
        return new OperationOutcomeCondition(
            "service.http.status", status.ToString());
    }

    private static OperationServiceCheckSpecification Specification(
        string endpoint = "https://example.com/health",
        int expectedStatus = 200)
    {
        return new OperationServiceCheckSpecification(
            new OperationServiceProtocol("http.response-status"),
            new OperationServiceEndpoint(endpoint),
            "GET",
            new OperationServiceResponseCriteria(expectedStatus));
    }

    private static OperationServiceComparison Compare(
        int expected = 200,
        int observed = 200,
        string endpoint = "https://example.com/health",
        string nativeId = "service-01",
        OperationCorrelationId? operation = null,
        OperationServiceObservationDisposition disposition =
            OperationServiceObservationDisposition.Responded)
    {
        var spec = Specification(endpoint, expected);

        var observation = new OperationServiceObservation(
            operation ?? Operation,
            Target(nativeId),
            spec,
            Timestamp,
            disposition,
            disposition ==
                OperationServiceObservationDisposition.Responded
                ? observed : null);

        return OperationServiceComparison.Evaluate(
            Condition(expected),
            Scope(),
            spec,
            observation);
    }

    private static OperationVerificationRequirement Required(
        string checkId = "http-health",
        string endpoint = "https://example.com/health")
    {
        return OperationVerificationRequirement.ForService(
            checkId, Condition(), Scope(), Specification(endpoint));
    }

    private static OperationVerificationResult Result(
        string checkId = "http-health",
        int expected = 200,
        int observed = 200,
        string endpoint = "https://example.com/health",
        string nativeId = "service-01",
        OperationCorrelationId? operation = null,
        OperationServiceObservationDisposition disposition =
            OperationServiceObservationDisposition.Responded)
    {
        return OperationVerificationResult.FromService(
            checkId,
            Compare(expected, observed, endpoint,
                nativeId, operation, disposition));
    }

    private static OperationVerificationOutcome Evaluate(
        IEnumerable<OperationVerificationRequirement> requirements,
        IEnumerable<OperationVerificationResult> results)
    {
        return OperationVerificationOutcome.Evaluate(
            Operation, Scope(), requirements, results);
    }

    [Fact]
    public void VerificationFamiliesHaveExplicitValues()
    {
        Assert.Equal(0, (int)OperationVerificationFamily.ResourceState);
        Assert.Equal(1, (int)OperationVerificationFamily.Health);
        Assert.Equal(2, (int)OperationVerificationFamily.Dependency);
        Assert.Equal(3, (int)OperationVerificationFamily.Reachability);
        Assert.Equal(4, (int)OperationVerificationFamily.Service);
    }

    [Fact]
    public void OutcomeDispositionsHaveExplicitValues()
    {
        Assert.Equal(0,
            (int)OperationVerificationOutcomeDisposition.NoRequirements);
        Assert.Equal(1,
            (int)OperationVerificationOutcomeDisposition.LocallyConsistent);
        Assert.Equal(2,
            (int)OperationVerificationOutcomeDisposition.Inconsistent);
        Assert.Equal(3,
            (int)OperationVerificationOutcomeDisposition.Incomplete);
        Assert.Equal(4,
            (int)OperationVerificationOutcomeDisposition.Unresolved);
        Assert.Equal(5,
            (int)OperationVerificationOutcomeDisposition.Conflict);
    }

    [Fact]
    public void OneMatchingRequiredServiceCheckIsLocallyConsistent()
    {
        var outcome = Evaluate(
            new[] { Required() },
            new[] { Result() });

        Assert.Equal(
            OperationVerificationOutcomeDisposition.LocallyConsistent,
            outcome.Disposition);
        Assert.Equal(Operation, outcome.Operation);
        Assert.Single(outcome.Requirements);
        Assert.Single(outcome.Results);
    }

    [Fact]
    public void MatchingIsNotRestrictedToHttp2xx()
    {
        var requirement =
            OperationVerificationRequirement.ForService(
                "expected-404", Condition(404), Scope(),
                Specification(expectedStatus: 404));

        var result =
            Result("expected-404", expected: 404, observed: 404);

        Assert.Equal(
            OperationVerificationOutcomeDisposition.LocallyConsistent,
            Evaluate(new[] { requirement }, new[] { result }).Disposition);
    }

    [Fact]
    public void MismatchedHttpStatusProducesInconsistentOutcome()
    {
        Assert.Equal(
            OperationVerificationOutcomeDisposition.Inconsistent,
            Evaluate(new[] { Required() },
                new[] { Result(observed: 500) }).Disposition);
    }

    [Fact]
    public void EmptyRequirementsCannotPass()
    {
        Assert.Equal(
            OperationVerificationOutcomeDisposition.NoRequirements,
            Evaluate(
                Array.Empty<OperationVerificationRequirement>(),
                Array.Empty<OperationVerificationResult>()).Disposition);
    }

    [Fact]
    public void MissingRequiredCheckProducesIncompleteOutcome()
    {
        Assert.Equal(
            OperationVerificationOutcomeDisposition.Incomplete,
            Evaluate(new[] { Required() },
                Array.Empty<OperationVerificationResult>()).Disposition);
    }

    [Fact]
    public void UnknownServiceObservationIsUnresolved()
    {
        Assert.Equal(
            OperationVerificationOutcomeDisposition.Unresolved,
            Evaluate(new[] { Required() },
                new[] {
                    Result(disposition:
                        OperationServiceObservationDisposition.Unknown)
                }).Disposition);
    }

    [Fact]
    public void UnavailableServiceObservationIsUnresolved()
    {
        Assert.Equal(
            OperationVerificationOutcomeDisposition.Unresolved,
            Evaluate(new[] { Required() },
                new[] {
                    Result(disposition:
                        OperationServiceObservationDisposition.Unavailable)
                }).Disposition);
    }

    [Fact]
    public void UnsupportedCapabilityIsUnresolved()
    {
        Assert.Equal(
            OperationVerificationOutcomeDisposition.Unresolved,
            Evaluate(new[] { Required() },
                new[] {
                    Result(disposition:
                        OperationServiceObservationDisposition.UnsupportedCapability)
                }).Disposition);
    }

    [Fact]
    public void NotAssessedIsUnresolved()
    {
        Assert.Equal(
            OperationVerificationOutcomeDisposition.Unresolved,
            Evaluate(new[] { Required() },
                new[] {
                    Result(disposition:
                        OperationServiceObservationDisposition.NotAssessed)
                }).Disposition);
    }

    [Fact]
    public void DuplicateResultsProduceConflict()
    {
        var comparison = Result();
        Assert.Equal(
            OperationVerificationOutcomeDisposition.Conflict,
            Evaluate(new[] { Required() },
                new[] { comparison, comparison }).Disposition);
    }

    [Fact]
    public void ContradictoryResultsProduceConflict()
    {
        Assert.Equal(
            OperationVerificationOutcomeDisposition.Conflict,
            Evaluate(new[] { Required() },
                new[] { Result(), Result(observed: 500) }).Disposition);
    }

    [Fact]
    public void DuplicateRequirementsProduceConflict()
    {
        var check = Required();
        Assert.Equal(
            OperationVerificationOutcomeDisposition.Conflict,
            Evaluate(new[] { check, check },
                new[] { Result() }).Disposition);
    }

    [Fact]
    public void DifferentOperationCorrelationCannotMatch()
    {
        Assert.NotEqual(
            OperationVerificationOutcomeDisposition.LocallyConsistent,
            Evaluate(new[] { Required() },
                new[] {
                    Result(operation:
                        new OperationCorrelationId("another-operation"))
                }).Disposition);
    }

    [Fact]
    public void DifferentResourceIdentityCannotMatch()
    {
        Assert.NotEqual(
            OperationVerificationOutcomeDisposition.LocallyConsistent,
            Evaluate(new[] { Required() },
                new[] { Result(nativeId: "another-service") }).Disposition);
    }

    [Fact]
    public void DifferentEndpointCannotSatisfyRequirement()
    {
        Assert.NotEqual(
            OperationVerificationOutcomeDisposition.LocallyConsistent,
            Evaluate(new[] { Required() },
                new[] {
                    Result(endpoint: "https://example.com/other")
                }).Disposition);
    }

    [Fact]
    public void ExpectedConditionMustMatchRequirement()
    {
        Assert.NotEqual(
            OperationVerificationOutcomeDisposition.LocallyConsistent,
            Evaluate(new[] { Required() },
                new[] { Result(expected: 404, observed: 404) })
                .Disposition);
    }

    [Fact]
    public void IncorrectCheckIdCannotSatisfyRequirement()
    {
        Assert.NotEqual(
            OperationVerificationOutcomeDisposition.LocallyConsistent,
            Evaluate(new[] { Required() },
                new[] { Result(checkId: "different-check") }).Disposition);
    }

    [Fact]
    public void UnexpectedResultCannotSilentlyPass()
    {
        Assert.NotEqual(
            OperationVerificationOutcomeDisposition.LocallyConsistent,
            Evaluate(new[] { Required() },
                new[] {
                    Result(),
                    Result(checkId: "unexpected-check")
                }).Disposition);
    }

    [Fact]
    public void RequirementPreservesDeclaredIdentity()
    {
        var condition = Condition();
        var scope = Scope();
        var specification = Specification();

        var required = OperationVerificationRequirement.ForService(
            "probe-a", condition, scope, specification);

        Assert.Equal(OperationVerificationFamily.Service,
            required.Family);
        Assert.Equal("probe-a", required.CheckId);
        Assert.Same(condition, required.Condition);
        Assert.Same(scope, required.Scope);
    }

    [Fact]
    public void ResultRetainsOriginalTypedComparison()
    {
        var comparison = Compare();
        var result = OperationVerificationResult.FromService(
            "probe-a", comparison);

        Assert.Equal(OperationVerificationFamily.Service,
            result.Family);
        Assert.Equal("probe-a", result.CheckId);
        Assert.Equal("Matched", result.DispositionName);
        Assert.Same(comparison, result.OriginalComparison);
    }

    [Fact]
    public void AggregateInputsAreDefensivelyCopied()
    {
        var requirements =
            new List<OperationVerificationRequirement> { Required() };
        var results =
            new List<OperationVerificationResult> { Result() };

        var outcome = Evaluate(requirements, results);
        requirements.Clear();
        results.Clear();

        Assert.Single(outcome.Requirements);
        Assert.Single(outcome.Results);
    }

    [Fact]
    public void AggregateCollectionsAreReadOnly()
    {
        var outcome = Evaluate(
            new[] { Required() }, new[] { Result() });

        var requirements =
            Assert.IsAssignableFrom<IList<OperationVerificationRequirement>>(
                outcome.Requirements);
        var results =
            Assert.IsAssignableFrom<IList<OperationVerificationResult>>(
                outcome.Results);

        Assert.Throws<NotSupportedException>(
            () => requirements.Clear());
        Assert.Throws<NotSupportedException>(
            () => results.Clear());
    }

    [Fact]
    public void NullCollectionsAreRejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            OperationVerificationOutcome.Evaluate(
                Operation, Scope(), null!,
                Array.Empty<OperationVerificationResult>()));

        Assert.Throws<ArgumentNullException>(() =>
            OperationVerificationOutcome.Evaluate(
                Operation, Scope(),
                Array.Empty<OperationVerificationRequirement>(), null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\n")]
    public void InvalidCheckIdsAreRejected(string checkId)
    {
        Assert.Throws<ArgumentException>(() =>
            OperationVerificationRequirement.ForService(
                checkId, Condition(), Scope(), Specification()));
    }

    [Fact]
    public void AllFiveComparisonFamiliesHaveTypedAdapters()
    {
        var requirementMethods = typeof(OperationVerificationRequirement)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Select(method => method.Name)
            .ToArray();

        var resultMethods = typeof(OperationVerificationResult)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Select(method => method.Name)
            .ToArray();

        foreach (var family in new[] {
            "ResourceState", "Health", "Dependency",
            "Reachability", "Service"
        })
        {
            Assert.Contains("For" + family, requirementMethods);
            Assert.Contains("From" + family, resultMethods);
        }
    }

    [Fact]
    public void NoAggregateApiClaimsVerifiedOperationSuccess()
    {
        var types = new[] {
            typeof(OperationVerificationRequirement),
            typeof(OperationVerificationResult),
            typeof(OperationVerificationOutcome)
        };

        foreach (var type in types)
        {
            Assert.True(type.IsSealed);
            foreach (var property in type.GetProperties(
                BindingFlags.Public | BindingFlags.Instance))
            {
                Assert.Null(property.SetMethod);
                Assert.DoesNotContain("Success", property.Name,
                    StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Verified", property.Name,
                    StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
