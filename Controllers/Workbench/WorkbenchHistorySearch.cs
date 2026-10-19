using System;
using System.Collections.Generic;

namespace TruthDoctor.Controllers.Workbench;

public enum WorkbenchHistorySearchDocumentKind
{
    Audit,
    History,
    Effect
}

public sealed class WorkbenchHistorySearchDocument
{
    public required WorkbenchHistorySearchDocumentKind Kind
    {
        get;
        set;
    }

    public string DisplayText
    {
        get;
        set;
    } = "";

    public string? Actor
    {
        get;
        set;
    }

    public string? Action
    {
        get;
        set;
    }

    public string? Target
    {
        get;
        set;
    }

    public string? DateText
    {
        get;
        set;
    }

    public string? Result
    {
        get;
        set;
    }

    public string? Provider
    {
        get;
        set;
    }

    public string? Environment
    {
        get;
        set;
    }

    public string? Approval
    {
        get;
        set;
    }

    public string? Verification
    {
        get;
        set;
    }

    public string? CorrelationId
    {
        get;
        set;
    }

    public string? OperationalEffect
    {
        get;
        set;
    }
}

public static class WorkbenchHistorySearch
{
    public static bool Matches(
        WorkbenchHistorySearchDocument document,
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
                   document.Actor,
                   search)
               || Contains(
                   document.Action,
                   search)
               || Contains(
                   document.Target,
                   search)
               || Contains(
                   document.DateText,
                   search)
               || Contains(
                   document.Result,
                   search)
               || Contains(
                   document.Provider,
                   search)
               || Contains(
                   document.Environment,
                   search)
               || Contains(
                   document.Approval,
                   search)
               || Contains(
                   document.Verification,
                   search)
               || Contains(
                   document.CorrelationId,
                   search)
               || Contains(
                   document.OperationalEffect,
                   search);
    }

    public static IReadOnlyList<WorkbenchHistorySearchDocument>
        Search(
            IEnumerable<WorkbenchHistorySearchDocument> documents,
            string? searchText)
    {
        ArgumentNullException.ThrowIfNull(documents);

        var results =
            new List<WorkbenchHistorySearchDocument>();

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
