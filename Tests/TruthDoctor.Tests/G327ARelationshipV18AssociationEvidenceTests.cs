using System.Collections.Generic;
using TruthDoctor.Models.Platform;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327ARelationshipV18AssociationEvidenceTests
{
    private static InfrastructureResource Resource(
        string resourceId,
        string nativeId,
        string type,
        string account = "account-a",
        string region = "us-east-1",
        string provider = "aws",
        params (string Key, string Value)[] properties)
    {
        var values = new Dictionary<string, string>();

        foreach (var (key, value) in properties)
        {
            values[key] = value;
        }

        return new InfrastructureResource
        {
            ResourceId = resourceId,
            NativeId = nativeId,
            ResourceType = type,
            ProviderId = provider,
            AccountId = account,
            Location = region,
            DisplayName = resourceId,
            Properties = values
        };
    }

    private static InfrastructureResource Subnet(
        string resourceId = "subnet-resource",
        string nativeId = "subnet-1",
        string account = "account-a",
        string region = "us-east-1",
        string provider = "aws",
        string vpcId = "vpc-1")
        => Resource(
            resourceId,
            nativeId,
            "subnet",
            account,
            region,
            provider,
            ("VpcId", vpcId));

    private static InfrastructureResource RouteTable(
        params (string Key, string Value)[] extra)
    {
        var properties = new List<(string Key, string Value)>
        {
            ("VpcId", "vpc-1")
        };

        properties.AddRange(extra);

        return Resource(
            "route-table-resource",
            "rtb-1",
            "route-table",
            properties: properties.ToArray());
    }

    [Fact]
    public void ExplicitScopedAssociationProducesEvidence()
    {
        InfrastructureResource[] resources =
        [
            Subnet(),
            RouteTable(
                ("Associations[0].SubnetId", "subnet-1"))
        ];

        var record = Assert.Single(
            AwsStandaloneAssociationEvidenceExtractor
                .Extract(resources));

        Assert.Equal(
            "subnet-resource",
            record.SourceResourceId);

        Assert.Equal(
            "route-table-resource",
            record.TargetResourceId);

        Assert.Equal(
            "subnet-route-table-association",
            record.AssociationType);

        Assert.Equal(
            "Associations[0].SubnetId",
            record.PropertyName);
    }

    [Fact]
    public void RepeatedAssociationEntriesProduceOneEvidenceRecord()
    {
        InfrastructureResource[] resources =
        [
            Subnet(),
            RouteTable(
                ("Associations[0].SubnetId", "subnet-1"),
                ("Associations[1].SubnetId", "subnet-1"))
        ];

        var record = Assert.Single(
            AwsStandaloneAssociationEvidenceExtractor
                .Extract(resources));

        Assert.Equal(
            "Associations[0].SubnetId",
            record.PropertyName);
    }

    [Fact]
    public void MissingAmbiguousOrCrossScopeTargetsAreRejected()
    {
        var table = RouteTable(
            ("Associations[0].SubnetId", "subnet-1"));

        InfrastructureResource[][] invalidCases =
        [
            [
                table
            ],
            [
                Subnet(vpcId: "vpc-other"),
                table
            ],
            [
                Subnet(account: "account-b"),
                table
            ],
            [
                Subnet(region: "us-west-2"),
                table
            ],
            [
                Subnet(provider: "other-provider"),
                table
            ],
            [
                Subnet(),
                Subnet(resourceId: "second-subnet"),
                table
            ],
            [
                Subnet(),
                RouteTable(
                    ("NotAssociations[0].SubnetId",
                     "subnet-1"))
            ]
        ];

        foreach (var resources in invalidCases)
        {
            var evidence =
                AwsStandaloneAssociationEvidenceExtractor
                    .Extract(resources);

            Assert.Empty(evidence);
        }

        // An unrelated property must be ignored even when
        // it appears alongside one valid association.
        var withUnrelatedProperty =
            AwsStandaloneAssociationEvidenceExtractor
                .Extract(
                [
                    Subnet(),
                    RouteTable(
                        ("NotAssociations[0].SubnetId",
                         "subnet-1"),
                        ("Associations[0].SubnetId",
                         "subnet-1"))
                ]);

        var valid = Assert.Single(withUnrelatedProperty);

        Assert.Equal(
            "Associations[0].SubnetId",
            valid.PropertyName);

        var missingSubnetVpc = Resource(
            "subnet-no-vpc",
            "subnet-1",
            "subnet");

        Assert.Empty(
            AwsStandaloneAssociationEvidenceExtractor
                .Extract([missingSubnetVpc, table]));

        var missingTableVpc = Resource(
            "rtb-no-vpc",
            "rtb-1",
            "route-table",
            properties:
            [
                (
                    "Associations[0].SubnetId",
                    "subnet-1"
                )
            ]);

        Assert.Empty(
            AwsStandaloneAssociationEvidenceExtractor
                .Extract([Subnet(), missingTableVpc]));
    }

    [Fact]
    public void EvidenceExtractionDoesNotModifyOperationalEdges()
    {
        InfrastructureResource[] resources =
        [
            Subnet(),
            RouteTable(
                ("Associations[0].SubnetId", "subnet-1"))
        ];

        var before =
            AwsStandaloneRelationshipBuilder.Build(resources);

        var evidence =
            AwsStandaloneAssociationEvidenceExtractor
                .Extract(resources);

        var after =
            AwsStandaloneRelationshipBuilder.Build(resources);

        Assert.Single(evidence);

        var original = Assert.Single(before);
        var unchanged = Assert.Single(after);

        Assert.Equal("uses", original.Type);
        Assert.Equal("uses", unchanged.Type);

        Assert.Equal(
            original.SourceResourceId,
            unchanged.SourceResourceId);

        Assert.Equal(
            original.TargetResourceId,
            unchanged.TargetResourceId);

        Assert.DoesNotContain(
            after,
            edge => edge.Type == "associated-with");

        Assert.DoesNotContain(
            after,
            edge => edge.Type == "routes-through"
                 || edge.Type == "connected-to"
                 || edge.Type == "serves"
                 || edge.Type == "targets");
    }
}
