using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TruthDoctor.Services.Providers;
using Xunit;

namespace TruthDoctor.Tests;

/// <summary>
/// Release artifact SHA-256 integrity only.
///
/// A matching digest never establishes publisher
/// authentication, signature verification, release
/// authorization or executable provider approval.
/// </summary>
public sealed class G327A6V3ReleaseArtifactIntegrityVerifierTests
{
    private static byte[] Artifact(string name)
    {
        return name switch
        {
            "empty-artifact" =>
                Array.Empty<byte>(),

            "short-artifact" =>
                Encoding.ASCII.GetBytes(
                    "TruthDoctor V3 artifact reference"),

            "binary-artifact" =>
                Enumerable.Range(0, 256)
                    .Select(x => (byte)x)
                    .ToArray(),

            "nul-rich-artifact" =>
                Enumerable.Range(0, 128)
                    .Select(x => (byte)(x % 4 switch
                    {
                        0 => 0,
                        1 => 255,
                        2 => 0,
                        _ => 1
                    }))
                    .ToArray(),

            "large-artifact" =>
                Enumerable.Range(0, 256)
                    .Select(x => (byte)x)
                    .ToArray()
                    .RepeatBytes(4096),

            _ => throw new InvalidOperationException(
                "Unknown test artifact: " + name)
        };
    }

    [Theory]
    [InlineData("empty-artifact")]
    [InlineData("short-artifact")]
    [InlineData("binary-artifact")]
    [InlineData("nul-rich-artifact")]
    [InlineData("large-artifact")]
    public void MatchingArtifactDigestReturnsTrue(string caseName)
    {
        byte[] artifact = Artifact(caseName);
        byte[] digest = SHA256.HashData(artifact);

        bool matched =
            new TruthDoctorV3ReleaseArtifactIntegrityVerifier()
                .Verify(artifact, digest);

        Assert.True(matched);
    }

    [Theory]
    [InlineData("first-byte-tamper")]
    [InlineData("last-byte-tamper")]
    [InlineData("truncated-artifact")]
    [InlineData("appended-artifact-byte")]
    [InlineData("incorrect-declared-digest")]
    public void MismatchedArtifactDigestReturnsFalse(string caseName)
    {
        byte[] original = Artifact("binary-artifact");
        byte[] artifact = (byte[])original.Clone();

        byte[] expected = SHA256.HashData(original);

        switch (caseName)
        {
            case "first-byte-tamper":
                artifact[0] ^= 1;
                break;

            case "last-byte-tamper":
                artifact[^1] ^= 1;
                break;

            case "truncated-artifact":
                artifact = artifact[..^1];
                break;

            case "appended-artifact-byte":
                artifact = artifact.Concat(new byte[] { 0 }).ToArray();
                break;

            case "incorrect-declared-digest":
                expected[0] ^= 1;
                break;

            default:
                throw new InvalidOperationException(caseName);
        }

        bool matched =
            new TruthDoctorV3ReleaseArtifactIntegrityVerifier()
                .Verify(artifact, expected);

        Assert.False(matched);
    }

    [Theory]
    [InlineData(31)]
    [InlineData(33)]
    public void InvalidDigestLengthIsRejected(int length)
    {
        byte[] artifact = Artifact("binary-artifact");
        byte[] digest = new byte[length];

        Assert.Throws<ArgumentException>(() =>
            new TruthDoctorV3ReleaseArtifactIntegrityVerifier()
                .Verify(artifact, digest));
    }

    [Fact]
    public void NullArtifactIsRejected()
    {
        byte[] digest = SHA256.HashData(
            Artifact("binary-artifact"));

        Assert.Throws<ArgumentNullException>(() =>
            new TruthDoctorV3ReleaseArtifactIntegrityVerifier()
                .Verify(null!, digest));
    }

    [Fact]
    public void NullExpectedDigestIsRejected()
    {
        byte[] artifact = Artifact("binary-artifact");

        Assert.Throws<ArgumentNullException>(() =>
            new TruthDoctorV3ReleaseArtifactIntegrityVerifier()
                .Verify(artifact, null!));
    }

    [Fact]
    public void SuccessfulVerificationPreservesInputBuffers()
    {
        byte[] artifact = Artifact("binary-artifact");
        byte[] digest = SHA256.HashData(artifact);

        byte[] originalArtifact = (byte[])artifact.Clone();
        byte[] originalDigest = (byte[])digest.Clone();

        Assert.True(
            new TruthDoctorV3ReleaseArtifactIntegrityVerifier()
                .Verify(artifact, digest));

        Assert.Equal(originalArtifact, artifact);
        Assert.Equal(originalDigest, digest);
    }

    [Fact]
    public void FailedVerificationPreservesInputBuffers()
    {
        byte[] artifact = Artifact("binary-artifact");
        byte[] digest = SHA256.HashData(artifact);

        digest[0] ^= 1;

        byte[] originalArtifact = (byte[])artifact.Clone();
        byte[] originalDigest = (byte[])digest.Clone();

        Assert.False(
            new TruthDoctorV3ReleaseArtifactIntegrityVerifier()
                .Verify(artifact, digest));

        Assert.Equal(originalArtifact, artifact);
        Assert.Equal(originalDigest, digest);
    }

    [Fact]
    public void CanonicalMetadataDigestDoesNotAuthenticateSyntheticArtifact()
    {
        // P58 reference metadata contains placeholder
        // digest bytes 00 through 1F.
        //
        // A structurally valid metadata record is not
        // an authenticated authority for artifact bytes.
        byte[] declaredDigest =
            Enumerable.Range(0, 32)
                .Select(x => (byte)x)
                .ToArray();

        byte[] syntheticArtifact =
            Encoding.ASCII.GetBytes(
                "TruthDoctor synthetic test artifact");

        Assert.False(
            new TruthDoctorV3ReleaseArtifactIntegrityVerifier()
                .Verify(syntheticArtifact, declaredDigest));
    }
}

internal static class G327A6ArtifactTestBytesExtensions
{
    public static byte[] RepeatBytes(
        this byte[] source,
        int repetitions)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        if (repetitions < 0)
            throw new ArgumentException(
                "Repetition count cannot be negative.",
                nameof(repetitions));

        byte[] output = new byte[
            checked(source.Length * repetitions)];

        for (int index = 0; index < repetitions; index++)
        {
            Buffer.BlockCopy(
                source,
                0,
                output,
                index * source.Length,
                source.Length);
        }

        return output;
    }
}
