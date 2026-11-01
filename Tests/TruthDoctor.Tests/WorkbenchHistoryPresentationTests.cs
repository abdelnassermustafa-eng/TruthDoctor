using System;
using System.Collections.Generic;
using System.Reflection;
using TruthDoctor.Controllers.Workbench;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class WorkbenchHistoryPresentationTests
{
    [Fact]
    public void NullDocumentsRejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            WorkbenchHistoryPresentation.Create(null!));
    }

    [Fact]
    public void EmptyDocumentsDoNotImplyNoHistoricalEvents()
    {
        var presentation =
            WorkbenchHistoryPresentation.Create(
                Array.Empty<WorkbenchHistorySearchDocument>());

        Assert.Equal(
            WorkbenchWorkspacePresentationState.Empty,
            presentation.State);
        Assert.Equal("History", presentation.Title);
        Assert.Equal(
            "No operation history records are currently loaded.",
            presentation.Description);
        Assert.Empty(presentation.Documents);
        Assert.DoesNotContain(
            "never occurred",
            presentation.Description.ToLowerInvariant());
    }

    [Fact]
    public void SuppliedDocumentsRetainReferencesAndOrder()
    {
        var first = Document("first", "first-actor");
        var second = Document("second", "second-actor");
        var input = new[] { first, second };

        var presentation =
            WorkbenchHistoryPresentation.Create(input);

        Assert.Equal(2, presentation.Documents.Count);
        Assert.Same(first, presentation.Documents[0]);
        Assert.Same(second, presentation.Documents[1]);
        Assert.Equal("first", first.DisplayText);
        Assert.Equal("second", second.DisplayText);
    }

    [Fact]
    public void SearchUsesExistingHistorySearchContract()
    {
        var matching = Document("one", "alice");
        var other = Document("two", "bob");

        var presentation =
            WorkbenchHistoryPresentation.Create(
                new[] { matching, other },
                searchText: "  ALICE  ");

        Assert.Equal(
            WorkbenchWorkspacePresentationState.Ready,
            presentation.State);
        Assert.Same(
            matching,
            Assert.Single(presentation.Documents));
    }

    [Fact]
    public void SearchWithNoMatchesIsEmpty()
    {
        var input = Document("one", "alice");

        var presentation =
            WorkbenchHistoryPresentation.Create(
                new[] { input },
                searchText: "unmatched-actor");

        Assert.Equal(
            WorkbenchWorkspacePresentationState.Empty,
            presentation.State);
        Assert.Empty(presentation.Documents);
    }

    [Fact]
    public void WhitespaceSearchPreservesDocuments()
    {
        var first = Document("first", "alice");
        var second = Document("second", "bob");

        var presentation =
            WorkbenchHistoryPresentation.Create(
                new[] { first, second },
                searchText: "   ");

        Assert.Equal(2, presentation.Documents.Count);
        Assert.Same(first, presentation.Documents[0]);
        Assert.Same(second, presentation.Documents[1]);
    }

    [Fact]
    public void ReadyDescriptionReflectsFilteredCount()
    {
        var first = Document("one", "alice");
        var second = Document("two", "alice");
        var other = Document("three", "bob");

        var presentation =
            WorkbenchHistoryPresentation.Create(
                new[] { first, second, other },
                searchText: "alice");

        Assert.Equal(
            WorkbenchWorkspacePresentationState.Ready,
            presentation.State);
        Assert.Equal(
            "2 operation history records available.",
            presentation.Description);
        Assert.Equal("History", presentation.Title);
        Assert.Equal(2, presentation.Documents.Count);
    }

    [Fact]
    public void ExplicitLoadingOverridesReady()
    {
        var presentation =
            WorkbenchHistoryPresentation.Create(
                new[] { Document("one", "alice") },
                isLoading: true);

        Assert.Equal(
            WorkbenchWorkspacePresentationState.Loading,
            presentation.State);
        Assert.Equal(
            "Operation history is loading.",
            presentation.Description);
    }

    [Fact]
    public void ExplicitErrorOverridesLoading()
    {
        var presentation =
            WorkbenchHistoryPresentation.Create(
                new[] { Document("one", "alice") },
                isLoading: true,
                errorMessage: "History unavailable");

        Assert.Equal(
            WorkbenchWorkspacePresentationState.Error,
            presentation.State);
        Assert.Equal(
            "History unavailable",
            presentation.Description);
    }

    [Fact]
    public void WhitespaceErrorDoesNotImplyFailure()
    {
        var presentation =
            WorkbenchHistoryPresentation.Create(
                Array.Empty<WorkbenchHistorySearchDocument>(),
                errorMessage: "   ");

        Assert.Equal(
            WorkbenchWorkspacePresentationState.Empty,
            presentation.State);
        Assert.Equal(
            "No operation history records are currently loaded.",
            presentation.Description);
    }

    [Fact]
    public void NoImplicitIntegrityVerification()
    {
        var document = Document("record", "alice");

        var presentation =
            WorkbenchHistoryPresentation.Create(
                new[] { document });

        Assert.False(
            presentation.Description.Contains(
                "verified",
                StringComparison.OrdinalIgnoreCase));
        Assert.False(
            presentation.Description.Contains(
                "tamper-proof",
                StringComparison.OrdinalIgnoreCase));
        Assert.Same(
            document,
            Assert.Single(presentation.Documents));
    }

    [Fact]
    public void PresentationHasNoOperationalDependencies()
    {
        var type = typeof(WorkbenchHistoryPresentation);

        Assert.True(type.IsClass);
        Assert.True(type.IsSealed);
        Assert.Empty(type.GetInterfaces());
        Assert.Empty(type.GetConstructors(
            BindingFlags.Public | BindingFlags.Instance));

        var declaredPublicProperties =
            type.GetProperties(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly);

        Assert.Equal(4, declaredPublicProperties.Length);

        foreach (var property in declaredPublicProperties)
        {
            Assert.NotNull(property.GetMethod);
            Assert.Null(property.SetMethod);
        }

        var declaredPublicStaticMethods =
            type.GetMethods(
                BindingFlags.Public |
                BindingFlags.Static |
                BindingFlags.DeclaredOnly);

        Assert.Single(declaredPublicStaticMethods);
        Assert.Equal(
            "Create",
            declaredPublicStaticMethods[0].Name);
    }

    private static WorkbenchHistorySearchDocument Document(
        string displayText,
        string actor)
    {
        return new WorkbenchHistorySearchDocument
        {
            Kind = WorkbenchHistorySearchDocumentKind.History,
            DisplayText = displayText,
            Actor = actor
        };
    }
}
