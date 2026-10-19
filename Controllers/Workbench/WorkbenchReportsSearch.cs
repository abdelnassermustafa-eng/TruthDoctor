using System;
using System.Collections.Generic;

namespace TruthDoctor.Controllers.Workbench;

public enum WorkbenchReportsSearchDocumentKind
{
    ReportDefinition,
    Template,
    HistoricalRun,
    Schedule,
    Delivery,
    DateRange
}

public sealed class WorkbenchReportsSearchDocument
{
    public required WorkbenchReportsSearchDocumentKind Kind
    {
        get;
        set;
    }

    public string DisplayText
    {
        get;
        set;
    } = "";

    public string? Report
    {
        get;
        set;
    }

    public string? Template
    {
        get;
        set;
    }

    public string? Category
    {
        get;
        set;
    }

    public string? Resource
    {
        get;
        set;
    }

    public string? HistoricalRun
    {
        get;
        set;
    }

    public string? DateRange
    {
        get;
        set;
    }

    public string? Schedule
    {
        get;
        set;
    }

    public string? DeliveryStatus
    {
        get;
        set;
    }
}

public static class WorkbenchReportsSearch
{
    public static bool Matches(
        WorkbenchReportsSearchDocument document,
        string? searchText)
    {
        ArgumentNullException.ThrowIfNull(document);

        var search =
            searchText?.Trim() ?? "";

        if (search.Length == 0)
        {
            return true;
        }

        return Contains(
                   document.Report,
                   search)
               || Contains(
                   document.Template,
                   search)
               || Contains(
                   document.Category,
                   search)
               || Contains(
                   document.Resource,
                   search)
               || Contains(
                   document.HistoricalRun,
                   search)
               || Contains(
                   document.DateRange,
                   search)
               || Contains(
                   document.Schedule,
                   search)
               || Contains(
                   document.DeliveryStatus,
                   search);
    }

    public static IReadOnlyList<WorkbenchReportsSearchDocument>
        Search(
            IEnumerable<WorkbenchReportsSearchDocument> documents,
            string? searchText)
    {
        ArgumentNullException.ThrowIfNull(documents);

        var results =
            new List<WorkbenchReportsSearchDocument>();

        foreach (var document in documents)
        {
            if (Matches(
                    document,
                    searchText))
            {
                results.Add(
                    document);
            }
        }

        return results;
    }

    private static bool Contains(
        string? value,
        string search)
    {
        return value?.Contains(
                   search,
                   StringComparison.OrdinalIgnoreCase)
               == true;
    }
}
