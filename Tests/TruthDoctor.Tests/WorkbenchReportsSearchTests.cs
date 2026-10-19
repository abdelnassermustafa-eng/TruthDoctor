using System;
using TruthDoctor.Controllers.Workbench;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class WorkbenchReportsSearchTests
{
    [Fact]
    public void SearchKindsRepresentReportsRoadmapDomains()
    {
        var kinds =
            Enum.GetValues<WorkbenchReportsSearchDocumentKind>();

        Assert.Contains(
            WorkbenchReportsSearchDocumentKind.ReportDefinition,
            kinds);

        Assert.Contains(
            WorkbenchReportsSearchDocumentKind.Template,
            kinds);

        Assert.Contains(
            WorkbenchReportsSearchDocumentKind.HistoricalRun,
            kinds);

        Assert.Contains(
            WorkbenchReportsSearchDocumentKind.Schedule,
            kinds);

        Assert.Contains(
            WorkbenchReportsSearchDocumentKind.Delivery,
            kinds);

        Assert.Contains(
            WorkbenchReportsSearchDocumentKind.DateRange,
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
            WorkbenchReportsSearch.Matches(
                document,
                searchText));
    }

    [Theory]
    [InlineData("Infrastructure Health")]
    [InlineData("Executive Summary")]
    [InlineData("operations")]
    [InlineData("payments-api")]
    [InlineData("run-20261019-001")]
    [InlineData("2026-10-01")]
    [InlineData("weekly monday")]
    [InlineData("delivered")]
    public void AuthoritativeReportsFieldsAreSearchable(
        string searchText)
    {
        var document =
            CreateDocument();

        Assert.True(
            WorkbenchReportsSearch.Matches(
                document,
                searchText));
    }

    [Fact]
    public void SearchIsTrimmedAndCaseInsensitive()
    {
        var document =
            CreateDocument();

        Assert.True(
            WorkbenchReportsSearch.Matches(
                document,
                "  EXECUTIVE SUMMARY  "));
    }

    [Fact]
    public void PartialFieldMatchIsSupported()
    {
        var document =
            CreateDocument();

        Assert.True(
            WorkbenchReportsSearch.Matches(
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
            WorkbenchReportsSearch.Matches(
                document,
                "unique-display-only-text"));
    }

    [Fact]
    public void SearchKindIsNotAnImplicitSearchField()
    {
        var document =
            CreateDocument();

        document.Kind =
            WorkbenchReportsSearchDocumentKind.DateRange;

        Assert.False(
            WorkbenchReportsSearch.Matches(
                document,
                "DateRange"));
    }

    [Fact]
    public void UnrelatedSearchDoesNotMatch()
    {
        var document =
            CreateDocument();

        Assert.False(
            WorkbenchReportsSearch.Matches(
                document,
                "does-not-exist"));
    }

    [Fact]
    public void NullAndEmptyFieldsAreSafe()
    {
        var document =
            new WorkbenchReportsSearchDocument
            {
                Kind =
                    WorkbenchReportsSearchDocumentKind.ReportDefinition,

                DisplayText =
                    "Sparse report document",

                Report = null,
                Template = "",
                Category = null,
                Resource = "",
                HistoricalRun = null,
                DateRange = "",
                Schedule = null,
                DeliveryStatus = ""
            };

        Assert.False(
            WorkbenchReportsSearch.Matches(
                document,
                "anything"));
    }

    [Fact]
    public void NullDocumentIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            WorkbenchReportsSearch.Matches(
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

        unrelated.Report =
            "Capacity Forecast";

        unrelated.Template =
            "Capacity Planning";

        unrelated.Category =
            "capacity";

        unrelated.Resource =
            "orders-database";

        unrelated.HistoricalRun =
            "run-20260901-001";

        unrelated.DateRange =
            "2026-09-01 to 2026-09-30";

        unrelated.Schedule =
            "monthly first-day 09:00";

        unrelated.DeliveryStatus =
            "pending";

        var results =
            WorkbenchReportsSearch.Search(
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

        first.Report =
            "Weekly Infrastructure Health";

        var second =
            CreateDocument();

        second.Report =
            "Weekly Operations Summary";

        var results =
            WorkbenchReportsSearch.Search(
                new[]
                {
                    first,
                    second
                },
                "weekly");

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
            WorkbenchReportsSearch.Search(
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
            WorkbenchReportsSearch.Search(
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
            WorkbenchReportsSearch.Search(
                null!,
                "anything"));
    }

    private static WorkbenchReportsSearchDocument
        CreateDocument()
    {
        return new WorkbenchReportsSearchDocument
        {
            Kind =
                WorkbenchReportsSearchDocumentKind.ReportDefinition,

            DisplayText =
                "Infrastructure Health Report",

            Report =
                "Infrastructure Health",

            Template =
                "Executive Summary",

            Category =
                "operations",

            Resource =
                "payments-api",

            HistoricalRun =
                "run-20261019-001",

            DateRange =
                "2026-10-01 to 2026-10-19",

            Schedule =
                "weekly monday 08:00",

            DeliveryStatus =
                "delivered"
        };
    }
}
