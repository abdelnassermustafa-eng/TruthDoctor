using System;
using System.Security.Cryptography;

namespace TruthDoctor.Services.Providers;

/// <summary>
/// Verifies the SHA-256 digest of caller-supplied artifact bytes.
///
/// A matching digest establishes only byte integrity relative
/// to the supplied expected digest.
///
/// This class does not verify:
/// - the origin or authenticity of the expected digest;
/// - package publisher identity;
/// - cryptographic package signatures;
/// - release approval or installation eligibility;
/// - package compatibility or dependency completeness.
///
/// It performs no filesystem, network, package installation,
/// executable loading, or provider activation operations.
/// </summary>
public sealed class ProviderPackageArtifactIntegrityVerifier
{
    /// <summary>
    /// Returns true when artifactBytes match expectedSha256.
    ///
    /// The expected digest must consist of exactly 64 ASCII
    /// hexadecimal characters, with no prefix or separators.
    ///
    /// Null artifact bytes are rejected explicitly.
    /// Invalid digest text is rejected with ArgumentException.
    /// </summary>
    public bool MatchesSha256(
        byte[] artifactBytes,
        string expectedSha256)
    {
        ArgumentNullException.ThrowIfNull(artifactBytes);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            expectedSha256,
            nameof(expectedSha256));

        if (expectedSha256.Length != 64)
        {
            throw new ArgumentException(
                "The expected SHA-256 digest must contain " +
                "exactly 64 hexadecimal characters.",
                nameof(expectedSha256));
        }

        foreach (var character in expectedSha256)
        {
            var isDecimalDigit =
                character >= '0' && character <= '9';

            var isLowerHex =
                character >= 'a' && character <= 'f';

            var isUpperHex =
                character >= 'A' && character <= 'F';

            if (!isDecimalDigit && !isLowerHex && !isUpperHex)
            {
                throw new ArgumentException(
                    "The expected SHA-256 digest contains " +
                    "a non-hexadecimal character.",
                    nameof(expectedSha256));
            }
        }

        var expectedDigest =
            Convert.FromHexString(expectedSha256);

        var actualDigest =
            SHA256.HashData(artifactBytes);

        return CryptographicOperations.FixedTimeEquals(
            actualDigest,
            expectedDigest);
    }
}
