using System;
using System.Collections.Generic;
using System.Linq;
using TruthDoctor.Services.Providers;
using Xunit;

namespace TruthDoctor.Tests;

/// <summary>
/// Tests synthetic scope-policy predicate evaluation only.
///
/// No supplied Boolean is authenticated evidence.
/// A true result is NOT authorization of a publisher,
/// release, installer, or executable provider.
/// </summary>
public sealed class G327A6V3PublisherScopePolicyReferenceEvaluatorTests
{
    private static readonly string[] RequiredKeys =
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
        "key_id_bound_to_spki",
    };

    public static IEnumerable<object[]> ReferenceCases()
    {
        yield return new object[] { "all-simulated-predicates-satisfied", Array.Empty<string>(), true };
        yield return new object[] { "deny-signature-valid", new string[] { "signature_valid" }, false };
        yield return new object[] { "deny-artifact-digest-matches", new string[] { "artifact_digest_matches" }, false };
        yield return new object[] { "deny-root-authenticated", new string[] { "root_authenticated" }, false };
        yield return new object[] { "deny-root-active", new string[] { "root_active" }, false };
        yield return new object[] { "deny-root-not-revoked", new string[] { "root_not_revoked" }, false };
        yield return new object[] { "deny-root-version-accepted", new string[] { "root_version_accepted" }, false };
        yield return new object[] { "deny-publisher-key-bound", new string[] { "publisher_key_bound" }, false };
        yield return new object[] { "deny-publisher-key-active", new string[] { "publisher_key_active" }, false };
        yield return new object[] { "deny-publisher-key-not-revoked", new string[] { "publisher_key_not_revoked" }, false };
        yield return new object[] { "deny-publisher-id-matches", new string[] { "publisher_id_matches" }, false };
        yield return new object[] { "deny-publisher-scope-matches", new string[] { "publisher_scope_matches" }, false };
        yield return new object[] { "deny-product-matches", new string[] { "product_matches" }, false };
        yield return new object[] { "deny-runtime-target-matches", new string[] { "runtime_target_matches" }, false };
        yield return new object[] { "deny-artifact-format-matches", new string[] { "artifact_format_matches" }, false };
        yield return new object[] { "deny-release-channel-matches", new string[] { "release_channel_matches" }, false };
        yield return new object[] { "deny-release-role-allowed", new string[] { "release_role_allowed" }, false };
        yield return new object[] { "deny-metadata-schema-supported", new string[] { "metadata_schema_supported" }, false };
        yield return new object[] { "deny-algorithm-allowed", new string[] { "algorithm_allowed" }, false };
        yield return new object[] { "deny-threshold-met", new string[] { "threshold_met" }, false };
        yield return new object[] { "deny-verified-time-available", new string[] { "verified_time_available" }, false };
        yield return new object[] { "deny-within-validity-period", new string[] { "within_validity_period" }, false };
        yield return new object[] { "deny-within-metadata-expiry", new string[] { "within_metadata_expiry" }, false };
        yield return new object[] { "deny-revocation-evidence-fresh", new string[] { "revocation_evidence_fresh" }, false };
        yield return new object[] { "deny-rollback-state-available", new string[] { "rollback_state_available" }, false };
        yield return new object[] { "deny-release-sequence-advanced", new string[] { "release_sequence_advanced" }, false };
        yield return new object[] { "deny-rollback-state-durable", new string[] { "rollback_state_durable" }, false };
        yield return new object[] { "deny-policy-state-complete", new string[] { "policy_state_complete" }, false };
        yield return new object[] { "deny-key-id-bound-to-spki", new string[] { "key_id_bound_to_spki" }, false };
        yield return new object[] { "valid-signature-untrusted-root", new string[] { "root_authenticated", "publisher_key_bound" }, false };
        yield return new object[] { "replayed-release-with-stale-revocation", new string[] { "revocation_evidence_fresh", "release_sequence_advanced" }, false };
        yield return new object[] { "valid-signature-mismatched-artifact-and-scope", new string[] { "artifact_digest_matches", "publisher_scope_matches" }, false };
    }

    private static Dictionary<string, bool> Baseline()
    {
        return RequiredKeys.ToDictionary(
            key => key,
            _ => true,
            StringComparer.Ordinal);
    }

    [Theory]
    [MemberData(nameof(ReferenceCases))]
    public void ReferenceMatrixMatchesStrictEligibility(
        string caseName,
        string[] disabledPredicates,
        bool expectedEligible)
    {
        Assert.False(string.IsNullOrWhiteSpace(caseName));

        var inputs = Baseline();

        foreach (string key in disabledPredicates)
        {
            Assert.True(inputs.ContainsKey(key));
            inputs[key] = false;
        }

        bool result =
            new TruthDoctorV3PublisherScopePolicyReferenceEvaluator()
                .EvaluateReferenceEligibility(inputs);

        Assert.Equal(expectedEligible, result);
    }

    [Fact]
    public void NullPolicyInputIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new TruthDoctorV3PublisherScopePolicyReferenceEvaluator()
                .EvaluateReferenceEligibility(null!));
    }

    [Fact]
    public void MissingRequiredPredicateFailsClosed()
    {
        var inputs = Baseline();

        Assert.True(inputs.Remove("root_authenticated"));

        Assert.False(
            new TruthDoctorV3PublisherScopePolicyReferenceEvaluator()
                .EvaluateReferenceEligibility(inputs));
    }

    [Fact]
    public void UnknownPredicateFailsClosed()
    {
        var inputs = Baseline();

        inputs["unrecognized_policy_authority"] = true;

        Assert.False(
            new TruthDoctorV3PublisherScopePolicyReferenceEvaluator()
                .EvaluateReferenceEligibility(inputs));
    }

    [Fact]
    public void ReferenceEvaluationPreservesCallerInputs()
    {
        var inputs = Baseline();

        inputs["publisher_key_not_revoked"] = false;

        var before = new Dictionary<string, bool>(
            inputs,
            StringComparer.Ordinal);

        Assert.False(
            new TruthDoctorV3PublisherScopePolicyReferenceEvaluator()
                .EvaluateReferenceEligibility(inputs));

        Assert.Equal(before.Count, inputs.Count);

        foreach (var entry in before)
        {
            Assert.True(inputs.TryGetValue(
                entry.Key, out bool actual));

            Assert.Equal(entry.Value, actual);
        }
    }
}
