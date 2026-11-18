using System;
using TruthDoctor.Services.Providers;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A6ProviderPackageTrustBoundaryTests
{
    private static ProviderPackageManifest Package()
    {
        return new ProviderPackageManifest(
            providerId: "aws",
            packageId: "truthdoctor.provider.aws",
            version: "1.0.0",
            minimumHostVersion: "3.0.0",
            dependencies: Array.Empty<string>(),
            declaredCapabilities: new[]
            {
                "infrastructure.discovery"
            });
    }

    [Fact]
    public void ValidMetadataDoesNotEstablishTrust()
    {
        var gate = new ProviderPackageTrustGate();

        Assert.False(gate.IsTrusted(Package()));
    }

    [Fact]
    public void RegistryMembershipDoesNotEstablishTrust()
    {
        var registry = new ProviderPackageRegistry();
        var package = Package();

        registry.Register(package);

        var resolved = registry.Resolve(package.PackageId);
        var gate = new ProviderPackageTrustGate();

        Assert.False(gate.IsTrusted(resolved));
    }

    [Fact]
    public void HostCompatibilityDoesNotEstablishTrust()
    {
        var package = Package();
        var compatibility =
            new ProviderPackageHostCompatibilityEvaluator();

        Assert.True(
            compatibility.IsCompatible(
                package,
                "3.10.0"));

        var gate = new ProviderPackageTrustGate();

        Assert.False(gate.IsTrusted(package));
    }

    [Fact]
    public void UnverifiedPackageCannotPassInstallationTrustGate()
    {
        var gate = new ProviderPackageTrustGate();

        Assert.Throws<InvalidOperationException>(() =>
        {
            gate.RequireTrusted(Package());
        });
    }

    [Fact]
    public void MissingManifestIsRejected()
    {
        var gate = new ProviderPackageTrustGate();

        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = gate.IsTrusted(null!);
        });
    }
}
