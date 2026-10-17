using System.Collections.Generic;
using TruthDoctor.Controllers.Workbench;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class InfrastructureResourceSearchResolverTests
{
    [Fact]
    public void UniqueMatchIsResolved()
    {
        var resources =
            new[]
            {
                Resource(
                    "resource-1",
                    "CloudWatch-Metrics-Lab"),

                Resource(
                    "resource-2",
                    "lab-key")
            };

        var result =
            InfrastructureResourceSearchResolver
                .ResolveUnique(
                    resources,
                    "CloudWatch-Metrics-Lab");

        Assert.NotNull(result);

        Assert.Equal(
            "resource-1",
            result.ResourceId);
    }

    [Fact]
    public void MultipleMatchesAreNotResolvedArbitrarily()
    {
        var resources =
            new[]
            {
                Resource(
                    "resource-1",
                    "private-subnet-a"),

                Resource(
                    "resource-2",
                    "private-subnet-b")
            };

        var result =
            InfrastructureResourceSearchResolver
                .ResolveUnique(
                    resources,
                    "private");

        Assert.Null(result);
    }

    [Fact]
    public void NoMatchReturnsNull()
    {
        var resources =
            new[]
            {
                Resource(
                    "resource-1",
                    "CloudWatch-Metrics-Lab")
            };

        var result =
            InfrastructureResourceSearchResolver
                .ResolveUnique(
                    resources,
                    "does-not-exist");

        Assert.Null(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankSearchReturnsNull(
        string searchText)
    {
        var resources =
            new[]
            {
                Resource(
                    "resource-1",
                    "CloudWatch-Metrics-Lab")
            };

        var result =
            InfrastructureResourceSearchResolver
                .ResolveUnique(
                    resources,
                    searchText);

        Assert.Null(result);
    }

    private static InfrastructureResource Resource(
        string resourceId,
        string displayName)
    {
        return new InfrastructureResource
        {
            ProviderId = "aws",
            AccountId = "123456789012",
            DomainId = "compute",
            ResourceType = "instance",
            ResourceId = resourceId,
            NativeId = resourceId,
            DisplayName = displayName,
            Location = "us-east-1",

            Properties =
                new Dictionary<string, string>(),

            Tags =
                new Dictionary<string, string>()
        };
    }
}
