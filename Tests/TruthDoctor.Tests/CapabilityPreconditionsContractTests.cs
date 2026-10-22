using System;
using System.Collections.Generic;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class CapabilityPreconditionsContractTests
{
    private static UniversalCapabilityIdentity Start =>
        new("compute.instance.start");

    private static UniversalCapabilityIdentity Stop =>
        new("compute.instance.stop");

    private static CapabilityPrecondition Requirement(
        string key,
        string value)
    {
        return new CapabilityPrecondition(key, value);
    }

    [Fact]
    public void PreconditionStoresNormalizedSymbolicValues()
    {
        var precondition = Requirement(
            " Resource.State ", " Stopped ");

        Assert.Equal("Resource.State", precondition.Key);
        Assert.Equal("Stopped", precondition.ExpectedValue);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void PreconditionRejectsInvalidKeys(string? key)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new CapabilityPrecondition(key!, "stopped"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void PreconditionRejectsInvalidExpectedValues(
        string? value)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new CapabilityPrecondition("resource.state", value!));
    }

    [Theory]
    [InlineData("resource.\nstate")]
    [InlineData("resource.\rstate")]
    [InlineData("resource.\0state")]
    public void PreconditionRejectsEmbeddedControlInKey(
        string key)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new CapabilityPrecondition(key, "stopped"));
    }

    [Theory]
    [InlineData("stop\nped")]
    [InlineData("stop\rped")]
    [InlineData("stop\0ped")]
    public void PreconditionRejectsEmbeddedControlInExpectedValue(
        string value)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new CapabilityPrecondition("resource.state", value));
    }

    [Fact]
    public void ContractStoresCapabilityAndOrderedRequirements()
    {
        var first = Requirement("resource.state", "stopped");
        var second = Requirement("context.account", "present");

        var contract = new CapabilityPreconditionsContract(
            Start, new[] { first, second });

        Assert.Equal(Start, contract.Capability);
        Assert.Equal(
            new[] { first, second },
            contract.Preconditions);
    }

    [Fact]
    public void ContractRejectsDefaultCapabilityIdentity()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new CapabilityPreconditionsContract(
                default,
                Array.Empty<CapabilityPrecondition>()));
    }

    [Fact]
    public void ContractRejectsNullRequirementCollection()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new CapabilityPreconditionsContract(Start, null!));
    }

    [Fact]
    public void ContractAllowsEmptyRequirements()
    {
        var contract = new CapabilityPreconditionsContract(
            Start, Array.Empty<CapabilityPrecondition>());

        Assert.Empty(contract.Preconditions);
    }

    [Fact]
    public void ContractRejectsNullRequirementEntry()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new CapabilityPreconditionsContract(
                Start,
                new CapabilityPrecondition[] {
                    Requirement("resource.state", "stopped"),
                    null!
                }));
    }

    [Fact]
    public void ContractRejectsDuplicateKeysIgnoringCase()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new CapabilityPreconditionsContract(
                Start,
                new[] {
                    Requirement("resource.state", "stopped"),
                    Requirement("RESOURCE.STATE", "running")
                }));
    }

    [Fact]
    public void ContractRejectsDuplicateKeysAfterTrimming()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new CapabilityPreconditionsContract(
                Start,
                new[] {
                    Requirement(" resource.state ", "stopped"),
                    Requirement("resource.state", "running")
                }));
    }

    [Fact]
    public void ContractCopiesInputRequirementCollection()
    {
        var source = new List<CapabilityPrecondition> {
            Requirement("resource.state", "stopped")
        };

        var contract = new CapabilityPreconditionsContract(
            Start, source);

        source.Clear();
        source.Add(Requirement("context.account", "present"));

        Assert.Single(contract.Preconditions);
        Assert.Equal(
            "resource.state",
            contract.Preconditions[0].Key);
    }

    [Fact]
    public void ContractExposesReadOnlyRequirements()
    {
        var contract = new CapabilityPreconditionsContract(
            Start,
            new[] { Requirement("resource.state", "stopped") });

        var list = Assert.IsAssignableFrom<
            IList<CapabilityPrecondition>>(
                contract.Preconditions);

        Assert.Throws<NotSupportedException>(() =>
            list[0] = Requirement("resource.state", "running"));
    }

    [Fact]
    public void ContractPreservesIndependentRequirementObjects()
    {
        var first = Requirement("resource.state", "stopped");
        var second = Requirement("context.account", "present");

        var contract = new CapabilityPreconditionsContract(
            Stop, new[] { first, second });

        Assert.Same(first, contract.Preconditions[0]);
        Assert.Same(second, contract.Preconditions[1]);
        Assert.Equal(Stop, contract.Capability);
    }
}
