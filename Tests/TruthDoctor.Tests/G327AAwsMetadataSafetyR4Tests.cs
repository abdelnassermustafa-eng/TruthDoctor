using System;
using System.Text.Json;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327AAwsMetadataSafetyR4Tests
{
    private static AwsNormalizedMetadata Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return AwsUniversalMetadataNormalizer.Normalize(
            document.RootElement);
    }

    [Fact]
    public void ExcludesEntireLambdaEnvironmentVariableContainer()
    {
        var result = Parse("""
        {
          "FunctionName": "example-function",
          "Runtime": "python3.12",
          "MemorySize": 256,
          "Environment": {
            "Variables": {
              "MODE": "a-confidential-value",
              "REGION": "us-east-1"
            },
            "Error": {
              "ErrorCode": "ConfigurationError"
            }
          }
        }
        """);

        Assert.Equal("256", result.Properties["MemorySize"]);
        Assert.Equal("python3.12", result.Properties["Runtime"]);
        Assert.False(result.Properties.ContainsKey(
            "Environment.Variables.MODE"));
        Assert.False(result.Properties.ContainsKey(
            "Environment.Variables.REGION"));
        Assert.Equal(
            "ConfigurationError",
            result.Properties["Environment.Error.ErrorCode"]);
    }

    [Fact]
    public void ExcludesNestedEnvironmentVariablesRegardlessOfService()
    {
        var result = Parse("""
        {
          "Name": "example",
          "Configuration": {
            "Environment": {
              "Variables": {
                "MODE": "a-confidential-value"
              }
            }
          },
          "EnvironmentVariables": {
            "DEBUG": "a-confidential-value"
          },
          "AvailabilityZone": "us-east-1a"
        }
        """);

        Assert.Equal(
            "us-east-1a", result.Properties["AvailabilityZone"]);

        Assert.DoesNotContain(
            result.Properties.Keys,
            key => key.Contains(
                "Variables", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ExcludesKnownCredentialValueFormats()
    {
        var result = Parse("""
        {
          "Name": "example",
          "Description": "-----BEGIN PRIVATE KEY----- example -----END PRIVATE KEY-----",
          "ExternalReference": "https://user:password@example.org/",
          "AccessIdentifier": "AKIAABCDEFGHIJKLMNOP",
          "State": "available"
        }
        """);

        Assert.Equal("available", result.Properties["State"]);
        Assert.False(result.Properties.ContainsKey("Description"));
        Assert.False(result.Properties.ContainsKey("ExternalReference"));
        Assert.False(result.Properties.ContainsKey("AccessIdentifier"));
    }

    [Fact]
    public void AppliesValueProtectionToOrdinaryNamedTags()
    {
        var result = Parse("""
        {
          "Tags": [
            {"Key":"Lab","Value":"Networking"},
            {"Key":"Reference","Value":"-----BEGIN PRIVATE KEY----- example"},
            {"Key":"ApiKey","Value":"excluded"},
            {"Key":"Owner","Value":"Platform"}
          ]
        }
        """);

        Assert.Equal("Networking", result.Tags["Lab"]);
        Assert.Equal("Platform", result.Tags["Owner"]);
        Assert.False(result.Tags.ContainsKey("Reference"));
        Assert.False(result.Tags.ContainsKey("ApiKey"));
    }

    [Fact]
    public void PreservesUsefulNestedNetworkingAndDatabaseMetadata()
    {
        var result = Parse("""
        {
          "VpcId": "vpc-001",
          "NetworkInterfaces": [
            {"NetworkInterfaceId": "eni-001"}
          ],
          "Endpoint": {
            "Address": "db.example.internal",
            "Port": 5432
          },
          "MultiAZ": false,
          "Tags": [
            {"Key":"Lab","Value":"Database"}
          ]
        }
        """);

        Assert.Equal(
            "eni-001",
            result.Properties[
                "NetworkInterfaces[0].NetworkInterfaceId"]);
        Assert.Equal(
            "5432", result.Properties["Endpoint.Port"]);
        Assert.Equal(
            "false", result.Properties["MultiAZ"]);
        Assert.Equal("Database", result.Tags["Lab"]);
    }
}
