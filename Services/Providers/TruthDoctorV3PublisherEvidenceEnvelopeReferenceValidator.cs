using System;
using System.Collections.Generic;

namespace TruthDoctor.Services.Providers;

/// <summary>
/// Validates synthetic publisher evidence-reference
/// envelope STRUCTURE ONLY.
///
/// A successfully validated structure does not
/// authenticate the referenced evidence.
///
/// This class does not establish publisher identity,
/// release authority, trusted-root provenance,
/// signature validity, revocation state, freshness,
/// or anti-rollback protection.
///
/// It must never be used as an installation gate.
/// </summary>
public sealed class TruthDoctorV3PublisherEvidenceEnvelopeReferenceValidator
{
    public const int RequiredFieldCount = 20;
    public const int Sha256HexLength = 64;

    private static readonly string[] RequiredFieldNames =
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

    /// <summary>
    /// Structural completeness only.
    ///
    /// Returns true only when the exact required field
    /// set is supplied and every reference consists of
    /// 64 uppercase ASCII hexadecimal characters.
    ///
    /// Missing, additional, malformed or duplicate
    /// field names fail closed.
    ///
    /// Caller-owned data is neither changed nor retained.
    /// </summary>
    public bool ValidateReferenceStructure(
        IReadOnlyDictionary<string, string> evidenceReferences)
    {
        ArgumentNullException.ThrowIfNull(evidenceReferences);

        if (evidenceReferences.Count != RequiredFieldCount)
        {
            return false;
        }

        var remaining = new HashSet<string>(
            RequiredFieldNames,
            StringComparer.Ordinal);

        if (remaining.Count != RequiredFieldCount)
        {
            throw new InvalidOperationException(
                "The evidence reference field contract is invalid.");
        }

        foreach (
            KeyValuePair<string, string> field
            in evidenceReferences)
        {
            if (field.Key is null ||
                !remaining.Remove(field.Key))
            {
                return false;
            }

            string? reference = field.Value;

            if (reference is null ||
                reference.Length != Sha256HexLength)
            {
                return false;
            }

            foreach (char character in reference)
            {
                bool digit =
                    character >= '0' && character <= '9';

                bool uppercaseHex =
                    character >= 'A' && character <= 'F';

                if (!digit && !uppercaseHex)
                {
                    return false;
                }
            }
        }

        // This result describes envelope structure,
        // not the authenticity of any referenced object.
        return remaining.Count == 0;
    }
}
