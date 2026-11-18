using System;
using System.Threading;
using System.Threading.Tasks;

namespace TruthDoctor.Controllers.Workbench;

/// <summary>
/// Provides a provider-neutral lifecycle boundary for
/// explicitly injected offline discovery operations.
///
/// The caller owns UI presentation, WorkbenchState,
/// provider selection, and snapshot publication.
/// This class does not start network requests or
/// acquire credentials.
/// </summary>
public static class DesktopOfflineDiscoveryLifecycle
{
    /// <summary>
    /// Executes one injected offline discovery operation,
    /// balances loading notifications, and forwards errors.
    ///
    /// An already-cancelled operation does not enter the
    /// loading state or dispatch the discovery delegate.
    ///
    /// Ordinary execution failures are reported and
    /// rethrown. Cancellation is propagated without being
    /// misclassified as an ordinary discovery failure.
    ///
    /// Once loading begins, its cleanup is guaranteed
    /// by the finally block.
    /// </summary>
    public static async Task ExecuteAsync(
        Func<CancellationToken, Task> discovery,
        Action<bool> setLoading,
        Action<Exception> reportFailure,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(discovery);
        ArgumentNullException.ThrowIfNull(setLoading);
        ArgumentNullException.ThrowIfNull(reportFailure);

        cancellationToken.ThrowIfCancellationRequested();

        setLoading(true);

        try
        {
            await discovery(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            reportFailure(exception);
            throw;
        }
        finally
        {
            setLoading(false);
        }
    }
}
