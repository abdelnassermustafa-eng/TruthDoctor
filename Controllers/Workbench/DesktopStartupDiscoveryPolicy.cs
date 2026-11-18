namespace TruthDoctor.Controllers.Workbench;

/// <summary>
/// Separates showing the workbench from explicitly requested discovery.
/// No selected discovery route means the desktop stays idle at startup.
/// Conflicting explicit routes still reach the existing fail-closed selector.
/// </summary>
public static class DesktopStartupDiscoveryPolicy
{
    public static bool ShouldAutoDiscover(
        bool localDemo,
        bool syntheticSecurityGroup,
        bool awsStandalone,
        bool inProcess)
        => localDemo || syntheticSecurityGroup || awsStandalone || inProcess;
}
