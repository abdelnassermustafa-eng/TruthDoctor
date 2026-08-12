using System;
using TruthDoctor.Graph;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class TopologyMinimapMapperTests
{
    private readonly TopologyMinimapMapper _mapper =
        new();

    [Fact]
    public void ProjectViewportMapsVisibleAreaToMinimap()
    {
        var viewport =
            _mapper.ProjectViewport(
                canvasWidth: 1600,
                canvasHeight: 1000,
                zoom: 1,
                viewportWidth: 800,
                viewportHeight: 500,
                offsetX: 400,
                offsetY: 250,
                minimapWidth: 240,
                minimapHeight: 150);

        Assert.Equal(
            60,
            viewport.X,
            6);

        Assert.Equal(
            37.5,
            viewport.Y,
            6);

        Assert.Equal(
            120,
            viewport.Width,
            6);

        Assert.Equal(
            75,
            viewport.Height,
            6);
    }

    [Fact]
    public void ProjectViewportIncludesZoomedContent()
    {
        var viewport =
            _mapper.ProjectViewport(
                canvasWidth: 1600,
                canvasHeight: 1000,
                zoom: 2,
                viewportWidth: 800,
                viewportHeight: 500,
                offsetX: 800,
                offsetY: 500,
                minimapWidth: 240,
                minimapHeight: 150);

        Assert.Equal(
            60,
            viewport.X,
            6);

        Assert.Equal(
            37.5,
            viewport.Y,
            6);

        Assert.Equal(
            60,
            viewport.Width,
            6);

        Assert.Equal(
            37.5,
            viewport.Height,
            6);
    }

    [Fact]
    public void NavigateToCentersViewportAtMinimapLocation()
    {
        var offset =
            _mapper.NavigateTo(
                minimapX: 120,
                minimapY: 75,
                canvasWidth: 1600,
                canvasHeight: 1000,
                zoom: 1,
                viewportWidth: 800,
                viewportHeight: 500,
                minimapWidth: 240,
                minimapHeight: 150);

        Assert.Equal(
            400,
            offset.X,
            6);

        Assert.Equal(
            250,
            offset.Y,
            6);
    }

    [Fact]
    public void NavigateToClampsAtContentBoundaries()
    {
        var beginning =
            _mapper.NavigateTo(
                minimapX: 0,
                minimapY: 0,
                canvasWidth: 1600,
                canvasHeight: 1000,
                zoom: 1,
                viewportWidth: 800,
                viewportHeight: 500,
                minimapWidth: 240,
                minimapHeight: 150);

        var ending =
            _mapper.NavigateTo(
                minimapX: 240,
                minimapY: 150,
                canvasWidth: 1600,
                canvasHeight: 1000,
                zoom: 1,
                viewportWidth: 800,
                viewportHeight: 500,
                minimapWidth: 240,
                minimapHeight: 150);

        Assert.Equal(
            new TopologyScrollOffset(0, 0),
            beginning);

        Assert.Equal(
            new TopologyScrollOffset(800, 500),
            ending);
    }

    [Fact]
    public void ProjectViewportUsesFullMinimapWhenContentFits()
    {
        var viewport =
            _mapper.ProjectViewport(
                canvasWidth: 800,
                canvasHeight: 500,
                zoom: 1,
                viewportWidth: 1000,
                viewportHeight: 700,
                offsetX: 100,
                offsetY: 100,
                minimapWidth: 240,
                minimapHeight: 150);

        Assert.Equal(
            new TopologyMinimapViewport(
                0,
                0,
                240,
                150),
            viewport);
    }

    [Fact]
    public void InvalidDimensionsAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                _mapper.ProjectViewport(
                    canvasWidth: 0,
                    canvasHeight: 1000,
                    zoom: 1,
                    viewportWidth: 800,
                    viewportHeight: 500,
                    offsetX: 0,
                    offsetY: 0,
                    minimapWidth: 240,
                    minimapHeight: 150));
    }
}
