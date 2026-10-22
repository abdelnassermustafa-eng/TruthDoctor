using System;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class CapabilityCancellationProgressMetadataTests
{
    private static UniversalCapabilityIdentity Start =>
        new("compute.instance.start");

    [Fact]
    public void CancellationUnknownIsDefault()
    {
        Assert.Equal(
            CapabilityCancellationStatus.Unknown,
            default(CapabilityCancellationStatus));
    }

    [Fact]
    public void ProgressUnknownIsDefault()
    {
        Assert.Equal(
            CapabilityProgressStatus.Unknown,
            default(CapabilityProgressStatus));
    }

    [Fact]
    public void CancellationStatesAreDistinct()
    {
        Assert.NotEqual(
            CapabilityCancellationStatus.Unknown,
            CapabilityCancellationStatus.Unsupported);
        Assert.NotEqual(
            CapabilityCancellationStatus.Unsupported,
            CapabilityCancellationStatus.Supported);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 1)]
    [InlineData(0, 2)]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    [InlineData(1, 2)]
    [InlineData(2, 0)]
    [InlineData(2, 1)]
    [InlineData(2, 2)]
    public void AllSupportCombinationsAreIndependent(
        int cancellation, int progress)
    {
        var cancellationStatus = (CapabilityCancellationStatus)cancellation;
        var progressStatus = (CapabilityProgressStatus)progress;

        var metadata = new CapabilityCancellationProgressMetadata(
            Start, cancellationStatus, progressStatus);

        Assert.Equal(Start, metadata.Capability);
        Assert.Equal(cancellationStatus, metadata.CancellationStatus);
        Assert.Equal(progressStatus, metadata.ProgressStatus);
        Assert.Null(metadata.CancellationLimitations);
        Assert.Null(metadata.ProgressLimitations);
    }

    [Fact]
    public void ProgressStatesAreDistinct()
    {
        Assert.NotEqual(
            CapabilityProgressStatus.Unknown,
            CapabilityProgressStatus.Unsupported);
        Assert.NotEqual(
            CapabilityProgressStatus.Unsupported,
            CapabilityProgressStatus.Supported);
    }

    [Fact]
    public void DefaultCapabilityIdentityIsRejected()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new CapabilityCancellationProgressMetadata(
                default,
                CapabilityCancellationStatus.Unknown,
                CapabilityProgressStatus.Unknown));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(999)]
    public void InvalidCancellationStatusesAreRejected(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CapabilityCancellationProgressMetadata(
                Start,
                (CapabilityCancellationStatus)value,
                CapabilityProgressStatus.Unknown));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(999)]
    public void InvalidProgressStatusesAreRejected(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CapabilityCancellationProgressMetadata(
                Start,
                CapabilityCancellationStatus.Unknown,
                (CapabilityProgressStatus)value));
    }

    [Fact]
    public void LimitationsAreIndependentTrimmedAndCasePreserving()
    {
        var metadata = new CapabilityCancellationProgressMetadata(
            Start,
            CapabilityCancellationStatus.Supported,
            CapabilityProgressStatus.Unsupported,
            "  Cancellation only before finalization  ",
            "  Phase markers only  ");

        Assert.Equal(
            "Cancellation only before finalization",
            metadata.CancellationLimitations);
        Assert.Equal(
            "Phase markers only",
            metadata.ProgressLimitations);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void AbsentCancellationLimitationsNormalizeToNull(string? text)
    {
        var metadata = new CapabilityCancellationProgressMetadata(
            Start,
            CapabilityCancellationStatus.Unknown,
            CapabilityProgressStatus.Supported,
            cancellationLimitations: text,
            progressLimitations: "phase markers");

        Assert.Null(metadata.CancellationLimitations);
        Assert.Equal("phase markers", metadata.ProgressLimitations);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void AbsentProgressLimitationsNormalizeToNull(string? text)
    {
        var metadata = new CapabilityCancellationProgressMetadata(
            Start,
            CapabilityCancellationStatus.Supported,
            CapabilityProgressStatus.Unknown,
            cancellationLimitations: "before finalization",
            progressLimitations: text);

        Assert.Equal("before finalization", metadata.CancellationLimitations);
        Assert.Null(metadata.ProgressLimitations);
    }

    [Theory]
    [InlineData("partial\ncancellation")]
    [InlineData("partial\rcancellation")]
    [InlineData("partial\0cancellation")]
    public void EmbeddedCancellationControlCharactersAreRejected(string text)
    {
        Assert.Throws<ArgumentException>(() =>
            new CapabilityCancellationProgressMetadata(
                Start,
                CapabilityCancellationStatus.Supported,
                CapabilityProgressStatus.Unknown,
                cancellationLimitations: text));
    }

    [Theory]
    [InlineData("phase\nmarkers")]
    [InlineData("phase\rmarkers")]
    [InlineData("phase\0markers")]
    public void EmbeddedProgressControlCharactersAreRejected(string text)
    {
        Assert.Throws<ArgumentException>(() =>
            new CapabilityCancellationProgressMetadata(
                Start,
                CapabilityCancellationStatus.Unknown,
                CapabilityProgressStatus.Supported,
                progressLimitations: text));
    }

    [Fact]
    public void UnsupportedStatesMayDescribeLimitations()
    {
        var metadata = new CapabilityCancellationProgressMetadata(
            Start,
            CapabilityCancellationStatus.Unsupported,
            CapabilityProgressStatus.Unsupported,
            cancellationLimitations: "No cancellation mechanism",
            progressLimitations: "No progress reporting mechanism");

        Assert.Equal(
            "No cancellation mechanism",
            metadata.CancellationLimitations);
        Assert.Equal(
            "No progress reporting mechanism",
            metadata.ProgressLimitations);
    }
}
