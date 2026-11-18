using System;
using System.Security.Cryptography;

namespace TruthDoctor.Services.Providers;

/// <summary>
/// Checks byte-level SHA-256 integrity of an artifact
/// against a caller-supplied expected digest.
///
/// A successful match proves only that the provided
/// bytes agree with the provided digest.
///
/// It does not authenticate the digest source,
/// release metadata, publisher, distribution channel,
/// installation package or executable provider.
///
/// No release-authorization decision is made here.
/// </summary>
public sealed class TruthDoctorV3ReleaseArtifactIntegrityVerifier
{
    public const int Sha256DigestLengthBytes = 32;

    /// <summary>
    /// Returns true only when SHA-256 of the complete
    /// supplied artifact exactly matches the supplied
    /// 32-byte digest.
    ///
    /// Invalid arguments are rejected. An ordinary
    /// digest mismatch returns false.
    ///
    /// Neither input buffer is modified or retained.
    /// No files are opened and no artifacts are run.
    /// </summary>
    public bool Verify(
        byte[] artifactBytes,
        byte[] expectedSha256)
    {
        ArgumentNullException.ThrowIfNull(artifactBytes);
        ArgumentNullException.ThrowIfNull(expectedSha256);

        if (expectedSha256.Length != Sha256DigestLengthBytes)
        {
            throw new ArgumentException(
                "Expected SHA-256 digest must contain " +
                "exactly 32 bytes.",
                nameof(expectedSha256));
        }

        byte[] calculatedSha256 =
            SHA256.HashData(artifactBytes);

        return CryptographicOperations.FixedTimeEquals(
            calculatedSha256,
            expectedSha256);
    }
}
