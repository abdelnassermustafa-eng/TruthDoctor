using System;
using System.Collections.Generic;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class ProviderCapabilityAdvertisementTests
{
    private static UniversalCapabilityIdentity Start =>
        new("compute.instance.start");

    private static UniversalCapabilityIdentity Stop =>
        new("compute.instance.stop");

    private static CapabilityToResourceContract Mapping(
        UniversalCapabilityIdentity identity,
        params string[] resourceTypes)
    {
        return new CapabilityToResourceContract(identity, resourceTypes);
    }

    [Fact]
    public void StoresTrimmedProviderIdAndOrderedCapabilities()
    {
        var first = Mapping(Start, "instance");
        var second = Mapping(Stop, "ec2-instance");

        var advertisement = new ProviderCapabilityAdvertisement(
            " AWS ", new[] { first, second });

        Assert.Equal("AWS", advertisement.ProviderId);
        Assert.Equal(
            new[] { first, second },
            advertisement.Capabilities);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void RejectsInvalidProviderIds(string? providerId)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new ProviderCapabilityAdvertisement(
                providerId!, Array.Empty<CapabilityToResourceContract>()));
    }

    [Fact]
    public void RejectsNullCapabilityCollection()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ProviderCapabilityAdvertisement("aws", null!));
    }

    [Fact]
    public void AllowsEmptyCapabilityAdvertisement()
    {
        var advertisement = new ProviderCapabilityAdvertisement(
            "aws", Array.Empty<CapabilityToResourceContract>());

        Assert.Empty(advertisement.Capabilities);
        Assert.False(advertisement.Advertises(Start, "instance"));
    }

    [Fact]
    public void RejectsNullCapabilityEntry()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new ProviderCapabilityAdvertisement(
                "aws", new CapabilityToResourceContract[] {
                    Mapping(Start, "instance"), null!
                }));
    }

    [Fact]
    public void RejectsDuplicateCapabilityIdentities()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new ProviderCapabilityAdvertisement(
                "aws",
                new[] {
                    Mapping(Start, "instance"),
                    Mapping(Start, "ec2-instance")
                }));
    }

    [Fact]
    public void CopiesCapabilitiesFromInputCollection()
    {
        var source = new List<CapabilityToResourceContract> {
            Mapping(Start, "instance")
        };

        var advertisement = new ProviderCapabilityAdvertisement(
            "aws", source);

        source.Clear();
        source.Add(Mapping(Stop, "database"));

        Assert.Single(advertisement.Capabilities);
        Assert.True(advertisement.Advertises(Start, "instance"));
        Assert.False(advertisement.Advertises(Stop, "database"));
    }

    [Fact]
    public void ExposedCapabilitiesCannotBeMutated()
    {
        var advertisement = new ProviderCapabilityAdvertisement(
            "aws", new[] { Mapping(Start, "instance") });

        var list = Assert.IsAssignableFrom<
            IList<CapabilityToResourceContract>>(
                advertisement.Capabilities);

        Assert.Throws<NotSupportedException>(() =>
            list[0] = Mapping(Stop, "database"));
    }

    [Theory]
    [InlineData("instance")]
    [InlineData("INSTANCE")]
    [InlineData(" ec2-instance ")]
    public void AdvertisesMatchingCapabilityAndResourceType(
        string resourceType)
    {
        var advertisement = new ProviderCapabilityAdvertisement(
            "aws",
            new[] { Mapping(Start, "instance", "ec2-instance") });

        Assert.True(advertisement.Advertises(Start, resourceType));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("inst")]
    [InlineData("rds-instance")]
    public void DoesNotAdvertiseUnmatchedResourceType(
        string? resourceType)
    {
        var advertisement = new ProviderCapabilityAdvertisement(
            "aws", new[] { Mapping(Start, "instance") });

        Assert.False(advertisement.Advertises(Start, resourceType));
    }

    [Fact]
    public void DoesNotAdvertiseDifferentCapabilityIdentity()
    {
        var advertisement = new ProviderCapabilityAdvertisement(
            "aws", new[] { Mapping(Start, "instance") });

        Assert.False(advertisement.Advertises(Stop, "instance"));
    }

    [Fact]
    public void DoesNotAdvertiseDefaultCapabilityIdentity()
    {
        var advertisement = new ProviderCapabilityAdvertisement(
            "aws", new[] { Mapping(Start, "instance") });

        Assert.False(advertisement.Advertises(
            default(UniversalCapabilityIdentity), "instance"));
    }

    [Fact]
    public void IndependentlyMatchesMultipleCapabilityMappings()
    {
        var advertisement = new ProviderCapabilityAdvertisement(
            "aws",
            new[] {
                Mapping(Start, "instance"),
                Mapping(Stop, "ec2-instance")
            });

        Assert.True(advertisement.Advertises(Start, "instance"));
        Assert.True(advertisement.Advertises(Stop, "ec2-instance"));
        Assert.False(advertisement.Advertises(Start, "ec2-instance"));
        Assert.False(advertisement.Advertises(Stop, "instance"));
    }

    [Fact]
    public void DoesNotModifyUnderlyingResourceApplicability()
    {
        var mapping = Mapping(Start, "instance");
        var advertisement = new ProviderCapabilityAdvertisement(
            "aws", new[] { mapping });

        Assert.Same(mapping, advertisement.Capabilities[0]);
        Assert.Equal(new[] { "instance" }, mapping.ResourceTypes);
    }
}
