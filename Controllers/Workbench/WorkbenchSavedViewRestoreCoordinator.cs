using System;
using System.Collections.Generic;
using System.Linq;
using TruthDoctor.Graph;
using TruthDoctor.State;

namespace TruthDoctor.Controllers.Workbench;

/// <summary>
/// Coordinates saved-view planning with Workbench-owned resource selection
/// and topology depth. It has no UI or provider dependency.
/// </summary>
public sealed class WorkbenchSavedViewRestoreCoordinator
{
    private readonly WorkbenchState _state;

    private readonly WorkbenchTopologyController
        _topology;

    private readonly TopologySavedViewRestoreService
        _restoreService =
            new();

    public WorkbenchSavedViewRestoreCoordinator(
        WorkbenchState state,
        WorkbenchTopologyController topology)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(topology);

        _state = state;
        _topology = topology;
    }

    public TopologySavedViewWorkbenchRestoreSummary Restore(
        TopologySavedView view)
    {
        ArgumentNullException.ThrowIfNull(view);

        var index =
            _state.InfrastructureGraphIndex;

        var availableResourceIds =
            index?.Graph.Nodes.Keys ??
            Enumerable.Empty<string>();

        var availableDomainIds =
            index is null
                ? Enumerable.Empty<string>()
                : index.Graph.Nodes.Values
                    .Select(node =>
                        NormalizeDomainId(
                            node.DomainId))
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase);

        var plan =
            _restoreService.CreatePlan(
                view,
                availableResourceIds,
                availableDomainIds);

        var selectedNode =
            string.IsNullOrWhiteSpace(
                plan.SelectedResourceId)
                ? null
                : index?.FindNode(
                    plan.SelectedResourceId);

        var workbenchResult =
            _topology.RestoreSavedViewSelection(
                plan,
                selectedNode);

        return new TopologySavedViewWorkbenchRestoreSummary
        {
            Plan =
                plan,

            WorkbenchResult =
                workbenchResult,

            ResourceWasUnavailable =
                !string.IsNullOrWhiteSpace(
                    view.SelectedResourceId) &&
                string.IsNullOrWhiteSpace(
                    plan.SelectedResourceId),

            DomainFellBackToAll =
                !view.SelectedDomainId.Equals(
                    TopologySavedViewDomainCodec
                        .AllDomainsStorageId,
                    StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(
                    plan.LiveSelectedDomainId),

            IgnoredCollapsedDomainCount =
                view.CollapsedDomainIds.Length -
                plan.CollapsedDomainIds.Length
        };
    }

    private static string NormalizeDomainId(
        string? domainId)
    {
        return string.IsNullOrWhiteSpace(domainId)
            ? "other"
            : domainId.Trim();
    }
}
