using System;

namespace TruthDoctor.Graph;

/// <summary>
/// Translates between the topology's live domain selector and the
/// portable representation stored in a saved view.
/// </summary>
public static class TopologySavedViewDomainCodec
{
    public const string AllDomainsStorageId =
        "all";

    public static string ToStorage(
        string? liveDomainId)
    {
        return string.IsNullOrWhiteSpace(
                liveDomainId)
            ? AllDomainsStorageId
            : liveDomainId.Trim();
    }

    public static string ToLive(
        string storedDomainId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            storedDomainId);

        var normalized =
            storedDomainId.Trim();

        return normalized.Equals(
                AllDomainsStorageId,
                StringComparison.OrdinalIgnoreCase)
            ? TopologyDomainFilter.AllDomains
            : normalized;
    }

    public static bool IsAllDomainsStorageId(
        string? storedDomainId)
    {
        return storedDomainId?
            .Trim()
            .Equals(
                AllDomainsStorageId,
                StringComparison.OrdinalIgnoreCase)
            == true;
    }
}
