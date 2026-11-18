using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Models.Platform;
using TruthDoctor.State;

namespace TruthDoctor.Services.Standalone;

/// <summary>
/// Explicitly synthetic AWS security-group discovery fixture.
///
/// Uses the existing local provider-adapter and discovery
/// orchestration contracts without AWS credentials, SDK,
/// CLI, HTTP, or other external provider access.
///
/// This class does not register itself in desktop startup.
/// </summary>
public sealed class StandaloneSyntheticAwsSecurityGroupSession
{
    private const string ConnectionId =
        "truthdoctor-synthetic-aws-sg";

    private const string ProviderIdValue = "aws";

    private readonly StandaloneDiscoveryOrchestrator _orchestrator;

    public StandaloneSyntheticAwsSecurityGroupSession()
    {
        var runtime = new StandaloneBackendRuntime();

        runtime.ConnectionCatalog.Register(
            new ProviderConnectionIdentity(
                ConnectionId,
                ProviderIdValue));

        var lifecycle = runtime.ConnectionLifecycle;

        lifecycle.Transition(
            ConnectionId,
            ProviderConnectionState.Configured);

        lifecycle.Transition(
            ConnectionId,
            ProviderConnectionState.Authenticating);

        lifecycle.Transition(
            ConnectionId,
            ProviderConnectionState.Authenticated);

        lifecycle.Transition(
            ConnectionId,
            ProviderConnectionState.Connecting);

        lifecycle.Transition(
            ConnectionId,
            ProviderConnectionState.Connected);

        var coordinator =
            new StandaloneProviderAdapterCoordinator(
                runtime.ConnectionCatalog,
                lifecycle);

        coordinator.RegisterAdapter(
            new SyntheticSecurityGroupAdapter());

        _orchestrator = new StandaloneDiscoveryOrchestrator(
            runtime.ConnectionCatalog,
            coordinator,
            lifecycle);
    }

    public Task<PlatformState> DiscoverAsync(
        WorkbenchState workbench,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workbench);

        return _orchestrator.DiscoverIntoWorkbenchAsync(
            ConnectionId,
            "000000000000",
            "synthetic-offline",
            "networking",
            workbench,
            cancellationToken);
    }

    private sealed class SyntheticSecurityGroupAdapter
        : IStandaloneProviderAdapter
    {
        public string ProviderId => ProviderIdValue;

        public Task AuthenticateAsync(
            string connectionId,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task DisconnectAsync(
            string connectionId,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<ProviderResourceRecord>>
            DiscoverAsync(
                string connectionId,
                CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IReadOnlyList<ProviderResourceRecord> resources =
            [
                new ProviderResourceRecord(
                    ProviderIdValue,
                    "sg-owner",
                    "security-group",
                    "Synthetic SG Owner",
                    Properties:
                        new Dictionary<string, string>(
                            StringComparer.Ordinal)
                        {
                            [
                                "IpPermissions[0].UserIdGroupPairs[0].GroupId"
                            ] = "sg-ingress",

                            [
                                "IpPermissionsEgress[0].UserIdGroupPairs[0].GroupId"
                            ] = "sg-egress"
                        }),

                new ProviderResourceRecord(
                    ProviderIdValue,
                    "sg-ingress",
                    "security-group",
                    "Synthetic SG Ingress"),

                new ProviderResourceRecord(
                    ProviderIdValue,
                    "sg-egress",
                    "security-group",
                    "Synthetic SG Egress")
            ];

            return Task.FromResult(resources);
        }
    }
}
