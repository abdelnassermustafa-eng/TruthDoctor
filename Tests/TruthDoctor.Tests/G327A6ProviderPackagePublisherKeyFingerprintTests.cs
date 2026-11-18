using System;
using System.Security.Cryptography;
using TruthDoctor.Services.Providers;
using Xunit;

namespace TruthDoctor.Tests;

/// <summary>
/// Tests an allowlist matching primitive, not trusted
/// provisioning, publisher identity, or installation.
///
/// Fingerprints are SHA-256 over exact DER-encoded
/// SubjectPublicKeyInfo bytes.
/// Test key pairs are generated only in memory.
/// </summary>
public sealed class G327A6ProviderPackagePublisherKeyFingerprintTests
{
    private static byte[] CreatePublicKey()
    {
        using var signer = ECDsa.Create(
            ECCurve.NamedCurves.nistP256);

        return signer.ExportSubjectPublicKeyInfo();
    }

    private static string Fingerprint(byte[] publicKeyDer)
    {
        return Convert.ToHexString(
            SHA256.HashData(publicKeyDer));
    }

    [Fact]
    public void EmptyApprovedSetRejectsEveryKey()
    {
        var matcher =
            new ProviderPackagePublisherKeyFingerprintMatcher(
                Array.Empty<string>());

        Assert.False(
            matcher.MatchesApprovedKey(CreatePublicKey()));
    }

    [Fact]
    public void MatchingApprovedFingerprintIsAccepted()
    {
        var publicKey = CreatePublicKey();

        var matcher =
            new ProviderPackagePublisherKeyFingerprintMatcher(
                new[] { Fingerprint(publicKey) });

        Assert.True(
            matcher.MatchesApprovedKey(publicKey));
    }

    [Fact]
    public void DifferentPublicKeyIsRejected()
    {
        var approvedKey = CreatePublicKey();
        var differentKey = CreatePublicKey();

        var matcher =
            new ProviderPackagePublisherKeyFingerprintMatcher(
                new[] { Fingerprint(approvedKey) });

        Assert.False(
            matcher.MatchesApprovedKey(differentKey));
    }

    [Fact]
    public void ApprovedFingerprintInputIsDefensivelyCopied()
    {
        var publicKey = CreatePublicKey();

        var supplied = new[]
        {
            Fingerprint(publicKey)
        };

        var matcher =
            new ProviderPackagePublisherKeyFingerprintMatcher(
                supplied);

        supplied[0] = new string('0', 64);

        Assert.True(
            matcher.MatchesApprovedKey(publicKey));
    }

    [Fact]
    public void MalformedApprovedFingerprintIsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            _ = new ProviderPackagePublisherKeyFingerprintMatcher(
                new[] { "not-a-sha256-fingerprint" });
        });
    }

    [Fact]
    public void MissingPublicKeyIsRejected()
    {
        var matcher =
            new ProviderPackagePublisherKeyFingerprintMatcher(
                Array.Empty<string>());

        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = matcher.MatchesApprovedKey(null!);
        });
    }
}
