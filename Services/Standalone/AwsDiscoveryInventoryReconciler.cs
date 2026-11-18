using System;
using System.Collections.Generic;
using System.Linq;

namespace TruthDoctor.Services.Standalone;

/// <summary>
/// A discovery scope is an AWS account, region and service.
/// Use a conventional global scope such as "global" for
/// services without regional endpoints.
/// </summary>
public sealed record AwsDiscoveryScope(
    string AccountId,
    string Region,
    string Service);

/// <summary>
/// Resource identity is independent of display name.
/// ResourceType participates in identity so unlike objects
/// cannot accidentally overwrite each other.
/// </summary>
public sealed record AwsDiscoveryResourceKey(
    AwsDiscoveryScope Scope,
    string ResourceType,
    string ResourceId);

public sealed record AwsDiscoveredResource(
    AwsDiscoveryResourceKey Key,
    string DisplayName,
    string State = "",
    string AvailabilityZone = "",
    string Arn = "",
    IReadOnlyDictionary<string, string>? Properties = null,
    IReadOnlyDictionary<string, string>? Tags = null);

/// <summary>
/// A successful scan is authoritative only for its own scope.
/// Access-denied and failed scans are never deletion evidence.
/// </summary>
public enum AwsDiscoveryScanStatus
{
    Success,
    AccessDenied,
    Failed
}

public sealed record AwsDiscoveryScopeScan(
    AwsDiscoveryScope Scope,
    AwsDiscoveryScanStatus Status,
    IReadOnlyList<AwsDiscoveredResource> Resources,
    string? DiagnosticCode = null);

public sealed record AwsDiscoveryCoverage(
    AwsDiscoveryScope Scope,
    AwsDiscoveryScanStatus Status,
    string? DiagnosticCode);

/// <summary>
/// Current resources and the last observed coverage per scope.
/// These are separate so a failed scope cannot be mistaken
/// for a successfully scanned empty scope.
/// </summary>
public sealed record AwsDiscoveryInventorySnapshot(
    long Revision,
    IReadOnlyList<AwsDiscoveredResource> Resources,
    IReadOnlyList<AwsDiscoveryCoverage> Coverage)
{
    public static AwsDiscoveryInventorySnapshot Empty { get; } =
        new(
            0,
            Array.Empty<AwsDiscoveredResource>(),
            Array.Empty<AwsDiscoveryCoverage>());
}

public sealed record AwsDiscoveryInventoryTransition(
    AwsDiscoveryInventorySnapshot Snapshot,
    IReadOnlyList<AwsDiscoveredResource> Added,
    IReadOnlyList<AwsDiscoveredResource> Removed);

/// <summary>
/// Reconciles a fresh collection of service-level scans without
/// contacting AWS, retaining stale scopes after partial failure.
/// No permission to create, modify or delete infrastructure
/// is needed or exposed by this class.
/// </summary>
public static class AwsDiscoveryInventoryReconciler
{
    public static AwsDiscoveryInventoryTransition Reconcile(
        AwsDiscoveryInventorySnapshot previous,
        IReadOnlyList<AwsDiscoveryScopeScan> scans)
    {
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentNullException.ThrowIfNull(scans);
        ArgumentNullException.ThrowIfNull(previous.Resources);
        ArgumentNullException.ThrowIfNull(previous.Coverage);

        var inventory = new Dictionary<
            AwsDiscoveryResourceKey, AwsDiscoveredResource>();

        foreach (var resource in previous.Resources)
        {
            ValidateResource(resource);

            if (!inventory.TryAdd(resource.Key, resource))
            {
                throw new InvalidOperationException(
                    "The previous inventory contains duplicate resource identities.");
            }
        }

        var coverage = new Dictionary<
            AwsDiscoveryScope, AwsDiscoveryCoverage>();

        foreach (var item in previous.Coverage)
        {
            ArgumentNullException.ThrowIfNull(item);
            ValidateScope(item.Scope);

            if (!coverage.TryAdd(item.Scope, item))
            {
                throw new InvalidOperationException(
                    "The previous inventory contains duplicate coverage scopes.");
            }
        }

        // Validate all input before performing any reconciliation.
        var scanScopes = new HashSet<AwsDiscoveryScope>();

        foreach (var scan in scans)
        {
            ArgumentNullException.ThrowIfNull(scan);
            ValidateScope(scan.Scope);
            ArgumentNullException.ThrowIfNull(scan.Resources);

            if (!Enum.IsDefined(scan.Status))
            {
                throw new InvalidOperationException(
                    "The scan contains an unknown status.");
            }

            if (!scanScopes.Add(scan.Scope))
            {
                throw new InvalidOperationException(
                    "Duplicate scan scope in one discovery batch.");
            }

            if (scan.Status != AwsDiscoveryScanStatus.Success
                && scan.Resources.Count != 0)
            {
                throw new InvalidOperationException(
                    "Incomplete scans must not publish resource records.");
            }

            var identities = new HashSet<AwsDiscoveryResourceKey>();

            foreach (var resource in scan.Resources)
            {
                ValidateResource(resource);

                if (resource.Key.Scope != scan.Scope)
                {
                    throw new InvalidOperationException(
                        "A scan returned a resource belonging to another scope.");
                }

                if (!identities.Add(resource.Key))
                {
                    throw new InvalidOperationException(
                        "A scan returned duplicate resource identities.");
                }
            }
        }

        var added = new List<AwsDiscoveredResource>();
        var removed = new List<AwsDiscoveredResource>();

        foreach (var scan in scans)
        {
            coverage[scan.Scope] = new AwsDiscoveryCoverage(
                scan.Scope,
                scan.Status,
                scan.DiagnosticCode);

            if (scan.Status != AwsDiscoveryScanStatus.Success)
            {
                // Preserve previously known resources for this scope.
                continue;
            }

            var current = scan.Resources.ToDictionary(
                resource => resource.Key,
                resource => resource);

            var oldKeys = inventory.Keys
                .Where(key => key.Scope == scan.Scope)
                .ToArray();

            foreach (var oldKey in oldKeys)
            {
                if (!current.ContainsKey(oldKey))
                {
                    removed.Add(inventory[oldKey]);
                    inventory.Remove(oldKey);
                }
            }

            foreach (var resource in scan.Resources)
            {
                if (!inventory.ContainsKey(resource.Key))
                {
                    added.Add(resource);
                }

                // Update the metadata even if the identity
                // existed in the previous inventory.
                inventory[resource.Key] = resource;
            }
        }

        var orderedResources = inventory.Values
            .OrderBy(r => r.Key.Scope.AccountId, StringComparer.Ordinal)
            .ThenBy(r => r.Key.Scope.Region, StringComparer.Ordinal)
            .ThenBy(r => r.Key.Scope.Service, StringComparer.Ordinal)
            .ThenBy(r => r.Key.ResourceType, StringComparer.Ordinal)
            .ThenBy(r => r.Key.ResourceId, StringComparer.Ordinal)
            .ToArray();

        var orderedCoverage = coverage.Values
            .OrderBy(c => c.Scope.AccountId, StringComparer.Ordinal)
            .ThenBy(c => c.Scope.Region, StringComparer.Ordinal)
            .ThenBy(c => c.Scope.Service, StringComparer.Ordinal)
            .ToArray();

        var snapshot = new AwsDiscoveryInventorySnapshot(
            checked(previous.Revision + 1),
            orderedResources,
            orderedCoverage);

        return new AwsDiscoveryInventoryTransition(
            snapshot,
            added.ToArray(),
            removed.ToArray());
    }

    private static void ValidateScope(AwsDiscoveryScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        if (string.IsNullOrWhiteSpace(scope.AccountId)
            || string.IsNullOrWhiteSpace(scope.Region)
            || string.IsNullOrWhiteSpace(scope.Service))
        {
            throw new ArgumentException(
                "AWS discovery scopes require account, region and service.");
        }
    }

    private static void ValidateResource(
        AwsDiscoveredResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(resource.Key);

        ValidateScope(resource.Key.Scope);

        if (string.IsNullOrWhiteSpace(resource.Key.ResourceType)
            || string.IsNullOrWhiteSpace(resource.Key.ResourceId)
            || string.IsNullOrWhiteSpace(resource.DisplayName))
        {
            throw new ArgumentException(
                "Discovered resources require type, ID and display name.");
        }
    }
}
