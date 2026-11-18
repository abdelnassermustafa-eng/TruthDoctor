using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TruthDoctor.Services.Providers;

/// <summary>
/// Produces deterministic, unsigned policy-body bytes
/// according to the proposed TruthDoctor policy v1 format.
///
/// SECURITY BOUNDARY:
/// This class performs canonical encoding only.
///
/// It does not sign policies, authenticate policy roots,
/// establish publisher trust, validate policy freshness,
/// prevent rollback, approve releases, or authorize
/// installation or execution of provider packages.
/// </summary>
public sealed class ProviderPackageCanonicalPolicyBodyEncoder
{
    private static readonly byte[] Domain = Encoding.ASCII.GetBytes(
        "TRUTHDOCTOR-POLICY-V1\0");

    private const ushort SchemaVersion = 1;
    private const int MaximumIdentifierLength = 64;
    private const int MaximumFingerprints = 1024;
    private const int FingerprintHexLength = 64;

    /// <summary>
    /// Creates the exact unsigned body whose bytes would
    /// be covered by a future authenticated policy signature.
    ///
    /// This method does not generate or verify signatures.
    /// </summary>
    public byte[] Encode(
        string policyId,
        string authorityId,
        string rootKeyId,
        ulong sequence,
        long effectiveAt,
        long expiresAt,
        IEnumerable<string> listed,
        IEnumerable<string> revoked)
    {
        ArgumentNullException.ThrowIfNull(policyId);
        ArgumentNullException.ThrowIfNull(authorityId);
        ArgumentNullException.ThrowIfNull(rootKeyId);
        ArgumentNullException.ThrowIfNull(listed);
        ArgumentNullException.ThrowIfNull(revoked);

        if (sequence == 0)
        {
            throw new ArgumentException(
                "Policy sequence must be greater than zero.",
                nameof(sequence));
        }

        if (expiresAt <= effectiveAt)
        {
            throw new ArgumentException(
                "Policy expiry must be later than effective time.",
                nameof(expiresAt));
        }

        // Validate and snapshot all collections before
        // producing any output bytes.
        var policyBytes = ValidateIdentifier(
            policyId,
            nameof(policyId));

        var authorityBytes = ValidateIdentifier(
            authorityId,
            nameof(authorityId));

        var rootKeyBytes = ValidateIdentifier(
            rootKeyId,
            nameof(rootKeyId));

        var listedBytes = CanonicalizeFingerprints(
            listed,
            nameof(listed));

        var revokedBytes = CanonicalizeFingerprints(
            revoked,
            nameof(revoked));

        using var output = new MemoryStream();

        output.Write(Domain);

        WriteUInt16(output, SchemaVersion);

        WriteIdentifier(output, policyBytes);
        WriteIdentifier(output, authorityBytes);
        WriteIdentifier(output, rootKeyBytes);

        WriteUInt64(output, sequence);
        WriteInt64(output, effectiveAt);
        WriteInt64(output, expiresAt);

        WriteFingerprints(output, listedBytes);
        WriteFingerprints(output, revokedBytes);

        return output.ToArray();
    }

    private static byte[] ValidateIdentifier(
        string value,
        string parameterName)
    {
        if (value.Length == 0 ||
            value.Length > MaximumIdentifierLength)
        {
            throw new ArgumentException(
                "Policy identifiers must contain 1 to " +
                "64 ASCII characters.",
                parameterName);
        }

        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];

            var uppercase =
                character >= 'A' && character <= 'Z';

            var lowercase =
                character >= 'a' && character <= 'z';

            var digit =
                character >= '0' && character <= '9';

            var separator =
                character == '.' ||
                character == '_' ||
                character == '-';

            var valid =
                uppercase || lowercase || digit ||
                (index > 0 && separator);

            if (!valid)
            {
                throw new ArgumentException(
                    "Policy identifier contains an invalid " +
                    "or noncanonical character.",
                    parameterName);
            }
        }

        return Encoding.ASCII.GetBytes(value);
    }

    private static List<byte[]> CanonicalizeFingerprints(
        IEnumerable<string> values,
        string parameterName)
    {
        var fingerprints = new List<byte[]>();

        var unique = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var fingerprint in values)
        {
            if (fingerprints.Count >= MaximumFingerprints)
            {
                throw new ArgumentException(
                    "A policy fingerprint collection cannot " +
                    "contain more than 1024 entries.",
                    parameterName);
            }

            ValidateFingerprint(
                fingerprint,
                parameterName);

            if (!unique.Add(fingerprint))
            {
                throw new ArgumentException(
                    "Duplicate SHA-256 fingerprints are " +
                    "not permitted within a collection.",
                    parameterName);
            }

            fingerprints.Add(
                Convert.FromHexString(fingerprint));
        }

        // Sort decoded 32-byte fingerprints, not their
        // original hexadecimal text representation.
        fingerprints.Sort(
            (left, right) =>
                left.AsSpan().SequenceCompareTo(
                    right.AsSpan()));

        return fingerprints;
    }

    private static void ValidateFingerprint(
        string fingerprint,
        string parameterName)
    {
        if (fingerprint is null ||
            fingerprint.Length != FingerprintHexLength)
        {
            throw new ArgumentException(
                "Each fingerprint must contain exactly " +
                "64 ASCII hexadecimal characters.",
                parameterName);
        }

        foreach (var character in fingerprint)
        {
            var digit =
                character >= '0' && character <= '9';

            var lowerHex =
                character >= 'a' && character <= 'f';

            var upperHex =
                character >= 'A' && character <= 'F';

            if (!digit && !lowerHex && !upperHex)
            {
                throw new ArgumentException(
                    "A fingerprint contains an invalid " +
                    "hexadecimal character.",
                    parameterName);
            }
        }
    }

    private static void WriteIdentifier(
        Stream output,
        byte[] identifierBytes)
    {
        WriteUInt16(
            output,
            checked((ushort)identifierBytes.Length));

        output.Write(identifierBytes);
    }

    private static void WriteFingerprints(
        Stream output,
        List<byte[]> fingerprints)
    {
        WriteUInt16(
            output,
            checked((ushort)fingerprints.Count));

        foreach (var fingerprint in fingerprints)
        {
            output.Write(fingerprint);
        }
    }

    private static void WriteUInt16(
        Stream output,
        ushort value)
    {
        Span<byte> buffer = stackalloc byte[2];

        BinaryPrimitives.WriteUInt16BigEndian(
            buffer,
            value);

        output.Write(buffer);
    }

    private static void WriteUInt64(
        Stream output,
        ulong value)
    {
        Span<byte> buffer = stackalloc byte[8];

        BinaryPrimitives.WriteUInt64BigEndian(
            buffer,
            value);

        output.Write(buffer);
    }

    private static void WriteInt64(
        Stream output,
        long value)
    {
        Span<byte> buffer = stackalloc byte[8];

        BinaryPrimitives.WriteInt64BigEndian(
            buffer,
            value);

        output.Write(buffer);
    }
}
