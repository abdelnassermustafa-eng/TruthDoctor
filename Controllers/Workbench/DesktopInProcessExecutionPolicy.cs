namespace TruthDoctor.Controllers.Workbench;

/// <summary>
/// Represents a pure desktop in-process authorization
/// decision. This value does not initiate discovery.
/// </summary>
public enum DesktopInProcessExecutionDecision
{
    LegacyHttp,
    FailClosed,
    OfflineLocalDemo
}

/// <summary>
/// Evaluates explicitly requested in-process discovery
/// against a separate offline local-demo authorization.
///
/// This contract does not read environment variables,
/// authenticate providers, create sessions, modify
/// WorkbenchState, publish snapshots or launch operations.
///
/// The desktop owns configuration acquisition and
/// execution. An unconfigured in-process request
/// must fail closed.
/// </summary>
public static class DesktopInProcessExecutionPolicy
{
    public static DesktopInProcessExecutionDecision Resolve(
        bool inProcessRequested,
        bool offlineLocalDemoAuthorized)
    {
        if (!inProcessRequested)
        {
            return DesktopInProcessExecutionDecision.LegacyHttp;
        }

        if (!offlineLocalDemoAuthorized)
        {
            return DesktopInProcessExecutionDecision.FailClosed;
        }

        return DesktopInProcessExecutionDecision.OfflineLocalDemo;
    }
}
