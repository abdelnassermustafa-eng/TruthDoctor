using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Styling;

namespace TruthDoctor.Theming;

/// <summary>
/// Converts the published TruthDoctor appearance contract
/// into Avalonia theme variants and semantic brushes.
///
/// This adapter creates resources but does not mutate the
/// application, window, persistent settings, or system theme.
/// </summary>
public static class TruthDoctorAvaloniaThemeAdapter
{
    /// <summary>
    /// Resolve a user appearance selection to a concrete
    /// Avalonia Light or Dark theme variant.
    /// </summary>
    public static ThemeVariant ResolveVariant(
        TruthDoctorAppearance appearance,
        bool systemUsesDark)
    {
        var resolved = TruthDoctorThemeContract.Resolve(
            appearance,
            systemUsesDark);

        return resolved switch
        {
            TruthDoctorAppearance.ProfessionalLight =>
                ThemeVariant.Light,

            TruthDoctorAppearance.ProfessionalDark =>
                ThemeVariant.Dark,

            _ => throw new InvalidOperationException(
                "Expected a concrete TruthDoctor appearance.")
        };
    }

    /// <summary>
    /// Build the application-independent semantic brush set.
    ///
    /// Dictionary keys are stable resource names intended
    /// for future Avalonia DynamicResource bindings.
    /// </summary>
    public static IReadOnlyDictionary<string, object> CreateResources(
        TruthDoctorAppearance appearance,
        bool systemUsesDark)
    {
        var palette = TruthDoctorThemeContract.PaletteFor(
            appearance,
            systemUsesDark);

        return new Dictionary<string, object>(
            StringComparer.Ordinal)
        {
            ["TD.CanvasBrush"] =
                MakeBrush(palette.Canvas),

            ["TD.SurfaceBrush"] =
                MakeBrush(palette.Surface),

            ["TD.TextPrimaryBrush"] =
                MakeBrush(palette.TextPrimary),

            ["TD.PrimaryBrush"] =
                MakeBrush(palette.Primary),

            ["TD.BorderBrush"] =
                MakeBrush(palette.Border),

            ["TD.HoverSurfaceBrush"] =
                MakeBrush(palette.HoverSurface),

            ["TD.FocusRingBrush"] =
                MakeBrush(palette.FocusRing)
        };
    }

    private static SolidColorBrush MakeBrush(string hexColor)
    {
        return new SolidColorBrush(Color.Parse(hexColor));
    }
}
