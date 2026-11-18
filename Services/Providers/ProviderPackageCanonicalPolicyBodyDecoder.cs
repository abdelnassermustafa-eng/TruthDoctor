using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace TruthDoctor.Services.Providers;

/// <summary>
/// A defensively copied structural representation of
/// an unsigned canonical policy body.
///
/// This is parsed data, not an authenticated policy.
/// It conveys no publisher trust or installation authority.
/// </summary>
public sealed class ProviderPackageCanonicalPolicyBody
{
    public string PolicyId { get; }
    public string AuthorityId { get; }
    public string RootKeyId { get; }

    public ulong Sequence { get; }
    public long EffectiveAt { get; }
    public long ExpiresAt { get; }

    public IReadOnlyList<string> ListedFingerprints { get; }
    public IReadOnlyList<string> RevokedFingerprints { get; }

    internal ProviderPackageCanonicalPolicyBody(
        string policyId,
        string authorityId,
        string rootKeyId,
        ulong sequence,
        long effectiveAt,
        long expiresAt,
        IEnumerable<string> listed,
        IEnumerable<string> revoked)
    {
        PolicyId = policyId;
        AuthorityId = authorityId;
        RootKeyId = rootKeyId;

        Sequence = sequence;
        EffectiveAt = effectiveAt;
        ExpiresAt = expiresAt;

        ListedFingerprints = Array.AsReadOnly(
            new List<string>(listed).ToArray());

        RevokedFingerprints = Array.AsReadOnly(
            new List<string>(revoked).ToArray());
    }
}

/// <summary>
/// Strictly decodes the proposed TruthDoctor unsigned
/// canonical policy body v1.
///
/// Only one byte representation is accepted for each
/// structurally valid policy.
///
/// SECURITY BOUNDARY:
/// Decoding does not authenticate a policy signature,
/// identify an authorized policy authority, enforce
/// trusted time or anti-rollback state, approve releases,
/// or authorize executable provider installation.
///
/// The existing ProviderPackageTrustGate remains deny-all.
/// </summary>
public sealed class ProviderPackageCanonicalPolicyBodyDecoder
{
    private static readonly byte[] Domain =
        Encoding.ASCII.GetBytes(
            "TRUTHDOCTOR-POLICY-V1\0");

    private const ushort SchemaVersion = 1;
    private const int MaximumIdentifierLength = 64;
    private const int MaximumFingerprints = 1024;
    private const int FingerprintByteLength = 32;

    // Maximum P36 canonical body size:
    // 22 domain + 2 version + 3 * (2 + 64) identifiers
    // + 8 sequence + 16 timestamps + 4 list counts
    // + 2048 * 32 fingerprint bytes.
    private const int MaximumCanonicalBodyBytes = 65786;

    /// <summary>
    /// Decodes exact, bounded, canonical unsigned bytes.
    ///
    /// Null input raises ArgumentNullException.
    /// All malformed non-null input raises ArgumentException.
    /// </summary>
    public ProviderPackageCanonicalPolicyBody Decode(
        byte[] body)
    {
        ArgumentNullException.ThrowIfNull(body);

        if (body.Length > MaximumCanonicalBodyBytes)
        {
            throw new ArgumentException(
                "Canonical policy body exceeds its maximum size.",
                nameof(body));
        }

        var reader = new Reader(body);

        var actualDomain = reader.Take(Domain.Length);

        if (!actualDomain.SequenceEqual(Domain))
        {
            throw new ArgumentException(
                "Canonical policy domain is invalid.",
                nameof(body));
        }

        if (reader.ReadUInt16() != SchemaVersion)
        {
            throw new ArgumentException(
                "Canonical policy schema version is unsupported.",
                nameof(body));
        }

        var policyId = reader.ReadIdentifier();
        var authorityId = reader.ReadIdentifier();
        var rootKeyId = reader.ReadIdentifier();

        var sequence = reader.ReadUInt64();

        if (sequence == 0)
        {
            throw new ArgumentException(
                "Canonical policy sequence cannot be zero.",
                nameof(body));
        }

        var effectiveAt = reader.ReadInt64();
        var expiresAt = reader.ReadInt64();

        if (expiresAt <= effectiveAt)
        {
            throw new ArgumentException(
                "Canonical policy validity interval is invalid.",
                nameof(body));
        }

        var listed = reader.ReadFingerprints();
        var revoked = reader.ReadFingerprints();

        reader.RequireEnd();

        return new ProviderPackageCanonicalPolicyBody(
            policyId,
            authorityId,
            rootKeyId,
            sequence,
            effectiveAt,
            expiresAt,
            listed,
            revoked);
    }

    /// <summary>
    /// Reader with bounded access and no implicit
    /// padding, normalization, or trailing-data tolerance.
    /// </summary>
    private sealed class Reader
    {
        private readonly byte[] _body;
        private int _position;

        public Reader(byte[] body)
        {
            _body = body;
        }

        public ReadOnlySpan<byte> Take(int length)
        {
            if (length < 0 ||
                length > _body.Length - _position)
            {
                throw new ArgumentException(
                    "Canonical policy body is truncated.",
                    "body");
            }

            var result = _body.AsSpan(
                _position,
                length);

            _position += length;
            return result;
        }

        public ushort ReadUInt16()
        {
            return BinaryPrimitives.ReadUInt16BigEndian(
                Take(2));
        }

        public ulong ReadUInt64()
        {
            return BinaryPrimitives.ReadUInt64BigEndian(
                Take(8));
        }

        public long ReadInt64()
        {
            return BinaryPrimitives.ReadInt64BigEndian(
                Take(8));
        }

        public string ReadIdentifier()
        {
            var length = ReadUInt16();

            if (length == 0 ||
                length > MaximumIdentifierLength)
            {
                throw new ArgumentException(
                    "Canonical policy identifier length is invalid.",
                    "body");
            }

            var bytes = Take(length);

            for (var index = 0; index < bytes.Length; index++)
            {
                var character = bytes[index];

                var uppercase =
                    character >= (byte)'A' &&
                    character <= (byte)'Z';

                var lowercase =
                    character >= (byte)'a' &&
                    character <= (byte)'z';

                var digit =
                    character >= (byte)'0' &&
                    character <= (byte)'9';

                var separator =
                    character == (byte)'.' ||
                    character == (byte)'_' ||
                    character == (byte)'-';

                if (!(uppercase ||
                      lowercase ||
                      digit ||
                      (index > 0 && separator)))
                {
                    throw new ArgumentException(
                        "Canonical policy identifier contains " +
                        "an invalid ASCII character.",
                        "body");
                }
            }

            return Encoding.ASCII.GetString(bytes);
        }

        public string[] ReadFingerprints()
        {
            var count = ReadUInt16();

            if (count > MaximumFingerprints)
            {
                throw new ArgumentException(
                    "Canonical policy contains too many fingerprints.",
                    "body");
            }

            var fingerprints = new string[count];

            ReadOnlySpan<byte> previous = default;

            for (var index = 0; index < count; index++)
            {
                var current = Take(FingerprintByteLength);

                if (index > 0 &&
                    previous.SequenceCompareTo(current) >= 0)
                {
                    throw new ArgumentException(
                        "Canonical fingerprints must be " +
                        "strictly increasing without duplicates.",
                        "body");
                }

                fingerprints[index] =
                    Convert.ToHexString(current)
                        .ToLowerInvariant();

                previous = current;
            }

            return fingerprints;
        }

        public void RequireEnd()
        {
            if (_position != _body.Length)
            {
                throw new ArgumentException(
                    "Canonical policy contains unexpected " +
                    "trailing bytes.",
                    "body");
            }
        }
    }
}
