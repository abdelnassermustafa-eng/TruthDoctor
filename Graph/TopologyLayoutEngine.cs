using System;
using System.Collections.Generic;
using System.Linq;

namespace TruthDoctor.Graph;

public sealed class TopologyLayoutEngine
{
    private const double HorizontalMargin = 80;
    private const double VerticalMargin = 70;
    private const double NodeWidthAllowance = 220;
    private const double NodeHeightAllowance = 120;

    public IReadOnlyDictionary<
        string,
        TopologyLayoutPosition> Arrange(
        TopologyView topology,
        TopologyLayoutMode mode,
        double canvasWidth = 1600,
        double canvasHeight = 1000)
    {
        ArgumentNullException.ThrowIfNull(topology);

        if (canvasWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(canvasWidth));
        }

        if (canvasHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(canvasHeight));
        }

        if (topology.Nodes.Count == 0)
        {
            return new Dictionary<
                string,
                TopologyLayoutPosition>(
                StringComparer.OrdinalIgnoreCase);
        }

        return mode switch
        {
            TopologyLayoutMode.Hierarchical =>
                ArrangeHierarchical(
                    topology,
                    canvasWidth,
                    canvasHeight),

            TopologyLayoutMode.Network =>
                ArrangeNetwork(
                    topology,
                    canvasWidth,
                    canvasHeight),

            _ =>
                ArrangeRadial(
                    topology,
                    canvasWidth,
                    canvasHeight)
        };
    }

    private static Dictionary<
        string,
        TopologyLayoutPosition> ArrangeRadial(
        TopologyView topology,
        double canvasWidth,
        double canvasHeight)
    {
        var positions =
            CreatePositionDictionary();

        var selected =
            FindSelected(topology);

        var center =
            LayoutCenter(
                canvasWidth,
                canvasHeight);

        positions[selected.Id] =
            center;

        var others =
            topology.Nodes
                .Where(node =>
                    !node.Id.Equals(
                        selected.Id,
                        StringComparison.OrdinalIgnoreCase))
                .OrderBy(node => node.DomainId)
                .ThenBy(node => node.DisplayName)
                .ThenBy(node => node.Id)
                .ToList();

        if (others.Count == 0)
        {
            return positions;
        }

        var radius =
            Math.Min(
                canvasWidth,
                canvasHeight) *
            0.31;

        for (var index = 0;
             index < others.Count;
             index++)
        {
            var angle =
                2 *
                Math.PI *
                index /
                others.Count;

            positions[others[index].Id] =
                Clamp(
                    new TopologyLayoutPosition(
                        center.X +
                        Math.Cos(angle) *
                        radius,

                        center.Y +
                        Math.Sin(angle) *
                        radius),
                    canvasWidth,
                    canvasHeight);
        }

        return positions;
    }

    private static Dictionary<
        string,
        TopologyLayoutPosition> ArrangeHierarchical(
        TopologyView topology,
        double canvasWidth,
        double canvasHeight)
    {
        var selected =
            FindSelected(topology);

        var levels =
            BuildDirectedLevels(
                topology,
                selected.Id);

        var grouped =
            topology.Nodes
                .GroupBy(node =>
                    levels.TryGetValue(
                        node.Id,
                        out var level)
                        ? level
                        : int.MaxValue)
                .OrderBy(group => group.Key)
                .ToList();

        var positions =
            CreatePositionDictionary();

        var usableHeight =
            Math.Max(
                1,
                canvasHeight -
                (2 * VerticalMargin) -
                NodeHeightAllowance);

        var levelSpacing =
            grouped.Count <= 1
                ? 0
                : usableHeight /
                  (grouped.Count - 1);

        for (var levelIndex = 0;
             levelIndex < grouped.Count;
             levelIndex++)
        {
            var nodes =
                grouped[levelIndex]
                    .OrderBy(node => node.DomainId)
                    .ThenBy(node => node.DisplayName)
                    .ThenBy(node => node.Id)
                    .ToList();

            var usableWidth =
                Math.Max(
                    1,
                    canvasWidth -
                    (2 * HorizontalMargin) -
                    NodeWidthAllowance);

            var nodeSpacing =
                nodes.Count <= 1
                    ? 0
                    : usableWidth /
                      (nodes.Count - 1);

            for (var nodeIndex = 0;
                 nodeIndex < nodes.Count;
                 nodeIndex++)
            {
                var x =
                    nodes.Count == 1
                        ? LayoutCenter(
                            canvasWidth,
                            canvasHeight).X
                        : HorizontalMargin +
                          (nodeIndex * nodeSpacing);

                var y =
                    grouped.Count == 1
                        ? LayoutCenter(
                            canvasWidth,
                            canvasHeight).Y
                        : VerticalMargin +
                          (levelIndex * levelSpacing);

                positions[nodes[nodeIndex].Id] =
                    Clamp(
                        new TopologyLayoutPosition(
                            x,
                            y),
                        canvasWidth,
                        canvasHeight);
            }
        }

        return positions;
    }

    private static Dictionary<
        string,
        TopologyLayoutPosition> ArrangeNetwork(
        TopologyView topology,
        double canvasWidth,
        double canvasHeight)
    {
        var positions =
            ArrangeRadial(
                topology,
                canvasWidth,
                canvasHeight);

        var selected =
            FindSelected(topology);

        var orderedNodes =
            topology.Nodes
                .OrderBy(node => node.Id)
                .ToList();

        const int iterations = 140;
        const double idealEdgeLength = 240;
        const double repulsionStrength = 85000;
        const double attractionStrength = 0.018;

        for (var iteration = 0;
             iteration < iterations;
             iteration++)
        {
            var forces =
                orderedNodes.ToDictionary(
                    node => node.Id,
                    _ => (X: 0.0, Y: 0.0),
                    StringComparer.OrdinalIgnoreCase);

            for (var leftIndex = 0;
                 leftIndex < orderedNodes.Count;
                 leftIndex++)
            {
                for (var rightIndex = leftIndex + 1;
                     rightIndex < orderedNodes.Count;
                     rightIndex++)
                {
                    var left =
                        orderedNodes[leftIndex];

                    var right =
                        orderedNodes[rightIndex];

                    var delta =
                        Difference(
                            positions[left.Id],
                            positions[right.Id],
                            leftIndex,
                            rightIndex);

                    var distance =
                        Math.Max(
                            1,
                            Math.Sqrt(
                                (delta.X * delta.X) +
                                (delta.Y * delta.Y)));

                    var magnitude =
                        repulsionStrength /
                        (distance * distance);

                    var forceX =
                        delta.X /
                        distance *
                        magnitude;

                    var forceY =
                        delta.Y /
                        distance *
                        magnitude;

                    forces[left.Id] =
                        (
                            forces[left.Id].X + forceX,
                            forces[left.Id].Y + forceY
                        );

                    forces[right.Id] =
                        (
                            forces[right.Id].X - forceX,
                            forces[right.Id].Y - forceY
                        );
                }
            }

            foreach (var edge in topology.Edges)
            {
                if (!positions.TryGetValue(
                        edge.SourceId,
                        out var source) ||
                    !positions.TryGetValue(
                        edge.TargetId,
                        out var target))
                {
                    continue;
                }

                var deltaX =
                    target.X -
                    source.X;

                var deltaY =
                    target.Y -
                    source.Y;

                var distance =
                    Math.Max(
                        1,
                        Math.Sqrt(
                            (deltaX * deltaX) +
                            (deltaY * deltaY)));

                var magnitude =
                    (distance - idealEdgeLength) *
                    attractionStrength;

                var forceX =
                    deltaX /
                    distance *
                    magnitude;

                var forceY =
                    deltaY /
                    distance *
                    magnitude;

                forces[edge.SourceId] =
                    (
                        forces[edge.SourceId].X + forceX,
                        forces[edge.SourceId].Y + forceY
                    );

                forces[edge.TargetId] =
                    (
                        forces[edge.TargetId].X - forceX,
                        forces[edge.TargetId].Y - forceY
                    );
            }

            var cooling =
                10.0 *
                (1.0 -
                 ((double)iteration / iterations)) +
                0.5;

            foreach (var node in orderedNodes)
            {
                if (node.Id.Equals(
                        selected.Id,
                        StringComparison.OrdinalIgnoreCase))
                {
                    positions[node.Id] =
                        LayoutCenter(
                            canvasWidth,
                            canvasHeight);

                    continue;
                }

                var force =
                    forces[node.Id];

                var movementX =
                    Math.Clamp(
                        force.X,
                        -cooling,
                        cooling);

                var movementY =
                    Math.Clamp(
                        force.Y,
                        -cooling,
                        cooling);

                positions[node.Id] =
                    Clamp(
                        new TopologyLayoutPosition(
                            positions[node.Id].X +
                            movementX,

                            positions[node.Id].Y +
                            movementY),
                        canvasWidth,
                        canvasHeight);
            }
        }

        return positions;
    }

    private static Dictionary<string, int>
        BuildDirectedLevels(
        TopologyView topology,
        string selectedId)
    {
        var levels =
            new Dictionary<string, int>(
                StringComparer.OrdinalIgnoreCase)
            {
                [selectedId] = 0
            };

        var queue =
            new Queue<string>();

        queue.Enqueue(selectedId);

        while (queue.Count > 0)
        {
            var current =
                queue.Dequeue();

            var currentLevel =
                levels[current];

            foreach (var edge in topology.Edges)
            {
                string? neighbor = null;
                var candidateLevel = currentLevel;

                if (edge.SourceId.Equals(
                        current,
                        StringComparison.OrdinalIgnoreCase))
                {
                    neighbor = edge.TargetId;
                    candidateLevel =
                        currentLevel + 1;
                }
                else if (edge.TargetId.Equals(
                             current,
                             StringComparison.OrdinalIgnoreCase))
                {
                    neighbor = edge.SourceId;
                    candidateLevel =
                        currentLevel - 1;
                }

                if (neighbor is null ||
                    levels.ContainsKey(neighbor))
                {
                    continue;
                }

                levels[neighbor] =
                    candidateLevel;

                queue.Enqueue(neighbor);
            }
        }

        var fallbackLevel =
            levels.Count == 0
                ? 0
                : levels.Values.Max() + 1;

        foreach (var node in topology.Nodes)
        {
            if (!levels.ContainsKey(node.Id))
            {
                levels[node.Id] =
                    fallbackLevel;
            }
        }

        return levels;
    }

    private static TopologyNode FindSelected(
        TopologyView topology)
    {
        return topology.Nodes.FirstOrDefault(node =>
                   node.IsSelected ||
                   node.Id.Equals(
                       topology.SelectedResourceId,
                       StringComparison.OrdinalIgnoreCase))
               ?? topology.Nodes[0];
    }

    private static TopologyLayoutPosition
        LayoutCenter(
        double canvasWidth,
        double canvasHeight)
    {
        return new TopologyLayoutPosition(
            (canvasWidth / 2) - 100,
            (canvasHeight / 2) - 80);
    }

    private static TopologyLayoutPosition Difference(
        TopologyLayoutPosition left,
        TopologyLayoutPosition right,
        int leftIndex,
        int rightIndex)
    {
        var deltaX =
            left.X -
            right.X;

        var deltaY =
            left.Y -
            right.Y;

        if (Math.Abs(deltaX) > 0.01 ||
            Math.Abs(deltaY) > 0.01)
        {
            return new TopologyLayoutPosition(
                deltaX,
                deltaY);
        }

        return new TopologyLayoutPosition(
            leftIndex - rightIndex,
            rightIndex - leftIndex);
    }

    private static TopologyLayoutPosition Clamp(
        TopologyLayoutPosition position,
        double canvasWidth,
        double canvasHeight)
    {
        return new TopologyLayoutPosition(
            Math.Clamp(
                position.X,
                HorizontalMargin,
                Math.Max(
                    HorizontalMargin,
                    canvasWidth -
                    HorizontalMargin -
                    NodeWidthAllowance)),

            Math.Clamp(
                position.Y,
                VerticalMargin,
                Math.Max(
                    VerticalMargin,
                    canvasHeight -
                    VerticalMargin -
                    NodeHeightAllowance)));
    }

    private static Dictionary<
        string,
        TopologyLayoutPosition>
        CreatePositionDictionary()
    {
        return new Dictionary<
            string,
            TopologyLayoutPosition>(
            StringComparer.OrdinalIgnoreCase);
    }
}
