using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A319ProviderAdapterBoundaryContractTests
{
    private const string ConnectionId = "aws-primary";

    private sealed class MockProviderAdapter :
        IStandaloneProviderAdapter
    {
        private readonly Func<string, CancellationToken, Task>
            _authenticate;

        private readonly Func<string, CancellationToken, Task>
            _disconnect;

        private readonly Func<
            string,
            CancellationToken,
            Task<IReadOnlyList<ProviderResourceRecord>>>
            _discover;

        public MockProviderAdapter(
            Func<string, CancellationToken, Task>? authenticate = null,
            Func<string, CancellationToken, Task>? disconnect = null,
            Func<
                string,
                CancellationToken,
                Task<IReadOnlyList<ProviderResourceRecord>>>?
                discover = null)
        {
            _authenticate = authenticate ??
                ((_, _) => Task.CompletedTask);

            _disconnect = disconnect ??
                ((_, _) => Task.CompletedTask);

            _discover = discover ??
                ((_, _) => Task.FromResult<
                    IReadOnlyList<ProviderResourceRecord>>(
                    Array.Empty<ProviderResourceRecord>()));
        }

        public string ProviderId => "aws";

        public Task AuthenticateAsync(
            string connectionId,
            CancellationToken cancellationToken) =>
            _authenticate(connectionId, cancellationToken);

        public Task DisconnectAsync(
            string connectionId,
            CancellationToken cancellationToken) =>
            _disconnect(connectionId, cancellationToken);

        public Task<IReadOnlyList<ProviderResourceRecord>>
            DiscoverAsync(
                string connectionId,
                CancellationToken cancellationToken) =>
            _discover(connectionId, cancellationToken);
    }

    [Fact]
    public void ProviderIdentityIsStableAndExplicit()
    {
        IStandaloneProviderAdapter adapter =
            new MockProviderAdapter();

        Assert.Equal("aws", adapter.ProviderId);

        Assert.True(
            typeof(IStandaloneProviderAdapter).IsInterface);

        Assert.Equal(
            typeof(string),
            typeof(IStandaloneProviderAdapter)
                .GetProperty(nameof(
                    IStandaloneProviderAdapter.ProviderId))!
                .PropertyType);
    }

    [Fact]
    public async Task AuthenticationReceivesConnectionIdentity()
    {
        string? observedConnection = null;

        IStandaloneProviderAdapter adapter =
            new MockProviderAdapter(
                authenticate: (connectionId, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    observedConnection = connectionId;
                    return Task.CompletedTask;
                });

        await adapter.AuthenticateAsync(
            ConnectionId,
            CancellationToken.None);

        Assert.Equal(ConnectionId, observedConnection);

        var method = typeof(IStandaloneProviderAdapter)
            .GetMethod(nameof(
                IStandaloneProviderAdapter.AuthenticateAsync));

        Assert.NotNull(method);
        Assert.Equal(typeof(Task), method!.ReturnType);

        Assert.Equal(
            new[] {
                typeof(string),
                typeof(CancellationToken)
            },
            method.GetParameters()
                .Select(parameter => parameter.ParameterType));
    }

    [Fact]
    public async Task AuthenticationCancellationReachesAdapter()
    {
        bool invoked = false;

        IStandaloneProviderAdapter adapter =
            new MockProviderAdapter(
                authenticate: (_, token) =>
                {
                    invoked = true;
                    token.ThrowIfCancellationRequested();
                    return Task.CompletedTask;
                });

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => adapter.AuthenticateAsync(
                ConnectionId,
                cancellation.Token));

        Assert.True(invoked);
    }

    [Fact]
    public async Task DisconnectReceivesConnectionIdentity()
    {
        string? observedConnection = null;

        IStandaloneProviderAdapter adapter =
            new MockProviderAdapter(
                disconnect: (connectionId, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    observedConnection = connectionId;
                    return Task.CompletedTask;
                });

        await adapter.DisconnectAsync(
            ConnectionId,
            CancellationToken.None);

        Assert.Equal(ConnectionId, observedConnection);

        var method = typeof(IStandaloneProviderAdapter)
            .GetMethod(nameof(
                IStandaloneProviderAdapter.DisconnectAsync));

        Assert.NotNull(method);
        Assert.Equal(typeof(Task), method!.ReturnType);
    }

    [Fact]
    public async Task DiscoveryReturnsTypedResourceRecords()
    {
        var resource = new ProviderResourceRecord(
            "aws",
            "vpc-test-001",
            "vpc",
            "Deterministic Test VPC");

        IStandaloneProviderAdapter adapter =
            new MockProviderAdapter(
                discover: (_, token) =>
                {
                    token.ThrowIfCancellationRequested();

                    IReadOnlyList<ProviderResourceRecord> records =
                        new[] { resource };

                    return Task.FromResult(records);
                });

        var discovered = await adapter.DiscoverAsync(
            ConnectionId,
            CancellationToken.None);

        var result = Assert.Single(discovered);

        Assert.Equal("aws", result.ProviderId);
        Assert.Equal("vpc-test-001", result.ResourceId);
        Assert.Equal("vpc", result.ResourceType);
        Assert.Equal("Deterministic Test VPC", result.DisplayName);
    }

    [Fact]
    public async Task DiscoveryCancellationReachesAdapter()
    {
        bool invoked = false;

        IStandaloneProviderAdapter adapter =
            new MockProviderAdapter(
                discover: (_, token) =>
                {
                    invoked = true;
                    token.ThrowIfCancellationRequested();

                    IReadOnlyList<ProviderResourceRecord> empty =
                        Array.Empty<ProviderResourceRecord>();

                    return Task.FromResult(empty);
                });

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => adapter.DiscoverAsync(
                ConnectionId,
                cancellation.Token));

        Assert.True(invoked);
    }
}
