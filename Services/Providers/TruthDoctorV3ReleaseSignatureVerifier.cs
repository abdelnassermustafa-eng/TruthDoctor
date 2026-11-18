using System;
using System.Numerics;
using System.Security.Cryptography;

namespace TruthDoctor.Services.Providers;

/// <summary>
/// Strict cryptographic signature verification for
/// canonical TruthDoctor V3 release metadata.
///
/// A successful result proves mathematical signature
/// consistency under the caller-provided public key.
///
/// It does not establish publisher identity, release
/// authorization, trusted-root provenance, installer
/// authenticity, rollback safety or provider approval.
/// </summary>
public sealed class TruthDoctorV3ReleaseSignatureVerifier
{
    public const int SignatureLengthBytes = 64;

    // Order of the NIST P-256 base point.
    // Defined by the curve specification; never derived
    // from caller-controlled signature or key values.
    private static readonly BigInteger P256Order =
        new BigInteger(
            Convert.FromHexString(
                "FFFFFFFF00000000FFFFFFFFFFFFFFFF" +
                "BCE6FAADA7179E84F3B9CAC2FC632551"),
            isUnsigned: true,
            isBigEndian: true);

    private static readonly BigInteger P256HalfOrder =
        P256Order >> 1;

    private const string P256CurveOid =
        "1.2.840.10045.3.1.7";

    /// <summary>
    /// Returns true only when canonical metadata is
    /// signed by the private key corresponding to the
    /// supplied public key.
    ///
    /// This result is never a release-authorization
    /// or trusted-publisher decision.
    ///
    /// Null inputs throw ArgumentNullException.
    /// Malformed metadata throws FormatException.
    /// Malformed keys or noncanonical signatures
    /// throw ArgumentException.
    /// Invalid mathematical signatures return false.
    /// </summary>
    public bool Verify(
        byte[] canonicalMetadata,
        byte[] signatureP1363,
        byte[] publisherPublicKeySpki)
    {
        ArgumentNullException.ThrowIfNull(canonicalMetadata);
        ArgumentNullException.ThrowIfNull(signatureP1363);
        ArgumentNullException.ThrowIfNull(publisherPublicKeySpki);

        // This is structural validation only. The
        // decoder's IsTrusted and IsAuthorized results
        // remain permanently false.
        //
        // Invalid canonical metadata must never reach
        // cryptographic signature verification.
        var metadata =
            new TruthDoctorV3CanonicalReleaseMetadataDecoder()
                .Decode(canonicalMetadata);

        if (metadata.IsTrusted || metadata.IsAuthorized)
        {
            throw new InvalidOperationException(
                "Canonical metadata must remain non-authorizing.");
        }

        if (!string.Equals(
                metadata.SignatureAlgorithm,
                "ECDSA-P256-SHA256",
                StringComparison.Ordinal))
        {
            throw new FormatException(
                "Unsupported canonical signature algorithm.");
        }

        if (signatureP1363.Length != SignatureLengthBytes)
        {
            throw new ArgumentException(
                "P-256 signature must contain exactly 64 bytes.",
                nameof(signatureP1363));
        }

        if (publisherPublicKeySpki.Length == 0)
        {
            throw new ArgumentException(
                "Publisher public key cannot be empty.",
                nameof(publisherPublicKeySpki));
        }

        // P1363 r and s are unsigned, big-endian,
        // fixed-width 32-byte integers.
        ReadOnlySpan<byte> signature = signatureP1363;

        BigInteger r = new BigInteger(
            signature[..32],
            isUnsigned: true,
            isBigEndian: true);

        BigInteger s = new BigInteger(
            signature[32..],
            isUnsigned: true,
            isBigEndian: true);

        // Mathematically invalid signatures produce
        // false, never authentication or authorization.
        if (r < BigInteger.One || r >= P256Order ||
            s < BigInteger.One || s >= P256Order)
        {
            return false;
        }

        // Canonical low-S encoding prevents accepting
        // the high-S alternate representation of an
        // otherwise mathematically valid ECDSA signature.
        if (s > P256HalfOrder)
        {
            throw new ArgumentException(
                "High-S ECDSA signatures are noncanonical.",
                nameof(signatureP1363));
        }

        using ECDsa key = ECDsa.Create();

        int bytesRead;

        try
        {
            key.ImportSubjectPublicKeyInfo(
                publisherPublicKeySpki,
                out bytesRead);
        }
        catch (CryptographicException ex)
        {
            throw new ArgumentException(
                "Invalid DER SubjectPublicKeyInfo public key.",
                nameof(publisherPublicKeySpki),
                ex);
        }

        // No trailing SPKI data may be accepted.
        if (bytesRead != publisherPublicKeySpki.Length)
        {
            throw new ArgumentException(
                "Trailing data in public-key representation.",
                nameof(publisherPublicKeySpki));
        }

        ECParameters parameters;

        try
        {
            parameters = key.ExportParameters(false);
        }
        catch (CryptographicException ex)
        {
            throw new ArgumentException(
                "Public key parameters are invalid.",
                nameof(publisherPublicKeySpki),
                ex);
        }

        if (key.KeySize != 256 ||
            !string.Equals(
                parameters.Curve.Oid.Value,
                P256CurveOid,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Only named NIST P-256 public keys are supported.",
                nameof(publisherPublicKeySpki));
        }

        // Require canonical DER SPKI encoding:
        // importing and re-exporting must not change
        // the supplied public-key representation.
        byte[] canonicalSpki;

        try
        {
            canonicalSpki = key.ExportSubjectPublicKeyInfo();
        }
        catch (CryptographicException ex)
        {
            throw new ArgumentException(
                "Public key cannot be canonically exported.",
                nameof(publisherPublicKeySpki),
                ex);
        }

        if (!canonicalSpki.AsSpan().SequenceEqual(
                publisherPublicKeySpki))
        {
            throw new ArgumentException(
                "Noncanonical DER public-key representation.",
                nameof(publisherPublicKeySpki));
        }

        // Verify the exact canonical byte sequence,
        // not the decoded metadata object, JSON,
        // or the artifact digest alone.
        //
        // No private keys or signing operations
        // are used by this component.
        return key.VerifyData(
            canonicalMetadata,
            signatureP1363,
            HashAlgorithmName.SHA256,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
    }
}
