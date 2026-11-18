using System;
using System.Collections.Generic;
using System.Linq;

namespace TruthDoctor.Services.Providers;

/// <summary>
/// In-memory catalogue of declarative provider package manifests.
///
/// This is intentionally separate from ProviderRegistry,
/// which resolves runtime provider plugins.
///
/// Registration does not install, verify, activate, load,
/// authenticate, or execute a provider package.
/// </summary>
public sealed class ProviderPackageRegistry
{
    private readonly Dictionary<string, ProviderPackageManifest>
        _packages = new(StringComparer.OrdinalIgnoreCase);

    public void Register(ProviderPackageManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        if (!_packages.TryAdd(manifest.PackageId, manifest))
        {
            throw new ArgumentException(
                "A package with this identifier is already registered.",
                nameof(manifest));
        }
    }

    public ProviderPackageManifest Resolve(string packageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            packageId);

        var normalized = packageId.Trim();

        if (!_packages.TryGetValue(
                normalized,
                out var manifest))
        {
            throw new KeyNotFoundException(
                "No provider package manifest is registered " +
                "for the requested identifier.");
        }

        return manifest;
    }

    public IReadOnlyList<ProviderPackageManifest> GetAll()
    {
        return _packages.Values
            .OrderBy(
                manifest => manifest.PackageId,
                StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();
    }
}
