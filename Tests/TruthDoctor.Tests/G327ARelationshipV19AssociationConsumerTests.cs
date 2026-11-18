using System.Collections.Generic;
using System.Linq;
using TruthDoctor.Graph;
using TruthDoctor.Models.Platform;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327ARelationshipV19AssociationConsumerTests
{
    private static InfrastructureResource Resource(
        string id,
        string provider = "aws",
        string account = "account-a",
        string location = "us-east-1")
    {
        return new InfrastructureResource
        {
            ResourceId = id,
            NativeId = id,
            ResourceType = "test-resource",
            ProviderId = provider,
            AccountId = account,
            Location = location,
            DisplayName = id,
            Properties = new Dictionary<string, string>()
        };
    }

    private static InfrastructureAssociationEvidence Evidence(
        string source,
        string target,
        string type = "subnet-route-table-association",
        string key = "Associations[0].SubnetId")
    {
        return new InfrastructureAssociationEvidence(
            source, target, type, key);
    }

    [Fact]
    public void VerifiedAwsEvidenceCanBeConsumedWithoutAwsSpecificLogic()
    {
        var resources = new[]
        {
            Resource("subnet-resource"),
            Resource("route-table-resource")
        };

        var awsEvidence = new AwsStandaloneAssociationEvidence(
            "subnet-resource",
            "route-table-resource",
            "subnet-route-table-association",
            "Associations[0].SubnetId");

        var neutralEvidence = new[]
        {
            new InfrastructureAssociationEvidence(
                awsEvidence.SourceResourceId,
                awsEvidence.TargetResourceId,
                awsEvidence.AssociationType,
                awsEvidence.PropertyName)
        };

        var result = Assert.Single(
            InfrastructureAssociationEvidenceConsumer.Resolve(
                resources, neutralEvidence));

        Assert.Equal("subnet-resource", result.SourceResourceId);
        Assert.Equal("route-table-resource", result.TargetResourceId);
        Assert.Equal("aws", result.ProviderId);
        Assert.Equal("account-a", result.AccountId);
        Assert.Equal("us-east-1", result.Location);
        Assert.Equal(
            "subnet-route-table-association",
            result.AssociationType);
    }

    [Fact]
    public void AssociationViewsAreDeduplicatedAndDeterministicallyOrdered()
    {
        var resources = new[]
        {
            Resource("a"),
            Resource("b"),
            Resource("c")
        };

        var evidence = new[]
        {
            Evidence("b", "c", key: "z"),
            Evidence("a", "b", key: "z"),
            Evidence("a", "b", key: "a"),
            Evidence("b", "c", key: "a")
        };

        var results =
            InfrastructureAssociationEvidenceConsumer.Resolve(
                resources, evidence);

        Assert.Equal(2, results.Count);

        Assert.Equal("a", results[0].SourceResourceId);
        Assert.Equal("b", results[0].TargetResourceId);
        Assert.Equal("a", results[0].EvidenceKey);

        Assert.Equal("b", results[1].SourceResourceId);
        Assert.Equal("c", results[1].TargetResourceId);
        Assert.Equal("a", results[1].EvidenceKey);

        var reversed =
            InfrastructureAssociationEvidenceConsumer.Resolve(
                resources,
                evidence.Reverse().ToArray());

        Assert.Equal(results, reversed);
    }

    [Fact]
    public void MissingAmbiguousAndCrossScopeAssociationsAreRejected()
    {
        var validEvidence = Evidence("source", "target");

        var invalidCases = new[]
        {
            new[] { Resource("source") },
            new[]
            {
                Resource("source"),
                Resource("target", provider: "azure")
            },
            new[]
            {
                Resource("source"),
                Resource("target", account: "account-b")
            },
            new[]
            {
                Resource("source"),
                Resource("target", location: "us-west-2")
            },
            new[]
            {
                Resource("source"),
                Resource("target"),
                Resource("target")
            }
        };

        foreach (var resources in invalidCases)
        {
            Assert.Empty(
                InfrastructureAssociationEvidenceConsumer.Resolve(
                    resources, new[] { validEvidence }));
        }

        var validResources = new[]
        {
            Resource("source"),
            Resource("target")
        };

        var invalidEvidence = new[]
        {
            Evidence("missing", "target"),
            Evidence("source", "missing"),
            Evidence("source", "source"),
            Evidence("source", "target", type: ""),
            Evidence("source", "target", key: "")
        };

        Assert.Empty(
            InfrastructureAssociationEvidenceConsumer.Resolve(
                validResources, invalidEvidence));
    }

    [Fact]
    public void AssociationConsumptionDoesNotAlterOperationalGraph()
    {
        var graph = new InfrastructureGraph();

        graph.AddEdge(new GraphEdge
        {
            SourceId = "source",
            TargetId = "target",
            Relationship = "uses"
        });

        var resources = new[]
        {
            Resource("source"),
            Resource("target")
        };

        var evidence = new[]
        {
            Evidence("source", "target")
        };

        var before = graph.Edges
            .Select(edge => edge.Relationship)
            .ToArray();

        var result =
            InfrastructureAssociationEvidenceConsumer.Resolve(
                resources, evidence);

        Assert.Single(result);

        var after = graph.Edges
            .Select(edge => edge.Relationship)
            .ToArray();

        Assert.Equal(before, after);

        var edge = Assert.Single(graph.Edges);

        Assert.Equal("uses", edge.Relationship);

        var analyzer = new InfrastructureRelationshipAnalyzer(
            new InfrastructureGraphIndex(graph));

        Assert.Single(
            analyzer.GetDependencyRelationships("source"));

        Assert.Empty(
            analyzer.GetConnectivityRelationships("source"));

        Assert.Empty(
            analyzer.GetSecurityRelationships("source"));

        Assert.DoesNotContain(
            graph.Edges,
            item => item.Relationship == "associated-with");
    }
}
