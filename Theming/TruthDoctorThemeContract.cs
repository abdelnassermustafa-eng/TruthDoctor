using System;

namespace TruthDoctor.Theming;

/// <summary>
/// User-facing appearance preferences.
///
/// FollowSystem is a preference, not a concrete palette.
/// </summary>
public enum TruthDoctorAppearance
{
    ProfessionalLight = 0,
    ProfessionalDark = 1,
    FollowSystem = 2
}

/// <summary>
/// Immutable semantic palette contract.
///
/// Colors are represented as #RRGGBB strings so the
/// palette is testable without initializing Avalonia.
///
/// Future presentation adapters will convert these
/// values into Avalonia brushes and resources.
/// </summary>
public sealed record TruthDoctorThemePalette(
    string Canvas,
    string Surface,
    string TextPrimary,
    string Primary,
    string Border,
    string HoverSurface,
    string FocusRing
);

/// <summary>
/// Central theme selection and semantic token foundation.
///
/// This contract does not alter the running Avalonia
/// application or user preference storage.
/// </summary>
public static class TruthDoctorThemeContract
{
    public static TruthDoctorAppearance DefaultAppearance =>
        TruthDoctorAppearance.ProfessionalLight;

    private static readonly TruthDoctorThemePalette LightPalette =
        new(
            Canvas: "#F5F7FB",
            Surface: "#FFFFFF",
            TextPrimary: "#172B4D",
            Primary: "#2563EB",
            Border: "#DCE4EE",
            HoverSurface: "#EAF0F7",
            FocusRing: "#1D4ED8"
        );

    private static readonly TruthDoctorThemePalette DarkPalette =
        new(
            Canvas: "#07111F",
            Surface: "#101F35",
            TextPrimary: "#F8FAFC",
            Primary: "#60A5FA",
            Border: "#334A68",
            HoverSurface: "#223149",
            FocusRing: "#93C5FD"
        );

    public static TruthDoctorAppearance Resolve(
        TruthDoctorAppearance appearance,
        bool systemUsesDark)
    {
        return appearance switch
        {
            TruthDoctorAppearance.ProfessionalLight =>
                TruthDoctorAppearance.ProfessionalLight,

            TruthDoctorAppearance.ProfessionalDark =>
                TruthDoctorAppearance.ProfessionalDark,

            TruthDoctorAppearance.FollowSystem =>
                systemUsesDark
                    ? TruthDoctorAppearance.ProfessionalDark
                    : TruthDoctorAppearance.ProfessionalLight,

            _ => throw new ArgumentOutOfRangeException(
                nameof(appearance),
                appearance,
                "Unsupported TruthDoctor appearance.")
        };
    }

    public static TruthDoctorThemePalette PaletteFor(
        TruthDoctorAppearance appearance,
        bool systemUsesDark)
    {
        return Resolve(appearance, systemUsesDark) switch
        {
            TruthDoctorAppearance.ProfessionalLight =>
                LightPalette,

            TruthDoctorAppearance.ProfessionalDark =>
                DarkPalette,

            _ => throw new InvalidOperationException(
                "Theme resolution did not return a concrete palette.")
        };
    }
}
