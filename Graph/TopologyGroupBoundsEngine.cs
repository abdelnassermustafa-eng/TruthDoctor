using System;
using System.Collections.Generic;
using System.Linq;

namespace TruthDoctor.Graph;

/// <summary>
/// Projects topology domain groups into deterministic rectangular
/// regions using existing node positions.
///
/// This engine has no Avalonia or cloud-provider dependencies.
/// </summary>
public sealed class TopologyGroupBoundsEngine
{
    private readonly TopologyGroupingEngine _groupingEngine =
        new();

    public IReadOnlyList<TopologyGroupBounds> Calculate(
        TopologyView topology,
        IReadOnlyDictionary<
            string,
            TopologyLayoutPosition> positions,
        double nodeWidth,
        double nodeHeight,
        double horizontalPadding = 34,
        double verticalPadding = 30,
        double headerHeight = 28)
    {
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(positions);

        ValidatePositive(
            nodeWidth,
            nameof(nodeWidth));

        ValidatePositive(
            nodeHeight,
            nameof(nodeHeight));

        ValidateNonNegative(
            horizontalPadding,
            nameof(horizontalPadding));

        ValidateNonNegative(
            verticalPadding,
            nameof(verticalPadding));

        ValidateNonNegative(
            headerHeight,
            nameof(headerHeight));

        var results =
            new List<TopologyGroupBounds>();

        foreach (var group in
                 _groupingEngine.GroupByDomain(
                     topology))
        {
            var memberPositions =
                group.NodeIds
                    .Where(positions.ContainsKey)
                    .Select(nodeId =>
                        positions[nodeId])
                    .ToList();

            if (memberPositions.Count == 0)
            {
                continue;
            }

            var minimumX =
                memberPositions.Min(position =>
                    position.X);

            var minimumY =
                memberPositions.Min(position =>
                    position.Y);

            var maximumX =
                memberPositions.Max(position =>
                    position.X +
                    nodeWidth);

            var maximumY =
                memberPositions.Max(position =>
                    position.Y +
                    nodeHeight);

            results.Add(
                new TopologyGroupBounds
                {
                    GroupId =
                        group.Id,

                    DisplayName =
                        group.DisplayName,

                    NodeCount =
                        memberPositions.Count,

                    X =
                        minimumX -
                        horizontalPadding,

                    Y =
                        minimumY -
                        verticalPadding -
                        headerHeight,

                    Width =
                        maximumX -
                        minimumX +
                        (2 * horizontalPadding),

                    Height =
                        maximumY -
                        minimumY +
                        (2 * verticalPadding) +
                        headerHeight
                });
        }

        return results
            .OrderBy(result =>
                result.GroupId,
                StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void ValidatePositive(
        double value,
        string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName);
        }
    }

    private static void ValidateNonNegative(
        double value,
        string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName);
        }
    }
}
