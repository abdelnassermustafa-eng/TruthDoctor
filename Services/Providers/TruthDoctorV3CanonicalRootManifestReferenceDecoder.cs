using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace TruthDoctor.Services.Providers;

/// <summary>
/// Provisional canonical root-manifest STRUCTURAL reference
/// decoder.
///
/// Parsing or re-encoding success does not authenticate
/// a publisher, a root, a signature, or an artifact.
///
/// No trust roots are provisioned and no installation
/// or release authorization is provided by this type.
/// </summary>
public sealed class TruthDoctorV3CanonicalRootManifestReferenceDecoder
{
    private const int FieldCount = 12;
    private const int MaxWireLength = 8192;
    private const int MaxPayloadLength = 4096;

    private static readonly byte[] Domain =
        Encoding.ASCII.GetBytes(
            "TRUTHDOCTOR\0ROOT-PROVENANCE\0V1\0");

    private static readonly byte[] FieldTypes =
    {
        1, 1, 1, 2, 1, 1, 3, 4, 1, 1, 5, 5
    };

    private sealed record KeyReference(
        string KeyId,
        string SpkiSha256);

    /// <summary>
    /// Returns true only for structurally canonical bytes.
    /// Caller bytes are not modified or retained.
    /// </summary>
    public bool IsCanonicalReference(byte[] wire)
    {
        ArgumentNullException.ThrowIfNull(wire);

        try
        {
            object[] values = DecodeSemantics(wire);
            byte[] canonical = EncodeSemantics(values);

            return wire.AsSpan().SequenceEqual(
                canonical.AsSpan());
        }
        catch (FormatException)
        {
            return false;
        }
        catch (OverflowException)
        {
            return false;
        }
    }

    private static object[] DecodeSemantics(byte[] wire)
    {
        Require(
            wire.Length >= Domain.Length &&
            wire.Length <= MaxWireLength);

        Require(
            wire.AsSpan(0, Domain.Length)
                .SequenceEqual(Domain.AsSpan()));

        object[] values = new object[FieldCount];
        int offset = Domain.Length;

        for (int index = 0; index < FieldCount; index++)
        {
            Require(wire.Length - offset >= 6);

            byte tag = wire[offset];
            byte kind = wire[offset + 1];

            uint declaredLength =
                BinaryPrimitives.ReadUInt32BigEndian(
                    wire.AsSpan(offset + 2, 4));

            offset += 6;

            Require(tag == index + 1);
            Require(kind == FieldTypes[index]);
            Require(declaredLength <= MaxPayloadLength);
            Require(declaredLength <= wire.Length - offset);

            int length = checked((int)declaredLength);

            byte[] payload =
                wire.AsSpan(offset, length).ToArray();

            offset += length;

            values[index] = kind switch
            {
                1 => DecodeString(payload),
                2 => DecodeUnsigned64(payload),
                3 => DecodeKeys(payload),
                4 => DecodeUnsigned16(payload),
                5 => DecodeOptionalDigest(payload)!,
                _ => throw new FormatException(
                    "Unknown root-manifest type.")
            };
        }

        Require(offset == wire.Length);

        ValidateSemantics(values);

        return values;
    }

    private static string DecodeString(byte[] payload)
    {
        Require(payload.Length >= 1);
        Require(payload.Length <= 128);

        foreach (byte value in payload)
        {
            Require(value >= 32 && value <= 126);
        }

        return Encoding.ASCII.GetString(payload);
    }

    private static ulong DecodeUnsigned64(byte[] payload)
    {
        Require(payload.Length == 8);

        return BinaryPrimitives.ReadUInt64BigEndian(
            payload);
    }

    private static ushort DecodeUnsigned16(byte[] payload)
    {
        Require(payload.Length == 2);

        return BinaryPrimitives.ReadUInt16BigEndian(
            payload);
    }

    private static List<KeyReference> DecodeKeys(
        byte[] payload)
    {
        Require(payload.Length >= 1);

        int count = payload[0];

        Require(count >= 1 && count <= 8);

        var keys = new List<KeyReference>(count);
        int offset = 1;

        for (int index = 0; index < count; index++)
        {
            Require(payload.Length - offset >= 2);

            int nameLength =
                BinaryPrimitives.ReadUInt16BigEndian(
                    payload.AsSpan(offset, 2));

            offset += 2;

            Require(
                nameLength >= 1 && nameLength <= 64);

            Require(
                payload.Length - offset >= nameLength + 32);

            byte[] nameBytes =
                payload.AsSpan(offset, nameLength).ToArray();

            offset += nameLength;

            string name = DecodeString(nameBytes);

            string fingerprint = Convert.ToHexString(
                payload.AsSpan(offset, 32));

            offset += 32;

            keys.Add(new KeyReference(
                name, fingerprint));
        }

        Require(offset == payload.Length);

        return keys;
    }

    private static string? DecodeOptionalDigest(
        byte[] payload)
    {
        if (payload.Length == 1 && payload[0] == 0)
        {
            return null;
        }

        Require(
            payload.Length == 33 &&
            payload[0] == 1);

        return Convert.ToHexString(
            payload.AsSpan(1, 32));
    }

    private static void ValidateSemantics(
        object[] values)
    {
        Require(values.Length == FieldCount);

        Require(
            (string)values[0] ==
            "truthdoctor.root-provenance.reference.v1");

        Require((string)values[1] == "TruthDoctor");

        Require(IsIdentifier((string)values[2]));

        Require((ulong)values[3] >= 1);

        Require((string)values[4] == "root");

        Require(
            (string)values[5] ==
            "ECDSA-P256-SHA256");

        var keys =
            (List<KeyReference>)values[6];

        Require(keys.Count >= 1 && keys.Count <= 8);

        var fingerprints = new HashSet<string>(
            StringComparer.Ordinal);

        string? priorId = null;

        foreach (KeyReference key in keys)
        {
            Require(IsIdentifier(key.KeyId));

            if (priorId is not null)
            {
                Require(
                    string.CompareOrdinal(
                        priorId, key.KeyId) < 0);
            }

            Require(IsUppercaseSha256(
                key.SpkiSha256));

            Require(
                fingerprints.Add(
                    key.SpkiSha256));

            priorId = key.KeyId;
        }

        int threshold = (ushort)values[7];

        Require(
            threshold >= 1 &&
            threshold <= keys.Count);

        string expiresUtc = (string)values[8];

        Require(expiresUtc.Length == 20);
        Require(expiresUtc[4] == '-');
        Require(expiresUtc[7] == '-');
        Require(expiresUtc[10] == 'T');
        Require(expiresUtc[13] == ':');
        Require(expiresUtc[16] == ':');
        Require(expiresUtc[19] == 'Z');

        Require(
            DateTime.TryParseExact(
                expiresUtc,
                "yyyy-MM-dd'T'HH:mm:ss'Z'",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _));

        string mode = (string)values[9];
        string? predecessor =
            values[10] as string;

        if (mode == "initial")
        {
            Require(predecessor is null);
        }
        else if (mode == "rotation")
        {
            Require(predecessor is not null);
            Require((ulong)values[3] > 1);
        }
        else
        {
            throw new FormatException(
                "Invalid root rotation mode.");
        }

        if (predecessor is not null)
        {
            Require(IsUppercaseSha256(predecessor));
        }

        Require(
            values[11] is string revocation &&
            IsUppercaseSha256(revocation));
    }

    private static bool IsIdentifier(string value)
    {
        if (value.Length < 1 || value.Length > 64)
        {
            return false;
        }

        if (value[0] < 'a' || value[0] > 'z')
        {
            return false;
        }

        foreach (char character in value)
        {
            bool lower =
                character >= 'a' && character <= 'z';

            bool digit =
                character >= '0' && character <= '9';

            if (!lower && !digit && character != '-')
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsUppercaseSha256(string value)
    {
        if (value.Length != 64)
        {
            return false;
        }

        foreach (char character in value)
        {
            bool digit =
                character >= '0' && character <= '9';

            bool upper =
                character >= 'A' && character <= 'F';

            if (!digit && !upper)
            {
                return false;
            }
        }

        return true;
    }

    private static byte[] EncodeSemantics(
        object[] values)
    {
        ValidateSemantics(values);

        using var output = new MemoryStream();

        output.Write(Domain);

        for (int index = 0; index < FieldCount; index++)
        {
            byte kind = FieldTypes[index];

            byte[] payload = kind switch
            {
                1 => Encoding.ASCII.GetBytes(
                    (string)values[index]),

                2 => EncodeUnsigned64(
                    (ulong)values[index]),

                3 => EncodeKeys(
                    (List<KeyReference>)values[index]),

                4 => EncodeUnsigned16(
                    (ushort)values[index]),

                5 => EncodeOptionalDigest(
                    values[index] as string),

                _ => throw new FormatException(
                    "Invalid semantic field type.")
            };

            Require(payload.Length <= MaxPayloadLength);

            output.WriteByte(
                checked((byte)(index + 1)));

            output.WriteByte(kind);

            Span<byte> lengthBytes = stackalloc byte[4];

            BinaryPrimitives.WriteUInt32BigEndian(
                lengthBytes,
                checked((uint)payload.Length));

            output.Write(lengthBytes);
            output.Write(payload);
        }

        Require(output.Length <= MaxWireLength);

        return output.ToArray();
    }

    private static byte[] EncodeUnsigned64(ulong value)
    {
        byte[] bytes = new byte[8];

        BinaryPrimitives.WriteUInt64BigEndian(
            bytes, value);

        return bytes;
    }

    private static byte[] EncodeUnsigned16(ushort value)
    {
        byte[] bytes = new byte[2];

        BinaryPrimitives.WriteUInt16BigEndian(
            bytes, value);

        return bytes;
    }

    private static byte[] EncodeKeys(
        List<KeyReference> keys)
    {
        using var output = new MemoryStream();

        output.WriteByte(checked((byte)keys.Count));

        foreach (KeyReference key in keys)
        {
            byte[] name =
                Encoding.ASCII.GetBytes(key.KeyId);

            output.Write(
                EncodeUnsigned16(
                    checked((ushort)name.Length)));

            output.Write(name);

            output.Write(
                Convert.FromHexString(
                    key.SpkiSha256));
        }

        return output.ToArray();
    }

    private static byte[] EncodeOptionalDigest(
        string? digest)
    {
        if (digest is null)
        {
            return new byte[] { 0 };
        }

        byte[] result = new byte[33];
        result[0] = 1;

        Convert.FromHexString(digest).CopyTo(
            result, 1);

        return result;
    }

    private static void Require(bool condition)
    {
        if (!condition)
        {
            throw new FormatException(
                "Noncanonical root-manifest reference.");
        }
    }
}
