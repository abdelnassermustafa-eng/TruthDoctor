using TruthDoctor.Theming;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class UX3ThemeContractRedTests
{
    [Fact]
    public void ProfessionalLightIsDefault()
    {
        Assert.Equal(
            TruthDoctorAppearance.ProfessionalLight,
            TruthDoctorThemeContract.DefaultAppearance);
    }

    [Theory]
    [InlineData(false, TruthDoctorAppearance.ProfessionalLight)]
    [InlineData(true, TruthDoctorAppearance.ProfessionalDark)]
    public void FollowSystemResolvesCorrectly(
        bool systemUsesDark,
        TruthDoctorAppearance expected)
    {
        Assert.Equal(
            expected,
            TruthDoctorThemeContract.Resolve(
                TruthDoctorAppearance.FollowSystem,
                systemUsesDark));
    }

    [Fact]
    public void ProfessionalLightHasApprovedSemanticTokens()
    {
        var palette = TruthDoctorThemeContract.PaletteFor(
            TruthDoctorAppearance.ProfessionalLight,
            false);

        Assert.Equal("#F5F7FB", palette.Canvas);
        Assert.Equal("#FFFFFF", palette.Surface);
        Assert.Equal("#172B4D", palette.TextPrimary);
        Assert.Equal("#2563EB", palette.Primary);
        Assert.Equal("#DCE4EE", palette.Border);

        Assert.NotEqual(palette.Surface, palette.HoverSurface);
        Assert.NotEqual(palette.Border, palette.FocusRing);
    }

    [Fact]
    public void ProfessionalDarkHasDistinctReadableSurfaces()
    {
        var palette = TruthDoctorThemeContract.PaletteFor(
            TruthDoctorAppearance.ProfessionalDark,
            false);

        Assert.NotEqual(palette.Canvas, palette.Surface);
        Assert.NotEqual(palette.Surface, palette.HoverSurface);
        Assert.NotEqual(palette.TextPrimary, palette.Surface);
    }
}
