using System;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class CapabilityVerificationMetadataTests
{
    private static UniversalCapabilityIdentity Start =>
        new("compute.instance.start");

    private static UniversalCapabilityIdentity Verify =>
        new("compute.instance.inspect");

    [Fact]
    public void UnknownIsTheDefaultStatus()
    {
        Assert.Equal(
            CapabilityVerificationStatus.Unknown,
            default(CapabilityVerificationStatus));
    }

    [Theory]
    [InlineData(CapabilityVerificationStatus.Unknown)]
    [InlineData(CapabilityVerificationStatus.Unsupported)]
    [InlineData(CapabilityVerificationStatus.Supported)]
    public void ValidStatusesArePreserved(
        CapabilityVerificationStatus status)
    {
        var metadata = new CapabilityVerificationMetadata(
            Start, status);

        Assert.Equal(Start, metadata.Capability);
        Assert.Equal(status, metadata.Status);
        Assert.Null(metadata.VerificationCapability);
        Assert.Null(metadata.Limitations);
    }

    [Fact]
    public void SupportedCanDeclareVerificationCapability()
    {
        var metadata = new CapabilityVerificationMetadata(
            Start,
            CapabilityVerificationStatus.Supported,
            Verify);

        Assert.Equal(Verify, metadata.VerificationCapability);
    }

    [Fact]
    public void SupportedDoesNotRequireVerificationCapability()
    {
        var metadata = new CapabilityVerificationMetadata(
            Start,
            CapabilityVerificationStatus.Supported);

        Assert.Null(metadata.VerificationCapability);
    }

    [Theory]
    [InlineData(CapabilityVerificationStatus.Unknown)]
    [InlineData(CapabilityVerificationStatus.Unsupported)]
    public void NonsupportedStatusesRejectVerificationReference(
        CapabilityVerificationStatus status)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new CapabilityVerificationMetadata(
                Start, status, Verify));
    }

    [Fact]
    public void VerificationCapabilityCannotReferenceItself()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new CapabilityVerificationMetadata(
                Start,
                CapabilityVerificationStatus.Supported,
                Start));
    }

    [Fact]
    public void DefaultForwardIdentityIsRejected()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new CapabilityVerificationMetadata(
                default,
                CapabilityVerificationStatus.Unknown));
    }

    [Fact]
    public void DefaultVerificationIdentityIsRejected()
    {
        UniversalCapabilityIdentity? invalid = default(
            UniversalCapabilityIdentity);

        Assert.ThrowsAny<ArgumentException>(() =>
            new CapabilityVerificationMetadata(
                Start,
                CapabilityVerificationStatus.Supported,
                invalid));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(999)]
    public void InvalidStatusesAreRejected(int value)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new CapabilityVerificationMetadata(
                Start, (CapabilityVerificationStatus)value));
    }

    [Fact]
    public void LimitationsAreTrimmedAndPreserveCase()
    {
        var metadata = new CapabilityVerificationMetadata(
            Start,
            CapabilityVerificationStatus.Supported,
            Verify,
            "  Observation may be eventually consistent  ");

        Assert.Equal(
            "Observation may be eventually consistent",
            metadata.Limitations);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void AbsentLimitationsNormalizeToNull(string? text)
    {
        var metadata = new CapabilityVerificationMetadata(
            Start,
            CapabilityVerificationStatus.Unsupported,
            limitations: text);

        Assert.Null(metadata.Limitations);
    }

    [Theory]
    [InlineData("partial\nobservation")]
    [InlineData("partial\robservation")]
    [InlineData("partial\0observation")]
    public void EmbeddedControlCharactersInLimitationsAreRejected(
        string text)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new CapabilityVerificationMetadata(
                Start,
                CapabilityVerificationStatus.Supported,
                limitations: text));
    }

    [Fact]
    public void UnsupportedCanDescribeLimitations()
    {
        var metadata = new CapabilityVerificationMetadata(
            Start,
            CapabilityVerificationStatus.Unsupported,
            limitations: "No verification mechanism is provided");

        Assert.Equal(
            "No verification mechanism is provided",
            metadata.Limitations);
    }

    [Fact]
    public void UnknownCanDescribeLimitations()
    {
        var metadata = new CapabilityVerificationMetadata(
            Start,
            CapabilityVerificationStatus.Unknown,
            limitations: "Provider has not declared verification support");

        Assert.Equal(
            "Provider has not declared verification support",
            metadata.Limitations);
    }
}
