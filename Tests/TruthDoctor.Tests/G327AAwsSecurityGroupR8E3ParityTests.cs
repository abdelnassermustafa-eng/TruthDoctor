using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using TruthDoctor.Models.Platform;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327AAwsSecurityGroupR8E3ParityTests
{
    private static AwsNormalizedMetadata Normalize(string json)
    {
        using var document = JsonDocument.Parse(json);
        return AwsUniversalMetadataNormalizer.Normalize(
            document.RootElement);
    }

    private static List<InfrastructureRelationship> Edges(
        params (string Id, string Json)[] resources)
    {
        var records = resources
            .Select(item => new ProviderResourceRecord(
                "aws",
                item.Id,
                "security-group",
                item.Id,
                Properties: Normalize(item.Json).Properties))
            .ToArray();

        return StandaloneDiscoveryProjector.Project(
            records,
            "123456789012",
            "us-east-1",
            "infrastructure").Relationships;
    }

    [Fact]
    public void IngressAwsJsonProducesTypedReference()
    {
        const string owner = """
        {
          "GroupId": "sg-owner",
          "IpPermissions": [
            {
              "IpProtocol": "tcp",
              "FromPort": 443,
              "UserIdGroupPairs": [
                {"GroupId": "sg-target"}
              ]
            }
          ]
        }
        """;

        var metadata = Normalize(owner);

        Assert.Equal(
            "sg-target",
            metadata.Properties[
                "IpPermissions[0].UserIdGroupPairs[0].GroupId"]);

        var edge = Assert.Single(Edges(
            ("sg-owner", owner),
            ("sg-target", """{"GroupId":"sg-target"}""")));

        Assert.Equal("references", edge.Type);
        Assert.Equal(
            "IpPermissions[0].UserIdGroupPairs[0].GroupId",
            edge.Description);
    }

    [Fact]
    public void EgressAwsJsonProducesTypedReference()
    {
        const string owner = """
        {
          "GroupId": "sg-owner",
          "IpPermissionsEgress": [
            {
              "IpProtocol": "tcp",
              "ToPort": 443,
              "UserIdGroupPairs": [
                {"GroupId": "sg-target"}
              ]
            }
          ]
        }
        """;

        var edge = Assert.Single(Edges(
            ("sg-owner", owner),
            ("sg-target", """{"GroupId":"sg-target"}""")));

        Assert.Equal("references", edge.Type);
        Assert.Equal(
            "IpPermissionsEgress[0].UserIdGroupPairs[0].GroupId",
            edge.Description);
    }

    [Fact]
    public void CombinedAwsRulesDeduplicateIdenticalTarget()
    {
        const string owner = """
        {
          "GroupId": "sg-owner",
          "IpPermissions": [
            {
              "UserIdGroupPairs": [
                {"GroupId": "sg-target"},
                {"GroupId": "sg-target"}
              ]
            }
          ],
          "IpPermissionsEgress": [
            {
              "UserIdGroupPairs": [
                {"GroupId": "sg-target"}
              ]
            }
          ]
        }
        """;

        var edges = Edges(
            ("sg-owner", owner),
            ("sg-target", """{"GroupId":"sg-target"}"""));

        Assert.Single(edges);
        Assert.Equal("references", edges[0].Type);
    }

    [Fact]
    public void MissingGroupAndCidrOnlyRulesProduceNoReferences()
    {
        const string owner = """
        {
          "GroupId": "sg-owner",
          "IpPermissions": [
            {
              "IpRanges": [{"CidrIp":"10.0.0.0/24"}],
              "UserIdGroupPairs": [
                {"GroupId":"sg-missing"}
              ]
            }
          ]
        }
        """;

        var edges = Edges(("sg-owner", owner));

        Assert.Empty(edges);
    }

    [Fact]
    public void SensitiveFieldsRemainFilteredDuringFlattening()
    {
        const string owner = """
        {
          "GroupId": "sg-owner",
          "Secret": "example-sensitive-value",
          "IpPermissions": [
            {
              "UserIdGroupPairs": [
                {"GroupId":"sg-target"}
              ]
            }
          ]
        }
        """;

        var metadata = Normalize(owner);

        Assert.False(
            metadata.Properties.ContainsKey("Secret"));

        Assert.Equal(
            "sg-target",
            metadata.Properties[
                "IpPermissions[0].UserIdGroupPairs[0].GroupId"]);

        var edge = Assert.Single(Edges(
            ("sg-owner", owner),
            ("sg-target", """{"GroupId":"sg-target"}""")));

        Assert.Equal("references", edge.Type);
    }
}
