using System;

namespace TruthDoctor.Graph;

/// <summary>
/// Converts between the scrollable topology surface and its minimap.
///
/// This class contains no Avalonia dependencies so viewport projection
/// and minimap navigation can be validated independently of the UI.
/// </summary>
public sealed class TopologyMinimapMapper
{
    public TopologyMinimapViewport ProjectViewport(
        double canvasWidth,
        double canvasHeight,
        double zoom,
        double viewportWidth,
        double viewportHeight,
        double offsetX,
        double offsetY,
        double minimapWidth,
        double minimapHeight)
    {
        ValidateDimensions(
            canvasWidth,
            canvasHeight,
            zoom,
            minimapWidth,
            minimapHeight);

        var contentWidth =
            canvasWidth * zoom;

        var contentHeight =
            canvasHeight * zoom;

        var maximumOffsetX =
            Math.Max(
                0,
                contentWidth - viewportWidth);

        var maximumOffsetY =
            Math.Max(
                0,
                contentHeight - viewportHeight);

        var normalizedOffsetX =
            Math.Clamp(
                offsetX,
                0,
                maximumOffsetX);

        var normalizedOffsetY =
            Math.Clamp(
                offsetY,
                0,
                maximumOffsetY);

        var projectedWidth =
            Math.Clamp(
                viewportWidth /
                contentWidth *
                minimapWidth,
                0,
                minimapWidth);

        var projectedHeight =
            Math.Clamp(
                viewportHeight /
                contentHeight *
                minimapHeight,
                0,
                minimapHeight);

        var projectedX =
            contentWidth <= viewportWidth
                ? 0
                : normalizedOffsetX /
                  contentWidth *
                  minimapWidth;

        var projectedY =
            contentHeight <= viewportHeight
                ? 0
                : normalizedOffsetY /
                  contentHeight *
                  minimapHeight;

        projectedX =
            Math.Clamp(
                projectedX,
                0,
                minimapWidth - projectedWidth);

        projectedY =
            Math.Clamp(
                projectedY,
                0,
                minimapHeight - projectedHeight);

        return new TopologyMinimapViewport(
            projectedX,
            projectedY,
            projectedWidth,
            projectedHeight);
    }

    public TopologyScrollOffset NavigateTo(
        double minimapX,
        double minimapY,
        double canvasWidth,
        double canvasHeight,
        double zoom,
        double viewportWidth,
        double viewportHeight,
        double minimapWidth,
        double minimapHeight)
    {
        ValidateDimensions(
            canvasWidth,
            canvasHeight,
            zoom,
            minimapWidth,
            minimapHeight);

        var contentWidth =
            canvasWidth * zoom;

        var contentHeight =
            canvasHeight * zoom;

        var normalizedX =
            Math.Clamp(
                minimapX / minimapWidth,
                0,
                1);

        var normalizedY =
            Math.Clamp(
                minimapY / minimapHeight,
                0,
                1);

        var requestedOffsetX =
            normalizedX *
            contentWidth -
            viewportWidth / 2;

        var requestedOffsetY =
            normalizedY *
            contentHeight -
            viewportHeight / 2;

        var maximumOffsetX =
            Math.Max(
                0,
                contentWidth - viewportWidth);

        var maximumOffsetY =
            Math.Max(
                0,
                contentHeight - viewportHeight);

        return new TopologyScrollOffset(
            Math.Clamp(
                requestedOffsetX,
                0,
                maximumOffsetX),

            Math.Clamp(
                requestedOffsetY,
                0,
                maximumOffsetY));
    }

    private static void ValidateDimensions(
        double canvasWidth,
        double canvasHeight,
        double zoom,
        double minimapWidth,
        double minimapHeight)
    {
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

        if (zoom <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(zoom));
        }

        if (minimapWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minimapWidth));
        }

        if (minimapHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minimapHeight));
        }
    }
}
