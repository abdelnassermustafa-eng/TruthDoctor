using System;

namespace TruthDoctor.Services.Providers;

/// <summary>
/// Initial fail-closed trust boundary for provider packages.
///
/// No package is currently cryptographically verified.
///
/// This gate deliberately has no:
/// - trust-state setter;
/// - caller-supplied approval flag;
/// - mutable allowlist;
/// - package-signature bypass;
/// - installation or executable loading operation.
///
/// A manifest, registry entry, valid dependency graph,
/// or compatible host version does not establish trust.
///
/// This class does not perform cryptographic verification.
/// A future verified-artifact mechanism requires a separate,
/// explicitly reviewed contract and implementation.
/// </summary>
public sealed class ProviderPackageTrustGate
{
    /// <summary>
    /// Returns false for every non-null package manifest.
    ///
    /// This is a denial decision, not a cryptographic
    /// verification result.
    /// </summary>
    public bool IsTrusted(ProviderPackageManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        return false;
    }

    /// <summary>
    /// Rejects every package until independent
    /// cryptographic verification is implemented.
    ///
    /// This method does not install or load a package.
    /// </summary>
    public void RequireTrusted(ProviderPackageManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        throw new InvalidOperationException(
            "Provider package trust has not been independently " +
            "verified. Package installation is not authorized.");
    }
}
