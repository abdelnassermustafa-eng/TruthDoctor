using System;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Models.Platform;
using TruthDoctor.State;

namespace TruthDoctor.Controllers.Workbench;

/// <summary>
/// Delegates an explicitly configured in-process discovery
/// request to its owning standalone session.
///
/// The session and its orchestrator own all discovery,
/// authentication, graph construction, state publication,
/// notification, and connection lifecycle behavior.
///
/// This bridge does not independently publish or rebuild
/// the returned discovery snapshot.
/// </summary>
public static class DesktopInProcessDiscoveryBridge
{
    public static async Task<PlatformState> ExecuteAsync(
        WorkbenchState workbench,
        Func<
            WorkbenchState,
            string?,
            CancellationToken,
            Task<PlatformState>> sessionDiscovery,
        string? location,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workbench);
        ArgumentNullException.ThrowIfNull(sessionDiscovery);

        cancellationToken.ThrowIfCancellationRequested();

        // Exactly one call to the configured session.
        // Do not re-publish its already committed state.
        Task<PlatformState> discoveryTask =
            sessionDiscovery(
                workbench,
                location,
                cancellationToken);

        if (discoveryTask is null)
        {
            throw new InvalidOperationException(
                "In-process discovery returned no task.");
        }

        PlatformState platform =
            await discoveryTask.ConfigureAwait(false);

        return platform ??
            throw new InvalidOperationException(
                "In-process discovery returned no platform state.");
    }
}
