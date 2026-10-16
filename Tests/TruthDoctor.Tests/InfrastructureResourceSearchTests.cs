using System;
using System.Collections.Generic;
using TruthDoctor.Controllers.Workbench;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class InfrastructureResourceSearchTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankSearchMatchesResource(
        string? searchText)
    {
        var resource = CreateResource();

        var result =
            InfrastructureResourceSearch.Matches(
                resource,
                searchText);

        Assert.True(result);
    }

    [Theory]
    [InlineData("payments-api")]
    [InlineData("i-0123456789abcdef0")]
    [InlineData("resource-ec2-1")]
    [InlineData("ec2-instance")]
    [InlineData("compute")]
    [InlineData("running")]
    [InlineData("us-east-1")]
    [InlineData("123456789012")]
    public void MatchesCanonicalScalarFields(
        string searchText)
    {
        var resource = CreateResource();

        var result =
            InfrastructureResourceSearch.Matches(
                resource,
                searchText);

        Assert.True(result);
    }

    [Fact]
    public void SearchIsTrimmedAndCaseInsensitive()
    {
        var resource = CreateResource();

        var result =
            InfrastructureResourceSearch.Matches(
                resource,
                "  PAYMENTS-API  ");

        Assert.True(result);
    }

    [Theory]
    [InlineData("PrivateIpAddress")]
    [InlineData("10.0.1.10")]
    public void MatchesPropertyKeysAndValues(
        string searchText)
    {
        var resource = CreateResource();

        var result =
            InfrastructureResourceSearch.Matches(
                resource,
                searchText);

        Assert.True(result);
    }

    [Theory]
    [InlineData("Environment")]
    [InlineData("Production")]
    public void MatchesTagKeysAndValues(
        string searchText)
    {
        var resource = CreateResource();

        var result =
            InfrastructureResourceSearch.Matches(
                resource,
                searchText);

        Assert.True(result);
    }

    [Fact]
    public void UnrelatedSearchDoesNotMatch()
    {
        var resource = CreateResource();

        var result =
            InfrastructureResourceSearch.Matches(
                resource,
                "unrelated-value");

        Assert.False(result);
    }

    [Fact]
    public void NullResourceIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            InfrastructureResourceSearch.Matches(
                null!,
                "anything"));
    }

    private static InfrastructureResource
        CreateResource()
    {
        return new InfrastructureResource
        {
            ProviderId = "aws",
            AccountId = "123456789012",
            DomainId = "compute",
            ResourceType = "ec2-instance",
            ResourceId = "resource-ec2-1",
            NativeId = "i-0123456789abcdef0",
            DisplayName = "payments-api",
            State = "running",
            Location = "us-east-1",
            AvailabilityZone = "us-east-1a",
            Arn =
                "arn:aws:ec2:us-east-1:" +
                "123456789012:instance/" +
                "i-0123456789abcdef0",

            Properties =
                new Dictionary<string, string>
                {
                    ["PrivateIpAddress"] =
                        "10.0.1.10",

                    ["InstanceType"] =
                        "t3.micro"
                },

            Tags =
                new Dictionary<string, string>
                {
                    ["Environment"] =
                        "Production",

                    ["Owner"] =
                        "Platform"
                }
        };
    }
}
