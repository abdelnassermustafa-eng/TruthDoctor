using System;
using TruthDoctor.Services.Providers;
using Xunit;

namespace TruthDoctor.Tests;

/// <summary>
/// Declarative host bootstrap ownership requirements.
///
/// These tests do not provision or authenticate roots,
/// verify policy signatures, establish release trust,
/// enforce rollback protection, or permit installation.
///
/// Classification is an architectural responsibility map,
/// not a security authorization decision.
/// </summary>
public sealed class G327A6ProviderPolicyTrustAnchorBootstrapOwnershipTests
{
    private static string Owner(string concern)
    {
        return new ProviderPolicyTrustAnchorBootstrapOwnershipContract()
            .GetOwner(concern);
    }

    [Fact]
    public void PolicyRootCatalogBelongsToHostSecurity()
    {
        Assert.Equal(
            "HostSecurity",
            Owner("PolicyRootCatalog"));
    }

    [Fact]
    public void AuthorityBindingsBelongToHostSecurity()
    {
        Assert.Equal(
            "HostSecurity",
            Owner("AuthorityToRootBinding"));
    }

    [Fact]
    public void RootRotationPolicyBelongsToHostSecurity()
    {
        Assert.Equal(
            "HostSecurity",
            Owner("PolicyRootRotation"));
    }

    [Fact]
    public void AntiRollbackOwnershipIsHostSecurity()
    {
        Assert.Equal(
            "HostSecurity",
            Owner("PolicySequenceHighWater"));
    }

    [Fact]
    public void ApplicationCompositionOwnsSecurityWiringOnly()
    {
        Assert.Equal(
            "ApplicationComposition",
            Owner("SecurityServiceComposition"));
    }

    [Fact]
    public void ApplicationEntryRetainsEntryPointOwnership()
    {
        Assert.Equal(
            "ApplicationEntry",
            Owner("ApplicationEntryPoint"));
    }

    [Fact]
    public void ProviderManifestCannotOwnPolicyRoots()
    {
        Assert.Equal(
            "NotAuthorized",
            Owner("ProviderPackageManifest"));
    }

    [Fact]
    public void ProviderRegistryCannotOwnPolicyRoots()
    {
        Assert.Equal(
            "NotAuthorized",
            Owner("ProviderPackageRegistry"));
    }

    [Fact]
    public void UserSettingsAndEnvironmentCannotOwnPolicyRoots()
    {
        Assert.Equal(
            "NotAuthorized",
            Owner("UserStore"));

        Assert.Equal(
            "NotAuthorized",
            Owner("EnvironmentVariables"));

        Assert.Equal(
            "NotAuthorized",
            Owner("SavedViewStorage"));
    }

    [Fact]
    public void UnknownAndNullConcernsFailExplicitly()
    {
        Action unknown = () =>
        {
            _ = Owner("UnrecognizedConcern");
        };

        Action missing = () =>
        {
            _ = Owner(null!);
        };

        Assert.Throws<ArgumentException>(unknown);
        Assert.Throws<ArgumentNullException>(missing);
    }
}
