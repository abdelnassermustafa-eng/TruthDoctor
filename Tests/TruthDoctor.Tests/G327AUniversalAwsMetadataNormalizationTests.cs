using System;
using System.Linq;
using System.Text.Json;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327AUniversalAwsMetadataNormalizationTests
{
    private static AwsNormalizedMetadata Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return AwsUniversalMetadataNormalizer.Normalize(
            document.RootElement);
    }

    [Fact]
    public void PreservesScalarPropertiesAcrossResourceTypes()
    {
        var metadata = Parse("""
        {
          "InstanceType": "t3.micro",
          "AllocatedStorage": 100,
          "MultiAZ": false,
          "Engine": "postgres",
          "VersioningEnabled": true
        }
        """);

        Assert.Equal(
            "t3.micro",
            metadata.Properties["InstanceType"]);

        Assert.Equal(
            "100",
            metadata.Properties["AllocatedStorage"]);

        Assert.Equal(
            "false",
            metadata.Properties["MultiAZ"]);

        Assert.Equal(
            "postgres",
            metadata.Properties["Engine"]);

        Assert.Equal(
            "true",
            metadata.Properties["VersioningEnabled"]);
    }

    [Fact]
    public void FlattensNestedAwsRelationshipsAndArrays()
    {
        var metadata = Parse("""
        {
          "VpcId": "vpc-001",
          "NetworkInterfaces": [
            {
              "NetworkInterfaceId": "eni-001",
              "Attachment": {
                "InstanceId": "i-001"
              }
            }
          ],
          "SecurityGroups": [
            {
              "GroupId": "sg-001"
            }
          ]
        }
        """);

        Assert.Equal(
            "eni-001",
            metadata.Properties[
                "NetworkInterfaces[0].NetworkInterfaceId"]);

        Assert.Equal(
            "i-001",
            metadata.Properties[
                "NetworkInterfaces[0].Attachment.InstanceId"]);

        Assert.Equal(
            "sg-001",
            metadata.Properties[
                "SecurityGroups[0].GroupId"]);

        Assert.Equal(
            "1",
            metadata.Properties["SecurityGroups.Count"]);
    }

    [Fact]
    public void PreservesArbitraryNonSensitiveAwsTags()
    {
        var metadata = Parse("""
        {
          "Tags": [
            { "Key": "Name", "Value": "Example" },
            { "Key": "Project", "Value": "Labs" },
            { "Key": "Lab", "Value": "Networking" },
            { "Key": "Custom:Team", "Value": "Platform" }
          ]
        }
        """);

        Assert.Equal(4, metadata.Tags.Count);
        Assert.Equal(
            "Networking",
            metadata.Tags["Lab"]);
        Assert.Equal(
            "Platform",
            metadata.Tags["Custom:Team"]);
    }

    [Fact]
    public void ExcludesKnownSensitiveFieldsAndTags()
    {
        var metadata = Parse("""
        {
          "VpcId": "vpc-001",
          "ClientToken": "private-request-token",
          "Credentials": {
            "AccessKey": "private-access-key"
          },
          "Settings": {
            "ApiKey": "private-api-key",
            "Engine": "postgres"
          },
          "Tags": [
            { "Key": "Name", "Value": "Safe" },
            { "Key": "AccessToken", "Value": "private-tag" }
          ]
        }
        """);

        Assert.Equal(
            "vpc-001",
            metadata.Properties["VpcId"]);

        Assert.Equal(
            "postgres",
            metadata.Properties["Settings.Engine"]);

        Assert.DoesNotContain(
            metadata.Properties.Keys,
            key => key.Contains(
                "Token", StringComparison.OrdinalIgnoreCase));

        Assert.DoesNotContain(
            metadata.Properties.Values,
            value => value.Contains(
                "private", StringComparison.OrdinalIgnoreCase));

        Assert.Single(metadata.Tags);
    }

    [Fact]
    public void SupportsObjectStyleTagsAndMissingTags()
    {
        var withTags = Parse("""
        {
          "Tags": {
            "Name": "Bucket",
            "CostCenter": "Research"
          }
        }
        """);

        var withoutTags = Parse("""
        {
          "BucketName": "example",
          "Region": "us-east-1"
        }
        """);

        Assert.Equal(
            "Research",
            withTags.Tags["CostCenter"]);

        Assert.Empty(withoutTags.Tags);
        Assert.Equal(
            "us-east-1",
            withoutTags.Properties["Region"]);
    }

    [Fact]
    public void DoesNotInventNullOrMissingAttributes()
    {
        var metadata = Parse("""
        {
          "VpcId": "vpc-001",
          "AvailabilityZone": null,
          "PublicIpAddress": null
        }
        """);

        Assert.Single(metadata.Properties);
        Assert.False(
            metadata.Properties.ContainsKey(
                "AvailabilityZone"));
        Assert.False(
            metadata.Properties.ContainsKey(
                "PublicIpAddress"));
    }

    [Fact]
    public void RejectsNonObjectResourceDocuments()
    {
        using var document = JsonDocument.Parse(
            """["not-a-resource"]""");

        Assert.Throws<ArgumentException>(
            () => AwsUniversalMetadataNormalizer.Normalize(
                document.RootElement));
    }
}
