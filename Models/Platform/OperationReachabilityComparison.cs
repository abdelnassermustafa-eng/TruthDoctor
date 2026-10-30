using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable local comparison between a declared network probe
/// expectation and one caller-supplied observation.
///
/// Supports network.probe with satisfied/unsatisfied expectations,
/// and only icmp.echo / tcp.connect check mechanisms.
///
/// Does not probe networks, contact providers, authenticate
/// observations, verify temporal provenance, or assert success
/// of an operation.
/// </summary>
public sealed class OperationReachabilityComparison
{
    public OperationOutcomeCondition Condition { get; }

    public OperationResourceScope SourceScope { get; }

    public OperationReachabilityCheckSpecification Specification
    {
        get;
    }

    public OperationReachabilityObservation Observation { get; }

    public OperationReachabilityComparisonDisposition Disposition
    {
        get;
    }

    private OperationReachabilityComparison(
        OperationOutcomeCondition condition,
        OperationResourceScope sourceScope,
        OperationReachabilityCheckSpecification specification,
        OperationReachabilityObservation observation,
        OperationReachabilityComparisonDisposition disposition)
    {
        Condition = condition;
        SourceScope = sourceScope;
        Specification = specification;
        Observation = observation;
        Disposition = disposition;
    }

    public static OperationReachabilityComparison Evaluate(
        OperationOutcomeCondition condition,
        OperationResourceScope sourceScope,
        OperationReachabilityCheckSpecification specification,
        OperationReachabilityObservation observation)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(sourceScope);
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(observation);

        OperationReachabilityComparisonDisposition disposition;

        if (!sourceScope.Matches(observation.Source))
        {
            disposition =
                OperationReachabilityComparisonDisposition.SourceMismatch;
        }
        else if (!SpecificationsMatch(
            specification,
            observation.Specification))
        {
            disposition =
                OperationReachabilityComparisonDisposition.SpecificationMismatch;
        }
        else if (!string.Equals(
            condition.Key,
            "network.probe",
            StringComparison.OrdinalIgnoreCase) ||
            !TryExpectedSatisfied(
                condition.ExpectedValue,
                out var expectedSatisfied))
        {
            disposition =
                OperationReachabilityComparisonDisposition.UnsupportedCondition;
        }
        else if (!IsSupportedProtocol(
            specification.Protocol.Identifier))
        {
            disposition =
                OperationReachabilityComparisonDisposition.UnsupportedProtocol;
        }
        else
        {
            disposition = observation.Disposition switch
            {
                OperationReachabilityObservationDisposition.Unknown =>
                    OperationReachabilityComparisonDisposition.NoObservation,

                OperationReachabilityObservationDisposition.Unavailable =>
                    OperationReachabilityComparisonDisposition.Unavailable,

                OperationReachabilityObservationDisposition.NotAssessed =>
                    OperationReachabilityComparisonDisposition.NotAssessed,

                OperationReachabilityObservationDisposition.UnsupportedCapability =>
                    OperationReachabilityComparisonDisposition.UnsupportedCapability,

                OperationReachabilityObservationDisposition.Satisfied =>
                    expectedSatisfied
                        ? OperationReachabilityComparisonDisposition.Matched
                        : OperationReachabilityComparisonDisposition.Mismatched,

                OperationReachabilityObservationDisposition.Unsatisfied =>
                    !expectedSatisfied
                        ? OperationReachabilityComparisonDisposition.Matched
                        : OperationReachabilityComparisonDisposition.Mismatched,

                _ => OperationReachabilityComparisonDisposition.Unknown
            };
        }

        return new OperationReachabilityComparison(
            condition,
            sourceScope,
            specification,
            observation,
            disposition);
    }

    private static bool SpecificationsMatch(
        OperationReachabilityCheckSpecification expected,
        OperationReachabilityCheckSpecification observed)
    {
        return
            string.Equals(
                expected.Destination,
                observed.Destination,
                StringComparison.Ordinal) &&
            string.Equals(
                expected.Protocol.Identifier,
                observed.Protocol.Identifier,
                StringComparison.Ordinal) &&
            expected.DestinationPort == observed.DestinationPort &&
            ContextsMatch(expected.Context, observed.Context);
    }

    private static bool ContextsMatch(
        OperationReachabilityContext expected,
        OperationReachabilityContext observed)
    {
        return
            string.Equals(
                expected.InterfaceId,
                observed.InterfaceId,
                StringComparison.Ordinal) &&
            string.Equals(
                expected.Vrf,
                observed.Vrf,
                StringComparison.Ordinal) &&
            expected.VlanId == observed.VlanId &&
            string.Equals(
                expected.AddressFamily,
                observed.AddressFamily,
                StringComparison.Ordinal);
    }

    private static bool IsSupportedProtocol(string identifier)
    {
        return
            string.Equals(
                identifier,
                "icmp.echo",
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                identifier,
                "tcp.connect",
                StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryExpectedSatisfied(
        string value,
        out bool expectedSatisfied)
    {
        if (string.Equals(
            value,
            "satisfied",
            StringComparison.OrdinalIgnoreCase))
        {
            expectedSatisfied = true;
            return true;
        }

        if (string.Equals(
            value,
            "unsatisfied",
            StringComparison.OrdinalIgnoreCase))
        {
            expectedSatisfied = false;
            return true;
        }

        expectedSatisfied = false;
        return false;
    }
}
