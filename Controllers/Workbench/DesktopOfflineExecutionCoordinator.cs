using System;
using System.Threading;
using System.Threading.Tasks;

namespace TruthDoctor.Controllers.Workbench;

/// <summary>
/// Coordinates an explicitly authorized offline desktop
/// discovery delegate without owning infrastructure state.
///
/// This class does not select provider packages, execute
/// network requests, create sessions, authenticate, or
/// publish discovery snapshots.
/// </summary>
public static class DesktopOfflineExecutionCoordinator
{
    /// <summary>
    /// Evaluates the established dual-opt-in policy and
    /// executes the injected offline delegate exactly once
    /// only when both authorizations are present.
    ///
    /// LegacyHttp is returned without starting HTTP.
    /// FailClosed is returned without running discovery.
    /// Errors from an authorized delegate propagate.
    ///
    /// The desktop remains responsible for loading state,
    /// failure presentation, snapshot ownership, and UI.
    /// </summary>
    public static async Task<DesktopInProcessExecutionDecision>
        ExecuteAsync(
            bool inProcessRequested,
            bool offlineLocalDemoAuthorized,
            Func<CancellationToken, Task> offlineDiscovery,
            CancellationToken cancellationToken)
    {
        DesktopInProcessExecutionDecision decision =
            DesktopInProcessExecutionPolicy.Resolve(
                inProcessRequested,
                offlineLocalDemoAuthorized);

        if (decision !=
            DesktopInProcessExecutionDecision.OfflineLocalDemo)
        {
            return decision;
        }

        ArgumentNullException.ThrowIfNull(offlineDiscovery);

        cancellationToken.ThrowIfCancellationRequested();

        await offlineDiscovery(cancellationToken);

        return DesktopInProcessExecutionDecision.OfflineLocalDemo;
    }
}
