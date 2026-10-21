using System;
using TruthDoctor.Controllers.Workbench;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class WorkbenchWorkspacePresentationStateContractTests
{
    [Theory]
    [InlineData("dashboard")]
    [InlineData("resources")]
    [InlineData("topology")]
    [InlineData("operations")]
    [InlineData("history")]
    [InlineData("reports")]
    [InlineData("settings")]
    public void EveryRecognizedWorkspaceSupportsReady(
        string workspace)
    {
        Assert.Equal(
            WorkbenchWorkspacePresentationState.Ready,
            WorkbenchWorkspacePresentationStateContract.Classify(
                workspace,
                isLoading: false,
                hasContent: true,
                errorMessage: null));
    }

    [Theory]
    [InlineData("TOPOLOGY")]
    [InlineData("Topology")]
    [InlineData("topology")]
    public void WorkspaceIdentityIsCaseInsensitive(string workspace)
    {
        Assert.Equal(
            WorkbenchWorkspacePresentationState.Empty,
            WorkbenchWorkspacePresentationStateContract.Classify(
                workspace,
                isLoading: false,
                hasContent: false,
                errorMessage: null));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("unknown")]
    public void InvalidWorkspaceIsRejected(string? workspace)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            WorkbenchWorkspacePresentationStateContract.Classify(
                workspace!,
                isLoading: false,
                hasContent: true,
                errorMessage: null));
    }

    [Fact]
    public void CompletedWorkspaceWithoutContentIsEmpty()
    {
        Assert.Equal(
            WorkbenchWorkspacePresentationState.Empty,
            WorkbenchWorkspacePresentationStateContract.Classify(
                "resources", false, false, null));
    }

    [Fact]
    public void ActiveLoadIsLoading()
    {
        Assert.Equal(
            WorkbenchWorkspacePresentationState.Loading,
            WorkbenchWorkspacePresentationStateContract.Classify(
                "resources", true, false, null));
    }

    [Fact]
    public void LoadingTakesPrecedenceOverExistingContent()
    {
        Assert.Equal(
            WorkbenchWorkspacePresentationState.Loading,
            WorkbenchWorkspacePresentationStateContract.Classify(
                "resources", true, true, null));
    }

    [Fact]
    public void ErrorTakesPrecedenceOverLoadingAndContent()
    {
        Assert.Equal(
            WorkbenchWorkspacePresentationState.Error,
            WorkbenchWorkspacePresentationStateContract.Classify(
                "resources", true, true, "Load failed"));
    }

    [Fact]
    public void WhitespaceErrorDoesNotBecomeFailure()
    {
        Assert.Equal(
            WorkbenchWorkspacePresentationState.Empty,
            WorkbenchWorkspacePresentationStateContract.Classify(
                "resources", false, false, "   "));
    }

    [Fact]
    public void ErrorDoesNotRequireExistingContent()
    {
        Assert.Equal(
            WorkbenchWorkspacePresentationState.Error,
            WorkbenchWorkspacePresentationStateContract.Classify(
                "reports", false, false, "Unavailable"));
    }
}
