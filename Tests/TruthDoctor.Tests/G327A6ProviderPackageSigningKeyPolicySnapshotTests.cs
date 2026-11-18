using System;
using TruthDoctor.Services.Providers;
using Xunit;

namespace TruthDoctor.Tests;

/// <summary>
/// Tests classification of caller-supplied key-policy data.
///
/// "Listed" is not publisher trust.
/// This snapshot has no authenticated provenance,
/// no policy freshness or anti-rollback guarantee,
/// and no executable installation authority.
/// </summary>
public sealed class G327A6ProviderPackageSigningKeyPolicySnapshotTests
{
    private static readonly string KeyA =
        new string('a', 64);

    private static readonly string KeyB =
        new string('b', 64);

    [Fact]
    public void EmptySnapshotClassifiesKeyAsUnlisted()
    {
        var policy = new ProviderPackageSigningKeyPolicySnapshot(
            Array.Empty<string>(),
            Array.Empty<string>());

        Assert.Equal("Unlisted", policy.GetStatus(KeyA).ToString());
    }

    [Fact]
    public void ListedFingerprintRemainsOnlyListed()
    {
        var policy = new ProviderPackageSigningKeyPolicySnapshot(
            new[] { KeyA },
            Array.Empty<string>());

        Assert.Equal("Listed", policy.GetStatus(KeyA).ToString());
    }

    [Fact]
    public void RevokedFingerprintIsClassifiedAsRevoked()
    {
        var policy = new ProviderPackageSigningKeyPolicySnapshot(
            Array.Empty<string>(),
            new[] { KeyA });

        Assert.Equal("Revoked", policy.GetStatus(KeyA).ToString());
    }

    [Fact]
    public void RevocationTakesPrecedenceOverListing()
    {
        var policy = new ProviderPackageSigningKeyPolicySnapshot(
            new[] { KeyA },
            new[] { KeyA.ToUpperInvariant() });

        Assert.Equal("Revoked", policy.GetStatus(KeyA).ToString());
    }

    [Fact]
    public void UnlistedFingerprintIsNotImplicitlyAuthorized()
    {
        var policy = new ProviderPackageSigningKeyPolicySnapshot(
            new[] { KeyA },
            Array.Empty<string>());

        Assert.Equal("Unlisted", policy.GetStatus(KeyB).ToString());
    }

    [Fact]
    public void SuppliedFingerprintCollectionsAreDefensivelyCopied()
    {
        var listed = new[] { KeyA };
        var revoked = new[] { KeyB };

        var policy = new ProviderPackageSigningKeyPolicySnapshot(
            listed,
            revoked);

        listed[0] = new string('c', 64);
        revoked[0] = new string('d', 64);

        Assert.Equal("Listed", policy.GetStatus(KeyA).ToString());
        Assert.Equal("Revoked", policy.GetStatus(KeyB).ToString());
    }

    [Fact]
    public void MalformedPolicyFingerprintIsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            _ = new ProviderPackageSigningKeyPolicySnapshot(
                new[] { "invalid-fingerprint" },
                Array.Empty<string>());
        });
    }
}
