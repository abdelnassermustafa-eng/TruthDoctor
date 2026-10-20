using System;
using TruthDoctor.Models.Platform;

namespace TruthDoctor.Controllers.Workbench;

/// <summary>
/// Determines when workspace transitions require a new
/// topology navigation session.
/// </summary>
public sealed class WorkbenchCrossWorkspaceNavigationCoordinator
{
    private bool _hasTopologySession;
    private bool _topologyActive;
    private ResourceIdentity? _topologySelection;

    /// <summary>
    /// Records the Topology session established by an
    /// explicit saved-view restoration without resetting
    /// its navigation history or changing active workspace.
    /// </summary>
    public void SynchronizeRestoredTopologySession(
        InfrastructureResource? selectedResource)
    {
        _hasTopologySession = true;
        _topologySelection = ResourceIdentity.From(selectedResource);
    }

    public bool EnterWorkspace(
        string workspace,
        InfrastructureResource? selectedResource)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        var enteringTopology =
            workspace.Equals(
                "topology",
                StringComparison.OrdinalIgnoreCase);

        var selectedIdentity =
            ResourceIdentity.From(selectedResource);

        if (!enteringTopology)
        {
            _topologyActive = false;
            return false;
        }

        if (!_hasTopologySession)
        {
            _hasTopologySession = true;
            _topologyActive = true;
            _topologySelection = selectedIdentity;
            return true;
        }

        if (_topologyActive)
        {
            // Selection changes within Topology are part
            // of the existing navigation session.
            _topologySelection = selectedIdentity;
            return false;
        }

        var selectionChanged =
            _topologySelection != selectedIdentity;

        _topologyActive = true;
        _topologySelection = selectedIdentity;

        return selectionChanged;
    }

    private sealed record ResourceIdentity(
        string ProviderId,
        string AccountId,
        string DomainId,
        string NativeId)
    {
        public static ResourceIdentity? From(
            InfrastructureResource? resource)
        {
            if (resource is null)
            {
                return null;
            }

            return new ResourceIdentity(
                resource.ProviderId.ToUpperInvariant(),
                resource.AccountId.ToUpperInvariant(),
                resource.DomainId.ToUpperInvariant(),
                resource.NativeId.ToUpperInvariant());
        }
    }
}
