using System;
using System.Collections.Generic;
using System.Linq;

namespace TruthDoctor.Services.Providers;

/// <summary>
/// Immutable declarative metadata for a provider package.
///
/// A manifest is not proof that a package is trusted,
/// compatible, installed, enabled, or executable.
///
/// This type performs no filesystem, network, authentication,
/// package loading, or provider execution operations.
/// </summary>
public sealed class ProviderPackageManifest
{
    public string ProviderId { get; }

    public string PackageId { get; }

    public string Version { get; }

    public string MinimumHostVersion { get; }

    public IReadOnlyList<string> Dependencies { get; }

    public IReadOnlyList<string> DeclaredCapabilities { get; }

    public ProviderPackageManifest(
        string providerId,
        string packageId,
        string version,
        string minimumHostVersion,
        IEnumerable<string> dependencies,
        IEnumerable<string> declaredCapabilities)
    {
        ProviderId = RequireText(
            providerId,
            nameof(providerId));

        PackageId = RequireText(
            packageId,
            nameof(packageId));

        Version = RequireThreeComponentVersion(
            version,
            nameof(version));

        MinimumHostVersion = RequireThreeComponentVersion(
            minimumHostVersion,
            nameof(minimumHostVersion));

        Dependencies = SnapshotUnique(
            dependencies,
            nameof(dependencies));

        foreach (var dependency in Dependencies)
        {
            if (string.Equals(
                    dependency,
                    PackageId,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "A provider package cannot depend on itself.",
                    nameof(dependencies));
            }
        }

        DeclaredCapabilities = SnapshotUnique(
            declaredCapabilities,
            nameof(declaredCapabilities));
    }

    private static string RequireText(
        string? value,
        string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            value,
            parameterName);

        return value.Trim();
    }

    private static string RequireThreeComponentVersion(
        string? value,
        string parameterName)
    {
        var normalized = RequireText(value, parameterName);

        var separatorCount = 0;
        foreach (var character in normalized)
        {
            if (character == '.')
            {
                separatorCount++;
            }
        }

        if (separatorCount != 2 ||
            !System.Version.TryParse(
                normalized,
                out var parsed) ||
            parsed is null ||
            parsed.Build < 0 ||
            parsed.Revision != -1)
        {
            throw new ArgumentException(
                "Version must contain three numeric components.",
                parameterName);
        }

        return normalized;
    }

    private static IReadOnlyList<string> SnapshotUnique(
        IEnumerable<string> values,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(
            values,
            parameterName);

        var entries = new List<string>();

        var seen = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var value in values)
        {
            var entry = RequireText(
                value,
                parameterName);

            if (!seen.Add(entry))
            {
                throw new ArgumentException(
                    "Duplicate metadata entries are not allowed.",
                    parameterName);
            }

            entries.Add(entry);
        }

        return entries.AsReadOnly();
    }
}
