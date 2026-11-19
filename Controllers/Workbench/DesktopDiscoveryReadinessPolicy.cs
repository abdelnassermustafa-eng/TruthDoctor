namespace TruthDoctor.Controllers.Workbench;

/// <summary>
/// Pure outcome of desktop discovery readiness evaluation.
/// A ready result does not initiate discovery.
/// </summary>
public enum DesktopDiscoveryReadinessDecision
{
    Ready,
    ConfigurationRequired,
    Rejected
}

/// <summary>
/// Evaluates whether an explicitly selected desktop discovery
/// source is configured and authorized.
///
/// The route flags describe mutually exclusive execution sources.
/// StandaloneProvider is provider-neutral and is not tied to AWS.
///
/// The caller owns configuration and authorization evidence.
/// This policy never reads environment variables or credentials,
/// creates providers, starts HTTP, discovers infrastructure,
/// or changes Workbench state.
///
/// LegacyHttp is an explicit compatibility selection only.
/// A ready legacy decision does not establish standalone
/// desktop acceptance.
/// </summary>
public static class DesktopDiscoveryReadinessPolicy
{
    public static DesktopDiscoveryReadinessDecision Evaluate(
        bool offlineDemo,
        bool offlineFixture,
        bool standaloneProvider,
        bool inProcess,
        bool legacyHttp,
        bool sourceConfigured,
        bool discoveryAuthorized)
    {
        int selectedCount =
            (offlineDemo ? 1 : 0) +
            (offlineFixture ? 1 : 0) +
            (standaloneProvider ? 1 : 0) +
            (inProcess ? 1 : 0) +
            (legacyHttp ? 1 : 0);

        // Conflicting explicit sources must fail closed.
        if (selectedCount > 1)
        {
            return DesktopDiscoveryReadinessDecision.Rejected;
        }

        // Neither permissions nor configuration can implicitly
        // select a discovery source.
        if (selectedCount == 0)
        {
            return DesktopDiscoveryReadinessDecision
                .ConfigurationRequired;
        }

        // Authorization denial is not merely missing configuration.
        if (!discoveryAuthorized)
        {
            return DesktopDiscoveryReadinessDecision.Rejected;
        }

        // A selected source without the required configuration
        // cannot execute discovery.
        if (!sourceConfigured)
        {
            return DesktopDiscoveryReadinessDecision
                .ConfigurationRequired;
        }

        return DesktopDiscoveryReadinessDecision.Ready;
    }
}
