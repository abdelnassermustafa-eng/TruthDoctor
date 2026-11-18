using System;
using TruthDoctor.Services.Providers;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A6ProviderPackageDependencyGraphTests
{
    private static ProviderPackageManifest Package(
        string packageId,
        params string[] dependencies)
    {
        return new ProviderPackageManifest(
            providerId: "test-provider",
            packageId: packageId,
            version: "1.0.0",
            minimumHostVersion: "3.0.0",
            dependencies: dependencies,
            declaredCapabilities: Array.Empty<string>());
    }

    [Fact]
    public void AcceptsValidDependencyChain()
    {
        var validator = new ProviderPackageDependencyValidator();

        var packages = new[]
        {
            Package("package.alpha", "package.beta"),
            Package("package.beta", "package.gamma"),
            Package("package.gamma")
        };

        validator.Validate(packages);
    }

    [Fact]
    public void RejectsMissingDependency()
    {
        var validator = new ProviderPackageDependencyValidator();

        var packages = new[]
        {
            Package("package.alpha", "package.missing")
        };

        Assert.Throws<ArgumentException>(() =>
        {
            validator.Validate(packages);
        });
    }

    [Fact]
    public void RejectsDirectDependencyCycle()
    {
        var validator = new ProviderPackageDependencyValidator();

        var packages = new[]
        {
            Package("package.alpha", "package.beta"),
            Package("package.beta", "package.alpha")
        };

        Assert.Throws<ArgumentException>(() =>
        {
            validator.Validate(packages);
        });
    }

    [Fact]
    public void RejectsIndirectDependencyCycle()
    {
        var validator = new ProviderPackageDependencyValidator();

        var packages = new[]
        {
            Package("package.alpha", "package.beta"),
            Package("package.beta", "package.gamma"),
            Package("package.gamma", "package.alpha")
        };

        Assert.Throws<ArgumentException>(() =>
        {
            validator.Validate(packages);
        });
    }
}
