using System;
using System.Collections.Generic;
using System.Linq;

namespace TruthDoctor.Services.Providers;

/// <summary>
/// Validates the dependency graph of declarative provider
/// package manifests.
///
/// Validation checks package identity uniqueness, missing
/// dependencies, and directed dependency cycles.
///
/// Validation does not establish package trust, signature
/// validity, host compatibility, installation readiness,
/// or execution authorization.
///
/// No files, networks, processes, executable plugins,
/// credentials, or provider connections are accessed.
/// </summary>
public sealed class ProviderPackageDependencyValidator
{
    public void Validate(
        IEnumerable<ProviderPackageManifest> packages)
    {
        ArgumentNullException.ThrowIfNull(packages);

        var byId = new Dictionary<
            string,
            ProviderPackageManifest>(
                StringComparer.OrdinalIgnoreCase);

        foreach (var manifest in packages)
        {
            if (manifest is null)
            {
                throw new ArgumentException(
                    "The package collection cannot contain null.",
                    nameof(packages));
            }

            if (!byId.TryAdd(
                    manifest.PackageId,
                    manifest))
            {
                throw new ArgumentException(
                    "The dependency graph contains duplicate package IDs.",
                    nameof(packages));
            }
        }

        var remainingDependencies = new Dictionary<
            string,
            int>(StringComparer.OrdinalIgnoreCase);

        var dependents = new Dictionary<
            string,
            List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var packageId in byId.Keys)
        {
            remainingDependencies.Add(packageId, 0);
            dependents.Add(packageId, new List<string>());
        }

        foreach (var manifest in byId.Values)
        {
            foreach (var dependencyId in manifest.Dependencies)
            {
                if (!byId.ContainsKey(dependencyId))
                {
                    throw new ArgumentException(
                        "The dependency graph references a missing package.",
                        nameof(packages));
                }

                remainingDependencies[manifest.PackageId]++;

                dependents[dependencyId].Add(
                    manifest.PackageId);
            }
        }

        // Iterative topological validation avoids recursive
        // traversal and rejects both direct and indirect cycles.
        var ready = new Queue<string>();

        foreach (var entry in remainingDependencies)
        {
            if (entry.Value == 0)
            {
                ready.Enqueue(entry.Key);
            }
        }

        var visited = 0;

        while (ready.Count > 0)
        {
            var packageId = ready.Dequeue();
            visited++;

            foreach (var dependentId in dependents[packageId])
            {
                remainingDependencies[dependentId]--;

                if (remainingDependencies[dependentId] == 0)
                {
                    ready.Enqueue(dependentId);
                }
            }
        }

        if (visited != byId.Count)
        {
            throw new ArgumentException(
                "The dependency graph contains a dependency cycle.",
                nameof(packages));
        }
    }
}
