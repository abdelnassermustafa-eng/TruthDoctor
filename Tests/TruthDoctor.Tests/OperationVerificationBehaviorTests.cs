using System;
using System.Collections.Generic;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

/// <summary>
/// Five-family behavioral verification against caller-supplied data.
/// Local consistency never establishes operation execution success.
/// </summary>
public sealed class OperationVerificationBehaviorTests
{
    private static readonly OperationCorrelationId Operation =
        new("aggregate-behavior-01");

    private static readonly DateTimeOffset ObservedAt =
        new(2026, 10, 30, 0, 0, 0, TimeSpan.Zero);

    private static OperationTarget Target(string id = "resource-01")
        => new("provider-a", "account-a", "us-east-1",
            "domain-a", "resource", id);

    private static OperationResourceScope Scope()
        => new(Target());

    private static OperationVerificationOutcome Evaluate(
        OperationVerificationRequirement requirement,
        OperationVerificationResult result)
        => OperationVerificationOutcome.Evaluate(
            Operation, Scope(), new[] { requirement },
            new[] { result });

    private static OperationVerificationOutcome Evaluate(
        IEnumerable<OperationVerificationRequirement> requirements,
        IEnumerable<OperationVerificationResult> results)
        => OperationVerificationOutcome.Evaluate(
            Operation, Scope(), requirements, results);

    private static (OperationVerificationRequirement,
        OperationVerificationResult) State(
        OperationRediscoveryDisposition disposition,
        string? observedState = null)
    {
        var condition =
            new OperationOutcomeCondition("resource.state", "running");

        var observation = new OperationRediscoveryObservation(
            Operation, Target(), ObservedAt,
            disposition, observedState);

        var comparison = OperationResourceStateComparison.Evaluate(
            condition, Scope(), observation);

        return (
            OperationVerificationRequirement.ForResourceState(
                "state", condition, Scope()),
            OperationVerificationResult.FromResourceState(
                "state", comparison));
    }

    private static (OperationVerificationRequirement,
        OperationVerificationResult) Health(
        OperationHealthObservationDisposition disposition,
        OperationHealthStatus? status = null)
    {
        var condition =
            new OperationOutcomeCondition("resource.health", "healthy");

        var observation = new OperationHealthObservation(
            Operation, Target(), ObservedAt,
            disposition, status);

        var comparison = OperationHealthComparison.Evaluate(
            condition, Scope(), observation);

        return (
            OperationVerificationRequirement.ForHealth(
                "health", condition, Scope()),
            OperationVerificationResult.FromHealth(
                "health", comparison));
    }

    private static (OperationVerificationRequirement,
        OperationVerificationResult) Dependency(
        string expected,
        OperationDependencyObservationDisposition disposition,
        string requirementDependency = "dependency-01",
        string observedDependency = "dependency-01")
    {
        var condition = new OperationOutcomeCondition(
            "dependency.relationship", expected);

        var declaredDependency = Target(requirementDependency);
        var comparisonDependency = Target(observedDependency);

        var observation = new OperationDependencyObservation(
            Operation, Target(), comparisonDependency,
            "depends-on", ObservedAt, disposition);

        var comparison = OperationDependencyComparison.Evaluate(
            condition, Scope(), comparisonDependency,
            "depends-on", observation);

        return (
            OperationVerificationRequirement.ForDependency(
                "dependency", condition, Scope(),
                declaredDependency, "depends-on"),
            OperationVerificationResult.FromDependency(
                "dependency", comparison));
    }

    private static OperationReachabilityCheckSpecification ReachSpec(
        string destination = "192.0.2.10",
        int port = 443,
        string vrf = "default")
        => new(
            destination,
            new OperationReachabilityProtocol("tcp.connect"),
            new OperationReachabilityContext(vrf: vrf),
            port);

    private static (OperationVerificationRequirement,
        OperationVerificationResult) Reachability(
        string expected,
        OperationReachabilityObservationDisposition disposition,
        string requiredDestination = "192.0.2.10",
        string resultDestination = "192.0.2.10",
        string requiredVrf = "default",
        string resultVrf = "default")
    {
        var condition =
            new OperationOutcomeCondition("network.probe", expected);

        var required = ReachSpec(
            requiredDestination, vrf: requiredVrf);
        var actual = ReachSpec(
            resultDestination, vrf: resultVrf);

        var observation = new OperationReachabilityObservation(
            Operation, Target(), actual, ObservedAt, disposition);

        var comparison = OperationReachabilityComparison.Evaluate(
            condition, Scope(), actual, observation);

        return (
            OperationVerificationRequirement.ForReachability(
                "reachability", condition, Scope(), required),
            OperationVerificationResult.FromReachability(
                "reachability", comparison));
    }

    private static OperationServiceCheckSpecification ServiceSpec(
        string method = "GET",
        string endpoint = "https://example.com/health",
        int status = 200,
        string protocol = "http.response-status")
        => new(
            new OperationServiceProtocol(protocol),
            new OperationServiceEndpoint(endpoint),
            method,
            new OperationServiceResponseCriteria(status));

    private static (OperationVerificationRequirement,
        OperationVerificationResult) Service(
        string requiredMethod = "GET",
        string resultMethod = "GET",
        string requiredProtocol = "http.response-status",
        string resultProtocol = "http.response-status",
        int status = 200)
    {
        var condition = new OperationOutcomeCondition(
            "service.http.status", status.ToString());

        var required = ServiceSpec(
            requiredMethod, status: status,
            protocol: requiredProtocol);
        var actual = ServiceSpec(
            resultMethod, status: status,
            protocol: resultProtocol);

        var observation = new OperationServiceObservation(
            Operation, Target(), actual, ObservedAt,
            OperationServiceObservationDisposition.Responded,
            status);

        var comparison = OperationServiceComparison.Evaluate(
            condition, Scope(), actual, observation);

        return (
            OperationVerificationRequirement.ForService(
                "service", condition, Scope(), required),
            OperationVerificationResult.FromService(
                "service", comparison));
    }

    [Fact]
    public void ObservedExpectedResourceStateIsLocallyConsistent()
    {
        var (required, result) = State(
            OperationRediscoveryDisposition.Observed, "running");

        Assert.Equal("Matched", result.DispositionName);
        Assert.Equal(
            OperationVerificationOutcomeDisposition.LocallyConsistent,
            Evaluate(required, result).Disposition);
    }

    [Fact]
    public void AbsentResourceStateRemainsUnresolved()
    {
        var (required, result) =
            State(OperationRediscoveryDisposition.Absent);

        Assert.Equal("Absent", result.DispositionName);
        Assert.Equal(
            OperationVerificationOutcomeDisposition.Unresolved,
            Evaluate(required, result).Disposition);
    }

    [Fact]
    public void UnknownResourceStateRemainsUnresolved()
    {
        var (required, result) =
            State(OperationRediscoveryDisposition.Unknown);

        Assert.Equal("NoObservation", result.DispositionName);
        Assert.Equal(
            OperationVerificationOutcomeDisposition.Unresolved,
            Evaluate(required, result).Disposition);
    }

    [Fact]
    public void ExpectedHealthyObservationIsLocallyConsistent()
    {
        var (required, result) = Health(
            OperationHealthObservationDisposition.Observed,
            OperationHealthStatus.Healthy);

        Assert.Equal("Matched", result.DispositionName);
        Assert.Equal(
            OperationVerificationOutcomeDisposition.LocallyConsistent,
            Evaluate(required, result).Disposition);
    }

    [Fact]
    public void UnexpectedDegradedHealthIsInconsistent()
    {
        var (required, result) = Health(
            OperationHealthObservationDisposition.Observed,
            OperationHealthStatus.Degraded);

        Assert.Equal("Mismatched", result.DispositionName);
        Assert.Equal(
            OperationVerificationOutcomeDisposition.Inconsistent,
            Evaluate(required, result).Disposition);
    }

    [Fact]
    public void UnavailableHealthRemainsUnresolved()
    {
        var (required, result) = Health(
            OperationHealthObservationDisposition.Unavailable);

        Assert.Equal("Unavailable", result.DispositionName);
        Assert.Equal(
            OperationVerificationOutcomeDisposition.Unresolved,
            Evaluate(required, result).Disposition);
    }

    [Fact]
    public void ExpectedAbsentDependencyCanMatch()
    {
        var (required, result) = Dependency(
            "absent", OperationDependencyObservationDisposition.Absent);

        Assert.Equal("Matched", result.DispositionName);
        Assert.Equal(
            OperationVerificationOutcomeDisposition.LocallyConsistent,
            Evaluate(required, result).Disposition);
    }

    [Fact]
    public void UnexpectedAbsentDependencyIsInconsistent()
    {
        var (required, result) = Dependency(
            "present", OperationDependencyObservationDisposition.Absent);

        Assert.Equal("Absent", result.DispositionName);
        Assert.Equal(
            OperationVerificationOutcomeDisposition.Inconsistent,
            Evaluate(required, result).Disposition);
    }

    [Fact]
    public void DifferentDependencyTargetCannotSatisfyRequirement()
    {
        var (required, result) = Dependency(
            "present", OperationDependencyObservationDisposition.Observed,
            requirementDependency: "dependency-01",
            observedDependency: "dependency-02");

        Assert.Equal(
            OperationVerificationOutcomeDisposition.Conflict,
            Evaluate(required, result).Disposition);
    }

    [Fact]
    public void ExpectedSatisfiedReachabilityCanMatch()
    {
        var (required, result) = Reachability(
            "satisfied",
            OperationReachabilityObservationDisposition.Satisfied);

        Assert.Equal("Matched", result.DispositionName);
        Assert.Equal(
            OperationVerificationOutcomeDisposition.LocallyConsistent,
            Evaluate(required, result).Disposition);
    }

    [Fact]
    public void ExpectedUnsatisfiedReachabilityCanMatch()
    {
        var (required, result) = Reachability(
            "unsatisfied",
            OperationReachabilityObservationDisposition.Unsatisfied);

        Assert.Equal("Matched", result.DispositionName);
        Assert.Equal(
            OperationVerificationOutcomeDisposition.LocallyConsistent,
            Evaluate(required, result).Disposition);
    }

    [Fact]
    public void DifferentReachabilityDestinationIsConflict()
    {
        var (required, result) = Reachability(
            "satisfied",
            OperationReachabilityObservationDisposition.Satisfied,
            requiredDestination: "192.0.2.10",
            resultDestination: "192.0.2.11");

        Assert.Equal(
            OperationVerificationOutcomeDisposition.Conflict,
            Evaluate(required, result).Disposition);
    }

    [Fact]
    public void DifferentReachabilityContextIsConflict()
    {
        var (required, result) = Reachability(
            "satisfied",
            OperationReachabilityObservationDisposition.Satisfied,
            requiredVrf: "blue",
            resultVrf: "red");

        Assert.Equal(
            OperationVerificationOutcomeDisposition.Conflict,
            Evaluate(required, result).Disposition);
    }

    [Fact]
    public void ExpectedHttp404CanMatch()
    {
        var (required, result) = Service(status: 404);

        Assert.Equal("Matched", result.DispositionName);
        Assert.Equal(
            OperationVerificationOutcomeDisposition.LocallyConsistent,
            Evaluate(required, result).Disposition);
    }

    [Fact]
    public void DifferentServiceMethodIsConflict()
    {
        var (required, result) = Service(
            requiredMethod: "GET", resultMethod: "HEAD");

        Assert.Equal(
            OperationVerificationOutcomeDisposition.Conflict,
            Evaluate(required, result).Disposition);
    }

    [Fact]
    public void UnsupportedServiceProtocolRemainsUnresolved()
    {
        var (required, result) = Service(
            requiredProtocol: "unknown.protocol",
            resultProtocol: "unknown.protocol");

        Assert.Equal("UnsupportedProtocol", result.DispositionName);
        Assert.Equal(
            OperationVerificationOutcomeDisposition.Unresolved,
            Evaluate(required, result).Disposition);
    }

    [Fact]
    public void MissingRequirementTakesPrecedenceOverMismatch()
    {
        var (healthRequired, healthResult) = Health(
            OperationHealthObservationDisposition.Observed,
            OperationHealthStatus.Degraded);

        var (stateRequired, _) = State(
            OperationRediscoveryDisposition.Observed, "running");

        Assert.Equal(
            OperationVerificationOutcomeDisposition.Incomplete,
            Evaluate(
                new[] { healthRequired, stateRequired },
                new[] { healthResult }).Disposition);
    }

    [Fact]
    public void DuplicateResultTakesPrecedenceOverMissingRequirement()
    {
        var (stateRequired, stateResult) = State(
            OperationRediscoveryDisposition.Observed, "running");

        var (healthRequired, _) = Health(
            OperationHealthObservationDisposition.Observed,
            OperationHealthStatus.Healthy);

        Assert.Equal(
            OperationVerificationOutcomeDisposition.Conflict,
            Evaluate(
                new[] { stateRequired, healthRequired },
                new[] { stateResult, stateResult }).Disposition);
    }
}
