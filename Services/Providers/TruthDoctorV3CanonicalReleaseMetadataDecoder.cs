using System;
using System.Buffers.Binary;
using System.Text;

namespace TruthDoctor.Services.Providers;

/// <summary>
/// Structurally decoded TruthDoctor V3 release metadata.
///
/// This is not authenticated release evidence.
/// In particular, a supplied publisher identifier,
/// artifact digest or algorithm label does not
/// establish authorization.
///
/// The result is permanently non-authorizing.
/// </summary>
public sealed class TruthDoctorV3CanonicalReleaseMetadata
{
    public string Product { get; }
    public string RuntimeTarget { get; }
    public string ArtifactFormat { get; }

    public ushort VersionMajor { get; }
    public ushort VersionMinor { get; }
    public ushort VersionPatch { get; }

    public ulong ReleaseSequence { get; }

    public string ArtifactSha256Hex { get; }
    public string PublisherKeyId { get; }
    public string SignatureAlgorithm { get; }

    public bool IsTrusted => false;
    public bool IsAuthorized => false;

    internal TruthDoctorV3CanonicalReleaseMetadata(
        string product,
        string runtimeTarget,
        string artifactFormat,
        ushort versionMajor,
        ushort versionMinor,
        ushort versionPatch,
        ulong releaseSequence,
        string artifactSha256Hex,
        string publisherKeyId,
        string signatureAlgorithm)
    {
        Product = product;
        RuntimeTarget = runtimeTarget;
        ArtifactFormat = artifactFormat;

        VersionMajor = versionMajor;
        VersionMinor = versionMinor;
        VersionPatch = versionPatch;

        ReleaseSequence = releaseSequence;
        ArtifactSha256Hex = artifactSha256Hex;
        PublisherKeyId = publisherKeyId;
        SignatureAlgorithm = signatureAlgorithm;
    }
}

/// <summary>
/// Strict structural decoder for proposed canonical
/// TruthDoctor V3 release metadata schema V1.
///
/// This decoder:
/// - Requires an exact domain and product identity.
/// - Bounds the complete input before parsing.
/// - Reads integer fields in network byte order.
/// - Rejects unsupported wire identifiers.
/// - Rejects malformed or noncanonical publisher IDs.
/// - Rejects truncated and trailing data.
/// - Never authenticates a release or publisher.
///
/// Release-signature verification, publisher authority,
/// authenticated distribution, rollback policy and
/// provider authorization are outside this type.
/// </summary>
public sealed class TruthDoctorV3CanonicalReleaseMetadataDecoder
{
    public const int MaximumCanonicalBodyBytes = 128;

    public TruthDoctorV3CanonicalReleaseMetadata Decode(
        byte[] canonicalBytes)
    {
        ArgumentNullException.ThrowIfNull(canonicalBytes);

        if (canonicalBytes.Length > MaximumCanonicalBodyBytes)
        {
            throw new FormatException(
                "Canonical release metadata exceeds maximum size.");
        }

        ReadOnlySpan<byte> body = canonicalBytes;
        int cursor = 0;

        ReadOnlySpan<byte> domain =
            "TRUTHDOCTOR-RELEASE-V1\0"u8;

        if (!Take(body, ref cursor, domain.Length)
            .SequenceEqual(domain))
        {
            throw new FormatException(
                "Incorrect canonical release metadata domain.");
        }

        ushort schemaVersion = ReadUInt16(body, ref cursor);

        if (schemaVersion != 1)
        {
            throw new FormatException(
                "Unsupported canonical release metadata version.");
        }

        ReadOnlySpan<byte> expectedProduct = "TruthDoctor"u8;

        byte productLength = ReadByte(body, ref cursor);

        if (productLength != expectedProduct.Length)
        {
            throw new FormatException(
                "Incorrect canonical product length.");
        }

        if (!Take(body, ref cursor, productLength)
            .SequenceEqual(expectedProduct))
        {
            throw new FormatException(
                "Incorrect canonical product identity.");
        }

        byte targetCode = ReadByte(body, ref cursor);

        string runtimeTarget = targetCode switch
        {
            1 => "osx-arm64",
            2 => "osx-x64",
            3 => "win-x64",
            4 => "linux-x64",
            _ => throw new FormatException(
                "Unsupported canonical runtime target.")
        };

        byte formatCode = ReadByte(body, ref cursor);

        string artifactFormat = formatCode switch
        {
            1 => "application-bundle",
            2 => "installer",
            3 => "signed-package",
            4 => "archive",
            _ => throw new FormatException(
                "Unsupported canonical artifact format.")
        };

        ushort major = ReadUInt16(body, ref cursor);
        ushort minor = ReadUInt16(body, ref cursor);
        ushort patch = ReadUInt16(body, ref cursor);

        ulong sequence = ReadUInt64(body, ref cursor);

        if (sequence == 0)
        {
            throw new FormatException(
                "Canonical release sequence must be nonzero.");
        }

        ReadOnlySpan<byte> artifactDigest =
            Take(body, ref cursor, 32);

        byte publisherIdLength = ReadByte(body, ref cursor);

        if (publisherIdLength is < 1 or > 32)
        {
            throw new FormatException(
                "Invalid canonical publisher ID length.");
        }

        ReadOnlySpan<byte> publisherBytes =
            Take(body, ref cursor, publisherIdLength);

        ValidatePublisherIdentifier(publisherBytes);

        string publisherId =
            Encoding.ASCII.GetString(publisherBytes);

        byte signatureAlgorithmCode =
            ReadByte(body, ref cursor);

        string signatureAlgorithm =
            signatureAlgorithmCode switch
            {
                1 => "ECDSA-P256-SHA256",
                _ => throw new FormatException(
                    "Unsupported canonical signature algorithm.")
            };

        if (cursor != body.Length)
        {
            throw new FormatException(
                "Trailing canonical release metadata bytes.");
        }

        return new TruthDoctorV3CanonicalReleaseMetadata(
            "TruthDoctor",
            runtimeTarget,
            artifactFormat,
            major,
            minor,
            patch,
            sequence,
            Convert.ToHexString(artifactDigest),
            publisherId,
            signatureAlgorithm);
    }

    private static ReadOnlySpan<byte> Take(
        ReadOnlySpan<byte> body,
        ref int cursor,
        int count)
    {
        if (count < 0 ||
            cursor < 0 ||
            cursor > body.Length ||
            count > body.Length - cursor)
        {
            throw new FormatException(
                "Truncated canonical release metadata.");
        }

        ReadOnlySpan<byte> value =
            body.Slice(cursor, count);

        cursor += count;

        return value;
    }

    private static byte ReadByte(
        ReadOnlySpan<byte> body,
        ref int cursor)
    {
        return Take(body, ref cursor, 1)[0];
    }

    private static ushort ReadUInt16(
        ReadOnlySpan<byte> body,
        ref int cursor)
    {
        return BinaryPrimitives.ReadUInt16BigEndian(
            Take(body, ref cursor, 2));
    }

    private static ulong ReadUInt64(
        ReadOnlySpan<byte> body,
        ref int cursor)
    {
        return BinaryPrimitives.ReadUInt64BigEndian(
            Take(body, ref cursor, 8));
    }

    private static void ValidatePublisherIdentifier(
        ReadOnlySpan<byte> publisher)
    {
        if (publisher.Length is < 1 or > 32)
        {
            throw new FormatException(
                "Invalid canonical publisher identifier.");
        }

        for (int index = 0; index < publisher.Length; index++)
        {
            byte value = publisher[index];

            bool isLetterOrDigit =
                (value >= (byte)'A' && value <= (byte)'Z') ||
                (value >= (byte)'a' && value <= (byte)'z') ||
                (value >= (byte)'0' && value <= (byte)'9');

            bool isSeparator =
                value == (byte)'.' ||
                value == (byte)'_' ||
                value == (byte)'-';

            if (!isLetterOrDigit &&
                (index == 0 || !isSeparator))
            {
                throw new FormatException(
                    "Noncanonical publisher identifier.");
            }
        }
    }
}
