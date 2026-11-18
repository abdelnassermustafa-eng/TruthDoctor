using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TruthDoctor.Services.Standalone;

/// <summary>
/// Coordinates provider connections through the shared registration
/// catalog and lifecycle coordinator.
/// </summary>
/// <remarks>
/// Authentication is supplied by the caller. This component does not
/// access AWS credentials, start TruthApi, or perform discovery.
///
/// Logical cancellation may complete before a provider callback exits.
/// The callback must cooperate with cancellation for physical shutdown.
/// </remarks>
public sealed class ProviderConnectionManager
    : IProviderConnectionManager
{
    private sealed class ConnectionOperation
    {
        public ConnectionOperation(long generation)
        {
            Generation = generation;
        }

        public long Generation { get; }

        // Owned exclusively by the authentication worker.
        // It is disposed only after the callback and cancellation
        // processing have both completed.
        public CancellationTokenSource Cancellation { get; } = new();

        public CancellationTokenRegistration OwnerRegistration { get; set; }

        public Task? CancellationTask { get; set; }

        public TaskCompletionSource<bool> Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool Invalidated { get; set; }
    }

    private readonly object _gate = new();

    private readonly ProviderConnectionCatalog _catalog;
    private readonly ProviderConnectionLifecycle _lifecycle;

    private readonly Func<string, CancellationToken, Task>
        _authenticate;

    private readonly Dictionary<string, ConnectionOperation>
        _operations = new(StringComparer.Ordinal);

    public ProviderConnectionManager(
        ProviderConnectionCatalog catalog,
        ProviderConnectionLifecycle lifecycle,
        Func<string, CancellationToken, Task> authenticate)
    {
        _catalog = catalog ??
            throw new ArgumentNullException(nameof(catalog));

        _lifecycle = lifecycle ??
            throw new ArgumentNullException(nameof(lifecycle));

        _authenticate = authenticate ??
            throw new ArgumentNullException(nameof(authenticate));
    }

    public ProviderConnectionState GetState(string connectionId)
    {
        return _lifecycle.GetState(connectionId);
    }

    public Task ConnectAsync(
        string connectionId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ConnectionOperation operation;
        bool owner = false;

        lock (_gate)
        {
            long generation =
                _catalog.GetRegistrationGeneration(connectionId);

            if (_operations.TryGetValue(
                connectionId, out var existing)
                && existing.Generation == generation
                && !existing.Invalidated)
            {
                operation = existing;
            }
            else
            {
                if (existing is not null)
                {
                    InvalidateWithoutStateMutation(
                        connectionId, existing);
                }

                ProviderConnectionState state =
                    _lifecycle.GetState(connectionId);

                if (state == ProviderConnectionState.Connected)
                {
                    return Task.CompletedTask;
                }

                if (state == ProviderConnectionState.Failed
                    || state == ProviderConnectionState.Expired)
                {
                    _lifecycle.Transition(
                        connectionId,
                        ProviderConnectionState.Disconnected);

                    state = ProviderConnectionState.Disconnected;
                }

                if (state == ProviderConnectionState.Disconnected)
                {
                    _lifecycle.Transition(
                        connectionId,
                        ProviderConnectionState.Configured);
                }
                else if (state != ProviderConnectionState.Configured)
                {
                    throw new InvalidOperationException(
                        "Connection is already undergoing a lifecycle operation.");
                }

                _lifecycle.Transition(
                    connectionId,
                    ProviderConnectionState.Authenticating);

                operation = new ConnectionOperation(generation);
                _operations.Add(connectionId, operation);
                owner = true;
            }
        }

        if (owner)
        {
            // Registration may invoke the callback immediately when
            // the caller is already canceled. Do this outside _gate.
            operation.OwnerRegistration = cancellationToken.Register(
                () => CancelOwnerOperation(connectionId, operation));

            // Always start the worker, including when cancellation
            // has already invalidated the logical operation. The
            // worker owns cancellation registration/resource cleanup.
            _ = RunAuthenticationAsync(connectionId, operation);

            // Return the logical task directly. An extra async/await
            // wrapper would delay visibility of its completion.
            return operation.Completion.Task;
        }
        else
        {
            // Joining caller cancellation is independent of
            // the initiating caller's authentication operation.
            return operation.Completion.Task.WaitAsync(
                cancellationToken);
        }
    }

    private bool IsCurrent(
        string connectionId,
        ConnectionOperation operation)
    {
        return _operations.TryGetValue(
            connectionId, out var current)
            && ReferenceEquals(current, operation)
            && !operation.Invalidated;
    }

    private void RemoveIfCurrent(
        string connectionId,
        ConnectionOperation operation)
    {
        if (_operations.TryGetValue(
            connectionId, out var current)
            && ReferenceEquals(current, operation))
        {
            _operations.Remove(connectionId);
        }
    }

    private void RequestCancellationLocked(
        ConnectionOperation operation)
    {
        // CancelAsync schedules cancellation callbacks and captures
        // their exceptions in its returned Task. It does not deliver
        // provider callback exceptions to DisconnectAsync or to the
        // caller executing CancellationTokenSource.Cancel().
        operation.CancellationTask ??=
            operation.Cancellation.CancelAsync();
    }

    private void InvalidateWithoutStateMutation(
        string connectionId,
        ConnectionOperation operation)
    {
        operation.Invalidated = true;
        RemoveIfCurrent(connectionId, operation);

        // Logical completion must not await a callback that ignores
        // cancellation or remains blocked in an external provider.
        operation.Completion.TrySetCanceled();
        RequestCancellationLocked(operation);
    }

    private void InvalidateCurrent(
        string connectionId,
        ConnectionOperation operation)
    {
        if (!IsCurrent(connectionId, operation))
        {
            return;
        }

        // Change only the same registration generation. Never
        // mutate the lifecycle of a replacement registration.
        try
        {
            _catalog.ExecuteForRegistration(
                connectionId,
                generation =>
                {
                    if (generation != operation.Generation)
                    {
                        return;
                    }

                    var state = _lifecycle.GetState(connectionId);

                    if (state == ProviderConnectionState.Authenticating)
                    {
                        _lifecycle.Transition(
                            connectionId,
                            ProviderConnectionState.Failed);

                        _lifecycle.Transition(
                            connectionId,
                            ProviderConnectionState.Disconnected);
                    }
                });
        }
        catch (KeyNotFoundException)
        {
            // A removed registration has no lifecycle to reset.
        }

        InvalidateWithoutStateMutation(connectionId, operation);
    }

    private void CancelOwnerOperation(
        string connectionId,
        ConnectionOperation operation)
    {
        // This callback must never propagate provider cancellation
        // callback failures to the owner cancellation caller.
        lock (_gate)
        {
            InvalidateCurrent(connectionId, operation);
        }
    }

    private async Task RunAuthenticationAsync(
        string connectionId,
        ConnectionOperation operation)
    {
        try
        {
            operation.Cancellation.Token
                .ThrowIfCancellationRequested();

            await _authenticate(
                connectionId,
                operation.Cancellation.Token)
                .ConfigureAwait(false);

            lock (_gate)
            {
                if (!IsCurrent(connectionId, operation))
                {
                    throw new OperationCanceledException(
                        "Connection operation was invalidated.");
                }

                operation.Cancellation.Token
                    .ThrowIfCancellationRequested();

                _catalog.ExecuteForRegistration(
                    connectionId,
                    generation =>
                    {
                        if (generation != operation.Generation)
                        {
                            throw new OperationCanceledException(
                                "Registration generation was replaced.");
                        }

                        if (!IsCurrent(connectionId, operation))
                        {
                            throw new OperationCanceledException(
                                "Connection operation was invalidated.");
                        }

                        operation.Cancellation.Token
                            .ThrowIfCancellationRequested();

                        _lifecycle.Transition(
                            connectionId,
                            ProviderConnectionState.Authenticated);

                        _lifecycle.Transition(
                            connectionId,
                            ProviderConnectionState.Connecting);

                        _lifecycle.Transition(
                            connectionId,
                            ProviderConnectionState.Connected);
                    });

                RemoveIfCurrent(connectionId, operation);
            }

            operation.Completion.TrySetResult(true);
        }
        catch (Exception error)
        {
            bool canceled;
            Exception failure = error;

            lock (_gate)
            {
                canceled =
                    error is OperationCanceledException
                    || operation.Invalidated
                    || operation.Cancellation.IsCancellationRequested;

                if (IsCurrent(connectionId, operation))
                {
                    try
                    {
                        _catalog.ExecuteForRegistration(
                            connectionId,
                            generation =>
                            {
                                if (generation != operation.Generation)
                                {
                                    canceled = true;
                                    return;
                                }

                                var state =
                                    _lifecycle.GetState(connectionId);

                                if (state ==
                                    ProviderConnectionState.Authenticating)
                                {
                                    _lifecycle.Transition(
                                        connectionId,
                                        ProviderConnectionState.Failed);

                                    if (canceled)
                                    {
                                        _lifecycle.Transition(
                                            connectionId,
                                            ProviderConnectionState.Disconnected);
                                    }
                                }
                            });
                    }
                    catch (KeyNotFoundException)
                    {
                        canceled = true;
                    }
                    catch (Exception cleanupError)
                    {
                        failure = new AggregateException(
                            error, cleanupError);
                    }

                    RemoveIfCurrent(connectionId, operation);
                }
                else
                {
                    canceled = true;
                }
            }

            if (canceled)
            {
                operation.Completion.TrySetCanceled();
            }
            else
            {
                operation.Completion.TrySetException(failure);
            }
        }
        finally
        {
            // Never dispose the source while cancellation callbacks
            // may still be running. An uncooperative authentication
            // callback retains resources until its physical exit.
            await operation.OwnerRegistration
                .DisposeAsync().ConfigureAwait(false);

            Task cancellationTask;

            lock (_gate)
            {
                cancellationTask =
                    operation.CancellationTask ?? Task.CompletedTask;
            }

            try
            {
                await cancellationTask.ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Cancellation callbacks may throw. Those failures
                // must not escape and overturn logical disconnect.
            }

            operation.Cancellation.Dispose();
        }
    }

    public Task DisconnectAsync(
        string connectionId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Task? staleCancellation = null;

        lock (_gate)
        {
            long generation =
                _catalog.GetRegistrationGeneration(connectionId);

            if (_operations.TryGetValue(
                connectionId, out var active)
                && !active.Invalidated)
            {
                if (active.Generation == generation)
                {
                    // The active operation belongs to this
                    // registration; preserve existing behavior.
                    InvalidateCurrent(connectionId, active);
                    return Task.CompletedTask;
                }

                // The catalog now identifies a replacement
                // registration. Cancel the predecessor without
                // changing the replacement's lifecycle state.
                InvalidateWithoutStateMutation(
                    connectionId, active);

                staleCancellation = active.CancellationTask;
            }

            // Apply disconnect semantics to the current
            // registration independently of stale cleanup.
            var state = _lifecycle.GetState(connectionId);

            if (state == ProviderConnectionState.Connected
                || state == ProviderConnectionState.Failed
                || state == ProviderConnectionState.Expired)
            {
                _lifecycle.Transition(
                    connectionId,
                    ProviderConnectionState.Disconnected);
            }
            else if (state != ProviderConnectionState.Disconnected)
            {
                throw new InvalidOperationException(
                    "Cannot disconnect an unmanaged lifecycle operation.");
            }
        }

        // Ensure a stale provider callback has observed cancellation
        // before the stale-disconnect task completes. No manager
        // or catalog lock is held while awaiting cancellation.
        return staleCancellation is null
            ? Task.CompletedTask
            : ObserveStaleCancellationAsync(staleCancellation);
    }

    private static async Task ObserveStaleCancellationAsync(
        Task cancellationTask)
    {
        try
        {
            await cancellationTask.ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Provider cancellation callbacks are allowed to fail.
            // Their exceptions must not override logical disconnect.
            // The authentication worker also observes this task
            // before disposing the cancellation source.
        }
    }
}
