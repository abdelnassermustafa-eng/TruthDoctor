using System;
using System.Security.Cryptography;
using System.Text;
using TruthDoctor.Services.Providers;
using Xunit;

namespace TruthDoctor.Tests;

/// <summary>
/// Tests only ECDSA signature mathematics.
///
/// These tests do not establish:
/// - trusted publisher identity;
/// - trusted public-key provenance;
/// - package metadata binding;
/// - release approval;
/// - installation authorization.
///
/// All test keys are transient and generated in memory.
/// </summary>
public sealed class G327A6ProviderPackageEcdsaSignatureTests
{
    private static byte[] Payload()
    {
        return Encoding.UTF8.GetBytes(
            "truthdoctor-package-signature-test-v1");
    }

    private static byte[] Sign(
        ECDsa signer,
        byte[] payload)
    {
        return signer.SignData(
            payload,
            HashAlgorithmName.SHA256,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
    }

    [Fact]
    public void ValidP256Sha256SignatureVerifies()
    {
        using var signer =
            ECDsa.Create(ECCurve.NamedCurves.nistP256);

        var payload = Payload();
        var signature = Sign(signer, payload);
        var publicKey = signer.ExportSubjectPublicKeyInfo();

        var verifier =
            new ProviderPackageEcdsaSignatureVerifier();

        Assert.True(
            verifier.Verify(
                payload,
                signature,
                publicKey));
    }

    [Fact]
    public void TamperedPayloadIsRejected()
    {
        using var signer =
            ECDsa.Create(ECCurve.NamedCurves.nistP256);

        var signature = Sign(signer, Payload());
        var modifiedPayload =
            Encoding.UTF8.GetBytes("tampered-payload");

        var verifier =
            new ProviderPackageEcdsaSignatureVerifier();

        Assert.False(
            verifier.Verify(
                modifiedPayload,
                signature,
                signer.ExportSubjectPublicKeyInfo()));
    }

    [Fact]
    public void SignatureFromDifferentKeyIsRejected()
    {
        using var signer =
            ECDsa.Create(ECCurve.NamedCurves.nistP256);

        using var otherSigner =
            ECDsa.Create(ECCurve.NamedCurves.nistP256);

        var payload = Payload();
        var signature = Sign(signer, payload);

        var verifier =
            new ProviderPackageEcdsaSignatureVerifier();

        Assert.False(
            verifier.Verify(
                payload,
                signature,
                otherSigner.ExportSubjectPublicKeyInfo()));
    }

    [Fact]
    public void MalformedSignatureIsRejected()
    {
        using var signer =
            ECDsa.Create(ECCurve.NamedCurves.nistP256);

        var verifier =
            new ProviderPackageEcdsaSignatureVerifier();

        Assert.False(
            verifier.Verify(
                Payload(),
                new byte[] { 0x01, 0x02, 0x03 },
                signer.ExportSubjectPublicKeyInfo()));
    }

    [Fact]
    public void MalformedPublicKeyIsRejected()
    {
        using var signer =
            ECDsa.Create(ECCurve.NamedCurves.nistP256);

        var payload = Payload();
        var signature = Sign(signer, payload);

        var verifier =
            new ProviderPackageEcdsaSignatureVerifier();

        Assert.Throws<ArgumentException>(() =>
        {
            _ = verifier.Verify(
                payload,
                signature,
                new byte[] { 0x01, 0x02, 0x03 });
        });
    }

    [Fact]
    public void MissingPayloadIsRejected()
    {
        using var signer =
            ECDsa.Create(ECCurve.NamedCurves.nistP256);

        var payload = Payload();
        var signature = Sign(signer, payload);

        var verifier =
            new ProviderPackageEcdsaSignatureVerifier();

        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = verifier.Verify(
                null!,
                signature,
                signer.ExportSubjectPublicKeyInfo());
        });
    }
}
