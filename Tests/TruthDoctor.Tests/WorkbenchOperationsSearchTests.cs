using System;
using System.Collections.Generic;
using TruthDoctor.Controllers.Workbench;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class WorkbenchOperationsSearchTests
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
            WorkbenchOperationsSearch.Match(
                resource,
                searchText);

        Assert.NotNull(result);

        Assert.Equal(
            WorkbenchOperationsSearchMatchKind.Resource,
            result.Kind);
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
    public void CanonicalResourceFieldsMatch(
        string searchText)
    {
        var resource = CreateResource();

        var result =
            WorkbenchOperationsSearch.Match(
                resource,
                searchText);

        Assert.NotNull(result);

        Assert.Equal(
            WorkbenchOperationsSearchMatchKind.Resource,
            result.Kind);
    }

    [Theory]
    [InlineData("Start")]
    [InlineData("Stop")]
    [InlineData("Restart")]
    public void CapabilityMatches(
        string searchText)
    {
        var resource = CreateResource();

        var result =
            WorkbenchOperationsSearch.Match(
                resource,
                searchText);

        Assert.NotNull(result);

        Assert.Equal(
            WorkbenchOperationsSearchMatchKind.Capability,
            result.Kind);

        Assert.Equal(
            searchText,
            result.DisplayText);
    }

    [Fact]
    public void CapabilitySearchIsTrimmedAndCaseInsensitive()
    {
        var resource = CreateResource();

        var result =
            WorkbenchOperationsSearch.Match(
                resource,
                "  RESTART  ");

        Assert.NotNull(result);

        Assert.Equal(
            WorkbenchOperationsSearchMatchKind.Capability,
            result.Kind);

        Assert.Equal(
            "Restart",
            result.DisplayText);
    }

    [Fact]
    public void UnrelatedSearchDoesNotMatch()
    {
        var resource = CreateResource();

        var result =
            WorkbenchOperationsSearch.Match(
                resource,
                "does-not-exist");

        Assert.Null(result);
    }

    [Fact]
    public void EmptyCapabilitiesAreSafe()
    {
        var resource = CreateResource();

        resource.Capabilities = [];

        var result =
            WorkbenchOperationsSearch.Match(
                resource,
                "Restart");

        Assert.Null(result);
    }

    [Fact]
    public void NullCapabilitiesAreSafe()
    {
        var resource = CreateResource();

        resource.Capabilities = null!;

        var result =
            WorkbenchOperationsSearch.Match(
                resource,
                "Restart");

        Assert.Null(result);
    }

    [Fact]
    public void NullResourceIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            WorkbenchOperationsSearch.Match(
                null!,
                "anything"));
    }

    [Fact]
    public void SearchKindsRepresentOperationsRoadmapDomains()
    {
        var kinds =
            Enum.GetValues<WorkbenchOperationsSearchMatchKind>();

        Assert.Contains(
            WorkbenchOperationsSearchMatchKind.Resource,
            kinds);

        Assert.Contains(
            WorkbenchOperationsSearchMatchKind.Capability,
            kinds);

        Assert.Contains(
            WorkbenchOperationsSearchMatchKind.Action,
            kinds);

        Assert.Contains(
            WorkbenchOperationsSearchMatchKind.Runbook,
            kinds);

        Assert.Contains(
            WorkbenchOperationsSearchMatchKind.OperationRequest,
            kinds);

        Assert.Contains(
            WorkbenchOperationsSearchMatchKind.ApprovalState,
            kinds);

        Assert.Contains(
            WorkbenchOperationsSearchMatchKind.ExecutionState,
            kinds);
    }

    [Fact]
    public void SearchReturnsMatchingResources()
    {
        var matching =
            CreateResource();

        var unrelated =
            CreateResource();

        unrelated.ResourceId = "resource-db-1";
        unrelated.NativeId = "db-1";
        unrelated.DisplayName = "orders-database";
        unrelated.DomainId = "database";
        unrelated.ResourceType = "database";
        unrelated.State = "available";
        unrelated.Capabilities = ["Backup"];

        var results =
            WorkbenchOperationsSearch.Search(
                new[]
                {
                    matching,
                    unrelated
                },
                "payments-api");

        var result =
            Assert.Single(results);

        Assert.Same(
            matching,
            result.Resource);

        Assert.Equal(
            WorkbenchOperationsSearchMatchKind.Resource,
            result.Kind);
    }

    [Fact]
    public void SearchReturnsCapabilityMatchWithOwningResource()
    {
        var resource =
            CreateResource();

        var results =
            WorkbenchOperationsSearch.Search(
                new[]
                {
                    resource
                },
                "restart");

        var result =
            Assert.Single(results);

        Assert.Same(
            resource,
            result.Resource);

        Assert.Equal(
            WorkbenchOperationsSearchMatchKind.Capability,
            result.Kind);

        Assert.Equal(
            "Restart",
            result.DisplayText);
    }

    [Fact]
    public void SearchUsesOneBestMatchPerResource()
    {
        var resource =
            CreateResource();

        resource.DisplayName =
            "restart-controller";

        var results =
            WorkbenchOperationsSearch.Search(
                new[]
                {
                    resource
                },
                "restart");

        var result =
            Assert.Single(results);

        Assert.Equal(
            WorkbenchOperationsSearchMatchKind.Resource,
            result.Kind);
    }

    [Fact]
    public void BlankSearchReturnsOneResourceEntryPerResource()
    {
        var first =
            CreateResource();

        var second =
            CreateResource();

        second.ResourceId = "resource-db-1";
        second.NativeId = "db-1";
        second.DisplayName = "orders-database";
        second.Capabilities = ["Backup"];

        var results =
            WorkbenchOperationsSearch.Search(
                new[]
                {
                    first,
                    second
                },
                "   ");

        Assert.Equal(
            2,
            results.Count);

        Assert.All(
            results,
            result =>
                Assert.Equal(
                    WorkbenchOperationsSearchMatchKind.Resource,
                    result.Kind));
    }

    [Fact]
    public void SearchReturnsEmptyWhenNothingMatches()
    {
        var results =
            WorkbenchOperationsSearch.Search(
                new[]
                {
                    CreateResource()
                },
                "does-not-exist");

        Assert.Empty(results);
    }

    [Fact]
    public void NullResourceCollectionIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            WorkbenchOperationsSearch.Search(
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

            Properties =
                new Dictionary<string, string>
                {
                    ["PrivateIpAddress"] =
                        "10.0.1.10"
                },

            Tags =
                new Dictionary<string, string>
                {
                    ["Environment"] =
                        "Production"
                },

            Capabilities =
            [
                "Start",
                "Stop",
                "Restart"
            ]
        };
    }
}
