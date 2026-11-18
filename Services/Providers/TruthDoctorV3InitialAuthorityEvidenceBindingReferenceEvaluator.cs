using System;
using System.Collections.Generic;

namespace TruthDoctor.Services.Providers;

/// <summary>
/// Non-authorizing equality comparison between two
/// caller-supplied initial-authority evidence references.
///
/// Matching references are not proof of independent
/// authority, publisher authenticity, root authenticity,
/// signed artifact validity or enrollment permission.
///
/// DENY means the input shape or reference equality fails.
/// REVIEW_ONLY means both references have acceptable
/// structure and match at the specified fields.
///
/// This component does not authorize provider installation.
/// </summary>
public static class TruthDoctorV3InitialAuthorityEvidenceBindingReferenceEvaluator
{
    private static readonly string[] BindingFields =
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
    /// Performs offline structural consistency checks only.
    /// The expected record is not automatically trusted.
    /// </summary>
    public static string Evaluate(
        IReadOnlyDictionary<string, string>? claimed,
        IReadOnlyDictionary<string, string>? expected)
    {
        if (claimed is null || expected is null)
        {
            return "DENY";
        }

        if (!expected.TryGetValue(
                "product_identity", out string? product)
            || !expected.TryGetValue(
                "environment_identity", out string? environment)
            || string.IsNullOrWhiteSpace(product)
            || string.IsNullOrWhiteSpace(environment))
        {
            return "DENY";
        }

        // Validate both records against the same claimed
        // product/environment scope. This is not authority
        // verification or a trust-anchor lookup.
        if (!string.Equals(
                TruthDoctorV3InitialAuthorityEvidenceShapeInspector
                    .Inspect(expected, product, environment),
                "REVIEW_ONLY",
                StringComparison.Ordinal))
        {
            return "DENY";
        }

        if (!string.Equals(
                TruthDoctorV3InitialAuthorityEvidenceShapeInspector
                    .Inspect(claimed, product, environment),
                "REVIEW_ONLY",
                StringComparison.Ordinal))
        {
            return "DENY";
        }

        foreach (string field in BindingFields)
        {
            if (!expected.TryGetValue(field, out string? expectedValue)
                || !claimed.TryGetValue(field, out string? claimedValue))
            {
                return "DENY";
            }

            if (!string.Equals(
                    expectedValue,
                    claimedValue,
                    StringComparison.Ordinal))
            {
                return "DENY";
            }
        }

        // The references match structurally, but neither
        // record has been independently authenticated.
        return "REVIEW_ONLY";
    }
}
