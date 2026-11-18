using System;
using System.Collections.Generic;
using TruthDoctor.Services.Providers;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A6ProviderPackageRegistryContractTests
{
    [Fact]
    public void RegisterPreservesDeclarativePackageMetadata()
    {
        var registry = new ProviderPackageRegistry();

        var manifest = new ProviderPackageManifest(
            providerId: "aws",
            packageId: "truthdoctor.provider.aws",
            version: "1.0.0",
            minimumHostVersion: "3.0.0",
            dependencies: Array.Empty<string>(),
            declaredCapabilities: new[] { "infrastructure.discovery" });

        registry.Register(manifest);

        var resolved = registry.Resolve("truthdoctor.provider.aws");

        Assert.Same(manifest, resolved);
        Assert.Equal("aws", resolved.ProviderId);
        Assert.Equal("1.0.0", resolved.Version);
        Assert.Equal("3.0.0", resolved.MinimumHostVersion);
        Assert.Empty(resolved.Dependencies);

        Assert.Equal(
            "infrastructure.discovery",
            Assert.Single(resolved.DeclaredCapabilities));

        Assert.Single(registry.GetAll());
    }

    [Fact]
    public void DuplicatePackageIdentityIsRejected()
    {
        var registry = new ProviderPackageRegistry();

        var first = new ProviderPackageManifest(
            "aws",
            "truthdoctor.provider.aws",
            "1.0.0",
            "3.0.0",
            Array.Empty<string>(),
            Array.Empty<string>());

        var duplicate = new ProviderPackageManifest(
            "aws",
            "TRUTHDOCTOR.PROVIDER.AWS",
            "2.0.0",
            "3.0.0",
            Array.Empty<string>(),
            Array.Empty<string>());

        registry.Register(first);

        Assert.Throws<ArgumentException>(() =>
        {
            registry.Register(duplicate);
        });

        Assert.Same(first, Assert.Single(registry.GetAll()));
    }

    [Fact]
    public void InvalidPackageIdentityIsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            _ = new ProviderPackageManifest(
                "aws",
                " ",
                "1.0.0",
                "3.0.0",
                Array.Empty<string>(),
                Array.Empty<string>());
        });
    }

    [Fact]
    public void UnknownPackageCannotBeResolvedByFallback()
    {
        var registry = new ProviderPackageRegistry();

        Assert.Throws<KeyNotFoundException>(() =>
        {
            _ = registry.Resolve("unknown.package");
        });
    }
}
