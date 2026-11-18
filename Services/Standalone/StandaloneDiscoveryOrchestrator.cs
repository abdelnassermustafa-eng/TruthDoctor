using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Graph;
using TruthDoctor.Models.Platform;
using TruthDoctor.State;

namespace TruthDoctor.Services.Standalone;

/// <summary>
/// Coordinates authorized provider discovery into workbench state.
/// Authentication and connection lifecycle are owned elsewhere.
/// </summary>
public sealed class StandaloneDiscoveryOrchestrator
{
    // Shared across orchestrator instances, but isolated per
    // workbench. Weak keys avoid retaining unused workbenches.
    private sealed class PublicationGate
    {
        public object Sync { get; } = new();

        public long LatestRequestId { get; set; }
    }

    private static readonly
        ConditionalWeakTable<WorkbenchState, PublicationGate>
        PublicationGates = new();

    private readonly ProviderConnectionCatalog _catalog;
    private readonly StandaloneProviderAdapterCoordinator _coordinator;
    private readonly ProviderConnectionLifecycle _lifecycle;

    public StandaloneDiscoveryOrchestrator(
        ProviderConnectionCatalog catalog,
        StandaloneProviderAdapterCoordinator coordinator,
        ProviderConnectionLifecycle lifecycle)
    {
        _catalog = catalog ??
            throw new ArgumentNullException(nameof(catalog));
        _coordinator = coordinator ??
            throw new ArgumentNullException(nameof(coordinator));
        _lifecycle = lifecycle ??
            throw new ArgumentNullException(nameof(lifecycle));
    }

    private void RequireConnected(string connectionId)
    {
        if (_lifecycle.GetState(connectionId)
            != ProviderConnectionState.Connected)
        {
            throw new InvalidOperationException(
                "Provider connection must be Connected "
                + "before discovery can publish resources.");
        }
    }

    private void RequireSameRegistration(
        string connectionId,
        long originalGeneration)
    {
        long currentGeneration =
            _catalog.GetRegistrationGeneration(connectionId);

        if (currentGeneration != originalGeneration)
        {
            throw new InvalidOperationException(
                "Discovery belongs to a superseded "
                + "provider connection registration.");
        }
    }

    public async Task<PlatformState> DiscoverIntoWorkbenchAsync(
        string connectionId,
        string accountId,
        string location,
        string domainId,
        WorkbenchState workbench,
        CancellationToken cancellationToken = default)
        => await DiscoverIntoWorkbenchWithAwsDomainsAsync(connectionId, accountId, location, domainId, workbench, cancellationToken, false);

    public async Task<PlatformState> DiscoverIntoWorkbenchWithAwsDomainsAsync(
        string connectionId,
        string accountId,
        string location,
        string domainId,
        WorkbenchState workbench,
        CancellationToken cancellationToken = default,
        bool classifyAwsDomains = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        ArgumentNullException.ThrowIfNull(workbench);
        cancellationToken.ThrowIfCancellationRequested();

        long originalGeneration =
            _catalog.GetRegistrationGeneration(connectionId);

        ProviderConnectionIdentity connection =
            _catalog.Get(connectionId);

        RequireSameRegistration(
            connectionId, originalGeneration);
        RequireConnected(connectionId);

        // Latest-started request owns subsequent publication.
        // A failed newer request still supersedes an older one.
        PublicationGate gate = PublicationGates.GetValue(
            workbench, static _ => new PublicationGate());

        long requestId;

        lock (gate.Sync)
        {
            gate.LatestRequestId =
                checked(gate.LatestRequestId + 1);

            requestId = gate.LatestRequestId;
        }

        IStandaloneProviderAdapter adapter =
            _coordinator.ResolveAdapter(connection.ProviderId);

        IReadOnlyList<ProviderResourceRecord> records =
            await adapter.DiscoverAsync(
                connectionId, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
        RequireSameRegistration(
            connectionId, originalGeneration);
        RequireConnected(connectionId);

        if (records is null)
        {
            throw new InvalidOperationException(
                "The provider returned a null discovery snapshot.");
        }

        foreach (ProviderResourceRecord record in records)
        {
            if (record is null ||
                !StringComparer.Ordinal.Equals(
                    connection.ProviderId,
                    record.ProviderId))
            {
                throw new ArgumentException(
                    "Provider discovery results do not match "
                    + "the registered connection.",
                    nameof(records));
            }
        }

        PlatformState platform =
            StandaloneDiscoveryProjector.ProjectWithAwsDomains(
                records, accountId, location, domainId,
                classifyAwsDomains);

        InfrastructureGraph graph =
            new InfrastructureGraphBuilder().Build(platform);

        // Prepare association evidence before publication authorization.
        // This runs outside all provider-registration and publication locks.
        IReadOnlyList<InfrastructureAssociationView> associationViews =
            StringComparer.OrdinalIgnoreCase.Equals(
                connection.ProviderId, "aws")
                ? AwsStandaloneAssociationEvidenceHandoff.Build(
                    platform.Resources)
                : Array.Empty<InfrastructureAssociationView>();

        // Prepare graph-derived objects before authorization.
        var prepared = workbench.PrepareDiscoverySnapshot(
            platform, graph, associationViews);

        // Lock order:
        // workbench publication gate -> catalog -> lifecycle.
        //
        // Registration cannot be replaced or removed between
        // authorization and state commit.
        lock (gate.Sync)
        {
            if (gate.LatestRequestId != requestId)
            {
                throw new InvalidOperationException(
                    "Discovery has been superseded by "
                    + "a newer workbench discovery request.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            _catalog.ExecuteForRegistration(
                connectionId, currentGeneration =>
                {
                    if (currentGeneration != originalGeneration)
                    {
                        throw new InvalidOperationException(
                            "Discovery belongs to a superseded "
                            + "provider connection registration.");
                    }

                    if (_lifecycle.GetState(connectionId)
                        != ProviderConnectionState.Connected)
                    {
                        throw new InvalidOperationException(
                            "Provider connection must be Connected "
                            + "before discovery can publish resources.");
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    // No Changed notification under catalog lock.
                    workbench.CommitPreparedDiscoverySnapshot(
                        prepared);
                });
        }

        // Notify observers outside catalog and publication locks.
        workbench.NotifyCommittedDiscoverySnapshot();

        return platform;
    }
}
