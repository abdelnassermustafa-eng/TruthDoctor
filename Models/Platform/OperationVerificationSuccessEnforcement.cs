using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Provider-neutral, read-only enforcement of conservative
/// verification-outcome classifications.
///
/// This evaluator does not execute operations, authenticate evidence,
/// validate temporal freshness, or certify provider resource state.
/// Even locally consistent verification is not execution success.
/// </summary>
public sealed class OperationVerificationSuccessEnforcement
{
    private OperationVerificationSuccessEnforcement()
    {
    }

    public static OperationVerificationEnforcementDecision Evaluate(
        OperationVerificationOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);

        return Classify(outcome);
    }

    public static OperationVerificationEnforcementDecision Evaluate(
        OperationVerificationOutcome outcome,
        OperationVerificationEvidenceSet evidenceSet)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        ArgumentNullException.ThrowIfNull(evidenceSet);

        ValidateEvidence(outcome, evidenceSet);

        return Classify(outcome);
    }

    public static OperationVerificationEnforcementDecision Evaluate(
        OperationVerificationOutcome outcome,
        OperationVerificationEvidenceSet evidenceSet,
        OperationProviderResponseEvidence providerResponse)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        ArgumentNullException.ThrowIfNull(evidenceSet);
        ArgumentNullException.ThrowIfNull(providerResponse);

        ValidateEvidence(outcome, evidenceSet);

        if (providerResponse.Operation != outcome.Operation)
        {
            throw new ArgumentException(
                "Provider response belongs to a different operation.",
                nameof(providerResponse));
        }

        // Provider acknowledgement is context, not resource-state proof.
        return Classify(outcome);
    }

    private static void ValidateEvidence(
        OperationVerificationOutcome outcome,
        OperationVerificationEvidenceSet evidenceSet)
    {
        if (evidenceSet.Operation != outcome.Operation)
        {
            throw new ArgumentException(
                "Evidence belongs to a different operation.",
                nameof(evidenceSet));
        }

        if (!outcome.Scope.Matches(evidenceSet.Scope.Target))
        {
            throw new ArgumentException(
                "Evidence belongs to a different resource scope.",
                nameof(evidenceSet));
        }

        foreach (var record in evidenceSet.Records)
        {
            bool found = false;

            foreach (var result in outcome.Results)
            {
                if (ReferenceEquals(record.Result, result))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                throw new ArgumentException(
                    "Evidence references a result not present " +
                    "in the evaluated verification outcome.",
                    nameof(evidenceSet));
            }
        }
    }

    private static OperationVerificationEnforcementDecision Classify(
        OperationVerificationOutcome outcome)
    {
        var disposition = outcome.Disposition switch
        {
            OperationVerificationOutcomeDisposition.NoRequirements =>
                OperationVerificationEnforcementDisposition.NotEstablished,

            OperationVerificationOutcomeDisposition.Incomplete =>
                OperationVerificationEnforcementDisposition.NotEstablished,

            OperationVerificationOutcomeDisposition.Unresolved =>
                OperationVerificationEnforcementDisposition.NotEstablished,

            OperationVerificationOutcomeDisposition.Inconsistent =>
                OperationVerificationEnforcementDisposition.Blocked,

            OperationVerificationOutcomeDisposition.Conflict =>
                OperationVerificationEnforcementDisposition.Blocked,

            OperationVerificationOutcomeDisposition.LocallyConsistent =>
                OperationVerificationEnforcementDisposition.LocallyConsistentOnly,

            _ => throw new ArgumentOutOfRangeException(
                nameof(outcome),
                "Unknown verification outcome disposition.")
        };

        return OperationVerificationEnforcementDecision.Create(disposition);
    }
}
