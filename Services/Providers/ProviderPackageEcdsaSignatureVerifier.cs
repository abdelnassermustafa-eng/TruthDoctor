using System;
using System.Security.Cryptography;

namespace TruthDoctor.Services.Providers;

/// <summary>
/// Verifies ECDSA P-256 / SHA-256 signatures over exact
/// caller-supplied bytes.
///
/// Public keys must use DER-encoded X.509 SubjectPublicKeyInfo.
/// Signatures must use 64-byte IEEE P1363 (r || s) encoding.
///
/// This verifier checks cryptographic validity only.
/// It does not authenticate public-key provenance, authorize
/// publishers, approve releases, or permit installation.
///
/// Trusted key policy, signed envelope canonicalization,
/// package artifact integrity, and release authorization
/// are separate security requirements.
/// </summary>
public sealed class ProviderPackageEcdsaSignatureVerifier
{
    private const string P256CurveOid =
        "1.2.840.10045.3.1.7";

    private const int P256SignatureLength = 64;

    /// <summary>
    /// Returns true only for a valid ECDSA P-256 / SHA-256
    /// signature over the supplied payload.
    ///
    /// Invalid signatures return false.
    /// Malformed public keys are rejected explicitly.
    /// Null inputs are rejected explicitly.
    /// </summary>
    public bool Verify(
        byte[] payload,
        byte[] signature,
        byte[] publicKeyDer)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(signature);
        ArgumentNullException.ThrowIfNull(publicKeyDer);

        if (signature.Length != P256SignatureLength)
        {
            return false;
        }

        using var verifier = ECDsa.Create();

        int bytesRead;

        try
        {
            verifier.ImportSubjectPublicKeyInfo(
                publicKeyDer,
                out bytesRead);
        }
        catch (CryptographicException exception)
        {
            throw new ArgumentException(
                "The public key is not a valid DER-encoded " +
                "ECDSA SubjectPublicKeyInfo key.",
                nameof(publicKeyDer),
                exception);
        }

        if (bytesRead != publicKeyDer.Length)
        {
            throw new ArgumentException(
                "The public key contains unexpected trailing data.",
                nameof(publicKeyDer));
        }

        ECParameters parameters;

        try
        {
            parameters = verifier.ExportParameters(
                includePrivateParameters: false);
        }
        catch (CryptographicException exception)
        {
            throw new ArgumentException(
                "The imported public key cannot be inspected.",
                nameof(publicKeyDer),
                exception);
        }

        if (verifier.KeySize != 256 ||
            !string.Equals(
                parameters.Curve.Oid.Value,
                P256CurveOid,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Only ECDSA NIST P-256 public keys are supported.",
                nameof(publicKeyDer));
        }

        return verifier.VerifyData(
            payload,
            signature,
            HashAlgorithmName.SHA256,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
    }
}
