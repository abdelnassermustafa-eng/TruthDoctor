using System;
using System.Collections.Generic;
using TruthDoctor.Graph;
using TruthDoctor.Models.Platform;
using TruthDoctor.State;

namespace TruthDoctor.Controllers.Workbench;

/// <summary>
/// Owns topology projection and navigation history.
/// </summary>
public sealed class WorkbenchTopologyController
{
    private readonly WorkbenchGraphContextController _graph;
    private readonly WorkbenchSelectionController _selection;
    private readonly WorkbenchState _state;

    private readonly Stack<InfrastructureResource> _back = [];
    private readonly Stack<InfrastructureResource> _forward = [];

    private InfrastructureResource? _home;

    public WorkbenchTopologyController(
        WorkbenchGraphContextController graph,
        WorkbenchSelectionController selection,
        WorkbenchState state)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(state);

        _graph = graph;
        _selection = selection;
        _state = state;
    }

    public TopologyView Current =>
        _graph.BuildTopology(2);

    public bool CanGoBack =>
        _back.Count > 0;

    public bool CanGoForward =>
        _forward.Count > 0;

    public bool CanGoHome =>
        _home is not null &&
        !IsCurrent(_home);

    public void BeginSession()
    {
        _back.Clear();
        _forward.Clear();

        _home =
            _state.SelectedResource;
    }

    public bool NavigateTo(
        InfrastructureResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        var current =
            _state.SelectedResource;

        if (IsSameResource(
                current,
                resource))
        {
            return false;
        }

        _home ??=
            current ?? resource;

        if (current is not null)
        {
            _back.Push(current);
        }

        _forward.Clear();

        _selection.SelectResource(
            resource);

        return true;
    }

    public bool GoBack()
    {
        if (_back.Count == 0)
        {
            return false;
        }

        var current =
            _state.SelectedResource;

        if (current is not null)
        {
            _forward.Push(current);
        }

        _selection.SelectResource(
            _back.Pop());

        return true;
    }

    public bool GoForward()
    {
        if (_forward.Count == 0)
        {
            return false;
        }

        var current =
            _state.SelectedResource;

        if (current is not null)
        {
            _back.Push(current);
        }

        _selection.SelectResource(
            _forward.Pop());

        return true;
    }

    public bool GoHome()
    {
        if (_home is null ||
            IsCurrent(_home))
        {
            return false;
        }

        var current =
            _state.SelectedResource;

        if (current is not null)
        {
            _back.Push(current);
        }

        _forward.Clear();

        _selection.SelectResource(
            _home);

        return true;
    }

    public TopologyView Build(
        int depth)
    {
        return _graph.BuildTopology(depth);
    }

    private bool IsCurrent(
        InfrastructureResource resource)
    {
        return IsSameResource(
            _state.SelectedResource,
            resource);
    }

    private static bool IsSameResource(
        InfrastructureResource? left,
        InfrastructureResource? right)
    {
        if (left is null ||
            right is null)
        {
            return false;
        }

        return ReferenceEquals(
                   left,
                   right) ||
               (
                   left.ProviderId.Equals(
                       right.ProviderId,
                       StringComparison.OrdinalIgnoreCase) &&
                   left.AccountId.Equals(
                       right.AccountId,
                       StringComparison.OrdinalIgnoreCase) &&
                   left.DomainId.Equals(
                       right.DomainId,
                       StringComparison.OrdinalIgnoreCase) &&
                   left.NativeId.Equals(
                       right.NativeId,
                       StringComparison.OrdinalIgnoreCase)
               );
    }
}
