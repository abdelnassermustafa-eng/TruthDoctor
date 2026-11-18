namespace TruthDoctor.Controllers.Workbench;

/// <summary>
/// Manual discovery is fail-closed when neither an explicitly configured
/// route nor an explicitly selected offline demo is present.
/// </summary>
public static class DesktopManualDiscoveryPolicy
{
    public static bool MayUseLegacyHttp(
        bool legacyRouteSelected,
        bool legacyHttpExplicitlyAuthorized)
        => legacyRouteSelected && legacyHttpExplicitlyAuthorized;

    public static bool MayUseOfflineDemo(bool userSelectedOfflineDemo)
        => userSelectedOfflineDemo;
}
