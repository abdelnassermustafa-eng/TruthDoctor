using System.Collections.Generic;
using TruthDoctor.Controllers.Workbench;
using TruthDoctor.Models.Platform;
using TruthDoctor.State;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class WorkbenchResourceControllerTests
{
    [Theory]
    [InlineData("resource-payment")]
    [InlineData("PrivateIpAddress")]
    [InlineData("10.0.1.10")]
    [InlineData("Production")]
    public void ApplyFiltersUsesCanonicalResourceSearch(
        string searchText)
    {
        var fixture = CreateFixture();

        var result =
            fixture.Controller.ApplyFilters(
                searchText,
                "All domains",
                "All states");

        var resource =
            Assert.Single(result);

        Assert.Equal(
            "resource-payment",
            resource.ResourceId);

        Assert.Equal(
            searchText,
            fixture.State.SearchText);

        Assert.Single(
            fixture.State.VisibleResources);

        Assert.Same(
            resource,
            fixture.State.VisibleResources[0]);
    }

    [Fact]
    public void SharedTagKeyMatchesAllRelevantResources()
    {
        var fixture = CreateFixture();

        var result =
            fixture.Controller.ApplyFilters(
                "Environment",
                "All domains",
                "All states");

        Assert.Equal(
            2,
            result.Count);

        Assert.Equal(
            "Environment",
            fixture.State.SearchText);

        Assert.Equal(
            2,
            fixture.State.VisibleResources.Count);

        Assert.Contains(
            result,
            resource =>
                resource.ResourceId ==
                "resource-payment");

        Assert.Contains(
            result,
            resource =>
                resource.ResourceId ==
                "resource-database");
    }

    [Fact]
    public void SearchDomainAndStateFiltersCompose()
    {
        var fixture = CreateFixture();

        var result =
            fixture.Controller.ApplyFilters(
                "PAYMENTS",
                "compute",
                "RUNNING");

        var resource =
            Assert.Single(result);

        Assert.Equal(
            "resource-payment",
            resource.ResourceId);

        Assert.Equal(
            "PAYMENTS",
            fixture.State.SearchText);

        Assert.Equal(
            "compute",
            fixture.State.SelectedDomainId);

        Assert.Equal(
            "RUNNING",
            fixture.State.SelectedStateFilter);

        Assert.Single(
            fixture.State.VisibleResources);

        Assert.Same(
            resource,
            fixture.State.VisibleResources[0]);
    }

    private static ControllerFixture
        CreateFixture()
    {
        var state =
            new WorkbenchState();

        state.SetPlatformState(
            new PlatformState
            {
                Context =
                    new PlatformContext
                    {
                        ProviderId = "aws",
                        ProviderName = "AWS",
                        AccountId = "123456789012",
                        AccountName = "Production",
                        DefaultLocation = "us-east-1",

                        Locations =
                            new List<string>
                            {
                                "us-east-1",
                                "us-west-2"
                            }
                    },

                Domains =
                    new List<InfrastructureDomain>
                    {
                        new()
                        {
                            Id = "compute",
                            DisplayName = "Compute",
                            ResourceCount = 1
                        },

                        new()
                        {
                            Id = "data",
                            DisplayName = "Data",
                            ResourceCount = 1
                        }
                    },

                Resources =
                    new List<InfrastructureResource>
                    {
                        CreatePaymentResource(),
                        CreateDatabaseResource()
                    },

                TotalResourceCount = 2
            });

        return new ControllerFixture(
            state,
            new WorkbenchResourceController(
                state));
    }

    private static InfrastructureResource
        CreatePaymentResource()
    {
        return new InfrastructureResource
        {
            ProviderId = "aws",
            AccountId = "123456789012",
            DomainId = "compute",
            ResourceType = "ec2-instance",
            ResourceId = "resource-payment",
            NativeId = "i-0123456789abcdef0",
            DisplayName = "payments-api",
            State = "running",
            Location = "us-east-1",
            AvailabilityZone = "us-east-1a",

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

    private static InfrastructureResource
        CreateDatabaseResource()
    {
        return new InfrastructureResource
        {
            ProviderId = "aws",
            AccountId = "123456789012",
            DomainId = "data",
            ResourceType = "rds-instance",
            ResourceId = "resource-database",
            NativeId = "orders-db-01",
            DisplayName = "orders-database",
            State = "available",
            Location = "us-west-2",
            AvailabilityZone = "us-west-2a",

            Properties =
                new Dictionary<string, string>
                {
                    ["Engine"] =
                        "postgres"
                },

            Tags =
                new Dictionary<string, string>
                {
                    ["Environment"] =
                        "Staging",

                    ["Owner"] =
                        "Database"
                }
        };
    }

    private sealed record ControllerFixture(
        WorkbenchState State,
        WorkbenchResourceController Controller);
}
