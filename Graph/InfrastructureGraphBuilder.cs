using System;
using TruthDoctor.Models.Platform;

namespace TruthDoctor.Graph;

public sealed class InfrastructureGraphBuilder
{
    public InfrastructureGraph Build(
        PlatformState platformState)
    {
        ArgumentNullException.ThrowIfNull(platformState);

        var graph = new InfrastructureGraph();

        foreach (var resource in platformState.Resources)
        {
            var nodeId = GetNodeId(resource);

            graph.AddNode(
                new GraphNode
                {
                    Id = nodeId,
                    ProviderId = resource.ProviderId,
                    DomainId = resource.DomainId,
                    ResourceType = resource.ResourceType,
                    DisplayName = resource.DisplayName,
                    Resource = resource
                });
        }

        AddDeclaredRelationships(
            platformState,
            graph);

        return graph;
    }

    private static void AddDeclaredRelationships(
        PlatformState platformState,
        InfrastructureGraph graph)
    {
        foreach (var relationship in
                 platformState.Relationships)
        {
            AddRelationship(
                graph,
                relationship);
        }

        foreach (var resource in
                 platformState.Resources)
        {
            foreach (var relationship in
                     resource.Relationships)
            {
                AddRelationship(
                    graph,
                    relationship);
            }
        }
    }

    private static void AddRelationship(
        InfrastructureGraph graph,
        InfrastructureRelationship relationship)
    {
        if (string.IsNullOrWhiteSpace(
                relationship.SourceResourceId) ||
            string.IsNullOrWhiteSpace(
                relationship.TargetResourceId))
        {
            return;
        }

        if (!graph.Nodes.ContainsKey(
                relationship.SourceResourceId) ||
            !graph.Nodes.ContainsKey(
                relationship.TargetResourceId))
        {
            return;
        }

        graph.AddEdge(
            new GraphEdge
            {
                SourceId =
                    relationship.SourceResourceId,

                TargetId =
                    relationship.TargetResourceId,

                Relationship =
                    string.IsNullOrWhiteSpace(
                        relationship.Type)
                        ? "related-to"
                        : relationship.Type
            });
    }

    private static string GetNodeId(
        InfrastructureResource resource)
    {
        if (!string.IsNullOrWhiteSpace(
                resource.ResourceId))
        {
            return resource.ResourceId;
        }

        if (!string.IsNullOrWhiteSpace(
                resource.NativeId))
        {
            return BuildFallbackId(
                resource,
                resource.NativeId);
        }

        return BuildFallbackId(
            resource,
            resource.DisplayName);
    }

    private static string BuildFallbackId(
        InfrastructureResource resource,
        string identifier)
    {
        return string.Join(
            ":",
            resource.ProviderId,
            resource.AccountId,
            resource.Location,
            resource.DomainId,
            resource.ResourceType,
            identifier);
    }
}
