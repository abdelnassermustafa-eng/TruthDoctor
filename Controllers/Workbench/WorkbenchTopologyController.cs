using System;
using TruthDoctor.Graph;
using TruthDoctor.State;

namespace TruthDoctor.Controllers.Workbench;

/// <summary>
/// Owns the topology workspace.
///
/// The UI never computes graph topology directly.
/// It simply asks this controller for the current projection.
/// </summary>
public sealed class WorkbenchTopologyController
{
    private readonly WorkbenchGraphContextController _graph;

    public WorkbenchTopologyController(
        WorkbenchGraphContextController graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        _graph = graph;
    }

    public TopologyView Current =>
        _graph.BuildTopology(2);

    public TopologyView Build(
        int depth)
    {
        return _graph.BuildTopology(depth);
    }
}
