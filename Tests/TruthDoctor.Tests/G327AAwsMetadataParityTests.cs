using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327AAwsMetadataParityTests
{
    private const string Connection = "aws-metadata-test";

    [Fact]
    public void ProviderRecordPreservesResourceMetadata()
    {
        var record = new ProviderResourceRecord(
            "aws", "i-001", "compute-instance", "Example",
            State: "running",
            AvailabilityZone: "us-east-1a",
            Properties: new Dictionary<string, string>
            {
                ["InstanceType"] = "t3.micro",
                ["VpcId"] = "vpc-001"
            },
            Tags: new Dictionary<string, string>
            {
                ["Environment"] = "test"
            });

        var state = StandaloneDiscoveryProjector.Project(
            [record], "123456789012",
            "us-east-1", "infrastructure");

        var resource = Assert.Single(state.Resources);

        Assert.Equal("i-001", resource.NativeId);
        Assert.Equal("running", resource.State);
        Assert.Equal("us-east-1a", resource.AvailabilityZone);
        Assert.Equal("t3.micro", resource.Properties["InstanceType"]);
        Assert.Equal("vpc-001", resource.Properties["VpcId"]);
        Assert.Equal("test", resource.Tags["Environment"]);
    }

    [Fact]
    public async Task AwsAdapterRetainsSafePropertiesAndTags()
    {
        var adapter = new AwsCliMultiServiceAdapter(
            Connection, "test-profile", ["us-east-1"],
            (args, _) =>
            {
                string command = string.Join(" ", args);

                if (command.Contains("get-caller-identity"))
                    return Task.FromResult(
                        """{"Account":"123456789012","Arn":"arn:aws:iam::123456789012:role/test"}""");

                if (command.Contains("describe-instances"))
                    return Task.FromResult(
                        """{"Reservations":[{"Instances":[{"InstanceId":"i-001","InstanceType":"t3.micro","VpcId":"vpc-001","SubnetId":"subnet-001","ClientToken":"DO-NOT-EXPOSE","State":{"Name":"running"},"Placement":{"AvailabilityZone":"us-east-1a"},"Tags":[{"Key":"Name","Value":"Compute Lab"},{"Key":"Environment","Value":"test"}]}]}]}""");

                if (command.Contains("describe-vpcs"))
                    return Task.FromResult(
                        """{"Vpcs":[{"VpcId":"vpc-001"}]}""");

                if (command.Contains("describe-subnets"))
                    return Task.FromResult(
                        """{"Subnets":[{"SubnetId":"subnet-001","VpcId":"vpc-001"}]}""");

                if (command.Contains("describe-db-instances"))
                    return Task.FromResult("""{"DBInstances":[]}""");

                if (command.Contains("list-functions"))
                    return Task.FromResult("""{"Functions":[]}""");

                if (command.Contains("list-buckets"))
                    return Task.FromResult("""{"Buckets":[]}""");

                throw new InvalidOperationException("Unexpected command");
            });

        await adapter.AuthenticateAsync(
            Connection, CancellationToken.None);

        var records = await adapter.DiscoverAsync(
            Connection, CancellationToken.None);

        var instance = Assert.Single(
            records.Where(x => x.ResourceType == "compute-instance"));

        Assert.Equal("Compute Lab", instance.DisplayName);
        Assert.Equal("running", instance.State);
        Assert.Equal("us-east-1a", instance.AvailabilityZone);
        Assert.Equal("t3.micro", instance.Properties!["InstanceType"]);
        Assert.Equal("vpc-001", instance.Properties["VpcId"]);
        Assert.Equal("test", instance.Tags!["Environment"]);
        Assert.False(instance.Properties.ContainsKey("ClientToken"));
    }

    [Fact]
    public async Task AwsMetadataSurvivesInventoryReconciliation()
    {
        var adapter = new AwsCliMultiServiceAdapter(
            Connection, "test-profile", ["us-east-1"],
            (args, _) =>
            {
                string command = string.Join(" ", args);

                if (command.Contains("get-caller-identity"))
                    return Task.FromResult(
                        """{"Account":"123456789012","Arn":"arn:aws:iam::123456789012:role/test"}""");

                if (command.Contains("describe-vpcs"))
                    return Task.FromResult(
                        """{"Vpcs":[{"VpcId":"vpc-001","CidrBlock":"10.0.0.0/16","State":"available"}]}""");

                if (command.Contains("describe-subnets"))
                    return Task.FromResult("""{"Subnets":[]}""");

                if (command.Contains("describe-instances"))
                    return Task.FromResult("""{"Reservations":[]}""");

                if (command.Contains("describe-db-instances"))
                    return Task.FromResult("""{"DBInstances":[]}""");

                if (command.Contains("list-functions"))
                    return Task.FromResult("""{"Functions":[]}""");

                if (command.Contains("list-buckets"))
                    return Task.FromResult("""{"Buckets":[]}""");

                throw new InvalidOperationException("Unexpected command");
            });

        await adapter.AuthenticateAsync(
            Connection, CancellationToken.None);

        var result = await adapter.DiscoverInventoryAsync(
            Connection, AwsDiscoveryInventorySnapshot.Empty);

        var vpc = Assert.Single(result.Snapshot.Resources);

        Assert.Equal("available", vpc.State);
        Assert.Equal("10.0.0.0/16", vpc.Properties!["CidrBlock"]);
        Assert.Equal("vpc-001", vpc.Key.ResourceId);
    }
}
