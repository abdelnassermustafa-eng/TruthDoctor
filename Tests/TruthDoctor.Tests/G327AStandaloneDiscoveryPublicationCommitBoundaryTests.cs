using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Graph;
using TruthDoctor.Models.Platform;
using TruthDoctor.Services.Standalone;
using TruthDoctor.State;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327AStandaloneDiscoveryPublicationCommitBoundaryTests
{
    [Fact]
    public void PrepareCommitAndNotifyAreSeparateOperations()
    {
        const BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic;

        Type workbenchType = typeof(WorkbenchState);

        var prepare = workbenchType.GetMethods(flags)
            .SingleOrDefault(method =>
                method.Name == "PrepareDiscoverySnapshot" &&
                method.GetParameters().Length == 2);

        Assert.True(
            prepare is not null,
            "RED_EXPECTED_PREPARED_DISCOVERY_PUBLICATION_BOUNDARY");

        var commit = workbenchType.GetMethods(flags)
            .SingleOrDefault(method =>
                method.Name == "CommitPreparedDiscoverySnapshot" &&
                method.GetParameters().Length == 1);

        var notify = workbenchType.GetMethods(flags)
            .SingleOrDefault(method =>
                method.Name == "NotifyCommittedDiscoverySnapshot" &&
                method.GetParameters().Length == 0);

        Assert.NotNull(commit);
        Assert.NotNull(notify);

        var records = new[]
        {
            new ProviderResourceRecord(
                "mock-provider",
                "demo-resource",
                "virtual-machine",
                "Demo Resource")
        };

        PlatformState platform =
            StandaloneDiscoveryProjector.Project(
                records, "account-001", "location-001", "compute");

        InfrastructureGraph graph =
            new InfrastructureGraphBuilder().Build(platform);

        var workbench = new WorkbenchState();
        int notifications = 0;

        workbench.Changed += (_, _) => notifications++;

        object? prepared = prepare!.Invoke(
            workbench, new object[] { platform, graph });

        Assert.NotNull(prepared);
        Assert.Null(workbench.PlatformState);
        Assert.Null(workbench.InfrastructureGraph);
        Assert.Equal(0, notifications);

        commit!.Invoke(workbench, new[] { prepared });

        Assert.Same(platform, workbench.PlatformState);
        Assert.Same(graph, workbench.InfrastructureGraph);
        Assert.NotNull(workbench.InfrastructureGraphIndex);
        Assert.Equal(0, notifications);

        notify!.Invoke(workbench, null);

        Assert.Equal(1, notifications);
    }
}
