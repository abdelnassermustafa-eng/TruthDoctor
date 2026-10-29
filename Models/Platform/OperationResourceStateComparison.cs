using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable classification of an expected resource-state condition
/// against one caller-supplied rediscovery observation.
///
/// Evaluation does not contact a provider, authenticate observation
/// provenance, prove post-execution timing, or verify operation success.
/// Only resource.state is supported by the current observation contract.
/// </summary>
public sealed class OperationResourceStateComparison
{
    public OperationOutcomeCondition Condition { get; }

    public OperationResourceScope Scope { get; }

    public OperationRediscoveryObservation Observation { get; }

    public OperationResourceStateComparisonDisposition Disposition { get; }

    private OperationResourceStateComparison(
        OperationOutcomeCondition condition,
        OperationResourceScope scope,
        OperationRediscoveryObservation observation,
        OperationResourceStateComparisonDisposition disposition)
    {
        Condition = condition;
        Scope = scope;
        Observation = observation;
        Disposition = disposition;
    }

    /// <summary>
    /// Classifies a local comparison without asserting a verified result.
    /// Exact target identity is checked before condition or state.
    /// </summary>
    public static OperationResourceStateComparison Evaluate(
        OperationOutcomeCondition condition,
        OperationResourceScope scope,
        OperationRediscoveryObservation observation)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(observation);

        OperationResourceStateComparisonDisposition disposition;

        if (!scope.Matches(observation.Target))
        {
            disposition =
                OperationResourceStateComparisonDisposition.TargetMismatch;
        }
        else if (!string.Equals(
            condition.Key,
            "resource.state",
            StringComparison.OrdinalIgnoreCase))
        {
            disposition =
                OperationResourceStateComparisonDisposition.UnsupportedCondition;
        }
        else
        {
            disposition = observation.Disposition switch
            {
                OperationRediscoveryDisposition.Absent =>
                    OperationResourceStateComparisonDisposition.Absent,

                OperationRediscoveryDisposition.Unavailable =>
                    OperationResourceStateComparisonDisposition.Unavailable,

                OperationRediscoveryDisposition.Unknown =>
                    OperationResourceStateComparisonDisposition.NoObservation,

                OperationRediscoveryDisposition.Observed =>
                    string.Equals(
                        condition.ExpectedValue,
                        observation.ObservedState,
                        StringComparison.Ordinal)
                        ? OperationResourceStateComparisonDisposition.Matched
                        : OperationResourceStateComparisonDisposition.Mismatched,

                _ => OperationResourceStateComparisonDisposition.Unknown
            };
        }

        return new OperationResourceStateComparison(
            condition,
            scope,
            observation,
            disposition);
    }
}
