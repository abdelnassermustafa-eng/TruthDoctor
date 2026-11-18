using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using TruthDoctor.State;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327AAwsRegionSelectorTests
{
    [Fact]
    public void CatalogIncludesEnabledAndOptedInRegions()
    {
        const string response = """
        {
          "Regions": [
            {
              "RegionName": "us-west-2",
              "OptInStatus": "opt-in-not-required"
            },
            {
              "RegionName": "eu-central-1",
              "OptInStatus": "opted-in"
            },
            {
              "RegionName": "ap-east-1",
              "OptInStatus": "not-opted-in"
            }
          ]
        }
        """;

        var regions = AwsRegionCatalog.Parse(response);

        Assert.Equal(
            new[] { "eu-central-1", "us-west-2" },
            regions);
    }

    [Fact]
    public void CatalogRejectsMalformedResponses()
    {
        Assert.Throws<FormatException>(
            () => AwsRegionCatalog.Parse("""{"Other":[]}"""));
    }

    [Fact]
    public async Task SelectingRegionChangesActualAwsCliScanTarget()
    {
        var invocations = new List<string>();

        Task<string> Runner(
            IReadOnlyList<string> arguments,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int regionIndex = arguments.ToList().IndexOf("--region");

            Assert.True(regionIndex >= 0);

            string region = arguments[regionIndex + 1];

            string operation = arguments.FirstOrDefault(
                value => value is
                    "get-caller-identity" or
                    "describe-regions" or
                    "describe-vpcs" or
                    "describe-subnets" or
                    "describe-instances" or
                    "describe-db-instances" or
                    "list-functions" or
                    "list-buckets" or
                    "describe-route-tables" or
                    "describe-internet-gateways" or
                    "describe-nat-gateways" or
                    "describe-security-groups" or
                    "describe-network-acls" or
                    "describe-network-interfaces" or
                    "describe-volumes") ?? "";

            invocations.Add($"{region}:{operation}");

            string response = operation switch
            {
                "get-caller-identity" =>
                    """{"Account":"123456789012","Arn":"arn:aws:iam::123456789012:role/test"}""",

                "describe-regions" =>
                    """{"Regions":[{"RegionName":"us-east-1"},{"RegionName":"us-west-2"}]}""",

                "describe-vpcs" =>
                    """{"Vpcs":[{"VpcId":"vpc-west","State":"available"}]}""",

                "describe-subnets" =>
                    """{"Subnets":[]}""",

                "describe-instances" =>
                    """{"Reservations":[]}""",

                "describe-db-instances" =>
                    """{"DBInstances":[]}""",

                "list-functions" =>
                    """{"Functions":[]}""",

                "list-buckets" =>
                    """{"Buckets":[]}""",

                "describe-route-tables" =>
                    """{"RouteTables":[]}""",

                "describe-internet-gateways" =>
                    """{"InternetGateways":[]}""",

                "describe-nat-gateways" =>
                    """{"NatGateways":[]}""",

                "describe-security-groups" =>
                    """{"SecurityGroups":[]}""",

                "describe-network-acls" =>
                    """{"NetworkAcls":[]}""",

                "describe-network-interfaces" =>
                    """{"NetworkInterfaces":[]}""",

                "describe-volumes" =>
                    """{"Volumes":[]}""",

                _ => throw new InvalidOperationException(
                    $"Unexpected AWS command: {operation}")
            };

            return Task.FromResult(response);
        }

        var session = new StandaloneAwsDesktopSession(
            "test-profile",
            new[] { "us-east-1" },
            Runner,
            enableRegionCatalog: true);

        var workbench = new WorkbenchState();

        var platform = await session.DiscoverAsync(
            workbench,
            "us-west-2",
            CancellationToken.None);

        Assert.Equal(
            "us-west-2",
            platform.Context.DefaultLocation);

        Assert.Contains(
            "us-east-1",
            platform.Context.Locations);

        Assert.Contains(
            "us-west-2",
            platform.Context.Locations);

        Assert.Contains(
            "us-west-2:describe-vpcs",
            invocations);

        Assert.DoesNotContain(
            "us-east-1:describe-vpcs",
            invocations);
    }
}
