using System;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A243ConnectionIdentityBehaviorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RejectsInvalidConnectionId(string? invalid)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new ProviderConnectionIdentity(
                invalid!,
                "aws"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RejectsInvalidProviderId(string? invalid)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new ProviderConnectionIdentity(
                "aws-account-primary",
                invalid!));
    }

    [Fact]
    public void IdenticalConnectionIdentitiesAreEqual()
    {
        var first = new ProviderConnectionIdentity(
            "aws-account-primary", "aws");

        var second = new ProviderConnectionIdentity(
            "aws-account-primary", "aws");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void DistinctConnectionsMayShareOneProvider()
    {
        var first = new ProviderConnectionIdentity(
            "aws-account-primary", "aws");

        var second = new ProviderConnectionIdentity(
            "aws-account-secondary", "aws");

        Assert.Equal(first.ProviderId, second.ProviderId);
        Assert.NotEqual(first.ConnectionId, second.ConnectionId);
        Assert.NotEqual(first, second);
    }
}
