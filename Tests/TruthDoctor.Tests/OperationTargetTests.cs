using System;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationTargetTests
{
    private static OperationTarget Create(
        string providerId = "aws",
        string accountId = "123456789012",
        string location = "us-east-1",
        string domainId = "compute",
        string resourceType = "instance",
        string nativeId = "i-AbC123")
    {
        return new OperationTarget(
            providerId,
            accountId,
            location,
            domainId,
            resourceType,
            nativeId);
    }

    [Fact]
    public void StoresSixIdentityFields()
    {
        var target = Create();

        Assert.Equal("aws", target.ProviderId);
        Assert.Equal("123456789012", target.AccountId);
        Assert.Equal("us-east-1", target.Location);
        Assert.Equal("compute", target.DomainId);
        Assert.Equal("instance", target.ResourceType);
        Assert.Equal("i-AbC123", target.NativeId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void RejectsNullIdentityFields(int index)
    {
        Assert.Throws<ArgumentException>(() =>
            CreateWithReplacement(index, null!));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void RejectsBlankIdentityFields(int index)
    {
        Assert.Throws<ArgumentException>(() =>
            CreateWithReplacement(index, "   "));
    }

    [Fact]
    public void TrimsAllIdentityFields()
    {
        var target = Create(
            " aws ",
            " 123456789012 ",
            " us-east-1 ",
            " compute ",
            " instance ",
            " i-AbC123 ");

        Assert.Equal("aws", target.ProviderId);
        Assert.Equal("123456789012", target.AccountId);
        Assert.Equal("us-east-1", target.Location);
        Assert.Equal("compute", target.DomainId);
        Assert.Equal("instance", target.ResourceType);
        Assert.Equal("i-AbC123", target.NativeId);
    }

    [Fact]
    public void PreservesIdentifierCase()
    {
        var target = Create(
            "AWS",
            "Account-ABC",
            "Region-X",
            "Compute",
            "VirtualMachine",
            "Native-AbC");

        Assert.Equal("AWS", target.ProviderId);
        Assert.Equal("Account-ABC", target.AccountId);
        Assert.Equal("Region-X", target.Location);
        Assert.Equal("Compute", target.DomainId);
        Assert.Equal("VirtualMachine", target.ResourceType);
        Assert.Equal("Native-AbC", target.NativeId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void RejectsEmbeddedControlCharacters(int index)
    {
        Assert.Throws<ArgumentException>(() =>
            CreateWithReplacement(index, "part\nother"));
    }

    [Fact]
    public void PreservesDifferentNativeIdentifiers()
    {
        var first = Create(nativeId: "Resource-A");
        var second = Create(nativeId: "Resource-a");

        Assert.Equal("Resource-A", first.NativeId);
        Assert.Equal("Resource-a", second.NativeId);
        Assert.NotEqual(first.NativeId, second.NativeId);
    }

    [Fact]
    public void PreservesDistinctProviderAccountAndLocation()
    {
        var first = Create();
        var second = Create(
            providerId: "azure",
            accountId: "another-account",
            location: "global");

        Assert.NotEqual(first.ProviderId, second.ProviderId);
        Assert.NotEqual(first.AccountId, second.AccountId);
        Assert.NotEqual(first.Location, second.Location);
    }

    [Fact]
    public void PreservesDistinctDomainAndResourceType()
    {
        var first = Create();
        var second = Create(
            domainId: "storage",
            resourceType: "volume");

        Assert.NotEqual(first.DomainId, second.DomainId);
        Assert.NotEqual(first.ResourceType, second.ResourceType);
    }

    [Fact]
    public void ExposesExactlySixReadOnlyProperties()
    {
        var type = typeof(OperationTarget);

        Assert.True(type.IsSealed);

        foreach (var name in new[]
        {
            "ProviderId",
            "AccountId",
            "Location",
            "DomainId",
            "ResourceType",
            "NativeId"
        })
        {
            var property = type.GetProperty(
                name, BindingFlags.Instance | BindingFlags.Public);

            Assert.NotNull(property);
            Assert.NotNull(property!.GetMethod);
            Assert.Null(property.SetMethod);
        }

        Assert.Equal(
            6,
            type.GetProperties(
                BindingFlags.Instance | BindingFlags.Public).Length);
    }

    [Fact]
    public void HasExactlyOneSixArgumentConstructor()
    {
        var constructor = Assert.Single(
            typeof(OperationTarget).GetConstructors());

        var parameters = constructor.GetParameters();

        Assert.Equal(6, parameters.Length);
        Assert.Equal("providerId", parameters[0].Name);
        Assert.Equal("accountId", parameters[1].Name);
        Assert.Equal("location", parameters[2].Name);
        Assert.Equal("domainId", parameters[3].Name);
        Assert.Equal("resourceType", parameters[4].Name);
        Assert.Equal("nativeId", parameters[5].Name);

        foreach (var parameter in parameters)
        {
            Assert.Equal(typeof(string), parameter.ParameterType);
            Assert.False(parameter.IsOptional);
        }
    }

    [Fact]
    public void DoesNotRequireDiscoveryOrGraphResourceId()
    {
        var target = Create(
            providerId: "test-provider",
            accountId: "offline-account",
            location: "global",
            domainId: "storage",
            resourceType: "volume",
            nativeId: "volume-42");

        Assert.Equal("test-provider", target.ProviderId);
        Assert.Equal("global", target.Location);
        Assert.Equal("volume-42", target.NativeId);
    }

    private static OperationTarget CreateWithReplacement(
        int index,
        string replacement)
    {
        var values = new[]
        {
            "aws",
            "123456789012",
            "us-east-1",
            "compute",
            "instance",
            "i-AbC123"
        };

        values[index] = replacement;

        return new OperationTarget(
            values[0],
            values[1],
            values[2],
            values[3],
            values[4],
            values[5]);
    }
}
