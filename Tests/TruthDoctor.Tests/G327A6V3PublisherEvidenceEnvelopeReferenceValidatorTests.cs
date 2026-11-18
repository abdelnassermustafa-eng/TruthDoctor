using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TruthDoctor.Services.Providers;
using Xunit;

namespace TruthDoctor.Tests;

/// <summary>
/// Synthetic evidence-reference envelope structure only.
/// Digests are fabricated test identifiers, NOT evidence
/// authentication, publisher trust or release authority.
/// </summary>
public sealed class G327A6V3PublisherEvidenceEnvelopeReferenceValidatorTests
{
    private static readonly string[] RequiredFields =
    {
        "bootstrap_distribution_authenticated",
        "bootstrap_root_integrity_verified",
        "bootstrap_root_identity_pinned",
        "root_metadata_signature_verified",
        "root_metadata_version_monotonic",
        "root_rotation_threshold_verified",
        "root_revocation_state_authenticated",
        "delegation_chain_cryptographically_verified",
        "delegation_scope_constrained",
        "publisher_identity_authenticated",
        "publisher_key_bound_to_identity",
        "publisher_spki_fingerprint_bound",
        "publisher_key_identifier_bound",
        "publisher_key_active",
        "publisher_key_not_revoked",
        "signing_role_authorized",
        "product_scope_authorized",
        "target_and_format_scope_authorized",
        "authenticated_freshness_evidence",
        "durable_rollback_state_verified",
    };

    public static IEnumerable<object[]> ReferenceCases()
    {
        yield return new object[] { "complete-simulated-evidence", Array.Empty<string>(), true };
        yield return new object[] { "deny-bootstrap-distribution-authenticated", new string[] { "bootstrap_distribution_authenticated" }, false };
        yield return new object[] { "deny-bootstrap-root-integrity-verified", new string[] { "bootstrap_root_integrity_verified" }, false };
        yield return new object[] { "deny-bootstrap-root-identity-pinned", new string[] { "bootstrap_root_identity_pinned" }, false };
        yield return new object[] { "deny-root-metadata-signature-verified", new string[] { "root_metadata_signature_verified" }, false };
        yield return new object[] { "deny-root-metadata-version-monotonic", new string[] { "root_metadata_version_monotonic" }, false };
        yield return new object[] { "deny-root-rotation-threshold-verified", new string[] { "root_rotation_threshold_verified" }, false };
        yield return new object[] { "deny-root-revocation-state-authenticated", new string[] { "root_revocation_state_authenticated" }, false };
        yield return new object[] { "deny-delegation-chain-cryptographically-verified", new string[] { "delegation_chain_cryptographically_verified" }, false };
        yield return new object[] { "deny-delegation-scope-constrained", new string[] { "delegation_scope_constrained" }, false };
        yield return new object[] { "deny-publisher-identity-authenticated", new string[] { "publisher_identity_authenticated" }, false };
        yield return new object[] { "deny-publisher-key-bound-to-identity", new string[] { "publisher_key_bound_to_identity" }, false };
        yield return new object[] { "deny-publisher-spki-fingerprint-bound", new string[] { "publisher_spki_fingerprint_bound" }, false };
        yield return new object[] { "deny-publisher-key-identifier-bound", new string[] { "publisher_key_identifier_bound" }, false };
        yield return new object[] { "deny-publisher-key-active", new string[] { "publisher_key_active" }, false };
        yield return new object[] { "deny-publisher-key-not-revoked", new string[] { "publisher_key_not_revoked" }, false };
        yield return new object[] { "deny-signing-role-authorized", new string[] { "signing_role_authorized" }, false };
        yield return new object[] { "deny-product-scope-authorized", new string[] { "product_scope_authorized" }, false };
        yield return new object[] { "deny-target-and-format-scope-authorized", new string[] { "target_and_format_scope_authorized" }, false };
        yield return new object[] { "deny-authenticated-freshness-evidence", new string[] { "authenticated_freshness_evidence" }, false };
        yield return new object[] { "deny-durable-rollback-state-verified", new string[] { "durable_rollback_state_verified" }, false };
        yield return new object[] { "attacker-controlled-key-and-root", new string[] { "bootstrap_root_identity_pinned", "publisher_key_bound_to_identity" }, false };
        yield return new object[] { "valid-signature-out-of-scope", new string[] { "delegation_scope_constrained", "product_scope_authorized" }, false };
        yield return new object[] { "revoked-delegated-key", new string[] { "root_revocation_state_authenticated", "publisher_key_not_revoked" }, false };
        yield return new object[] { "replayed-root-and-stale-evidence", new string[] { "root_metadata_version_monotonic", "authenticated_freshness_evidence", "durable_rollback_state_verified" }, false };
    }

    private static Dictionary<string, string> ReferenceEnvelope()
    {
        // Domain-separated, deterministic, synthetic tokens.
        // No real trust metadata, signature or evidence is used.
        return RequiredFields.ToDictionary(
            key => key,
            key => Convert.ToHexString(
                SHA256.HashData(
                    Encoding.UTF8.GetBytes(
                        "TRUTHDOCTOR-SYNTHETIC-EVIDENCE-ONLY:" + key))),
            StringComparer.Ordinal);
    }

    [Theory]
    [MemberData(nameof(ReferenceCases))]
    public void EvidenceReferenceMatrixMatchesStructure(
        string scenario,
        string[] missingFields,
        bool expectedComplete)
    {
        Assert.False(string.IsNullOrWhiteSpace(scenario));
        var fields = ReferenceEnvelope();
        foreach (string name in missingFields)
        {
            Assert.True(fields.Remove(name));
        }

        bool observed =
            new TruthDoctorV3PublisherEvidenceEnvelopeReferenceValidator()
                .ValidateReferenceStructure(fields);

        Assert.Equal(expectedComplete, observed);
    }

    [Fact]
    public void NullEvidenceEnvelopeIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new TruthDoctorV3PublisherEvidenceEnvelopeReferenceValidator()
                .ValidateReferenceStructure(null!));
    }

    [Fact]
    public void UnknownEvidenceFieldFailsClosed()
    {
        var fields = ReferenceEnvelope();
        fields["unapproved_evidence_source"] =
            new string('A', 64);
        Assert.False(
            new TruthDoctorV3PublisherEvidenceEnvelopeReferenceValidator()
                .ValidateReferenceStructure(fields));
    }

    [Fact]
    public void ShortEvidenceDigestFailsClosed()
    {
        var fields = ReferenceEnvelope();
        fields["publisher_spki_fingerprint_bound"] =
            new string('A', 63);
        Assert.False(
            new TruthDoctorV3PublisherEvidenceEnvelopeReferenceValidator()
                .ValidateReferenceStructure(fields));
    }

    [Fact]
    public void NonHexEvidenceDigestFailsClosed()
    {
        var fields = ReferenceEnvelope();
        fields["publisher_spki_fingerprint_bound"] =
            new string('Z', 64);
        Assert.False(
            new TruthDoctorV3PublisherEvidenceEnvelopeReferenceValidator()
                .ValidateReferenceStructure(fields));
    }

    [Fact]
    public void LowercaseEvidenceDigestFailsClosed()
    {
        var fields = ReferenceEnvelope();
        fields["publisher_spki_fingerprint_bound"] =
            new string('a', 64);
        Assert.False(
            new TruthDoctorV3PublisherEvidenceEnvelopeReferenceValidator()
                .ValidateReferenceStructure(fields));
    }

    [Fact]
    public void StructuralCheckPreservesCallerEvidence()
    {
        var fields = ReferenceEnvelope();
        var before = new Dictionary<string, string>(
            fields, StringComparer.Ordinal);

        Assert.True(
            new TruthDoctorV3PublisherEvidenceEnvelopeReferenceValidator()
                .ValidateReferenceStructure(fields));

        Assert.Equal(before.Count, fields.Count);
        foreach (var entry in before)
        {
            Assert.True(fields.TryGetValue(
                entry.Key, out string? actual));
            Assert.Equal(entry.Value, actual);
        }
    }
}
