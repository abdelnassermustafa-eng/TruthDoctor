using System;
using System.Collections.Generic;
using TruthDoctor.Controllers.Workbench;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class WorkbenchHistorySearchTests
{
    [Fact]
    public void SearchKindsRepresentHistoryRoadmapDomains()
    {
        var kinds =
            Enum.GetValues<WorkbenchHistorySearchDocumentKind>();

        Assert.Contains(
            WorkbenchHistorySearchDocumentKind.Audit,
            kinds);

        Assert.Contains(
            WorkbenchHistorySearchDocumentKind.History,
            kinds);

        Assert.Contains(
            WorkbenchHistorySearchDocumentKind.Effect,
            kinds);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankSearchMatchesDocument(
        string? searchText)
    {
        var document =
            CreateDocument();

        Assert.True(
            WorkbenchHistorySearch.Matches(
                document,
                searchText));
    }

    [Theory]
    [InlineData("nasser")]
    [InlineData("restart")]
    [InlineData("payments-api")]
    [InlineData("2026-10-19")]
    [InlineData("verified")]
    [InlineData("aws")]
    [InlineData("production")]
    [InlineData("approved")]
    [InlineData("successful")]
    [InlineData("corr-123")]
    [InlineData("service restored")]
    public void AuthoritativeHistoryFieldsAreSearchable(
        string searchText)
    {
        var document =
            CreateDocument();

        Assert.True(
            WorkbenchHistorySearch.Matches(
                document,
                searchText));
    }

    [Fact]
    public void SearchIsTrimmedAndCaseInsensitive()
    {
        var document =
            CreateDocument();

        Assert.True(
            WorkbenchHistorySearch.Matches(
                document,
                "  SERVICE RESTORED  "));
    }

    [Fact]
    public void PartialFieldMatchIsSupported()
    {
        var document =
            CreateDocument();

        Assert.True(
            WorkbenchHistorySearch.Matches(
                document,
                "payments"));
    }

    [Fact]
    public void DisplayTextIsNotAnImplicitSearchField()
    {
        var document =
            CreateDocument();

        document.DisplayText =
            "unique-display-only-text";

        Assert.False(
            WorkbenchHistorySearch.Matches(
                document,
                "unique-display-only-text"));
    }

    [Fact]
    public void SearchKindIsNotAnImplicitSearchField()
    {
        var document =
            CreateDocument();

        document.Kind =
            WorkbenchHistorySearchDocumentKind.Effect;

        Assert.False(
            WorkbenchHistorySearch.Matches(
                document,
                "effect"));
    }

    [Fact]
    public void UnrelatedSearchDoesNotMatch()
    {
        var document =
            CreateDocument();

        Assert.False(
            WorkbenchHistorySearch.Matches(
                document,
                "does-not-exist"));
    }

    [Fact]
    public void NullAndEmptyFieldsAreSafe()
    {
        var document =
            new WorkbenchHistorySearchDocument
            {
                Kind =
                    WorkbenchHistorySearchDocumentKind.History,

                DisplayText =
                    "Sparse history document",

                Actor = null,
                Action = "",
                Target = null,
                DateText = "",
                Result = null,
                Provider = "",
                Environment = null,
                Approval = "",
                Verification = null,
                CorrelationId = "",
                OperationalEffect = null
            };

        Assert.False(
            WorkbenchHistorySearch.Matches(
                document,
                "anything"));
    }

    [Fact]
    public void NullDocumentIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            WorkbenchHistorySearch.Matches(
                null!,
                "anything"));
    }

    [Fact]
    public void SearchReturnsMatchingDocuments()
    {
        var matching =
            CreateDocument();

        var unrelated =
            CreateDocument();

        unrelated.Actor = "other-user";
        unrelated.Action = "backup";
        unrelated.Target = "orders-database";
        unrelated.DateText = "2026-09-01";
        unrelated.Result = "completed";
        unrelated.Provider = "azure";
        unrelated.Environment = "development";
        unrelated.Approval = "not-required";
        unrelated.Verification = "pending";
        unrelated.CorrelationId = "corr-999";
        unrelated.OperationalEffect = "backup created";

        var results =
            WorkbenchHistorySearch.Search(
                new[]
                {
                    matching,
                    unrelated
                },
                "payments-api");

        var result =
            Assert.Single(results);

        Assert.Same(
            matching,
            result);
    }

    [Fact]
    public void SearchPreservesInputOrder()
    {
        var first =
            CreateDocument();

        first.Actor = "alice";

        var second =
            CreateDocument();

        second.Actor = "bob";

        var results =
            WorkbenchHistorySearch.Search(
                new[]
                {
                    first,
                    second
                },
                "restart");

        Assert.Equal(
            2,
            results.Count);

        Assert.Same(
            first,
            results[0]);

        Assert.Same(
            second,
            results[1]);
    }

    [Fact]
    public void BlankSearchReturnsOneEntryPerInputDocument()
    {
        var first =
            CreateDocument();

        var second =
            CreateDocument();

        var results =
            WorkbenchHistorySearch.Search(
                new[]
                {
                    first,
                    second
                },
                "   ");

        Assert.Equal(
            2,
            results.Count);

        Assert.Same(
            first,
            results[0]);

        Assert.Same(
            second,
            results[1]);
    }

    [Fact]
    public void SearchReturnsEmptyWhenNothingMatches()
    {
        var results =
            WorkbenchHistorySearch.Search(
                new[]
                {
                    CreateDocument()
                },
                "does-not-exist");

        Assert.Empty(results);
    }

    [Fact]
    public void NullDocumentCollectionIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            WorkbenchHistorySearch.Search(
                null!,
                "anything"));
    }

    private static WorkbenchHistorySearchDocument
        CreateDocument()
    {
        return new WorkbenchHistorySearchDocument
        {
            Kind =
                WorkbenchHistorySearchDocumentKind.History,

            DisplayText =
                "Restart payments-api",

            Actor =
                "nasser",

            Action =
                "restart",

            Target =
                "payments-api",

            DateText =
                "2026-10-19 00:30",

            Result =
                "verified",

            Provider =
                "aws",

            Environment =
                "production",

            Approval =
                "approved",

            Verification =
                "successful",

            CorrelationId =
                "corr-123",

            OperationalEffect =
                "service restored"
        };
    }
}
