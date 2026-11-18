using System.Collections.Generic;
using System.Linq;
using TruthDoctor.Graph;
using TruthDoctor.Models.Platform;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327ARelationshipV110AssociationHandoffTests
{
    private static InfrastructureResource Resource(
        string id,
        string nativeId,
        string type,
        string provider = "aws",
        string account = "account-a",
        string region = "us-east-1",
        params (string Key, string Value)[] properties)
    {
        var values = new Dictionary<string, string>();

        foreach (var (key, value) in properties)
        {
            values[key] = value;
        }

        return new InfrastructureResource
        {
            ResourceId = id,
            NativeId = nativeId,
            ResourceType = type,
            ProviderId = provider,
            AccountId = account,
            Location = region,
            DisplayName = id,
            Properties = values
        };
    }

    private static InfrastructureResource Subnet(
        string id = "subnet-resource",
        string nativeId = "subnet-1",
        string provider = "aws",
        string account = "account-a",
        string region = "us-east-1",
        string vpcId = "vpc-1")
    {
        return Resource(
            id,
            nativeId,
            "subnet",
            provider,
            account,
            region,
            ("VpcId", vpcId));
    }

    private static InfrastructureResource RouteTable(
        string id = "route-table-resource",
        string nativeId = "rtb-1",
        string provider = "aws",
        string account = "account-a",
        string region = "us-east-1",
        string vpcId = "vpc-1",
        params (string Key, string Value)[] extra)
    {
        var properties = new List<(string Key, string Value)>
        {
            ("VpcId", vpcId)
        };

        properties.AddRange(extra);

        return Resource(
            id,
            nativeId,
            "route-table",
            provider,
            account,
            region,
            properties: properties.ToArray());
    }

    [Fact]
    public void ExplicitAwsAssociationFlowsIntoUniversalView()
    {
        InfrastructureResource[] resources =
        [
            Subnet(),
            RouteTable(
                extra:
                [
                    ("Associations[0].SubnetId", "subnet-1")
                ])
        ];

        var view = Assert.Single(
            AwsStandaloneAssociationEvidenceHandoff
                .Build(resources));

        Assert.Equal(
            "subnet-resource",
            view.SourceResourceId);

        Assert.Equal(
            "route-table-resource",
            view.TargetResourceId);

        Assert.Equal("aws", view.ProviderId);
        Assert.Equal("account-a", view.AccountId);
        Assert.Equal("us-east-1", view.Location);

        Assert.Equal(
            "subnet-route-table-association",
            view.AssociationType);

        Assert.Equal(
            "Associations[0].SubnetId",
            view.EvidenceKey);
    }

    [Fact]
    public void RepeatedAssociationEvidenceIsDeduplicated()
    {
        InfrastructureResource[] resources =
        [
            Subnet(),
            RouteTable(
                extra:
                [
                    ("Associations[1].SubnetId", "subnet-1"),
                    ("Associations[0].SubnetId", "subnet-1")
                ])
        ];

        var first =
            AwsStandaloneAssociationEvidenceHandoff
                .Build(resources);

        var second =
            AwsStandaloneAssociationEvidenceHandoff
                .Build(resources.Reverse().ToArray());

        var view = Assert.Single(first);

        Assert.Equal(
            "Associations[0].SubnetId",
            view.EvidenceKey);

        Assert.Equal(first, second);
    }

    [Fact]
    public void InvalidScopeAndMissingAssociationsProduceNoViews()
    {
        var validTable = RouteTable(
            extra:
            [
                ("Associations[0].SubnetId", "subnet-1")
            ]);

        InfrastructureResource[][] invalidCases =
        [
            [
                validTable
            ],
            [
                Subnet(account: "account-b"),
                validTable
            ],
            [
                Subnet(region: "us-west-2"),
                validTable
            ],
            [
                Subnet(vpcId: "vpc-other"),
                validTable
            ],
            [
                Subnet(provider: "azure"),
                validTable
            ],
            [
                Subnet(),
                Subnet(id: "duplicate-subnet-resource"),
                validTable
            ],
            [
                Subnet(),
                RouteTable(
                    extra:
                    [
                        ("NotAssociations[0].SubnetId", "subnet-1")
                    ])
            ],
            [
                Subnet(),
                RouteTable(
                    extra:
                    [
                        ("Associations[0].SubnetId", "missing-subnet")
                    ])
            ],
            [
                Subnet(),
                RouteTable(
                    provider: "azure",
                    extra:
                    [
                        ("Associations[0].SubnetId", "subnet-1")
                    ])
            ]
        ];

        foreach (var resources in invalidCases)
        {
            Assert.Empty(
                AwsStandaloneAssociationEvidenceHandoff
                    .Build(resources));
        }
    }

    [Fact]
    public void HandoffPreservesOperationalGraphRelationships()
    {
        InfrastructureResource[] resources =
        [
            Subnet(),
            RouteTable(
                extra:
                [
                    ("Associations[0].SubnetId", "subnet-1")
                ])
        ];

        var operationalBefore =
            AwsStandaloneRelationshipBuilder.Build(resources);

        var views =
            AwsStandaloneAssociationEvidenceHandoff
                .Build(resources);

        var operationalAfter =
            AwsStandaloneRelationshipBuilder.Build(resources);

        Assert.Single(views);

        var before = Assert.Single(operationalBefore);
        var after = Assert.Single(operationalAfter);

        Assert.Equal("uses", before.Type);
        Assert.Equal("uses", after.Type);

        Assert.Equal(
            before.SourceResourceId,
            after.SourceResourceId);

        Assert.Equal(
            before.TargetResourceId,
            after.TargetResourceId);

        var graph = new InfrastructureGraph();

        graph.AddEdge(new GraphEdge
        {
            SourceId = before.SourceResourceId,
            TargetId = before.TargetResourceId,
            Relationship = before.Type
        });

        var index = new InfrastructureGraphIndex(graph);

        var analyzer =
            new InfrastructureRelationshipAnalyzer(index);

        Assert.Single(
            analyzer.GetDependencyRelationships(
                before.SourceResourceId));

        Assert.Empty(
            analyzer.GetConnectivityRelationships(
                before.SourceResourceId));

        Assert.Empty(
            analyzer.GetSecurityRelationships(
                before.SourceResourceId));

        var edge = Assert.Single(graph.Edges);

        Assert.Equal("uses", edge.Relationship);

        Assert.DoesNotContain(
            graph.Edges,
            item => item.Relationship == "associated-with");
    }
}
