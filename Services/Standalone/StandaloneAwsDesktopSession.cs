using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Models.Platform;
using TruthDoctor.State;

namespace TruthDoctor.Services.Standalone;

/// <summary>
/// Opt-in standalone AWS desktop session.
/// Uses an existing AWS CLI profile, never stores credentials,
/// and never invokes a provider during construction.
/// </summary>
public sealed class StandaloneAwsDesktopSession
{
    private const string ConnectionId = "truthdoctor-desktop-aws";

    private readonly StandaloneBackendRuntime _runtime = new();
    private readonly AwsCliMultiServiceAdapter _adapter;
    private readonly StandaloneDiscoveryOrchestrator _orchestrator;
    private readonly SemaphoreSlim _connectionGate = new(1, 1);
    private readonly string _primaryRegion;
    private readonly string _profile;
    private readonly bool _enableRegionCatalog;
    private readonly Func<IReadOnlyList<string>,
        CancellationToken, Task<string>>? _runner;

    private bool _connected;

    public StandaloneAwsDesktopSession(
        string profile,
        IEnumerable<string> regions,
        Func<IReadOnlyList<string>,
            CancellationToken, Task<string>>? runner = null)
        : this(profile, regions, runner, false)
    {
    }

    public StandaloneAwsDesktopSession(
        string profile,
        IEnumerable<string> regions,
        Func<IReadOnlyList<string>,
            CancellationToken, Task<string>>? runner,
        bool enableRegionCatalog)
    {
        ArgumentNullException.ThrowIfNull(regions);

        _enableRegionCatalog = enableRegionCatalog;

        string[] selectedRegions = regions.ToArray();

        if (selectedRegions.Length == 0)
        {
            throw new ArgumentException(
                "At least one AWS discovery region is required.",
                nameof(regions));
        }

        _primaryRegion = selectedRegions[0];
        _profile = profile;
        _runner = runner;

        _runtime.ConnectionCatalog.Register(
            new ProviderConnectionIdentity(
                ConnectionId, "aws"));

        _adapter = new AwsCliMultiServiceAdapter(
            ConnectionId,
            profile,
            selectedRegions,
            runner,
            includeNetworking: string.Equals(
                Environment.GetEnvironmentVariable(
                    "TRUTHDOCTOR_AWS_NETWORKING"),
                "1", StringComparison.Ordinal),
            enableCloudTrailCreationEvidence: string.Equals(
                Environment.GetEnvironmentVariable(
                    "TRUTHDOCTOR_AWS_CLOUDTRAIL"),
                "1", StringComparison.Ordinal));

        var lifecycle = _runtime.ConnectionLifecycle;

        var coordinator = new StandaloneProviderAdapterCoordinator(
            _runtime.ConnectionCatalog,
            lifecycle);

        coordinator.RegisterAdapter(_adapter);

        _orchestrator = new StandaloneDiscoveryOrchestrator(
            _runtime.ConnectionCatalog,
            coordinator,
            lifecycle);
    }

    /// <summary>
    /// Selecting this environment option explicitly authorizes
    /// read-only discovery when the desktop starts or refreshes.
    /// No AWS call occurs during configuration.
    /// </summary>
    public static StandaloneAwsDesktopSession? FromEnvironment()
    {
        if (Environment.GetEnvironmentVariable(
            "TRUTHDOCTOR_AWS_DISCOVERY") != "1")
        {
            return null;
        }

        string? profile = Environment.GetEnvironmentVariable(
            "TRUTHDOCTOR_AWS_PROFILE");

        if (string.IsNullOrWhiteSpace(profile))
        {
            throw new InvalidOperationException(
                "TRUTHDOCTOR_AWS_PROFILE is required when "
                + "AWS desktop discovery is enabled.");
        }

        string regionsValue =
            Environment.GetEnvironmentVariable(
                "TRUTHDOCTOR_AWS_REGIONS")
            ?? "us-east-1";

        string[] regions = regionsValue.Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries
            | StringSplitOptions.TrimEntries);

        return new StandaloneAwsDesktopSession(
            profile, regions, null, enableRegionCatalog: true);
    }

    private async Task EnsureConnectedAsync(
        CancellationToken cancellationToken)
    {
        await _connectionGate.WaitAsync(
            cancellationToken);

        try
        {
            if (_connected)
            {
                return;
            }

            var lifecycle = _runtime.ConnectionLifecycle;

            lifecycle.Transition(
                ConnectionId,
                ProviderConnectionState.Configured);

            lifecycle.Transition(
                ConnectionId,
                ProviderConnectionState.Authenticating);

            await _adapter.AuthenticateAsync(
                ConnectionId,
                cancellationToken);

            lifecycle.Transition(
                ConnectionId,
                ProviderConnectionState.Authenticated);

            lifecycle.Transition(
                ConnectionId,
                ProviderConnectionState.Connecting);

            lifecycle.Transition(
                ConnectionId,
                ProviderConnectionState.Connected);

            _connected = true;
        }
        finally
        {
            _connectionGate.Release();
        }
    }

    public async Task<PlatformState> DiscoverAsync(
        WorkbenchState workbench,
        string? requestedLocation = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workbench);

        // A selected Region must produce a real scan in that
        // Region, not simply change the workbench label.
        if (!string.IsNullOrWhiteSpace(requestedLocation)
            && !string.Equals(
                requestedLocation,
                _primaryRegion,
                StringComparison.OrdinalIgnoreCase))
        {
            var regionalSession =
                new StandaloneAwsDesktopSession(
                    _profile,
                    new[] { requestedLocation },
                    _runner,
                    _enableRegionCatalog);

            return await regionalSession.DiscoverAsync(
                workbench,
                cancellationToken: cancellationToken);
        }

        await EnsureConnectedAsync(cancellationToken);

        string accountId =
            _adapter.AuthenticatedAccountId
            ?? throw new InvalidOperationException(
                "AWS caller identity is unavailable.");

        // The adapter scans its explicitly configured regions.
        // The workbench location identifies the primary view.
        string location =
            string.IsNullOrWhiteSpace(requestedLocation)
                ? _primaryRegion
                : requestedLocation;

        PlatformState platform = await
            _orchestrator.DiscoverIntoWorkbenchWithAwsDomainsAsync(
            ConnectionId,
            accountId,
            location,
            "infrastructure",
            workbench,
            cancellationToken,
                classifyAwsDomains: true);

        // Populate selectable Regions separately from discovery.
        // A catalog failure must not erase a successful inventory.
        if (_enableRegionCatalog)
        {
            try
            {
                var available = await
                    _adapter.ListAvailableRegionsAsync(
                        cancellationToken);

            platform.Context.Locations = available.ToList();

            if (!platform.Context.Locations.Contains(
                location, StringComparer.OrdinalIgnoreCase))
            {
                platform.Context.Locations.Add(location);
            }
        }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                platform.Warnings.Add(
                    "AWS Region catalog unavailable: "
                    + exception.Message);
            }
        }

        return platform;
    }
}
