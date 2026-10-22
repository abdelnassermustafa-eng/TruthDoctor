using System;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class CapabilityContractConsistencyTests
{
    private static UniversalCapabilityIdentity Start =>
        new("compute.instance.start");

    private static UniversalCapabilityIdentity Stop =>
        new("compute.instance.stop");

    private static UniversalCapabilityIdentity Verify =>
        new("compute.instance.verify");

    [Fact]
    public void MissingAdvertisementDoesNotInferUnsupported()
    {
        var advertisement = new ProviderCapabilityAdvertisement(
            "aws", Array.Empty<CapabilityToResourceContract>());

        var support = new CapabilitySupportResult(
            Start, CapabilitySupportStatus.Unknown);

        Assert.False(advertisement.Advertises(Start, "instance"));
        Assert.Equal(Start, support.Capability);
        Assert.Equal(CapabilitySupportStatus.Unknown, support.Status);
        Assert.NotEqual(CapabilitySupportStatus.Unsupported, support.Status);
    }

    [Fact]
    public void AdvertisementAndExplicitSupportRemainIndependent()
    {
        var mapping = new CapabilityToResourceContract(
            Start, new[] { "instance" });

        var advertisement = new ProviderCapabilityAdvertisement(
            "aws", new[] { mapping });

        var support = new CapabilitySupportResult(
            Start,
            CapabilitySupportStatus.Unsupported,
            "Explicit provider support declaration");

        Assert.True(advertisement.Advertises(Start, "instance"));
        Assert.Equal(Start, support.Capability);
        Assert.Equal(CapabilitySupportStatus.Unsupported, support.Status);
        Assert.Equal("Explicit provider support declaration", support.Reason);
    }

    [Fact]
    public void ResourceApplicabilityDoesNotDetermineSupport()
    {
        var applicability = new CapabilityToResourceContract(
            Start, new[] { "instance" });

        var advertisement = new ProviderCapabilityAdvertisement(
            "aws", Array.Empty<CapabilityToResourceContract>());

        var support = new CapabilitySupportResult(
            Start, CapabilitySupportStatus.Unknown);

        Assert.True(applicability.AppliesTo("instance"));
        Assert.False(advertisement.Advertises(Start, "instance"));
        Assert.Equal(CapabilitySupportStatus.Unknown, support.Status);
        Assert.Equal(Start, applicability.Capability);
    }

    [Fact]
    public void SupportedCapabilityPreservesUnevaluatedPreconditions()
    {
        var requirement = new CapabilityPrecondition(
            "resource.state", "stopped");

        var preconditions = new CapabilityPreconditionsContract(
            Start, new[] { requirement });

        var support = new CapabilitySupportResult(
            Start, CapabilitySupportStatus.Supported);

        Assert.Equal(Start, preconditions.Capability);
        Assert.Equal(Start, support.Capability);
        Assert.Single(preconditions.Preconditions);
        Assert.Same(requirement, preconditions.Preconditions[0]);
        Assert.Equal("resource.state", preconditions.Preconditions[0].Key);
        Assert.Equal("stopped", preconditions.Preconditions[0].ExpectedValue);
        Assert.Equal(CapabilitySupportStatus.Supported, support.Status);
    }

    [Fact]
    public void VerificationAndRollbackReferencesRemainIndependent()
    {
        var verification = new CapabilityVerificationMetadata(
            Start, CapabilityVerificationStatus.Supported, Verify);

        var reversibility = new CapabilityReversibilityMetadata(
            Start, CapabilityReversibilityStatus.Supported, Stop);

        Assert.Equal(Start, verification.Capability);
        Assert.Equal(Start, reversibility.Capability);
        Assert.Equal(Verify, verification.VerificationCapability);
        Assert.Equal(Stop, reversibility.RollbackCapability);
        Assert.NotEqual(
            verification.VerificationCapability,
            reversibility.RollbackCapability);
    }

    [Fact]
    public void CancellationAndProgressDoNotDetermineOverallSupport()
    {
        var lifecycle = new CapabilityCancellationProgressMetadata(
            Start,
            CapabilityCancellationStatus.Unsupported,
            CapabilityProgressStatus.Supported);

        var support = new CapabilitySupportResult(
            Start, CapabilitySupportStatus.Unknown);

        Assert.Equal(Start, lifecycle.Capability);
        Assert.Equal(Start, support.Capability);
        Assert.Equal(
            CapabilityCancellationStatus.Unsupported,
            lifecycle.CancellationStatus);
        Assert.Equal(
            CapabilityProgressStatus.Supported,
            lifecycle.ProgressStatus);
        Assert.Equal(CapabilitySupportStatus.Unknown, support.Status);
    }

    [Fact]
    public void CapabilityIdentityIsConsistentAcrossContracts()
    {
        var applicability = new CapabilityToResourceContract(
            Start, new[] { "instance" });
        var advertisement = new ProviderCapabilityAdvertisement(
            "aws", new[] { applicability });
        var preconditions = new CapabilityPreconditionsContract(
            Start, Array.Empty<CapabilityPrecondition>());
        var support = new CapabilitySupportResult(
            Start, CapabilitySupportStatus.Supported);
        var verification = new CapabilityVerificationMetadata(
            Start, CapabilityVerificationStatus.Unknown);
        var reversibility = new CapabilityReversibilityMetadata(
            Start, CapabilityReversibilityStatus.Unknown);
        var lifecycle = new CapabilityCancellationProgressMetadata(
            Start,
            CapabilityCancellationStatus.Unknown,
            CapabilityProgressStatus.Unknown);

        Assert.Equal(Start, applicability.Capability);
        Assert.True(advertisement.Advertises(Start, "instance"));
        Assert.Equal(Start, advertisement.Capabilities[0].Capability);
        Assert.Equal(Start, preconditions.Capability);
        Assert.Equal(Start, support.Capability);
        Assert.Equal(Start, verification.Capability);
        Assert.Equal(Start, reversibility.Capability);
        Assert.Equal(Start, lifecycle.Capability);
        Assert.Equal(CapabilitySupportStatus.Supported, support.Status);
        Assert.Equal(CapabilityVerificationStatus.Unknown, verification.Status);
        Assert.Equal(CapabilityReversibilityStatus.Unknown, reversibility.Status);
    }

    [Fact]
    public void DifferentCapabilityIdentitiesDoNotBecomeInterchangeable()
    {
        var startMapping = new CapabilityToResourceContract(
            Start, new[] { "instance" });
        var advertisement = new ProviderCapabilityAdvertisement(
            "aws", new[] { startMapping });
        var stopSupport = new CapabilitySupportResult(
            Stop, CapabilitySupportStatus.Supported);
        var startVerification = new CapabilityVerificationMetadata(
            Start, CapabilityVerificationStatus.Supported, Verify);
        var stopReversibility = new CapabilityReversibilityMetadata(
            Stop, CapabilityReversibilityStatus.Supported, Start);

        Assert.NotEqual(Start, Stop);
        Assert.True(advertisement.Advertises(Start, "instance"));
        Assert.False(advertisement.Advertises(Stop, "instance"));
        Assert.Equal(Stop, stopSupport.Capability);
        Assert.Equal(Start, startVerification.Capability);
        Assert.Equal(Verify, startVerification.VerificationCapability);
        Assert.Equal(Stop, stopReversibility.Capability);
        Assert.Equal(Start, stopReversibility.RollbackCapability);
    }
}
