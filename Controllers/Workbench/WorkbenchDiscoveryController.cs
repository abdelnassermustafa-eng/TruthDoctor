using System;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Services;
using TruthDoctor.State;
using TruthDoctor.Graph;

namespace TruthDoctor.Controllers.Workbench;

public sealed class WorkbenchDiscoveryController
{
    private readonly WorkbenchState _state;
    private readonly PlatformStateClient _client;
    private readonly InfrastructureGraphBuilder
        _graphBuilder;

    public WorkbenchDiscoveryController(
        WorkbenchState state,
        PlatformStateClient? client = null,
        InfrastructureGraphBuilder? graphBuilder = null)
    {
        _state = state;

        _client =
            client ?? new PlatformStateClient();

        _graphBuilder =
            graphBuilder ??
            new InfrastructureGraphBuilder();
    }

    public event EventHandler? DiscoveryStarted;
    public event EventHandler? DiscoveryCompleted;
    public event EventHandler<string>? DiscoveryFailed;

    public async Task<bool> DiscoverAsync(
        string? location = null,
        CancellationToken cancellationToken = default)
    {
        if (_state.IsDiscovering)
        {
            return false;
        }

        try
        {
            _state.IsDiscovering = true;
            _state.LastError = "";
            _state.NotifyChanged();

            DiscoveryStarted?.Invoke(this, EventArgs.Empty);

            var username =
                Environment.GetEnvironmentVariable(
                    "TRUTHDOCTOR_USERNAME")
                ?? "admin";

            var password =
                Environment.GetEnvironmentVariable(
                    "TRUTHDOCTOR_PASSWORD")
                ?? "admin123";

            var authenticated = await _client.LoginAsync(
                username,
                password,
                cancellationToken);

            if (!authenticated)
            {
                throw new InvalidOperationException(
                    "TruthApi authentication failed.");
            }

            var platformState = await _client.GetStateAsync(
                location,
                cancellationToken);

            _state.SetPlatformState(platformState);

            var graph =
                _graphBuilder.Build(platformState);

            _state.SetInfrastructureGraph(graph);

            if (!string.IsNullOrWhiteSpace(location))
            {
                _state.SelectedLocation = location;
            }

            _state.IsDiscovering = false;
            _state.NotifyChanged();

            DiscoveryCompleted?.Invoke(this, EventArgs.Empty);

            return true;
        }
        catch (Exception exception)
        {
            _state.SetFailure(exception.Message);

            DiscoveryFailed?.Invoke(
                this,
                exception.Message);

            return false;
        }
    }

    public Task<bool> RefreshAsync(
        CancellationToken cancellationToken = default)
    {
        return DiscoverAsync(
            _state.SelectedLocation,
            cancellationToken);
    }

    public Task<bool> ChangeLocationAsync(
        string location,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(location);

        _state.SelectedLocation = location;

        return DiscoverAsync(
            location,
            cancellationToken);
    }
}
