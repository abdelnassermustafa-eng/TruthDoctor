using System;
using System.Collections.Generic;
using Avalonia.Media;
using TruthDoctor.Theming;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class UX54617SemanticStatusBrushTests
{
    private static void AssertBrush(
        IReadOnlyDictionary<string, object> resources,
        string key,
        string expectedColor)
    {
        Assert.True(
            resources.TryGetValue(key, out var resource),
            $"Missing semantic resource: {key}");

        var brush = Assert.IsType<SolidColorBrush>(resource);

        Assert.Equal(
            Color.Parse(expectedColor),
            brush.Color);
    }

    [Fact]
    public void ProfessionalLightStatusBrushesMeetContrastContract()
    {
        var resources =
            TruthDoctorAvaloniaThemeAdapter.CreateResources(
                TruthDoctorAppearance.ProfessionalLight,
                systemUsesDark: false);

        AssertBrush(
            resources, "TD.StatusSuccessBrush", "#166534");

        AssertBrush(
            resources, "TD.StatusErrorBrush", "#991B1B");
    }

    [Fact]
    public void ProfessionalDarkStatusBrushesPreserveContrastContract()
    {
        var resources =
            TruthDoctorAvaloniaThemeAdapter.CreateResources(
                TruthDoctorAppearance.ProfessionalDark,
                systemUsesDark: false);

        AssertBrush(
            resources, "TD.StatusSuccessBrush", "#86EFAC");

        AssertBrush(
            resources, "TD.StatusErrorBrush", "#FCA5A5");
    }

    [Fact]
    public void ExistingThemeResourcesAndDefaultRemainUnchanged()
    {
        Assert.Equal(
            TruthDoctorAppearance.ProfessionalLight,
            TruthDoctorThemeContract.DefaultAppearance);

        var light =
            TruthDoctorAvaloniaThemeAdapter.CreateResources(
                TruthDoctorAppearance.ProfessionalLight,
                systemUsesDark: false);

        var dark =
            TruthDoctorAvaloniaThemeAdapter.CreateResources(
                TruthDoctorAppearance.ProfessionalDark,
                systemUsesDark: false);

        foreach (var resources in new[] { light, dark })
        {
            foreach (string key in new[]
            {
                "TD.CanvasBrush",
                "TD.SurfaceBrush",
                "TD.TextPrimaryBrush",
                "TD.PrimaryBrush",
                "TD.BorderBrush",
                "TD.HoverSurfaceBrush",
                "TD.FocusRingBrush"
            })
            {
                Assert.True(
                    resources.ContainsKey(key),
                    $"Existing resource missing: {key}");

                Assert.IsType<SolidColorBrush>(
                    resources[key]);
            }
        }

        AssertBrush(light, "TD.SurfaceBrush", "#FFFFFF");
        AssertBrush(dark, "TD.SurfaceBrush", "#101F35");
        AssertBrush(light, "TD.TextPrimaryBrush", "#172B4D");
        AssertBrush(dark, "TD.TextPrimaryBrush", "#F8FAFC");
    }
}
