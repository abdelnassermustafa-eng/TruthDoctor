using System;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class CapabilitySupportResultTests
{
    private static UniversalCapabilityIdentity Start =>
        new("compute.instance.start");

    [Fact]
    public void UnknownIsTheDefaultStatus()
    {
        Assert.Equal(
            CapabilitySupportStatus.Unknown,
            default(CapabilitySupportStatus));
    }

    [Fact]
    public void SupportStatesAreDistinct()
    {
        Assert.NotEqual(
            CapabilitySupportStatus.Unknown,
            CapabilitySupportStatus.Unsupported);
        Assert.NotEqual(
            CapabilitySupportStatus.Unsupported,
            CapabilitySupportStatus.Supported);
        Assert.NotEqual(
            CapabilitySupportStatus.Unknown,
            CapabilitySupportStatus.Supported);
    }

    [Theory]
    [InlineData(CapabilitySupportStatus.Unknown)]
    [InlineData(CapabilitySupportStatus.Unsupported)]
    [InlineData(CapabilitySupportStatus.Supported)]
    public void ValidStatusesArePreserved(CapabilitySupportStatus status)
    {
        var result = new CapabilitySupportResult(Start, status);

        Assert.Equal(Start, result.Capability);
        Assert.Equal(status, result.Status);
        Assert.Null(result.Reason);
    }

    [Fact]
    public void DefaultCapabilityIdentityIsRejected()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new CapabilitySupportResult(
                default,
                CapabilitySupportStatus.Unknown));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(999)]
    public void InvalidStatusesAreRejected(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CapabilitySupportResult(
                Start, (CapabilitySupportStatus)value));
    }

    [Fact]
    public void ReasonIsTrimmedAndPreservesCase()
    {
        var result = new CapabilitySupportResult(
            Start,
            CapabilitySupportStatus.Unsupported,
            "  Provider Does Not Support This Operation  ");

        Assert.Equal(
            "Provider Does Not Support This Operation",
            result.Reason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void AbsentReasonsNormalizeToNull(string? reason)
    {
        var result = new CapabilitySupportResult(
            Start, CapabilitySupportStatus.Unknown, reason);

        Assert.Null(result.Reason);
    }

    [Theory]
    [InlineData("before\nexecution")]
    [InlineData("before\rexecution")]
    [InlineData("before\0execution")]
    public void EmbeddedControlCharactersAreRejected(string reason)
    {
        Assert.Throws<ArgumentException>(() =>
            new CapabilitySupportResult(
                Start,
                CapabilitySupportStatus.Unsupported,
                reason));
    }

    [Fact]
    public void UnsupportedCanDeclareAnExplicitReason()
    {
        var result = new CapabilitySupportResult(
            Start,
            CapabilitySupportStatus.Unsupported,
            "Provider does not implement this capability");

        Assert.Equal(
            CapabilitySupportStatus.Unsupported,
            result.Status);
        Assert.Equal(
            "Provider does not implement this capability",
            result.Reason);
    }

    [Fact]
    public void UnknownCanDescribeMissingDeclaration()
    {
        var result = new CapabilitySupportResult(
            Start,
            CapabilitySupportStatus.Unknown,
            "Provider has not declared support");

        Assert.Equal(CapabilitySupportStatus.Unknown, result.Status);
        Assert.Equal(
            "Provider has not declared support",
            result.Reason);
    }

    [Fact]
    public void SupportedIsOnlyADeclarativeStatus()
    {
        var result = new CapabilitySupportResult(
            Start,
            CapabilitySupportStatus.Supported,
            "Subject to separate preconditions and authorization");

        Assert.Equal(CapabilitySupportStatus.Supported, result.Status);
        Assert.Equal(
            "Subject to separate preconditions and authorization",
            result.Reason);
    }
}
