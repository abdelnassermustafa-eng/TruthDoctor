using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace TruthDoctor.Services.Providers;

/// <summary>
/// Matches a caller-supplied public key against an immutable
/// snapshot of approved SHA-256 public-key fingerprints.
///
/// Fingerprints are calculated over the exact supplied
/// DER-encoded SubjectPublicKeyInfo bytes.
///
/// IMPORTANT SECURITY BOUNDARY:
/// This class does not authenticate the fingerprint list,
/// validate publisher identity, or establish key provenance.
///
/// A successful match is not package trust, release approval,
/// or permission to install or execute a provider.
///
/// Trusted policy provisioning, key revocation, public-key
/// structural validation, and signed package metadata
/// verification are separate responsibilities.
/// </summary>
public sealed class ProviderPackagePublisherKeyFingerprintMatcher
{
    private const int FingerprintHexLength = 64;

    private readonly byte[][] _approvedDigests;

    /// <summary>
    /// Captures approved fingerprints defensively.
    ///
    /// Every fingerprint must contain exactly 64 ASCII
    /// hexadecimal characters.
    ///
    /// An empty collection creates a deny-all matcher.
    /// </summary>
    public ProviderPackagePublisherKeyFingerprintMatcher(
        IEnumerable<string> approvedFingerprints)
    {
        ArgumentNullException.ThrowIfNull(
            approvedFingerprints);

        var snapshot = new List<byte[]>();

        foreach (var fingerprint in approvedFingerprints)
        {
            if (fingerprint is null ||
                fingerprint.Length != FingerprintHexLength)
            {
                throw new ArgumentException(
                    "Every approved fingerprint must contain " +
                    "exactly 64 hexadecimal characters.",
                    nameof(approvedFingerprints));
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
                        "An approved fingerprint contains " +
                        "a non-hexadecimal character.",
                        nameof(approvedFingerprints));
                }
            }

            snapshot.Add(
                Convert.FromHexString(fingerprint));
        }

        _approvedDigests = snapshot.ToArray();
    }

    /// <summary>
    /// Returns true if the exact supplied public-key bytes
    /// match at least one approved SHA-256 fingerprint.
    ///
    /// An empty approved set always returns false.
    ///
    /// This method does not authorize a publisher or
    /// verify a cryptographic package signature.
    /// </summary>
    public bool MatchesApprovedKey(byte[] publicKeyDer)
    {
        ArgumentNullException.ThrowIfNull(publicKeyDer);

        if (_approvedDigests.Length == 0)
        {
            return false;
        }

        var actualDigest = SHA256.HashData(publicKeyDer);

        var matched = false;

        foreach (var approvedDigest in _approvedDigests)
        {
            matched |= CryptographicOperations.FixedTimeEquals(
                actualDigest,
                approvedDigest);
        }

        return matched;
    }
}
