using System.Linq;
using TruthDoctor.Graph;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327ARelationshipV15MultiplicityContractTests
{
    [Fact]
    public void GraphPreservesDistinctRelationshipsBetweenSameEndpoints()
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

        Assert.Equal(2, graph.Edges.Count);

        Assert.Equal(
            new[] { "uses", "associated-with" },
            graph.Edges.Select(edge => edge.Relationship));

        Assert.All(graph.Edges, edge =>
        {
            Assert.Equal("subnet-1", edge.SourceId);
            Assert.Equal("rtb-1", edge.TargetId);
        });
    }

    [Fact]
    public void IndexPreservesParallelRelationshipTypesAndOrdering()
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

        var index = new InfrastructureGraphIndex(graph);

        var outgoing = index.GetOutgoingEdges("subnet-1");
        var incoming = index.GetIncomingEdges("rtb-1");

        Assert.Equal(2, outgoing.Count);
        Assert.Equal(2, incoming.Count);

        Assert.Equal(
            new[] { "uses", "associated-with" },
            outgoing.Select(edge => edge.Relationship));

        Assert.Equal(
            new[] { "uses", "associated-with" },
            incoming.Select(edge => edge.Relationship));
    }
}
