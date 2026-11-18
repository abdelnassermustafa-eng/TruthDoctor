using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A247ConnectionCatalogExtendedTests
{
    [Fact]
    public void ConcurrentDuplicateRegistrationsHaveOneWinner()
    {
        var catalog = new ProviderConnectionCatalog();
        int succeeded = 0;
        int rejected = 0;

        Parallel.For(0, 32, index =>
        {
            try
            {
                catalog.Register(new ProviderConnectionIdentity(
                    "shared-connection",
                    "provider-" + index));

                Interlocked.Increment(ref succeeded);
            }
            catch (ArgumentException)
            {
                Interlocked.Increment(ref rejected);
            }
        });

        Assert.Equal(1, succeeded);
        Assert.Equal(31, rejected);
        Assert.Equal(
            "shared-connection",
            catalog.Get("shared-connection").ConnectionId);
    }

    [Fact]
    public void UnknownConnectionLookupIsDeterministic()
    {
        var catalog = new ProviderConnectionCatalog();

        Assert.Throws<KeyNotFoundException>(
            () => catalog.Get("missing-connection"));
    }

    [Fact]
    public void NullRegistrationIsRejected()
    {
        var catalog = new ProviderConnectionCatalog();

        Assert.Throws<ArgumentNullException>(
            () => catalog.Register(null!));
    }

    [Fact]
    public void CatalogInstancesRemainIndependent()
    {
        var first = new ProviderConnectionCatalog();
        var second = new ProviderConnectionCatalog();

        var identity = new ProviderConnectionIdentity(
            "shared-id", "aws");

        first.Register(identity);

        Assert.Equal(identity, first.Get("shared-id"));

        Assert.Throws<KeyNotFoundException>(
            () => second.Get("shared-id"));

        second.Register(new ProviderConnectionIdentity(
            "shared-id", "azure"));

        Assert.Equal("aws", first.Get("shared-id").ProviderId);
        Assert.Equal("azure", second.Get("shared-id").ProviderId);
    }

    [Fact]
    public void TryGetSupportsNonThrowingConnectionLookup()
    {
        var catalog = new ProviderConnectionCatalog();

        MethodInfo? method = typeof(ProviderConnectionCatalog)
            .GetMethod(
                "TryGet",
                new[]
                {
                    typeof(string),
                    typeof(ProviderConnectionIdentity).MakeByRefType()
                });

        Assert.NotNull(method);
        Assert.Equal(typeof(bool), method.ReturnType);

        var identity = new ProviderConnectionIdentity(
            "aws-primary", "aws");

        catalog.Register(identity);

        object?[] existingArgs = { "aws-primary", null };

        bool existing = Assert.IsType<bool>(
            method.Invoke(catalog, existingArgs));

        Assert.True(existing);
        Assert.Equal(identity, existingArgs[1]);

        object?[] missingArgs = { "missing", null };

        bool missing = Assert.IsType<bool>(
            method.Invoke(catalog, missingArgs));

        Assert.False(missing);
        Assert.Null(missingArgs[1]);
    }

    [Fact]
    public void UnregisterRemovesOnlySelectedConnection()
    {
        var catalog = new ProviderConnectionCatalog();

        MethodInfo? method = typeof(ProviderConnectionCatalog)
            .GetMethod("Unregister", new[] { typeof(string) });

        Assert.NotNull(method);
        Assert.Equal(typeof(bool), method.ReturnType);

        catalog.Register(new ProviderConnectionIdentity(
            "aws-primary", "aws"));

        var other = new ProviderConnectionIdentity(
            "aws-secondary", "aws");

        catalog.Register(other);

        Assert.True(Assert.IsType<bool>(
            method.Invoke(catalog, new object[] { "aws-primary" })));

        Assert.Throws<KeyNotFoundException>(
            () => catalog.Get("aws-primary"));

        Assert.Equal(other, catalog.Get("aws-secondary"));

        Assert.False(Assert.IsType<bool>(
            method.Invoke(catalog, new object[] { "aws-primary" })));
    }

    [Fact]
    public void GetByProviderReturnsOnlyMatchingConnections()
    {
        var catalog = new ProviderConnectionCatalog();

        MethodInfo? method = typeof(ProviderConnectionCatalog)
            .GetMethod("GetByProvider", new[] { typeof(string) });

        Assert.NotNull(method);

        var first = new ProviderConnectionIdentity(
            "aws-primary", "aws");

        var second = new ProviderConnectionIdentity(
            "aws-secondary", "aws");

        var unrelated = new ProviderConnectionIdentity(
            "azure-primary", "azure");

        catalog.Register(first);
        catalog.Register(second);
        catalog.Register(unrelated);

        object? result = method.Invoke(
            catalog, new object[] { "aws" });

        var matches =
            Assert.IsAssignableFrom<IEnumerable<ProviderConnectionIdentity>>(
                result).ToArray();

        Assert.Equal(2, matches.Length);
        Assert.Contains(first, matches);
        Assert.Contains(second, matches);
        Assert.DoesNotContain(unrelated, matches);
    }
}
