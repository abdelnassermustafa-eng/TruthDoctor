using System;
using System.Linq;
using System.Security.Cryptography;
using TruthDoctor.Services.Providers;
using Xunit;

namespace TruthDoctor.Tests;

/// <summary>
/// Contract for unsigned canonical policy-body bytes.
///
/// This tests encoding only. It does not authenticate a
/// policy-signing authority, provide a trust anchor,
/// verify policy freshness, or authorize installation.
/// </summary>
public sealed class G327A6ProviderPackageCanonicalPolicyBodyEncoderTests
{
    private const string ExpectedDigest =
        "CD724CC855E5BD50A194B3B239C701E60A26ABAFE286C642A4D1B129D850C960";

    private static readonly string KeyA = new string('a', 64);
    private static readonly string KeyB = new string('b', 64);

    private static byte[] Encode(
        string policyId = "truthdoctor-provider-policy",
        string authorityId = "truthdoctor-release-authority",
        string rootKeyId = "example-root-1",
        ulong sequence = 7,
        long effectiveAt = 1794100000,
        long expiresAt = 1796700000,
        string[]? listed = null,
        string[]? revoked = null)
    {
        var encoder =
            new ProviderPackageCanonicalPolicyBodyEncoder();

        return encoder.Encode(
            policyId,
            authorityId,
            rootKeyId,
            sequence,
            effectiveAt,
            expiresAt,
            listed ?? new[] { KeyA, KeyB },
            revoked ?? new[] { KeyB });
    }

    [Fact]
    public void ReferenceVectorMatchesP36ByteLengthAndDigest()
    {
        var body = Encode();

        Assert.Equal(224, body.Length);

        Assert.Equal(
            ExpectedDigest,
            Convert.ToHexString(SHA256.HashData(body)));
    }

    [Fact]
    public void FingerprintOrderAndHexCaseProduceIdenticalBytes()
    {
        var original = Encode();

        var reordered = Encode(
            listed: new[]
            {
                KeyB.ToUpperInvariant(),
                KeyA.ToUpperInvariant()
            },
            revoked: new[] { KeyB.ToUpperInvariant() });

        Assert.Equal(original, reordered);
    }

    [Fact]
    public void EverySecurityRelevantFieldChangesSignedBytes()
    {
        var original = Encode();

        byte[][] variants =
        {
            Encode(policyId: "different-policy"),
            Encode(authorityId: "different-authority"),
            Encode(rootKeyId: "different-root"),
            Encode(sequence: 8),
            Encode(effectiveAt: 1794100001),
            Encode(expiresAt: 1796700001),
            Encode(listed: new[] { KeyA }),
            Encode(revoked: Array.Empty<string>())
        };

        foreach (var variant in variants)
        {
            Assert.False(original.SequenceEqual(variant));
        }
    }

    [Fact]
    public void InvalidIdentifiersAreRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            Encode(policyId: ""));

        Assert.Throws<ArgumentException>(() =>
            Encode(policyId: "unsafe/identifier"));

        Assert.Throws<ArgumentException>(() =>
            Encode(authorityId: "bad space"));

        Assert.Throws<ArgumentException>(() =>
            Encode(rootKeyId: ""));
    }

    [Fact]
    public void MalformedFingerprintsAreRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            Encode(listed: new[] { "invalid" }));

        Assert.Throws<ArgumentException>(() =>
            Encode(revoked: new[] { "not-a-fingerprint" }));

        Assert.Throws<ArgumentException>(() =>
            Encode(listed: new[] { new string('g', 64) }));
    }

    [Fact]
    public void DuplicateFingerprintsAreRejectedWithoutCaseBypass()
    {
        Assert.Throws<ArgumentException>(() =>
            Encode(listed: new[]
            {
                KeyA,
                KeyA.ToUpperInvariant()
            }));

        Assert.Throws<ArgumentException>(() =>
            Encode(revoked: new[] { KeyB, KeyB }));
    }

    [Fact]
    public void InvalidSequenceAndTimestampOrderingAreRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            Encode(sequence: 0));

        Assert.Throws<ArgumentException>(() =>
            Encode(effectiveAt: 1796700000));

        Assert.Throws<ArgumentException>(() =>
            Encode(expiresAt: 1794100000));
    }

    [Fact]
    public void FingerprintCollectionsAboveMaximumAreRejected()
    {
        var oversized = Enumerable.Range(0, 1025)
            .Select(value => value.ToString("x64"))
            .ToArray();

        Assert.Throws<ArgumentException>(() =>
            Encode(listed: oversized));

        Assert.Throws<ArgumentException>(() =>
            Encode(revoked: oversized));
    }

    [Fact]
    public void NullPolicyCollectionsAreRejected()
    {
        var encoder =
            new ProviderPackageCanonicalPolicyBodyEncoder();

        // Explicit Action delegates prevent overload ambiguity
        // while the encoder type is intentionally missing
        // during the TDD RED compilation phase.
        Action missingListed = () =>
        {
            _ = encoder.Encode(
                "truthdoctor-provider-policy",
                "truthdoctor-release-authority",
                "example-root-1",
                7UL,
                1794100000L,
                1796700000L,
                null!,
                new[] { KeyB });
        };

        Action missingRevoked = () =>
        {
            _ = encoder.Encode(
                "truthdoctor-provider-policy",
                "truthdoctor-release-authority",
                "example-root-1",
                7UL,
                1794100000L,
                1796700000L,
                new[] { KeyA },
                null!);
        };

        Assert.Throws<ArgumentNullException>(missingListed);
        Assert.Throws<ArgumentNullException>(missingRevoked);
    }
}
