using System;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.State;

namespace TruthDoctor.Controllers.Workbench;

/// <summary>
/// Headless execution boundary for explicitly authorized
/// in-process desktop discovery.
///
/// The caller supplies discovery behavior. This component
/// does not construct providers, create infrastructure
/// snapshots, or own desktop error presentation.
/// </summary>
public static class DesktopInProcessDiscoveryRunner
{
    public static Task<DesktopInProcessExecutionDecision> ExecuteAsync(
        WorkbenchState workbench,
        bool inProcessRequested,
        bool localDemoRequested,
        Func<CancellationToken, Task> discovery,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workbench);
        ArgumentNullException.ThrowIfNull(discovery);

        return DesktopOfflineExecutionCoordinator.ExecuteAsync(
            inProcessRequested,
            localDemoRequested,
            token => DesktopOfflineDiscoveryLifecycle.ExecuteAsync(
                discovery,
                isLoading =>
                {
                    workbench.IsDiscovering = isLoading;
                    workbench.NotifyChanged();
                },
                _ =>
                {
                    // Ordinary failures propagate to the caller.
                    // The existing desktop owns SetFailure and
                    // presentation; do not report twice.
                },
                token),
            cancellationToken);
    }
}
