using System;
using System.Collections.Generic;
using System.Numerics;
using System.Security.Cryptography;
using TruthDoctor.Services.Providers;
using Xunit;

namespace TruthDoctor.Tests;

/// <summary>
/// Proposed TruthDoctor V3 canonical encoder test contract.
///
/// Encoding and decoding are structural operations only.
/// Passing tests do not approve a publisher, release,
/// trusted root or executable provider.
/// </summary>
public sealed class G327A6V3CanonicalReleaseMetadataEncoderTests
{
    private const string ReferenceSha256 = "CDF3EDCD34AF418B43D5F5F00730D49CC210AD18C58FE5F1CA72D3EFD11005BF";
    private const string ReferenceHex = "5452555448444F43544F522D52454C454153452D56310000010B5472757468446F63746F7201010001000000000000000000000001000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F137472757468646F63746F722D72656C6561736501";

    public static IEnumerable<object[]> ValidVectors()
    {
        yield return new object[] { "reference", "TruthDoctor", "osx-arm64", "application-bundle", 1, 0, 0, "1", "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F", "truthdoctor-release", "ECDSA-P256-SHA256", "5452555448444F43544F522D52454C454153452D56310000010B5472757468446F63746F7201010001000000000000000000000001000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F137472757468646F63746F722D72656C6561736501" };
        yield return new object[] { "macos-intel", "TruthDoctor", "osx-x64", "application-bundle", 1, 0, 0, "1", "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F", "truthdoctor-release", "ECDSA-P256-SHA256", "5452555448444F43544F522D52454C454153452D56310000010B5472757468446F63746F7202010001000000000000000000000001000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F137472757468646F63746F722D72656C6561736501" };
        yield return new object[] { "windows-installer", "TruthDoctor", "win-x64", "installer", 1, 0, 0, "1", "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F", "truthdoctor-release", "ECDSA-P256-SHA256", "5452555448444F43544F522D52454C454153452D56310000010B5472757468446F63746F7203020001000000000000000000000001000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F137472757468646F63746F722D72656C6561736501" };
        yield return new object[] { "linux-package", "TruthDoctor", "linux-x64", "signed-package", 1, 0, 0, "1", "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F", "truthdoctor-release", "ECDSA-P256-SHA256", "5452555448444F43544F522D52454C454153452D56310000010B5472757468446F63746F7204030001000000000000000000000001000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F137472757468646F63746F722D72656C6561736501" };
        yield return new object[] { "archive-format", "TruthDoctor", "osx-arm64", "archive", 1, 0, 0, "1", "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F", "truthdoctor-release", "ECDSA-P256-SHA256", "5452555448444F43544F522D52454C454153452D56310000010B5472757468446F63746F7201040001000000000000000000000001000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F137472757468646F63746F722D72656C6561736501" };
        yield return new object[] { "maximum-version", "TruthDoctor", "osx-arm64", "application-bundle", 65535, 65535, 65535, "1", "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F", "truthdoctor-release", "ECDSA-P256-SHA256", "5452555448444F43544F522D52454C454153452D56310000010B5472757468446F63746F720101FFFFFFFFFFFF0000000000000001000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F137472757468646F63746F722D72656C6561736501" };
        yield return new object[] { "maximum-sequence", "TruthDoctor", "osx-arm64", "application-bundle", 1, 0, 0, "18446744073709551615", "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F", "truthdoctor-release", "ECDSA-P256-SHA256", "5452555448444F43544F522D52454C454153452D56310000010B5472757468446F63746F720101000100000000FFFFFFFFFFFFFFFF000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F137472757468646F63746F722D72656C6561736501" };
        yield return new object[] { "minimum-publisher", "TruthDoctor", "osx-arm64", "application-bundle", 1, 0, 0, "1", "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F", "A", "ECDSA-P256-SHA256", "5452555448444F43544F522D52454C454153452D56310000010B5472757468446F63746F7201010001000000000000000000000001000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F014101" };
        yield return new object[] { "maximum-publisher", "TruthDoctor", "osx-arm64", "application-bundle", 1, 0, 0, "1", "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", "ECDSA-P256-SHA256", "5452555448444F43544F522D52454C454153452D56310000010B5472757468446F63746F7201010001000000000000000000000001000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F20414141414141414141414141414141414141414141414141414141414141414101" };
        yield return new object[] { "allowed-id-separators", "TruthDoctor", "osx-arm64", "application-bundle", 1, 0, 0, "1", "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F", "Release.v1_2-A", "ECDSA-P256-SHA256", "5452555448444F43544F522D52454C454153452D56310000010B5472757468446F63746F7201010001000000000000000000000001000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F0E52656C656173652E76315F322D4101" };
        yield return new object[] { "different-digest", "TruthDoctor", "osx-arm64", "application-bundle", 1, 0, 0, "1", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", "truthdoctor-release", "ECDSA-P256-SHA256", "5452555448444F43544F522D52454C454153452D56310000010B5472757468446F63746F7201010001000000000000000000000001AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA137472757468646F63746F722D72656C6561736501" };
    }

    public static IEnumerable<object[]> InvalidVectors()
    {
        yield return new object[] { "wrong-product", "TruthApi", "osx-arm64", "application-bundle", 1, 0, 0, "1", "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F", "truthdoctor-release", "ECDSA-P256-SHA256" };
        yield return new object[] { "unknown-target", "TruthDoctor", "other-target", "application-bundle", 1, 0, 0, "1", "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F", "truthdoctor-release", "ECDSA-P256-SHA256" };
        yield return new object[] { "unknown-format", "TruthDoctor", "osx-arm64", "other-format", 1, 0, 0, "1", "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F", "truthdoctor-release", "ECDSA-P256-SHA256" };
        yield return new object[] { "unknown-algorithm", "TruthDoctor", "osx-arm64", "application-bundle", 1, 0, 0, "1", "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F", "truthdoctor-release", "RSA-UNKNOWN" };
        yield return new object[] { "negative-major", "TruthDoctor", "osx-arm64", "application-bundle", -1, 0, 0, "1", "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F", "truthdoctor-release", "ECDSA-P256-SHA256" };
        yield return new object[] { "oversized-major", "TruthDoctor", "osx-arm64", "application-bundle", 65536, 0, 0, "1", "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F", "truthdoctor-release", "ECDSA-P256-SHA256" };
        yield return new object[] { "negative-minor", "TruthDoctor", "osx-arm64", "application-bundle", 1, -1, 0, "1", "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F", "truthdoctor-release", "ECDSA-P256-SHA256" };
        yield return new object[] { "oversized-patch", "TruthDoctor", "osx-arm64", "application-bundle", 1, 0, 65536, "1", "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F", "truthdoctor-release", "ECDSA-P256-SHA256" };
        yield return new object[] { "negative-sequence", "TruthDoctor", "osx-arm64", "application-bundle", 1, 0, 0, "-1", "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F", "truthdoctor-release", "ECDSA-P256-SHA256" };
        yield return new object[] { "zero-sequence", "TruthDoctor", "osx-arm64", "application-bundle", 1, 0, 0, "0", "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F", "truthdoctor-release", "ECDSA-P256-SHA256" };
        yield return new object[] { "oversized-sequence", "TruthDoctor", "osx-arm64", "application-bundle", 1, 0, 0, "18446744073709551616", "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F", "truthdoctor-release", "ECDSA-P256-SHA256" };
        yield return new object[] { "short-digest", "TruthDoctor", "osx-arm64", "application-bundle", 1, 0, 0, "1", "00000000000000000000000000000000000000000000000000000000000000", "truthdoctor-release", "ECDSA-P256-SHA256" };
        yield return new object[] { "long-digest", "TruthDoctor", "osx-arm64", "application-bundle", 1, 0, 0, "1", "000000000000000000000000000000000000000000000000000000000000000000", "truthdoctor-release", "ECDSA-P256-SHA256" };
        yield return new object[] { "empty-publisher", "TruthDoctor", "osx-arm64", "application-bundle", 1, 0, 0, "1", "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F", "", "ECDSA-P256-SHA256" };
        yield return new object[] { "oversized-publisher", "TruthDoctor", "osx-arm64", "application-bundle", 1, 0, 0, "1", "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", "ECDSA-P256-SHA256" };
        yield return new object[] { "publisher-leading-separator", "TruthDoctor", "osx-arm64", "application-bundle", 1, 0, 0, "1", "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F", "-test", "ECDSA-P256-SHA256" };
        yield return new object[] { "publisher-invalid-character", "TruthDoctor", "osx-arm64", "application-bundle", 1, 0, 0, "1", "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F", "a!b", "ECDSA-P256-SHA256" };
        yield return new object[] { "publisher-non-ascii", "TruthDoctor", "osx-arm64", "application-bundle", 1, 0, 0, "1", "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F", "r\u00e9lease", "ECDSA-P256-SHA256" };
    }

    private static byte[] Encode(
        string product,
        string target,
        string artifactFormat,
        int major,
        int minor,
        int patch,
        string sequence,
        string digestHex,
        string publisher,
        string algorithm)
    {
        return new TruthDoctorV3CanonicalReleaseMetadataEncoder()
            .Encode(
                product,
                target,
                artifactFormat,
                major,
                minor,
                patch,
                BigInteger.Parse(sequence),
                Convert.FromHexString(digestHex),
                publisher,
                algorithm);
    }

    [Theory]
    [MemberData(nameof(ValidVectors))]
    public void ValidInputProducesExactCanonicalBytes(
        string caseName,
        string product,
        string target,
        string artifactFormat,
        int major,
        int minor,
        int patch,
        string sequence,
        string digestHex,
        string publisher,
        string algorithm,
        string expectedHex)
    {
        Assert.False(string.IsNullOrWhiteSpace(caseName));

        byte[] output = Encode(
            product, target, artifactFormat,
            major, minor, patch,
            sequence, digestHex, publisher, algorithm);

        Assert.Equal(expectedHex, Convert.ToHexString(output));

        var decoded =
            new TruthDoctorV3CanonicalReleaseMetadataDecoder()
                .Decode(output);

        Assert.Equal(product, decoded.Product);
        Assert.Equal(target, decoded.RuntimeTarget);
        Assert.Equal(artifactFormat, decoded.ArtifactFormat);
        Assert.Equal(major, (int)decoded.VersionMajor);
        Assert.Equal(minor, (int)decoded.VersionMinor);
        Assert.Equal(patch, (int)decoded.VersionPatch);
        Assert.Equal(
            BigInteger.Parse(sequence),
            new BigInteger(decoded.ReleaseSequence));
        Assert.Equal(digestHex, decoded.ArtifactSha256Hex);
        Assert.Equal(publisher, decoded.PublisherKeyId);
        Assert.Equal(algorithm, decoded.SignatureAlgorithm);

        Assert.False(decoded.IsTrusted);
        Assert.False(decoded.IsAuthorized);
    }

    [Theory]
    [MemberData(nameof(InvalidVectors))]
    public void InvalidInputIsRejectedBeforeEncoding(
        string caseName,
        string product,
        string target,
        string artifactFormat,
        int major,
        int minor,
        int patch,
        string sequence,
        string digestHex,
        string publisher,
        string algorithm)
    {
        Assert.False(string.IsNullOrWhiteSpace(caseName));

        Assert.Throws<ArgumentException>(() =>
            Encode(product, target, artifactFormat,
                major, minor, patch, sequence,
                digestHex, publisher, algorithm));
    }

    [Fact]
    public void AcceptedReferenceHasExactLengthAndDigest()
    {
        byte[] encoded = Encode(
            "TruthDoctor", "osx-arm64",
            "application-bundle", 1, 0, 0, "1",
            Convert.ToHexString(
                new byte[] {
                    0, 1, 2, 3, 4, 5, 6, 7,
                    8, 9, 10, 11, 12, 13, 14, 15,
                    16, 17, 18, 19, 20, 21, 22, 23,
                    24, 25, 26, 27, 28, 29, 30, 31
                }),
            "truthdoctor-release", "ECDSA-P256-SHA256");

        Assert.Equal(106, encoded.Length);
        Assert.Equal(ReferenceHex, Convert.ToHexString(encoded));
        Assert.Equal(
            ReferenceSha256,
            Convert.ToHexString(SHA256.HashData(encoded)));
    }

    [Fact]
    public void CallerDigestMutationDoesNotChangeEncodedBytes()
    {
        byte[] digest = new byte[32];
        for (int index = 0; index < digest.Length; index++)
            digest[index] = (byte)index;

        byte[] encoded =
            new TruthDoctorV3CanonicalReleaseMetadataEncoder()
                .Encode(
                    "TruthDoctor", "osx-arm64",
                    "application-bundle", 1, 0, 0,
                    BigInteger.One, digest,
                    "truthdoctor-release", "ECDSA-P256-SHA256");

        string before = Convert.ToHexString(encoded);
        digest[0] ^= 1;

        Assert.Equal(before, Convert.ToHexString(encoded));

        var decoded =
            new TruthDoctorV3CanonicalReleaseMetadataDecoder()
                .Decode(encoded);

        Assert.Equal(
            Convert.ToHexString(
                new byte[] {
                    0, 1, 2, 3, 4, 5, 6, 7,
                    8, 9, 10, 11, 12, 13, 14, 15,
                    16, 17, 18, 19, 20, 21, 22, 23,
                    24, 25, 26, 27, 28, 29, 30, 31
                }),
            decoded.ArtifactSha256Hex);
    }

    [Theory]
    [InlineData("product")]
    [InlineData("target")]
    [InlineData("format")]
    [InlineData("digest")]
    [InlineData("publisher")]
    [InlineData("algorithm")]
    public void NullRequiredInputIsRejected(string field)
    {
        string product = "TruthDoctor";
        string target = "osx-arm64";
        string format = "application-bundle";
        byte[] digest = new byte[32];
        string publisher = "truthdoctor-release";
        string algorithm = "ECDSA-P256-SHA256";

        switch (field)
        {
            case "product": product = null!; break;
            case "target": target = null!; break;
            case "format": format = null!; break;
            case "digest": digest = null!; break;
            case "publisher": publisher = null!; break;
            case "algorithm": algorithm = null!; break;
            default: throw new InvalidOperationException(field);
        }

        Assert.Throws<ArgumentNullException>(() =>
            new TruthDoctorV3CanonicalReleaseMetadataEncoder()
                .Encode(
                    product, target, format, 1, 0, 0,
                    BigInteger.One, digest, publisher, algorithm));
    }
}
