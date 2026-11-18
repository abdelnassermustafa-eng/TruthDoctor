using System;
using System.Text;
using TruthDoctor.Services.Providers;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A6ProviderPackageArtifactIntegrityTests
{
    private const string AbcSha256 =
        "ba7816bf8f01cfea414140de5dae2223" +
        "b00361a396177a9cb410ff61f20015ad";

    [Fact]
    public void MatchingArtifactDigestIsAccepted()
    {
        var verifier =
            new ProviderPackageArtifactIntegrityVerifier();

        var artifactBytes = Encoding.UTF8.GetBytes("abc");

        Assert.True(
            verifier.MatchesSha256(
                artifactBytes,
                AbcSha256));
    }

    [Fact]
    public void ModifiedArtifactDigestIsRejected()
    {
        var verifier =
            new ProviderPackageArtifactIntegrityVerifier();

        var modifiedBytes =
            Encoding.UTF8.GetBytes("abc-modified");

        Assert.False(
            verifier.MatchesSha256(
                modifiedBytes,
                AbcSha256));
    }

    [Fact]
    public void HexadecimalDigestComparisonIgnoresLetterCase()
    {
        var verifier =
            new ProviderPackageArtifactIntegrityVerifier();

        Assert.True(
            verifier.MatchesSha256(
                Encoding.UTF8.GetBytes("abc"),
                AbcSha256.ToUpperInvariant()));
    }

    [Fact]
    public void MalformedExpectedDigestIsRejected()
    {
        var verifier =
            new ProviderPackageArtifactIntegrityVerifier();

        Assert.Throws<ArgumentException>(() =>
        {
            _ = verifier.MatchesSha256(
                Encoding.UTF8.GetBytes("abc"),
                "invalid-digest");
        });
    }

    [Fact]
    public void MissingArtifactBytesAreRejected()
    {
        var verifier =
            new ProviderPackageArtifactIntegrityVerifier();

        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = verifier.MatchesSha256(
                null!,
                AbcSha256);
        });
    }
}
