using System;
using System.Buffers.Binary;
using System.Numerics;
using System.Text;

namespace TruthDoctor.Services.Providers;

/// <summary>
/// Deterministic, non-authorizing TruthDoctor V3
/// canonical release metadata encoder.
///
/// This class does not sign releases, validate publishers,
/// verify artifact contents, provision trusted roots,
/// enforce anti-rollback, or authorize providers.
/// </summary>
public sealed class TruthDoctorV3CanonicalReleaseMetadataEncoder
{
    public const int MaximumCanonicalBodyBytes = 128;

    private static readonly byte[] Domain =
        Encoding.ASCII.GetBytes(
            "TRUTHDOCTOR-RELEASE-V1\0");

    private static readonly byte[] Product =
        Encoding.ASCII.GetBytes("TruthDoctor");

    /// <summary>
    /// Produces strictly validated V1 canonical metadata.
    /// Returned bytes convey no trust or authorization.
    ///
    /// Invalid non-null values throw ArgumentException,
    /// matching the accepted P63 test contract.
    /// Null inputs throw ArgumentNullException.
    /// </summary>
    public byte[] Encode(
        string product,
        string runtimeTarget,
        string artifactFormat,
        int versionMajor,
        int versionMinor,
        int versionPatch,
        BigInteger releaseSequence,
        byte[] artifactSha256,
        string publisherKeyId,
        string signatureAlgorithm)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(runtimeTarget);
        ArgumentNullException.ThrowIfNull(artifactFormat);
        ArgumentNullException.ThrowIfNull(artifactSha256);
        ArgumentNullException.ThrowIfNull(publisherKeyId);
        ArgumentNullException.ThrowIfNull(signatureAlgorithm);

        if (!string.Equals(
                product,
                "TruthDoctor",
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Unsupported canonical release product.",
                nameof(product));
        }

        byte targetCode = runtimeTarget switch
        {
            "osx-arm64" => 1,
            "osx-x64" => 2,
            "win-x64" => 3,
            "linux-x64" => 4,

            _ => throw new ArgumentException(
                "Unsupported canonical runtime target.",
                nameof(runtimeTarget))
        };

        byte formatCode = artifactFormat switch
        {
            "application-bundle" => 1,
            "installer" => 2,
            "signed-package" => 3,
            "archive" => 4,

            _ => throw new ArgumentException(
                "Unsupported canonical artifact format.",
                nameof(artifactFormat))
        };

        ValidateVersionPart(
            versionMajor, nameof(versionMajor));

        ValidateVersionPart(
            versionMinor, nameof(versionMinor));

        ValidateVersionPart(
            versionPatch, nameof(versionPatch));

        // Exact ArgumentException is required by P63.
        // Reject negative, zero and oversized sequences.
        if (releaseSequence < BigInteger.One ||
            releaseSequence > new BigInteger(ulong.MaxValue))
        {
            throw new ArgumentException(
                "Release sequence must be a nonzero " +
                "unsigned 64-bit integer.",
                nameof(releaseSequence));
        }

        if (artifactSha256.Length != 32)
        {
            throw new ArgumentException(
                "Artifact SHA-256 must contain exactly 32 bytes.",
                nameof(artifactSha256));
        }

        ValidatePublisherIdentifier(publisherKeyId);

        byte algorithmCode = signatureAlgorithm switch
        {
            "ECDSA-P256-SHA256" => 1,

            _ => throw new ArgumentException(
                "Unsupported canonical signature algorithm.",
                nameof(signatureAlgorithm))
        };

        byte[] publisherBytes =
            Encoding.ASCII.GetBytes(publisherKeyId);

        // Exact canonical V1 binary representation.
        // All integers are big-endian.
        int totalLength = checked(
            Domain.Length +
            2 +
            1 +
            Product.Length +
            1 +
            1 +
            6 +
            8 +
            32 +
            1 +
            publisherBytes.Length +
            1);

        if (totalLength > MaximumCanonicalBodyBytes)
        {
            throw new ArgumentException(
                "Canonical metadata exceeds the maximum size.");
        }

        byte[] result = new byte[totalLength];

        int offset = 0;

        WriteBytes(result, ref offset, Domain);

        WriteUInt16(result, ref offset, 1);

        WriteByte(
            result, ref offset, (byte)Product.Length);

        WriteBytes(result, ref offset, Product);

        WriteByte(result, ref offset, targetCode);
        WriteByte(result, ref offset, formatCode);

        WriteUInt16(
            result, ref offset, (ushort)versionMajor);

        WriteUInt16(
            result, ref offset, (ushort)versionMinor);

        WriteUInt16(
            result, ref offset, (ushort)versionPatch);

        WriteUInt64(
            result, ref offset, (ulong)releaseSequence);

        // Copy into the output buffer.
        // Never retain caller-owned digest storage.
        WriteBytes(result, ref offset, artifactSha256);

        WriteByte(
            result, ref offset, (byte)publisherBytes.Length);

        WriteBytes(result, ref offset, publisherBytes);

        WriteByte(result, ref offset, algorithmCode);

        if (offset != result.Length)
        {
            throw new InvalidOperationException(
                "Canonical encoder length invariant violated.");
        }

        return result;
    }

    private static void ValidateVersionPart(
        int version,
        string parameterName)
    {
        // CORRECTION:
        // Do not throw ArgumentOutOfRangeException here.
        // P63 requires exact ArgumentException.
        if (version < 0 || version > ushort.MaxValue)
        {
            throw new ArgumentException(
                "Version component must fit unsigned 16-bit.",
                parameterName);
        }
    }

    private static void ValidatePublisherIdentifier(
        string publisherKeyId)
    {
        if (publisherKeyId.Length is < 1 or > 32)
        {
            throw new ArgumentException(
                "Publisher identifier length must be 1 through 32.",
                nameof(publisherKeyId));
        }

        for (int index = 0;
             index < publisherKeyId.Length;
             index++)
        {
            char value = publisherKeyId[index];

            bool isAlphaNumeric =
                (value >= 'A' && value <= 'Z') ||
                (value >= 'a' && value <= 'z') ||
                (value >= '0' && value <= '9');

            bool isSeparator =
                value == '.' ||
                value == '_' ||
                value == '-';

            if (!isAlphaNumeric &&
                (index == 0 || !isSeparator))
            {
                throw new ArgumentException(
                    "Publisher identifier is not canonical ASCII.",
                    nameof(publisherKeyId));
            }
        }
    }

    private static void WriteByte(
        byte[] output,
        ref int offset,
        byte value)
    {
        output[offset] = value;
        offset++;
    }

    private static void WriteBytes(
        byte[] output,
        ref int offset,
        ReadOnlySpan<byte> bytes)
    {
        bytes.CopyTo(
            output.AsSpan(offset, bytes.Length));

        offset += bytes.Length;
    }

    private static void WriteUInt16(
        byte[] output,
        ref int offset,
        ushort value)
    {
        BinaryPrimitives.WriteUInt16BigEndian(
            output.AsSpan(offset, 2),
            value);

        offset += 2;
    }

    private static void WriteUInt64(
        byte[] output,
        ref int offset,
        ulong value)
    {
        BinaryPrimitives.WriteUInt64BigEndian(
            output.AsSpan(offset, 8),
            value);

        offset += 8;
    }
}
