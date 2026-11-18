using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327AUniversalAwsAdapterIntegrationTests
{
    private const string Connection = "aws-universal-metadata";

    private static readonly IReadOnlyDictionary<string, string>
        Responses = new Dictionary<string, string>
        {
            ["get-caller-identity"] =
                """{"Account":"123456789012","Arn":"arn:aws:iam::123456789012:role/test"}""",

            ["describe-vpcs"] = """
                {
                  "Vpcs": [{
                    "VpcId": "vpc-001",
                    "CidrBlock": "10.0.0.0/16",
                    "State": "available",
                    "IsDefault": false,
                    "Tags": [
                      {"Key":"Name","Value":"Network Lab"},
                      {"Key":"Lab","Value":"VPC"},
                      {"Key":"Custom:Team","Value":"Platform"}
                    ]
                  }]
                }
                """,

            ["describe-subnets"] = """
                {
                  "Subnets": [{
                    "SubnetId": "subnet-001",
                    "VpcId": "vpc-001",
                    "MapPublicIpOnLaunch": true,
                    "AvailabilityZone": "us-east-1a",
                    "Tags": [
                      {"Key":"Lab","Value":"Subnets"}
                    ]
                  }]
                }
                """,

            ["describe-instances"] = """
                {
                  "Reservations": [{
                    "Instances": [{
                      "InstanceId": "i-001",
                      "InstanceType": "t3.micro",
                      "VpcId": "vpc-001",
                      "SubnetId": "subnet-001",
                      "State": {"Name":"running"},
                      "Placement": {
                        "AvailabilityZone":"us-east-1a"
                      },
                      "EbsOptimized": false,
                      "PrivateDnsName": "ip-10-0-0-5.internal",
                      "NetworkInterfaces": [{
                        "NetworkInterfaceId": "eni-001",
                        "Attachment": {
                          "InstanceId": "i-001"
                        }
                      }],
                      "SecurityGroups": [{
                        "GroupId": "sg-001"
                      }],
                      "ClientToken": "DO-NOT-EXPOSE",
                      "Tags": [
                        {"Key":"Name","Value":"Compute Lab"},
                        {"Key":"Lab","Value":"EC2"},
                        {"Key":"AccessToken","Value":"SECRET"}
                      ]
                    }]
                  }]
                }
                """,

            ["describe-db-instances"] = """
                {
                  "DBInstances": [{
                    "DBInstanceIdentifier": "db-001",
                    "Engine": "postgres",
                    "AllocatedStorage": 100,
                    "MultiAZ": false,
                    "Endpoint": {
                      "Address": "db.example.internal",
                      "Port": 5432
                    }
                  }]
                }
                """,

            ["list-functions"] = """
                {
                  "Functions": [{
                    "FunctionName": "function-001",
                    "Runtime": "python3.12",
                    "MemorySize": 256,
                    "Environment": {
                      "Variables": {
                        "MODE": "test",
                        "DB_PASSWORD": "SECRET"
                      }
                    }
                  }]
                }
                """,

            ["list-buckets"] = """
                {
                  "Buckets": [{
                    "Name": "bucket-001",
                    "CreationDate": "2026-01-01T00:00:00Z"
                  }]
                }
                """
        };

    private static Func<IReadOnlyList<string>,
        CancellationToken, Task<string>> Runner()
    {
        return (arguments, cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            string command = arguments.FirstOrDefault(
                value => Responses.ContainsKey(value)) ?? "";

            if (!Responses.TryGetValue(command, out var response))
            {
                throw new InvalidOperationException(
                    "Unexpected mocked AWS command: " + command);
            }

            return Task.FromResult(response);
        };
    }

    private static async Task<AwsCliMultiServiceAdapter>
        ConnectedAdapterAsync()
    {
        var adapter = new AwsCliMultiServiceAdapter(
            Connection,
            "test-profile",
            ["us-east-1"],
            Runner());

        await adapter.AuthenticateAsync(
            Connection, CancellationToken.None);

        return adapter;
    }

    [Fact]
    public async Task NormalizesPropertiesAcrossSixAwsFamilies()
    {
        var adapter = await ConnectedAdapterAsync();
        var records = await adapter.DiscoverAsync(
            Connection, CancellationToken.None);

        Assert.Equal(6, records.Count);

        var vpc = Assert.Single(records.Where(
            r => r.ResourceType == "virtual-network"));
        Assert.Equal(
            "false", vpc.Properties!["IsDefault"]);
        Assert.Equal(
            "VPC", vpc.Tags!["Lab"]);
        Assert.Equal(
            "Platform", vpc.Tags["Custom:Team"]);

        var subnet = Assert.Single(records.Where(
            r => r.ResourceType == "subnet"));
        Assert.Equal(
            "true", subnet.Properties!["MapPublicIpOnLaunch"]);
        Assert.Equal("us-east-1a", subnet.AvailabilityZone);

        var instance = Assert.Single(records.Where(
            r => r.ResourceType == "compute-instance"));
        Assert.Equal("running", instance.State);
        Assert.Equal("us-east-1a", instance.AvailabilityZone);
        Assert.Equal(
            "false", instance.Properties!["EbsOptimized"]);
        Assert.Equal(
            "eni-001",
            instance.Properties[
                "NetworkInterfaces[0].NetworkInterfaceId"]);
        Assert.Equal(
            "sg-001",
            instance.Properties[
                "SecurityGroups[0].GroupId"]);
        Assert.Equal("EC2", instance.Tags!["Lab"]);

        var database = Assert.Single(records.Where(
            r => r.ResourceType == "database-instance"));
        Assert.Equal(
            "5432", database.Properties!["Endpoint.Port"]);
        Assert.Equal(
            "100", database.Properties["AllocatedStorage"]);

        var function = Assert.Single(records.Where(
            r => r.ResourceType == "function"));
        Assert.Equal(
            "256", function.Properties!["MemorySize"]);
        Assert.False(
            function.Properties.ContainsKey(
                "Environment.Variables.MODE"));

        var bucket = Assert.Single(records.Where(
            r => r.ResourceType == "storage-bucket"));
        Assert.Equal(
            "2026-01-01T00:00:00Z",
            bucket.Properties!["CreationDate"]);
    }

    [Fact]
    public async Task ReconciliationPreservesMetadataAndExcludesSecrets()
    {
        var adapter = await ConnectedAdapterAsync();

        var transition = await adapter.DiscoverInventoryAsync(
            Connection,
            AwsDiscoveryInventorySnapshot.Empty,
            CancellationToken.None);

        Assert.NotEmpty(transition.Snapshot.Resources);

        var instance = Assert.Single(
            transition.Snapshot.Resources.Where(
                r => r.Key.ResourceType == "compute-instance"));

        Assert.Equal("i-001", instance.Key.ResourceId);

        Assert.Equal(
            "eni-001",
            instance.Properties![
                "NetworkInterfaces[0].NetworkInterfaceId"]);

        Assert.False(instance.Properties.ContainsKey(
            "ClientToken"));

        Assert.DoesNotContain(
            instance.Properties.Keys,
            key => key.Contains(
                "PASSWORD", StringComparison.OrdinalIgnoreCase));

        Assert.DoesNotContain(
            instance.Tags!.Keys,
            key => key.Contains(
                "Token", StringComparison.OrdinalIgnoreCase));

        var function = Assert.Single(
            transition.Snapshot.Resources.Where(
                r => r.Key.ResourceType == "function"));

        Assert.DoesNotContain(
            function.Properties!.Values,
            value => value == "SECRET");
    }
}
