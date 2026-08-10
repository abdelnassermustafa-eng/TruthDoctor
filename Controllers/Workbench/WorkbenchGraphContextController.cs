using System.Collections.Generic;
using TruthDoctor.Graph;
using TruthDoctor.State;

namespace TruthDoctor.Controllers.Workbench;

public sealed class WorkbenchGraphContextController
{
    private readonly WorkbenchState _state;

    public WorkbenchGraphContextController(
        WorkbenchState state)
    {
        _state = state;
    }

    public ResourceGraphContext? Current =>
        _state.SelectedResourceContext;

    public IReadOnlyList<GraphNode> Dependencies =>
        Current?.Dependencies.Resources ?? [];

    public IReadOnlyList<GraphNode> Dependents =>
        Current?.Dependents.Resources ?? [];

    public IReadOnlyList<GraphNode> Security =>
        Current?.Security.Resources ?? [];

    public IReadOnlyList<GraphNode> Connectivity =>
        Current?.Connectivity.Resources ?? [];

    public IReadOnlyList<GraphNode> Neighborhood =>
        Current?.Neighborhood ?? [];

    public int BlastRadius =>
        Current?.Impact.TotalAffectedCount ?? 0;


    public TopologyView BuildTopology(
        int depth = 1)
    {
        var graphIndex =
            _state.InfrastructureGraphIndex;

        var selected =
            Current?.Resource;

        if (graphIndex is null ||
            selected is null)
        {
            return new TopologyView();
        }

        var projector =
            new InfrastructureTopologyProjector(
                graphIndex);

        return projector.ProjectNeighborhood(
            selected.Id,
            depth);
    }
}
