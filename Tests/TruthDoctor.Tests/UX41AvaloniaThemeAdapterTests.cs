using Avalonia.Media;
using Avalonia.Styling;
using TruthDoctor.Theming;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class UX41AvaloniaThemeAdapterTests
{
    [Fact]
    public void ProfessionalLightMapsToAvaloniaLight()
    {
        Assert.Equal(
            ThemeVariant.Light,
            TruthDoctorAvaloniaThemeAdapter.ResolveVariant(
                TruthDoctorAppearance.ProfessionalLight,
                systemUsesDark: true));
    }

    [Fact]
    public void ProfessionalDarkMapsToAvaloniaDark()
    {
        Assert.Equal(
            ThemeVariant.Dark,
            TruthDoctorAvaloniaThemeAdapter.ResolveVariant(
                TruthDoctorAppearance.ProfessionalDark,
                systemUsesDark: false));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FollowSystemResolvesBothAppearances(
        bool systemUsesDark)
    {
        var expected = systemUsesDark
            ? ThemeVariant.Dark
            : ThemeVariant.Light;

        Assert.Equal(
            expected,
            TruthDoctorAvaloniaThemeAdapter.ResolveVariant(
                TruthDoctorAppearance.FollowSystem,
                systemUsesDark));
    }

    [Fact]
    public void ProfessionalLightCreatesSemanticBrushes()
    {
        var resources =
            TruthDoctorAvaloniaThemeAdapter.CreateResources(
                TruthDoctorAppearance.ProfessionalLight,
                systemUsesDark: false);

        AssertBrush(
            resources["TD.CanvasBrush"],
            "#F5F7FB");

        AssertBrush(
            resources["TD.SurfaceBrush"],
            "#FFFFFF");

        AssertBrush(
            resources["TD.TextPrimaryBrush"],
            "#172B4D");

        AssertBrush(
            resources["TD.PrimaryBrush"],
            "#2563EB");

        AssertBrush(
            resources["TD.BorderBrush"],
            "#DCE4EE");

        AssertBrush(
            resources["TD.HoverSurfaceBrush"],
            "#EAF0F7");

        AssertBrush(
            resources["TD.FocusRingBrush"],
            "#1D4ED8");
    }

    [Fact]
    public void ProfessionalDarkUsesDistinctSurfaceBrushes()
    {
        var resources =
            TruthDoctorAvaloniaThemeAdapter.CreateResources(
                TruthDoctorAppearance.ProfessionalDark,
                systemUsesDark: false);

        AssertBrush(
            resources["TD.CanvasBrush"],
            "#07111F");

        AssertBrush(
            resources["TD.SurfaceBrush"],
            "#101F35");

        AssertBrush(
            resources["TD.HoverSurfaceBrush"],
            "#223149");
    }

    private static void AssertBrush(
        object? actual,
        string expectedColor)
    {
        var brush = Assert.IsType<SolidColorBrush>(actual);

        Assert.Equal(
            Color.Parse(expectedColor),
            brush.Color);
    }
}
