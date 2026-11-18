using System;

namespace TruthDoctor.Controllers.Workbench;

/// <summary>
/// Represents the desktop discovery execution route.
/// This value selects a route; it does not perform
/// discovery, authentication, or provider installation.
/// </summary>
public enum DesktopDiscoveryRoute
{
    LegacyHttp,
    LocalDemo,
    SyntheticSecurityGroup,
    AwsStandalone,
    InProcess
}

/// <summary>
/// Selects exactly one desktop discovery route.
///
/// The legacy HTTP route remains the default until
/// a separate desktop migration is authorized.
///
/// Explicit modes are mutually exclusive. Conflicting
/// selections fail closed rather than silently applying
/// precedence or launching a provider.
/// </summary>
public static class DesktopDiscoveryRouteSelector
{
    public static DesktopDiscoveryRoute Select(
        bool localDemo,
        bool syntheticSecurityGroup,
        bool awsStandalone,
        bool inProcess)
    {
        int selected =
            (localDemo ? 1 : 0) +
            (syntheticSecurityGroup ? 1 : 0) +
            (awsStandalone ? 1 : 0) +
            (inProcess ? 1 : 0);

        if (selected > 1)
        {
            throw new InvalidOperationException(
                "Conflicting desktop discovery modes are not permitted.");
        }

        if (localDemo)
        {
            return DesktopDiscoveryRoute.LocalDemo;
        }

        if (syntheticSecurityGroup)
        {
            return DesktopDiscoveryRoute.SyntheticSecurityGroup;
        }

        if (awsStandalone)
        {
            return DesktopDiscoveryRoute.AwsStandalone;
        }

        if (inProcess)
        {
            return DesktopDiscoveryRoute.InProcess;
        }

        return DesktopDiscoveryRoute.LegacyHttp;
    }
}
