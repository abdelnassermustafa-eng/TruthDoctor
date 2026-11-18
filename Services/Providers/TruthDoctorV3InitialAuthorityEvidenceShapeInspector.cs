using System;
using System.Collections.Generic;

namespace TruthDoctor.Services.Providers;

/// <summary>
/// Non-authorizing structural reference inspector for
/// synthetic initial-authority evidence.
///
/// DENY indicates that the expected reference shape or
/// scope is incomplete or inconsistent.
///
/// REVIEW_ONLY indicates structural suitability for
/// additional independent checks. It does not establish
/// publisher authenticity, root trust, signed-artifact
/// validity, enrollment eligibility or provider rights.
///
/// This inspector performs no network, cryptographic,
/// file-system or protected-state operations.
/// </summary>
public static class TruthDoctorV3InitialAuthorityEvidenceShapeInspector
{
    private const string SupportedSchemaVersion = "1";

    private static readonly string[] RequiredFields =
    {
        "authority_origin",
        "publisher_identity",
        "artifact_identity",
        "root_fingerprint",
        "schema_version",
        "product_identity",
        "environment_identity"
    };

    /// <summary>
    /// Inspects only field presence, bounded reference values,
    /// schema version and exact product/environment binding.
    ///
    /// A REVIEW_ONLY result MUST NOT be used as an
    /// authentication, enrollment or installation decision.
    /// </summary>
    public static string Inspect(
        IReadOnlyDictionary<string, string>? evidence,
        string product,
        string environment)
    {
        if (evidence is null)
        {
            return "DENY";
        }

        if (string.IsNullOrWhiteSpace(product)
            || string.IsNullOrWhiteSpace(environment))
        {
            return "DENY";
        }

        foreach (string field in RequiredFields)
        {
            if (!evidence.TryGetValue(field, out string? value))
            {
                return "DENY";
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                return "DENY";
            }

            // Provisional conservative shape limit.
            // This is not an approved production schema.
            if (value.Length > 256)
            {
                return "DENY";
            }
        }

        if (!string.Equals(
                evidence["schema_version"],
                SupportedSchemaVersion,
                StringComparison.Ordinal))
        {
            return "DENY";
        }

        if (!string.Equals(
                evidence["product_identity"],
                product,
                StringComparison.Ordinal))
        {
            return "DENY";
        }

        if (!string.Equals(
                evidence["environment_identity"],
                environment,
                StringComparison.Ordinal))
        {
            return "DENY";
        }

        // No authority verification has occurred.
        // This is not a trust or installation decision.
        return "REVIEW_ONLY";
    }
}
