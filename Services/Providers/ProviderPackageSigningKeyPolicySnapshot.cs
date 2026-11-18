using System;
using System.Collections.Generic;

namespace TruthDoctor.Services.Providers;

/// <summary>
/// Classification of a public-key fingerprint within
/// caller-supplied signing-key policy data.
///
/// These values do not establish authenticated publisher
/// identity or authorization to install or execute packages.
/// </summary>
public enum ProviderPackageSigningKeyPolicyStatus
{
    Unlisted = 0,
    Listed = 1,
    Revoked = 2
}

/// <summary>
/// Immutable snapshot of caller-supplied key listing and
/// revocation data.
///
/// Fingerprints are 64-character ASCII SHA-256 hexadecimal
/// strings and are compared without case sensitivity.
///
/// Revocation always takes precedence over listing.
///
/// SECURITY BOUNDARY:
/// This class does not authenticate the policy source,
/// validate signing-key provenance, verify a signature,
/// determine freshness, prevent rollback, or approve
/// a package release.
///
/// A status of Listed is not a trusted-publisher decision.
/// No installation or provider execution is authorized here.
/// </summary>
public sealed class ProviderPackageSigningKeyPolicySnapshot
{
    private const int FingerprintHexLength = 64;

    private readonly HashSet<string> _listedFingerprints;
    private readonly HashSet<string> _revokedFingerprints;

    /// <summary>
    /// Captures a defensive snapshot of supplied policy data.
    ///
    /// Empty collections are valid and classify every
    /// otherwise valid fingerprint as Unlisted.
    /// </summary>
    public ProviderPackageSigningKeyPolicySnapshot(
        IEnumerable<string> listedFingerprints,
        IEnumerable<string> revokedFingerprints)
    {
        ArgumentNullException.ThrowIfNull(
            listedFingerprints);

        ArgumentNullException.ThrowIfNull(
            revokedFingerprints);

        var listed = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        var revoked = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var fingerprint in listedFingerprints)
        {
            ValidateFingerprint(
                fingerprint,
                nameof(listedFingerprints));

            listed.Add(fingerprint);
        }

        foreach (var fingerprint in revokedFingerprints)
        {
            ValidateFingerprint(
                fingerprint,
                nameof(revokedFingerprints));

            revoked.Add(fingerprint);
        }

        _listedFingerprints = listed;
        _revokedFingerprints = revoked;
    }

    /// <summary>
    /// Classifies an exact SHA-256 fingerprint.
    ///
    /// Revoked takes precedence even when the fingerprint
    /// also appears in the listed collection.
    ///
    /// Listed never means independently authenticated.
    /// </summary>
    public ProviderPackageSigningKeyPolicyStatus GetStatus(
        string fingerprint)
    {
        ValidateFingerprint(
            fingerprint,
            nameof(fingerprint));

        if (_revokedFingerprints.Contains(fingerprint))
        {
            return ProviderPackageSigningKeyPolicyStatus.Revoked;
        }

        if (_listedFingerprints.Contains(fingerprint))
        {
            return ProviderPackageSigningKeyPolicyStatus.Listed;
        }

        return ProviderPackageSigningKeyPolicyStatus.Unlisted;
    }

    private static void ValidateFingerprint(
        string fingerprint,
        string parameterName)
    {
        if (fingerprint is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (fingerprint.Length != FingerprintHexLength)
        {
            throw new ArgumentException(
                "A SHA-256 fingerprint must contain exactly " +
                "64 ASCII hexadecimal characters.",
                parameterName);
        }

        foreach (var character in fingerprint)
        {
            var decimalDigit =
                character >= '0' && character <= '9';

            var lowerHex =
                character >= 'a' && character <= 'f';

            var upperHex =
                character >= 'A' && character <= 'F';

            if (!decimalDigit && !lowerHex && !upperHex)
            {
                throw new ArgumentException(
                    "A SHA-256 fingerprint contains a " +
                    "non-hexadecimal character.",
                    parameterName);
            }
        }
    }
}
