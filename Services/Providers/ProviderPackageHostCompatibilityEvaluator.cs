using System;

namespace TruthDoctor.Services.Providers;

/// <summary>
/// Performs a declarative minimum-host-version comparison
/// for a provider package manifest.
///
/// Compatibility only means that the supplied host version
/// meets or exceeds the manifest's minimum host version.
///
/// A compatible result does not establish:
/// - package authenticity or cryptographic trust;
/// - package installation or dependency readiness;
/// - provider activation or execution authorization.
///
/// This evaluator performs no network, filesystem,
/// installation, plugin-loading, or provider operations.
/// </summary>
public sealed class ProviderPackageHostCompatibilityEvaluator
{
    /// <summary>
    /// Returns true when hostVersion is greater than or
    /// equal to manifest.MinimumHostVersion.
    ///
    /// Returns false for a host that is too old.
    ///
    /// Rejects missing or malformed host versions.
    /// </summary>
    public bool IsCompatible(
        ProviderPackageManifest manifest,
        string hostVersion)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        var actualHost = ParseThreeComponentVersion(
            hostVersion,
            nameof(hostVersion));

        var requiredHost = ParseThreeComponentVersion(
            manifest.MinimumHostVersion,
            nameof(manifest));

        return actualHost.CompareTo(requiredHost) >= 0;
    }

    private static Version ParseThreeComponentVersion(
        string? value,
        string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            value,
            parameterName);

        var normalized = value.Trim();

        var separatorCount = 0;

        foreach (var character in normalized)
        {
            if (character == '.')
            {
                separatorCount++;
            }
        }

        if (separatorCount != 2 ||
            !Version.TryParse(
                normalized,
                out var parsed) ||
            parsed is null ||
            parsed.Build < 0 ||
            parsed.Revision != -1)
        {
            throw new ArgumentException(
                "Host version must contain three numeric components.",
                parameterName);
        }

        return parsed;
    }
}
