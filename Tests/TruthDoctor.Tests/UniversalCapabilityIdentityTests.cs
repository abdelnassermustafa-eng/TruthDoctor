using System;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class UniversalCapabilityIdentityTests
{
    [Theory]
    [InlineData("compute.instance.start")]
    [InlineData("network.interface.attach")]
    [InlineData("storage.volume.snapshot")]
    [InlineData("security.rule.update")]
    [InlineData("compute.instance-2.restart")]
    public void CanonicalIdentifiersAreAccepted(string value)
    {
        var identity = new UniversalCapabilityIdentity(value);

        Assert.Equal(value, identity.Value);
        Assert.Equal(value, identity.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("start")]
    [InlineData(".compute")]
    [InlineData("compute.")]
    [InlineData("compute..start")]
    [InlineData("Compute.Instance.Start")]
    [InlineData("compute.instance.Start")]
    [InlineData("compute.instance start")]
    [InlineData("compute.instance_start")]
    [InlineData("compute.-instance.start")]
    [InlineData("compute.instance-.start")]
    [InlineData("compute.2instance.start")]
    [InlineData("compute.instance.start ")]
    [InlineData(" compute.instance.start")]
    [InlineData("aws:ec2:start")]
    public void InvalidIdentifiersAreRejected(string? value)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new UniversalCapabilityIdentity(value!));
    }

    [Fact]
    public void IdenticalValuesProduceEqualIdentities()
    {
        var first = new UniversalCapabilityIdentity(
            "compute.instance.start");
        var second = new UniversalCapabilityIdentity(
            "compute.instance.start");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void DifferentValuesProduceDifferentIdentities()
    {
        var first = new UniversalCapabilityIdentity(
            "compute.instance.start");
        var second = new UniversalCapabilityIdentity(
            "compute.instance.stop");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void IdentityIsImmutableValueType()
    {
        Assert.True(
            typeof(UniversalCapabilityIdentity).IsValueType);
        Assert.True(
            typeof(UniversalCapabilityIdentity).IsDefined(
                typeof(System.Runtime.CompilerServices.IsReadOnlyAttribute),
                false));
    }

    [Fact]
    public void IdentityDoesNotRequireProviderOrResource()
    {
        var identity = new UniversalCapabilityIdentity(
            "storage.volume.snapshot");

        Assert.Equal(
            "storage.volume.snapshot",
            identity.Value);
    }
}
