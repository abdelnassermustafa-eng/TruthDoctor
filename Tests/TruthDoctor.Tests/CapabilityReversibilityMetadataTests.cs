using System;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class CapabilityReversibilityMetadataTests
{
    private static UniversalCapabilityIdentity Start =>
        new("compute.instance.start");

    private static UniversalCapabilityIdentity Stop =>
        new("compute.instance.stop");

    [Fact]
    public void UnknownIsTheDefaultStatus()
    {
        Assert.Equal(
            CapabilityReversibilityStatus.Unknown,
            default(CapabilityReversibilityStatus));
    }

    [Theory]
    [InlineData(CapabilityReversibilityStatus.Unknown)]
    [InlineData(CapabilityReversibilityStatus.Unsupported)]
    [InlineData(CapabilityReversibilityStatus.Supported)]
    public void ValidStatusesArePreserved(
        CapabilityReversibilityStatus status)
    {
        var metadata = new CapabilityReversibilityMetadata(
            Start, status);

        Assert.Equal(Start, metadata.Capability);
        Assert.Equal(status, metadata.Status);
        Assert.Null(metadata.RollbackCapability);
        Assert.Null(metadata.Limitations);
    }

    [Fact]
    public void SupportedCanDeclareRollbackCapability()
    {
        var metadata = new CapabilityReversibilityMetadata(
            Start,
            CapabilityReversibilityStatus.Supported,
            Stop);

        Assert.Equal(Stop, metadata.RollbackCapability);
    }

    [Fact]
    public void SupportedDoesNotRequireRollbackCapability()
    {
        var metadata = new CapabilityReversibilityMetadata(
            Start,
            CapabilityReversibilityStatus.Supported);

        Assert.Null(metadata.RollbackCapability);
    }

    [Theory]
    [InlineData(CapabilityReversibilityStatus.Unknown)]
    [InlineData(CapabilityReversibilityStatus.Unsupported)]
    public void NonsupportedStatusesRejectRollbackReference(
        CapabilityReversibilityStatus status)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new CapabilityReversibilityMetadata(
                Start, status, Stop));
    }

    [Fact]
    public void RollbackCapabilityCannotReferenceItself()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new CapabilityReversibilityMetadata(
                Start,
                CapabilityReversibilityStatus.Supported,
                Start));
    }

    [Fact]
    public void DefaultForwardIdentityIsRejected()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new CapabilityReversibilityMetadata(
                default,
                CapabilityReversibilityStatus.Unknown));
    }

    [Fact]
    public void DefaultRollbackIdentityIsRejected()
    {
        UniversalCapabilityIdentity? invalid = default(
            UniversalCapabilityIdentity);

        Assert.ThrowsAny<ArgumentException>(() =>
            new CapabilityReversibilityMetadata(
                Start,
                CapabilityReversibilityStatus.Supported,
                invalid));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(999)]
    public void InvalidStatusesAreRejected(int value)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new CapabilityReversibilityMetadata(
                Start, (CapabilityReversibilityStatus)value));
    }

    [Fact]
    public void LimitationsAreTrimmedAndPreserveCase()
    {
        var metadata = new CapabilityReversibilityMetadata(
            Start,
            CapabilityReversibilityStatus.Supported,
            Stop,
            "  Partial restoration only  ");

        Assert.Equal(
            "Partial restoration only",
            metadata.Limitations);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void AbsentLimitationsNormalizeToNull(string? text)
    {
        var metadata = new CapabilityReversibilityMetadata(
            Start,
            CapabilityReversibilityStatus.Unsupported,
            limitations: text);

        Assert.Null(metadata.Limitations);
    }

    [Theory]
    [InlineData("partial\nrestoration")]
    [InlineData("partial\rrestoration")]
    [InlineData("partial\0restoration")]
    public void EmbeddedControlCharactersInLimitationsAreRejected(
        string text)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new CapabilityReversibilityMetadata(
                Start,
                CapabilityReversibilityStatus.Supported,
                limitations: text));
    }

    [Fact]
    public void UnsupportedCanDescribeLimitations()
    {
        var metadata = new CapabilityReversibilityMetadata(
            Start,
            CapabilityReversibilityStatus.Unsupported,
            limitations: "Deletion is permanent");

        Assert.Equal(
            "Deletion is permanent",
            metadata.Limitations);
    }

    [Fact]
    public void UnknownCanDescribeLimitations()
    {
        var metadata = new CapabilityReversibilityMetadata(
            Start,
            CapabilityReversibilityStatus.Unknown,
            limitations: "Provider has not declared support");

        Assert.Equal(
            "Provider has not declared support",
            metadata.Limitations);
    }
}
