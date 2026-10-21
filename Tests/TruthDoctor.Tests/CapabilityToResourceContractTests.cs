using System;
using System.Collections.Generic;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class CapabilityToResourceContractTests
{
    private static UniversalCapabilityIdentity Start =>
        new("compute.instance.start");

    [Fact]
    public void StoresCapabilityAndOrderedResourceTypes()
    {
        var contract = new CapabilityToResourceContract(
            Start, new[] { "instance", "ec2-instance" });

        Assert.Equal(Start, contract.Capability);
        Assert.Equal(
            new[] { "instance", "ec2-instance" },
            contract.ResourceTypes);
    }

    [Theory]
    [InlineData("instance")]
    [InlineData("INSTANCE")]
    [InlineData(" ec2-instance ")]
    public void ApplicableResourceTypeMatchesExactlyIgnoringCase(
        string type)
    {
        var contract = new CapabilityToResourceContract(
            Start, new[] { "instance", "ec2-instance" });

        Assert.True(contract.AppliesTo(type));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("inst")]
    [InlineData("ec2")]
    [InlineData("rds-instance")]
    public void UnmatchedResourceTypesAreNotApplicable(string? type)
    {
        var contract = new CapabilityToResourceContract(
            Start, new[] { "instance", "ec2-instance" });

        Assert.False(contract.AppliesTo(type));
    }

    [Fact]
    public void TrimsAndDeduplicatesResourceTypes()
    {
        var contract = new CapabilityToResourceContract(
            Start,
            new[] {
                " instance ",
                "INSTANCE",
                "ec2-instance",
                " EC2-INSTANCE ",
                "rds-instance"
            });

        Assert.Equal(
            new[] { "instance", "ec2-instance", "rds-instance" },
            contract.ResourceTypes);
    }

    [Fact]
    public void ResourceTypesAreCopiedFromInput()
    {
        var source = new List<string> { "instance" };
        var contract = new CapabilityToResourceContract(Start, source);

        source[0] = "database";
        source.Add("rds-instance");

        Assert.Equal(new[] { "instance" }, contract.ResourceTypes);
    }

    [Fact]
    public void ExposedResourceTypesCannotBeMutated()
    {
        var contract = new CapabilityToResourceContract(
            Start, new[] { "instance" });

        var list = Assert.IsAssignableFrom<IList<string>>(
            contract.ResourceTypes);

        Assert.Throws<NotSupportedException>(() =>
            list[0] = "database");
    }

    [Fact]
    public void RejectsDefaultCapabilityIdentity()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new CapabilityToResourceContract(
                default(UniversalCapabilityIdentity),
                new[] { "instance" }));
    }

    [Fact]
    public void RejectsNullResourceCollection()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new CapabilityToResourceContract(Start, null!));
    }

    [Fact]
    public void RejectsEmptyResourceCollection()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new CapabilityToResourceContract(
                Start, Array.Empty<string>()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("instance\tbad")]
    public void RejectsInvalidResourceTypeEntries(string? type)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new CapabilityToResourceContract(
                Start, new[] { "instance", type! }));
    }

    [Fact]
    public void RejectsWhitespaceOnlyResourceTypeAfterTrimming()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new CapabilityToResourceContract(
                Start, new[] { "\t" }));
    }

    [Fact]
    public void SameCapabilityCanApplyToMultipleResourceTypes()
    {
        var contract = new CapabilityToResourceContract(
            Start, new[] { "instance", "ec2-instance" });

        Assert.True(contract.AppliesTo("instance"));
        Assert.True(contract.AppliesTo("ec2-instance"));
        Assert.False(contract.AppliesTo("database"));
    }
}
