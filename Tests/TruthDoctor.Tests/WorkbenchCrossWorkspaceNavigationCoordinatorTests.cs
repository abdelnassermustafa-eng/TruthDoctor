using TruthDoctor.Controllers.Workbench;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class WorkbenchCrossWorkspaceNavigationCoordinatorTests
{
    [Fact]
    public void FirstTopologyEntryBeginsSession()
    {
        var coordinator = new WorkbenchCrossWorkspaceNavigationCoordinator();

        Assert.True(
            coordinator.EnterWorkspace("topology", Resource("resource-1")));
    }

    [Theory]
    [InlineData("resources")]
    [InlineData("operations")]
    [InlineData("history")]
    [InlineData("reports")]
    [InlineData("settings")]
    [InlineData("dashboard")]
    public void ReturningWithUnchangedSelectionPreservesSession(
        string intermediateWorkspace)
    {
        var coordinator = new WorkbenchCrossWorkspaceNavigationCoordinator();
        var resource = Resource("resource-1");

        Assert.True(coordinator.EnterWorkspace("topology", resource));
        Assert.False(coordinator.EnterWorkspace(intermediateWorkspace, resource));
        Assert.False(coordinator.EnterWorkspace("topology", resource));
    }

    [Fact]
    public void ReenteringTopologyDoesNotResetSession()
    {
        var coordinator = new WorkbenchCrossWorkspaceNavigationCoordinator();
        var resource = Resource("resource-1");

        Assert.True(coordinator.EnterWorkspace("topology", resource));
        Assert.False(coordinator.EnterWorkspace("topology", resource));
    }

    [Fact]
    public void ChangedSelectionOutsideTopologyBeginsNewSession()
    {
        var coordinator = new WorkbenchCrossWorkspaceNavigationCoordinator();
        var first = Resource("resource-1");
        var second = Resource("resource-2");

        Assert.True(coordinator.EnterWorkspace("topology", first));
        Assert.False(coordinator.EnterWorkspace("resources", first));
        Assert.True(coordinator.EnterWorkspace("topology", second));
        Assert.False(coordinator.EnterWorkspace("topology", second));
    }

    [Fact]
    public void EquivalentResourceObjectPreservesSession()
    {
        var coordinator = new WorkbenchCrossWorkspaceNavigationCoordinator();

        Assert.True(coordinator.EnterWorkspace(
            "topology",
            Resource("resource-1")));

        Assert.False(coordinator.EnterWorkspace(
            "resources",
            Resource("resource-1")));

        Assert.False(coordinator.EnterWorkspace(
            "topology",
            Resource("RESOURCE-1")));
    }

    [Fact]
    public void ClearingSelectionOutsideTopologyBeginsNewSession()
    {
        var coordinator = new WorkbenchCrossWorkspaceNavigationCoordinator();

        Assert.True(coordinator.EnterWorkspace(
            "topology",
            Resource("resource-1")));

        Assert.False(coordinator.EnterWorkspace(
            "resources",
            null));

        Assert.True(coordinator.EnterWorkspace(
            "topology",
            null));
    }

    [Fact]
    public void FirstTopologyEntryWithoutSelectionBeginsSession()
    {
        var coordinator = new WorkbenchCrossWorkspaceNavigationCoordinator();

        Assert.True(coordinator.EnterWorkspace("topology", null));
        Assert.False(coordinator.EnterWorkspace("topology", null));
    }

    [Fact]
    public void TopologySelectionChangeDoesNotTriggerNextEntryReset()
    {
        var coordinator = new WorkbenchCrossWorkspaceNavigationCoordinator();
        var first = Resource("resource-1");
        var second = Resource("resource-2");

        Assert.True(coordinator.EnterWorkspace("topology", first));

        // Topology navigation updates the shared selected resource.
        Assert.False(coordinator.EnterWorkspace("topology", second));
        Assert.False(coordinator.EnterWorkspace("operations", second));
        Assert.False(coordinator.EnterWorkspace("topology", second));
    }

    [Fact]
    public void IdentityComparisonIncludesProviderAccountAndDomain()
    {
        var coordinator = new WorkbenchCrossWorkspaceNavigationCoordinator();

        var first = Resource("resource-1");

        var changedAccount = Resource("resource-1");
        changedAccount.AccountId = "another-account";

        Assert.True(coordinator.EnterWorkspace("topology", first));
        Assert.False(coordinator.EnterWorkspace("resources", first));
        Assert.True(coordinator.EnterWorkspace("topology", changedAccount));
    }

    [Fact]
    public void SynchronizeRestoredTopologySession_WhileActive_PreservesRestoredSessionOnReturn()
    {
        var coordinator = new WorkbenchCrossWorkspaceNavigationCoordinator();
        var original = Resource("original");
        var restored = Resource("restored");

        Assert.True(coordinator.EnterWorkspace("topology", original));

        coordinator.SynchronizeRestoredTopologySession(restored);

        Assert.False(coordinator.EnterWorkspace("resources", restored));
        Assert.False(coordinator.EnterWorkspace("topology", restored));
    }

    [Fact]
    public void SynchronizeRestoredTopologySession_WhileInactive_PreservesRestoredSessionOnEntry()
    {
        var coordinator = new WorkbenchCrossWorkspaceNavigationCoordinator();
        var original = Resource("original");
        var restored = Resource("restored");

        Assert.True(coordinator.EnterWorkspace("topology", original));
        Assert.False(coordinator.EnterWorkspace("resources", original));

        coordinator.SynchronizeRestoredTopologySession(restored);

        Assert.False(coordinator.EnterWorkspace("topology", restored));
    }

    [Fact]
    public void SynchronizeRestoredTopologySession_BeforeFirstEntry_PreservesAlreadyRestoredSession()
    {
        var coordinator = new WorkbenchCrossWorkspaceNavigationCoordinator();
        var restored = Resource("restored");

        coordinator.SynchronizeRestoredTopologySession(restored);

        Assert.False(coordinator.EnterWorkspace("topology", restored));
    }

    [Fact]
    public void SynchronizeRestoredTopologySession_WhileInactive_DoesNotHideAnExternalSelectionChange()
    {
        var coordinator = new WorkbenchCrossWorkspaceNavigationCoordinator();
        var original = Resource("original");
        var restored = Resource("restored");
        var external = Resource("external");

        Assert.True(coordinator.EnterWorkspace("topology", original));
        Assert.False(coordinator.EnterWorkspace("resources", original));

        coordinator.SynchronizeRestoredTopologySession(restored);

        Assert.True(coordinator.EnterWorkspace("topology", external));
    }

    [Fact]
    public void SynchronizeRestoredTopologySession_WhileActive_PreservesTopologyLocalNavigation()
    {
        var coordinator = new WorkbenchCrossWorkspaceNavigationCoordinator();
        var original = Resource("original");
        var restored = Resource("restored");
        var neighbor = Resource("neighbor");

        Assert.True(coordinator.EnterWorkspace("topology", original));

        coordinator.SynchronizeRestoredTopologySession(restored);

        Assert.False(coordinator.EnterWorkspace("topology", neighbor));
        Assert.False(coordinator.EnterWorkspace("resources", neighbor));
        Assert.False(coordinator.EnterWorkspace("topology", neighbor));
    }

    [Fact]
    public void SynchronizeRestoredTopologySession_WithClearedSelection_PreservesRestoredSession()
    {
        var coordinator = new WorkbenchCrossWorkspaceNavigationCoordinator();
        var original = Resource("original");

        Assert.True(coordinator.EnterWorkspace("topology", original));
        Assert.False(coordinator.EnterWorkspace("resources", original));

        coordinator.SynchronizeRestoredTopologySession(null);

        Assert.False(coordinator.EnterWorkspace("topology", null));
    }

    [Fact]
    public void SynchronizeRestoredTopologySession_BeforeExternalSelectionIsCleared_BeginsNewSession()
    {
        var coordinator = new WorkbenchCrossWorkspaceNavigationCoordinator();
        var original = Resource("original");
        var restored = Resource("restored");

        Assert.True(coordinator.EnterWorkspace("topology", original));
        Assert.False(coordinator.EnterWorkspace("resources", original));

        coordinator.SynchronizeRestoredTopologySession(restored);

        Assert.True(coordinator.EnterWorkspace("topology", null));
    }

    [Fact]
    public void SynchronizeRestoredTopologySession_WithClearedSelection_BeginsNewSessionForLaterExternalSelection()
    {
        var coordinator = new WorkbenchCrossWorkspaceNavigationCoordinator();
        var original = Resource("original");
        var external = Resource("external");

        Assert.True(coordinator.EnterWorkspace("topology", original));
        Assert.False(coordinator.EnterWorkspace("resources", original));

        coordinator.SynchronizeRestoredTopologySession(null);

        Assert.True(coordinator.EnterWorkspace("topology", external));
    }

    private static InfrastructureResource Resource(string nativeId)
    {
        return new InfrastructureResource
        {
            ProviderId = "test-provider",
            AccountId = "test-account",
            DomainId = "networking",
            NativeId = nativeId,
            ResourceId = nativeId,
            ResourceType = "test-resource",
            DisplayName = nativeId,
            Location = "test-location"
        };
    }
}
