using System;
using System.Globalization;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable local HTTP response-status comparison.
/// Does not execute HTTP, verify TLS, authenticate observations,
/// establish application health, or assert operation success.
/// </summary>
public sealed class OperationServiceComparison
{
    public OperationOutcomeCondition Condition { get; }

    public OperationResourceScope SourceScope { get; }

    public OperationServiceCheckSpecification Specification { get; }

    public OperationServiceObservation Observation { get; }

    public OperationServiceComparisonDisposition Disposition { get; }

    private OperationServiceComparison(
        OperationOutcomeCondition condition,
        OperationResourceScope sourceScope,
        OperationServiceCheckSpecification specification,
        OperationServiceObservation observation,
        OperationServiceComparisonDisposition disposition)
    {
        Condition = condition;
        SourceScope = sourceScope;
        Specification = specification;
        Observation = observation;
        Disposition = disposition;
    }

    public static OperationServiceComparison Evaluate(
        OperationOutcomeCondition condition,
        OperationResourceScope sourceScope,
        OperationServiceCheckSpecification specification,
        OperationServiceObservation observation)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(sourceScope);
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(observation);

        OperationServiceComparisonDisposition disposition;

        if (!sourceScope.Matches(observation.Source))
        {
            disposition =
                OperationServiceComparisonDisposition.SourceMismatch;
        }
        else if (!SpecificationsMatch(
            specification, observation.Specification))
        {
            disposition =
                OperationServiceComparisonDisposition.SpecificationMismatch;
        }
        else if (!TryExpectedStatus(
            condition, specification.Criteria.ExpectedHttpStatusCode))
        {
            disposition =
                OperationServiceComparisonDisposition.UnsupportedCondition;
        }
        else if (!string.Equals(
            specification.Protocol.Identifier,
            "http.response-status",
            StringComparison.OrdinalIgnoreCase))
        {
            disposition =
                OperationServiceComparisonDisposition.UnsupportedProtocol;
        }
        else
        {
            disposition = observation.Disposition switch
            {
                OperationServiceObservationDisposition.UnsupportedCapability =>
                    OperationServiceComparisonDisposition.UnsupportedCapability,

                OperationServiceObservationDisposition.Unknown =>
                    OperationServiceComparisonDisposition.NoObservation,

                OperationServiceObservationDisposition.Unavailable =>
                    OperationServiceComparisonDisposition.Unavailable,

                OperationServiceObservationDisposition.NotAssessed =>
                    OperationServiceComparisonDisposition.NotAssessed,

                OperationServiceObservationDisposition.Responded =>
                    observation.HttpStatusCode ==
                    specification.Criteria.ExpectedHttpStatusCode
                        ? OperationServiceComparisonDisposition.Matched
                        : OperationServiceComparisonDisposition.Mismatched,

                _ => OperationServiceComparisonDisposition.Unknown
            };
        }

        return new OperationServiceComparison(
            condition, sourceScope, specification,
            observation, disposition);
    }

    private static bool SpecificationsMatch(
        OperationServiceCheckSpecification expected,
        OperationServiceCheckSpecification observed)
    {
        return
            string.Equals(
                expected.Protocol.Identifier,
                observed.Protocol.Identifier,
                StringComparison.Ordinal) &&
            string.Equals(
                expected.Endpoint.Uri,
                observed.Endpoint.Uri,
                StringComparison.Ordinal) &&
            string.Equals(
                expected.Method,
                observed.Method,
                StringComparison.Ordinal) &&
            expected.Criteria.ExpectedHttpStatusCode ==
                observed.Criteria.ExpectedHttpStatusCode;
    }

    private static bool TryExpectedStatus(
        OperationOutcomeCondition condition,
        int declaredExpected)
    {
        if (!string.Equals(
            condition.Key,
            "service.http.status",
            StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!int.TryParse(
            condition.ExpectedValue,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out var parsed))
        {
            return false;
        }

        return parsed >= 100 &&
            parsed <= 599 &&
            parsed == declaredExpected &&
            string.Equals(
                condition.ExpectedValue,
                parsed.ToString(CultureInfo.InvariantCulture),
                StringComparison.Ordinal);
    }
}
