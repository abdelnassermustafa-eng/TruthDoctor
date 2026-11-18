using System;
using System.Collections.Generic;

namespace TruthDoctor.Services.Providers;

/// <summary>
/// Deterministic evaluation of synthetic publisher
/// scope-policy Boolean predicates.
///
/// This component does not authenticate its inputs,
/// discover trusted roots, validate publisher identities,
/// or authorize a software release.
///
/// True means only that the caller supplied exactly
/// the required predicate set and every value was true.
///
/// Never interpret this result as permission to
/// install, load, execute or trust a provider.
/// </summary>
public sealed class TruthDoctorV3PublisherScopePolicyReferenceEvaluator
{
    private static readonly string[] RequiredPredicateNames =
    {
        "signature_valid",
        "artifact_digest_matches",
        "root_authenticated",
        "root_active",
        "root_not_revoked",
        "root_version_accepted",
        "publisher_key_bound",
        "publisher_key_active",
        "publisher_key_not_revoked",
        "publisher_id_matches",
        "publisher_scope_matches",
        "product_matches",
        "runtime_target_matches",
        "artifact_format_matches",
        "release_channel_matches",
        "release_role_allowed",
        "metadata_schema_supported",
        "algorithm_allowed",
        "threshold_met",
        "verified_time_available",
        "within_validity_period",
        "within_metadata_expiry",
        "revocation_evidence_fresh",
        "rollback_state_available",
        "release_sequence_advanced",
        "rollback_state_durable",
        "policy_state_complete",
        "key_id_bound_to_spki"
    };

    public const int RequiredPredicateCount = 28;

    /// <summary>
    /// Evaluates reference eligibility only.
    ///
    /// Null inputs throw ArgumentNullException.
    /// Missing, unknown, duplicate or false
    /// predicates return false.
    ///
    /// The caller's dictionary is never modified
    /// or retained by this evaluator.
    /// </summary>
    public bool EvaluateReferenceEligibility(
        IReadOnlyDictionary<string, bool> predicates)
    {
        ArgumentNullException.ThrowIfNull(predicates);

        if (predicates.Count != RequiredPredicateCount)
        {
            return false;
        }

        var remaining = new HashSet<string>(
            RequiredPredicateNames,
            StringComparer.Ordinal);

        if (remaining.Count != RequiredPredicateCount)
        {
            throw new InvalidOperationException(
                "The reference predicate contract is invalid.");
        }

        foreach (KeyValuePair<string, bool> predicate in predicates)
        {
            // Exact, case-sensitive identity is required.
            //
            // An unrecognized, repeated or missing
            // predicate must never be treated as true.
            if (predicate.Key is null ||
                !remaining.Remove(predicate.Key))
            {
                return false;
            }

            if (!predicate.Value)
            {
                return false;
            }
        }

        // An empty remainder means every required key
        // appeared exactly once and was supplied as true.
        //
        // This is a synthetic reference result only.
        // It is not independently authenticated evidence.
        return remaining.Count == 0;
    }
}
