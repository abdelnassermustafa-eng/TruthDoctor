using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable local comparison of an expected resource-health condition
/// against one caller-supplied health observation.
///
/// Does not query a provider, establish observation provenance or timing,
/// perform reachability checks, or verify operation execution success.
/// Only resource.health and its three explicit values are supported.
/// </summary>
public sealed class OperationHealthComparison
{
    public OperationOutcomeCondition Condition { get; }

    public OperationResourceScope Scope { get; }

    public OperationHealthObservation Observation { get; }

    public OperationHealthComparisonDisposition Disposition { get; }

    private OperationHealthComparison(
        OperationOutcomeCondition condition,
        OperationResourceScope scope,
        OperationHealthObservation observation,
        OperationHealthComparisonDisposition disposition)
    {
        Condition = condition;
        Scope = scope;
        Observation = observation;
        Disposition = disposition;
    }

    public static OperationHealthComparison Evaluate(
        OperationOutcomeCondition condition,
        OperationResourceScope scope,
        OperationHealthObservation observation)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(observation);

        OperationHealthComparisonDisposition disposition;

        if (!scope.Matches(observation.Target))
        {
            disposition =
                OperationHealthComparisonDisposition.TargetMismatch;
        }
        else if (!string.Equals(
            condition.Key,
            "resource.health",
            StringComparison.OrdinalIgnoreCase) ||
            !TryParseExpectedHealth(
                condition.ExpectedValue,
                out var expectedHealth))
        {
            disposition =
                OperationHealthComparisonDisposition.UnsupportedCondition;
        }
        else
        {
            disposition = observation.Disposition switch
            {
                OperationHealthObservationDisposition.Unknown =>
                    OperationHealthComparisonDisposition.NoObservation,

                OperationHealthObservationDisposition.Unavailable =>
                    OperationHealthComparisonDisposition.Unavailable,

                OperationHealthObservationDisposition.NotAssessed =>
                    OperationHealthComparisonDisposition.NotAssessed,

                OperationHealthObservationDisposition.Observed =>
                    observation.Health == expectedHealth
                        ? OperationHealthComparisonDisposition.Matched
                        : OperationHealthComparisonDisposition.Mismatched,

                _ => OperationHealthComparisonDisposition.Unknown
            };
        }

        return new OperationHealthComparison(
            condition,
            scope,
            observation,
            disposition);
    }

    private static bool TryParseExpectedHealth(
        string value,
        out OperationHealthStatus health)
    {
        if (string.Equals(
            value, "healthy", StringComparison.OrdinalIgnoreCase))
        {
            health = OperationHealthStatus.Healthy;
            return true;
        }

        if (string.Equals(
            value, "degraded", StringComparison.OrdinalIgnoreCase))
        {
            health = OperationHealthStatus.Degraded;
            return true;
        }

        if (string.Equals(
            value, "unhealthy", StringComparison.OrdinalIgnoreCase))
        {
            health = OperationHealthStatus.Unhealthy;
            return true;
        }

        health = OperationHealthStatus.Unknown;
        return false;
    }
}
