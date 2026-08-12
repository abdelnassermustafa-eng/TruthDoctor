using System;
using System.Collections.Generic;
using System.Linq;

namespace TruthDoctor.Graph;

/// <summary>
/// Converts a portable saved view into safe live-workspace instructions.
/// Missing resources are cleared, missing domains fall back to all domains,
/// and unavailable collapsed domains are ignored.
/// </summary>
public sealed class TopologySavedViewRestoreService
{
    private readonly TopologySavedViewValidator
        _validator =
            new();

    public TopologySavedViewRestorePlan CreatePlan(
        TopologySavedView view,
        IEnumerable<string> availableResourceIds,
        IEnumerable<string> availableDomainIds)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(
            availableResourceIds);
        ArgumentNullException.ThrowIfNull(
            availableDomainIds);

        _validator.EnsureValid(view);

        var resources =
            NormalizeAvailableIds(
                availableResourceIds);

        var domains =
            NormalizeAvailableIds(
                availableDomainIds);

        var selectedResourceId =
            NormalizeOptionalId(
                view.SelectedResourceId);

        if (!resources.Contains(
                selectedResourceId))
        {
            selectedResourceId = "";
        }

        var liveSelectedDomainId =
            ToLiveDomainId(
                view.SelectedDomainId);

        if (!string.IsNullOrWhiteSpace(
                liveSelectedDomainId) &&
            !domains.Contains(
                liveSelectedDomainId))
        {
            liveSelectedDomainId =
                TopologyDomainFilter.AllDomains;
        }

        var collapsedDomainIds =
            view.CollapsedDomainIds
                .Select(NormalizeOptionalId)
                .Where(domainId =>
                    domains.Contains(domainId))
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .OrderBy(
                    domainId => domainId,
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

        return new TopologySavedViewRestorePlan
        {
            SelectedResourceId =
                selectedResourceId,

            Depth =
                view.Depth,

            LayoutMode =
                view.LayoutMode,

            LiveSelectedDomainId =
                liveSelectedDomainId,

            CollapsedDomainIds =
                collapsedDomainIds,

            RelationshipFilters =
                CopyRelationshipFilters(
                    view.RelationshipFilters),

            Zoom =
                view.Zoom,

            ScrollOffset =
                view.ScrollOffset,

            IsMinimapVisible =
                view.IsMinimapVisible,

            SearchText =
                view.SearchText?.Trim() ?? ""
        };
    }

    private static HashSet<string>
        NormalizeAvailableIds(
            IEnumerable<string> values)
    {
        return values
            .Where(value =>
                !string.IsNullOrWhiteSpace(value))
            .Select(value =>
                value.Trim())
            .ToHashSet(
                StringComparer.OrdinalIgnoreCase);
    }

    private static string NormalizeOptionalId(
        string? value)
    {
        return value?.Trim() ?? "";
    }

    private static string ToLiveDomainId(
        string storedDomainId)
    {
        var normalized =
            storedDomainId.Trim();

        return normalized.Equals(
                TopologySavedViewDomainCodec
                    .AllDomainsStorageId,
                StringComparison.OrdinalIgnoreCase)
            ? TopologyDomainFilter.AllDomains
            : normalized;
    }

    private static TopologyRelationshipFilterState
        CopyRelationshipFilters(
            TopologyRelationshipFilterState filters)
    {
        return new TopologyRelationshipFilterState
        {
            Containment =
                filters.Containment,

            Placement =
                filters.Placement,

            Dependency =
                filters.Dependency,

            Connectivity =
                filters.Connectivity,

            Security =
                filters.Security,

            Traffic =
                filters.Traffic,

            Association =
                filters.Association,

            Other =
                filters.Other
        };
    }
}
