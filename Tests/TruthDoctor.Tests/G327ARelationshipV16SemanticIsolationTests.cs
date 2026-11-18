using System.Linq;
using TruthDoctor.Graph;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327ARelationshipV16SemanticIsolationTests
{
    [Fact]
    public void AssociationSemanticsRemainIsolatedFromOperationalClaims()
    {
        var registry = new RelationshipSemanticsRegistry();

        var association = registry.Resolve("associated-with");

        Assert.Equal(
            RelationshipKind.AssociatedWith,
            association.Kind);

        Assert.False(association.IsDependency);
        Assert.False(association.IsContainment);
        Assert.False(association.IsConnectivity);
        Assert.False(association.IsSecurity);
        Assert.False(association.IsTrafficFlow);

        var dependency = registry.Resolve("uses");

        Assert.Equal(
            RelationshipKind.Uses,
            dependency.Kind);

        Assert.True(dependency.IsDependency);
        Assert.False(dependency.IsConnectivity);
        Assert.False(dependency.IsTrafficFlow);

        var reference = registry.Resolve("references");

        Assert.Equal(
            RelationshipKind.Uses,
            reference.Kind);

        Assert.True(reference.IsDependency);
        Assert.False(reference.IsConnectivity);
        Assert.False(reference.IsTrafficFlow);
        Assert.False(reference.IsSecurity);

        var security = registry.Resolve("secured-by");

        Assert.Equal(
            RelationshipKind.SecuredBy,
            security.Kind);

        Assert.True(security.IsDependency);
        Assert.True(security.IsSecurity);
        Assert.False(security.IsConnectivity);
        Assert.False(security.IsTrafficFlow);
    }

    [Fact]
    public void AnalyzerSeparatesAssociationFromDependencyAndSecurity()
    {
        var graph = new InfrastructureGraph();

        graph.AddEdge(new GraphEdge
        {
            SourceId = "subnet-1",
            TargetId = "rtb-1",
            Relationship = "uses"
        });

        graph.AddEdge(new GraphEdge
        {
            SourceId = "subnet-1",
            TargetId = "rtb-1",
            Relationship = "associated-with"
        });

        graph.AddEdge(new GraphEdge
        {
            SourceId = "subnet-1",
            TargetId = "acl-1",
            Relationship = "secured-by"
        });

        graph.AddEdge(new GraphEdge
        {
            SourceId = "subnet-1",
            TargetId = "sg-1",
            Relationship = "references"
        });

        var index = new InfrastructureGraphIndex(graph);

        var analyzer =
            new InfrastructureRelationshipAnalyzer(index);

        var outgoing =
            index.GetOutgoingEdges("subnet-1");

        Assert.Equal(4, outgoing.Count);

        var associations = analyzer.GetOutgoing(
            "subnet-1",
            RelationshipKind.AssociatedWith);

        var association = Assert.Single(associations);

        Assert.Equal("rtb-1", association.TargetId);
        Assert.Equal(
            "associated-with",
            association.Relationship);

        var dependencies =
            analyzer.GetDependencyRelationships("subnet-1");

        Assert.Equal(3, dependencies.Count);

        Assert.Contains(
            dependencies,
            edge => edge.Relationship == "uses");

        Assert.Contains(
            dependencies,
            edge => edge.Relationship == "secured-by");

        Assert.Contains(
            dependencies,
            edge => edge.Relationship == "references");

        Assert.DoesNotContain(
            dependencies,
            edge => edge.Relationship == "associated-with");

        var security =
            analyzer.GetSecurityRelationships("subnet-1");

        var securityEdge = Assert.Single(security);

        Assert.Equal(
            "secured-by",
            securityEdge.Relationship);

        var connectivity =
            analyzer.GetConnectivityRelationships("subnet-1");

        Assert.Empty(connectivity);

        Assert.Equal(
            RelationshipKind.AssociatedWith,
            analyzer.Describe(association).Kind);

        Assert.False(
            analyzer.Describe(association).IsDependency);

        Assert.False(
            analyzer.Describe(association).IsConnectivity);

        Assert.False(
            analyzer.Describe(association).IsTrafficFlow);
    }
}
