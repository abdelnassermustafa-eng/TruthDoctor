using System;
using TruthDoctor.Services.Providers;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A6ProviderPackageHostCompatibilityTests
{
    private static ProviderPackageManifest Package(
        string minimumHostVersion)
    {
        return new ProviderPackageManifest(
            providerId: "aws",
            packageId: "truthdoctor.provider.aws",
            version: "1.0.0",
            minimumHostVersion: minimumHostVersion,
            dependencies: Array.Empty<string>(),
            declaredCapabilities: new[] { "infrastructure.discovery" });
    }

    [Fact]
    public void EqualHostVersionIsCompatible()
    {
        var evaluator =
            new ProviderPackageHostCompatibilityEvaluator();

        Assert.True(
            evaluator.IsCompatible(
                Package("3.0.0"),
                "3.0.0"));
    }

    [Fact]
    public void NewerHostVersionIsCompatible()
    {
        var evaluator =
            new ProviderPackageHostCompatibilityEvaluator();

        Assert.True(
            evaluator.IsCompatible(
                Package("3.0.0"),
                "3.1.0"));
    }

    [Fact]
    public void OlderHostVersionIsIncompatible()
    {
        var evaluator =
            new ProviderPackageHostCompatibilityEvaluator();

        Assert.False(
            evaluator.IsCompatible(
                Package("3.2.0"),
                "3.1.9"));
    }

    [Fact]
    public void VersionComponentsAreComparedNumerically()
    {
        var evaluator =
            new ProviderPackageHostCompatibilityEvaluator();

        Assert.True(
            evaluator.IsCompatible(
                Package("3.9.0"),
                "3.10.0"));
    }

    [Fact]
    public void MalformedHostVersionIsRejected()
    {
        var evaluator =
            new ProviderPackageHostCompatibilityEvaluator();

        Assert.Throws<ArgumentException>(() =>
        {
            _ = evaluator.IsCompatible(
                Package("3.0.0"),
                "not-a-version");
        });
    }
}
