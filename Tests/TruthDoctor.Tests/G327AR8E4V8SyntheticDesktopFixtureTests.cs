using System;
using System.Linq;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using TruthDoctor.State;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327AR8E4V8SyntheticDesktopFixtureTests
{
    [Fact]
    public async Task OfflineFixturePublishesThreeGroupsAndTwoDirectedReferences()
    {
        var session =
            new StandaloneSyntheticAwsSecurityGroupSession();

        var workbench = new WorkbenchState();

        var platform = await session.DiscoverAsync(workbench);

        Assert.Equal("aws", platform.Context.ProviderId);
        Assert.Equal(3, platform.Resources.Count);

        Assert.All(
            platform.Resources,
            resource =>
            {
                Assert.Equal("aws", resource.ProviderId);
                Assert.Equal("security-group", resource.ResourceType);
            });

        Assert.Equal(
            new[] { "sg-owner", "sg-ingress", "sg-egress" }
                .OrderBy(id => id, StringComparer.Ordinal),
            platform.Resources
                .Select(resource => resource.NativeId)
                .OrderBy(id => id, StringComparer.Ordinal));

        Assert.Equal(2, platform.Relationships.Count);

        Assert.Contains(
            platform.Relationships,
            relationship =>
                relationship.Type == "references"
                && relationship.SourceResourceId == "sg-owner"
                && relationship.TargetResourceId == "sg-ingress");

        Assert.Contains(
            platform.Relationships,
            relationship =>
                relationship.Type == "references"
                && relationship.SourceResourceId == "sg-owner"
                && relationship.TargetResourceId == "sg-egress");

        Assert.All(
            platform.Relationships,
            relationship =>
                Assert.Equal("references", relationship.Type));
    }
}
