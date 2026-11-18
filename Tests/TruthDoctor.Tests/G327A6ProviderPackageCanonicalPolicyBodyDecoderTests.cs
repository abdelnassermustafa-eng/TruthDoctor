using System;
using System.Buffers.Binary;
using System.Linq;
using System.Security.Cryptography;
using TruthDoctor.Services.Providers;
using Xunit;

namespace TruthDoctor.Tests;

/// <summary>
/// Strict decoder contract for the proposed P36
/// unsigned canonical policy body.
///
/// This class tests structural decoding only.
/// It never authenticates a policy, trusts a publisher,
/// approves a release, or authorizes installation.
/// </summary>
public sealed class G327A6ProviderPackageCanonicalPolicyBodyDecoderTests
{
    private const string ExpectedDigest =
        "CD724CC855E5BD50A194B3B239C701E60A26ABAFE286C642A4D1B129D850C960";

    private static readonly string KeyA = new string('a', 64);
    private static readonly string KeyB = new string('b', 64);

    private const int DomainLength = 22;

    private static byte[] CanonicalBody()
    {
        var encoder =
            new ProviderPackageCanonicalPolicyBodyEncoder();

        return encoder.Encode(
            "truthdoctor-provider-policy",
            "truthdoctor-release-authority",
            "example-root-1",
            7UL,
            1794100000L,
            1796700000L,
            new[] { KeyA, KeyB },
            new[] { KeyB });
    }

    private static byte[] Changed(
        byte[] original,
        int offset,
        byte[] replacement)
    {
        var changed = (byte[])original.Clone();

        Array.Copy(
            replacement,
            0,
            changed,
            offset,
            replacement.Length);

        return changed;
    }

    private static byte[] U16(ushort value)
    {
        var result = new byte[2];

        BinaryPrimitives.WriteUInt16BigEndian(
            result,
            value);

        return result;
    }

    private static byte[] U64(ulong value)
    {
        var result = new byte[8];

        BinaryPrimitives.WriteUInt64BigEndian(
            result,
            value);

        return result;
    }

    private static byte[] I64(long value)
    {
        var result = new byte[8];

        BinaryPrimitives.WriteInt64BigEndian(
            result,
            value);

        return result;
    }

    private static ushort ReadU16(
        byte[] body,
        int offset)
    {
        return BinaryPrimitives.ReadUInt16BigEndian(
            body.AsSpan(offset, 2));
    }

    private static Offsets Locate(byte[] body)
    {
        var position = DomainLength + 2;

        var policyLength = position;
        var policyBytes = position + 2;

        position = policyBytes +
            ReadU16(body, policyLength);

        var authorityLength = position;
        position += 2 + ReadU16(body, position);

        var rootLength = position;
        position += 2 + ReadU16(body, position);

        var sequence = position;
        var effective = position + 8;
        var expires = position + 16;

        var listedCount = position + 24;
        var listedBytes = listedCount + 2;

        var revokedCount = listedBytes +
            32 * ReadU16(body, listedCount);

        return new Offsets(
            policyLength,
            policyBytes,
            authorityLength,
            rootLength,
            sequence,
            effective,
            expires,
            listedCount,
            listedBytes,
            revokedCount);
    }

    private readonly record struct Offsets(
        int PolicyLength,
        int PolicyBytes,
        int AuthorityLength,
        int RootLength,
        int Sequence,
        int Effective,
        int Expires,
        int ListedCount,
        int ListedBytes,
        int RevokedCount);

    private static void MustReject(byte[] candidate)
    {
        // Explicit Action prevents asynchronous Assert.Throws
        // overload ambiguity while the decoder is missing.
        Action decode = () =>
        {
            _ = new ProviderPackageCanonicalPolicyBodyDecoder()
                .Decode(candidate);
        };

        Assert.Throws<ArgumentException>(decode);
    }

    [Fact]
    public void P36ReferenceVectorDecodesAndRoundTripsExactly()
    {
        var body = CanonicalBody();

        Assert.Equal(224, body.Length);

        Assert.Equal(
            ExpectedDigest,
            Convert.ToHexString(SHA256.HashData(body)));

        var decoded =
            new ProviderPackageCanonicalPolicyBodyDecoder()
                .Decode(body);

        Assert.Equal(
            "truthdoctor-provider-policy",
            decoded.PolicyId);

        Assert.Equal(
            "truthdoctor-release-authority",
            decoded.AuthorityId);

        Assert.Equal(
            "example-root-1",
            decoded.RootKeyId);

        Assert.Equal(7UL, decoded.Sequence);
        Assert.Equal(1794100000L, decoded.EffectiveAt);
        Assert.Equal(1796700000L, decoded.ExpiresAt);

        Assert.Equal(
            new[] { KeyA, KeyB },
            decoded.ListedFingerprints);

        Assert.Equal(
            new[] { KeyB },
            decoded.RevokedFingerprints);

        var reencoded =
            new ProviderPackageCanonicalPolicyBodyEncoder()
                .Encode(
                    decoded.PolicyId,
                    decoded.AuthorityId,
                    decoded.RootKeyId,
                    decoded.Sequence,
                    decoded.EffectiveAt,
                    decoded.ExpiresAt,
                    decoded.ListedFingerprints,
                    decoded.RevokedFingerprints);

        Assert.Equal(body, reencoded);
    }

    [Fact]
    public void EveryTruncatedPrefixIsRejected()
    {
        var body = CanonicalBody();

        for (var length = 0; length < body.Length; length++)
        {
            MustReject(body[..length]);
        }
    }

    [Fact]
    public void TrailingBytesAndConcatenatedBodiesAreRejected()
    {
        var body = CanonicalBody();

        MustReject(
            body.Concat(new byte[] { 0 }).ToArray());

        MustReject(
            body.Concat(body).ToArray());
    }

    [Fact]
    public void WrongDomainAndUnsupportedVersionAreRejected()
    {
        var body = CanonicalBody();

        MustReject(
            Changed(body, 0, new byte[] { (byte)'X' }));

        MustReject(
            Changed(body, DomainLength, U16(2)));
    }

    [Fact]
    public void InvalidIdentifierLengthsAndCharactersAreRejected()
    {
        var body = CanonicalBody();
        var offsets = Locate(body);

        MustReject(
            Changed(body, offsets.PolicyLength, U16(0)));

        MustReject(
            Changed(body, offsets.PolicyLength, U16(65)));

        MustReject(
            Changed(
                body,
                offsets.PolicyBytes,
                new byte[] { (byte)'/' }));

        MustReject(
            Changed(
                body,
                offsets.AuthorityLength,
                U16(0)));

        MustReject(
            Changed(
                body,
                offsets.RootLength,
                U16(65)));
    }

    [Fact]
    public void ZeroSequenceAndInvalidValidityIntervalsAreRejected()
    {
        var body = CanonicalBody();
        var offsets = Locate(body);

        MustReject(
            Changed(body, offsets.Sequence, U64(0)));

        MustReject(
            Changed(
                body,
                offsets.Effective,
                I64(1796700000L)));

        MustReject(
            Changed(
                body,
                offsets.Expires,
                I64(1794100000L)));
    }

    [Fact]
    public void ExcessiveFingerprintCountsAreRejected()
    {
        var body = CanonicalBody();
        var offsets = Locate(body);

        MustReject(
            Changed(
                body,
                offsets.ListedCount,
                U16(1025)));

        MustReject(
            Changed(
                body,
                offsets.RevokedCount,
                U16(1025)));
    }

    [Fact]
    public void FingerprintCountLengthDisagreementsAreRejected()
    {
        var body = CanonicalBody();
        var offsets = Locate(body);

        MustReject(
            Changed(
                body,
                offsets.ListedCount,
                U16(3)));

        MustReject(
            Changed(
                body,
                offsets.RevokedCount,
                U16(2)));
    }

    [Fact]
    public void DuplicateFingerprintsAreRejected()
    {
        var body = CanonicalBody();
        var offsets = Locate(body);

        var first = body
            .AsSpan(offsets.ListedBytes, 32)
            .ToArray();

        MustReject(
            Changed(
                body,
                offsets.ListedBytes + 32,
                first));
    }

    [Fact]
    public void UnsortedFingerprintsAreRejected()
    {
        var body = CanonicalBody();
        var offsets = Locate(body);

        var first = body
            .AsSpan(offsets.ListedBytes, 32)
            .ToArray();

        var second = body
            .AsSpan(offsets.ListedBytes + 32, 32)
            .ToArray();

        var changed = Changed(
            body,
            offsets.ListedBytes,
            second);

        changed = Changed(
            changed,
            offsets.ListedBytes + 32,
            first);

        MustReject(changed);
    }

    [Fact]
    public void MaximumBodySizeIsEnforced()
    {
        // P36 maximum canonical body length.
        const int MaximumCanonicalBodyBytes = 65786;

        MustReject(
            new byte[MaximumCanonicalBodyBytes + 1]);
    }

    [Fact]
    public void NullBodyIsRejected()
    {
        Action decode = () =>
        {
            _ = new ProviderPackageCanonicalPolicyBodyDecoder()
                .Decode(null!);
        };

        Assert.Throws<ArgumentNullException>(decode);
    }
}
