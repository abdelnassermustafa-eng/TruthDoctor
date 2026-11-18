using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using TruthDoctor.Graph;
using TruthDoctor.Models.Platform;
using TruthDoctor.Services.Standalone;
using TruthDoctor.State;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327ARelationshipV114CoordinatedPublicationTests
{
    private static PlatformState Platform(string resourceId)
    {
        var resource = new InfrastructureResource
        {
            ProviderId = "aws",
            AccountId = "account-a",
            DomainId = "network",
            ResourceId = resourceId,
            NativeId = resourceId,
            ResourceType = "subnet",
            DisplayName = resourceId,
            State = "available",
            Location = "us-east-1",
            Properties = new Dictionary<string, string>(),
            Tags = new Dictionary<string, string>(),
            DiscoveredAt = DateTimeOffset.UtcNow
        };

        return new PlatformState
        {
            Context = new PlatformContext
            {
                ProviderId = "aws",
                AccountId = "account-a",
                DefaultLocation = "us-east-1",
                Locations = new List<string> { "us-east-1" }
            },
            Resources = new List<InfrastructureResource> { resource },
            Relationships = new List<InfrastructureRelationship>(),
            Domains = new List<InfrastructureDomain>(),
            DiscoveredAt = DateTimeOffset.UtcNow,
            TotalResourceCount = 1
        };
    }

    private static InfrastructureAssociationView Association(
        string source,
        string target)
        => new(
            source,
            target,
            "aws",
            "account-a",
            "us-east-1",
            "subnet-route-table-association",
            "Associations[0].SubnetId");

    private static object Prepare(
        WorkbenchState workbench,
        PlatformState platform,
        InfrastructureGraph graph,
        IReadOnlyList<InfrastructureAssociationView> associations)
    {
        const BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.NonPublic;

        var method = typeof(WorkbenchState)
            .GetMethods(flags)
            .Single(item =>
                item.Name == "PrepareDiscoverySnapshot" &&
                item.GetParameters().Length == 3);

        try
        {
            return method.Invoke(
                workbench,
                new object[] { platform, graph, associations })!;
        }
        catch (TargetInvocationException exception)
            when (exception.InnerException is not null)
        {
            // Reflection wraps the production exception.
            // Rethrow the original so tests observe its real type.
            ExceptionDispatchInfo.Capture(
                exception.InnerException).Throw();

            throw;
        }
    }

    private static void Commit(
        WorkbenchState workbench,
        object prepared)
    {
        const BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.NonPublic;

        var method = typeof(WorkbenchState)
            .GetMethods(flags)
            .Single(item =>
                item.Name == "CommitPreparedDiscoverySnapshot");

        method.Invoke(workbench, new[] { prepared });
    }

    private static void Notify(WorkbenchState workbench)
    {
        const BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.NonPublic;

        var method = typeof(WorkbenchState)
            .GetMethods(flags)
            .Single(item =>
                item.Name == "NotifyCommittedDiscoverySnapshot");

        method.Invoke(workbench, null);
    }

    [Fact]
    public void PreparationDoesNotPublishAndCommitCoordinatesViews()
    {
        var workbench = new WorkbenchState();
        var platform = Platform("subnet-one");

        var graph =
            new InfrastructureGraphBuilder().Build(platform);

        var input = new List<InfrastructureAssociationView>
        {
            Association("subnet-one", "route-table-one")
        };

        int notifications = 0;
        workbench.Changed += (_, _) => notifications++;

        object prepared = Prepare(
            workbench, platform, graph, input);

        // The prepared collection is detached from the caller's list.
        input.Clear();

        Assert.Null(workbench.PlatformState);
        Assert.Empty(workbench.AssociationViews);
        Assert.Equal(0, notifications);

        Commit(workbench, prepared);

        Assert.Same(platform, workbench.PlatformState);
        Assert.Same(graph, workbench.InfrastructureGraph);

        var view = Assert.Single(workbench.AssociationViews);

        Assert.Equal(
            "subnet-one",
            view.SourceResourceId);

        Assert.Equal(
            "route-table-one",
            view.TargetResourceId);

        Assert.Equal(0, notifications);

        Notify(workbench);

        Assert.Equal(1, notifications);
    }

    [Fact]
    public void RefreshReplacesRatherThanAccumulatesAssociations()
    {
        var workbench = new WorkbenchState();

        var first = Platform("subnet-first");
        var firstGraph =
            new InfrastructureGraphBuilder().Build(first);

        Commit(workbench, Prepare(
            workbench,
            first,
            firstGraph,
            new[]
            {
                Association("subnet-first", "route-table-first")
            }));

        Assert.Single(workbench.AssociationViews);

        var second = Platform("subnet-second");
        var secondGraph =
            new InfrastructureGraphBuilder().Build(second);

        Commit(workbench, Prepare(
            workbench,
            second,
            secondGraph,
            Array.Empty<InfrastructureAssociationView>()));

        Assert.Same(second, workbench.PlatformState);
        Assert.Same(secondGraph, workbench.InfrastructureGraph);

        Assert.Empty(workbench.AssociationViews);

        Assert.Single(workbench.VisibleResources);

        Assert.Equal(
            "subnet-second",
            workbench.VisibleResources[0].ResourceId);
    }

    [Fact]
    public void LegacySettersInvalidatePreviouslyPublishedEvidence()
    {
        var workbench = new WorkbenchState();

        var first = Platform("subnet-first");
        var firstGraph =
            new InfrastructureGraphBuilder().Build(first);

        Commit(workbench, Prepare(
            workbench,
            first,
            firstGraph,
            new[]
            {
                Association("subnet-first", "route-table-first")
            }));

        Assert.Single(workbench.AssociationViews);

        var second = Platform("subnet-second");

        workbench.SetPlatformState(second);

        Assert.Empty(workbench.AssociationViews);

        Commit(workbench, Prepare(
            workbench,
            first,
            firstGraph,
            new[]
            {
                Association("subnet-first", "route-table-first")
            }));

        Assert.Single(workbench.AssociationViews);

        var secondGraph =
            new InfrastructureGraphBuilder().Build(second);

        workbench.SetInfrastructureGraph(secondGraph);

        Assert.Empty(workbench.AssociationViews);
    }

    [Fact]
    public void FailedPreparationPreservesPreviouslyCommittedEvidence()
    {
        var workbench = new WorkbenchState();
        var original = Platform("subnet-original");

        var originalGraph =
            new InfrastructureGraphBuilder().Build(original);

        Commit(workbench, Prepare(
            workbench,
            original,
            originalGraph,
            new[]
            {
                Association(
                    "subnet-original",
                    "route-table-original")
            }));

        var priorPlatform = workbench.PlatformState;
        var priorGraph = workbench.InfrastructureGraph;
        var priorAssociations = workbench.AssociationViews;

        var replacement = Platform("subnet-replacement");
        var replacementGraph =
            new InfrastructureGraphBuilder().Build(replacement);

        // The helper unwraps TargetInvocationException,
        // so this assertion observes the production exception.
        var exception = Assert.Throws<ArgumentNullException>(() =>
            Prepare(
                workbench,
                replacement,
                replacementGraph,
                null!));

        Assert.Equal(
            "associations",
            exception.ParamName);

        Assert.Same(
            priorPlatform,
            workbench.PlatformState);

        Assert.Same(
            priorGraph,
            workbench.InfrastructureGraph);

        Assert.Same(
            priorAssociations,
            workbench.AssociationViews);

        var priorView =
            Assert.Single(workbench.AssociationViews);

        Assert.Equal(
            "subnet-original",
            priorView.SourceResourceId);
    }
}
