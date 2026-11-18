using System;
using System.Collections.Generic;
using TruthDoctor.Services.Providers;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A6ProviderPackageMetadataSafetyTests
{
    [Fact]
    public void RejectsSelfDependencyIgnoringCase()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            _ = new ProviderPackageManifest(
                providerId: "aws",
                packageId: "truthdoctor.provider.aws",
                version: "1.0.0",
                minimumHostVersion: "3.0.0",
                dependencies: new[] { "TRUTHDOCTOR.PROVIDER.AWS" },
                declaredCapabilities: Array.Empty<string>());
        });
    }

    [Fact]
    public void RejectsMalformedPackageVersion()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            _ = new ProviderPackageManifest(
                providerId: "aws",
                packageId: "truthdoctor.provider.aws",
                version: "not-a-version",
                minimumHostVersion: "3.0.0",
                dependencies: Array.Empty<string>(),
                declaredCapabilities: Array.Empty<string>());
        });
    }

    [Fact]
    public void RejectsMalformedMinimumHostVersion()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            _ = new ProviderPackageManifest(
                providerId: "aws",
                packageId: "truthdoctor.provider.aws",
                version: "1.0.0",
                minimumHostVersion: "three",
                dependencies: Array.Empty<string>(),
                declaredCapabilities: Array.Empty<string>());
        });
    }

    [Fact]
    public void SnapshotsCallerOwnedMetadataCollections()
    {
        var dependencies = new List<string>
        {
            "truthdoctor.provider.shared"
        };

        var capabilities = new List<string>
        {
            "infrastructure.discovery"
        };

        var manifest = new ProviderPackageManifest(
            providerId: "aws",
            packageId: "truthdoctor.provider.aws",
            version: "1.0.0",
            minimumHostVersion: "3.0.0",
            dependencies: dependencies,
            declaredCapabilities: capabilities);

        dependencies.Add("truthdoctor.provider.changed");
        capabilities.Add("infrastructure.modified");

        Assert.Equal(
            "truthdoctor.provider.shared",
            Assert.Single(manifest.Dependencies));

        Assert.Equal(
            "infrastructure.discovery",
            Assert.Single(manifest.DeclaredCapabilities));
    }
}
