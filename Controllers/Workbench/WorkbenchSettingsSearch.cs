using System;
using System.Collections.Generic;

namespace TruthDoctor.Controllers.Workbench;

public enum WorkbenchSettingsSearchDocumentKind
{
    PersonalPreference,
    Administration,
    Provider,
    Policy,
    Approval,
    Integration,
    Governance
}

public sealed class WorkbenchSettingsSearchDocument
{
    public required WorkbenchSettingsSearchDocumentKind Kind
    {
        get;
        set;
    }

    public string DisplayText
    {
        get;
        set;
    } = "";

    public string? PersonalPreference
    {
        get;
        set;
    }

    public string? Appearance
    {
        get;
        set;
    }

    public string? Provider
    {
        get;
        set;
    }

    public string? CredentialReference
    {
        get;
        set;
    }

    public string? Policy
    {
        get;
        set;
    }

    public string? Role
    {
        get;
        set;
    }

    public string? ApprovalRule
    {
        get;
        set;
    }

    public string? MaintenanceWindow
    {
        get;
        set;
    }

    public string? Notification
    {
        get;
        set;
    }

    public string? Integration
    {
        get;
        set;
    }

    public string? GovernanceSetting
    {
        get;
        set;
    }
}

public static class WorkbenchSettingsSearch
{
    public static bool Matches(
        WorkbenchSettingsSearchDocument document,
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
                   document.PersonalPreference,
                   search)
               || Contains(
                   document.Appearance,
                   search)
               || Contains(
                   document.Provider,
                   search)
               || Contains(
                   document.CredentialReference,
                   search)
               || Contains(
                   document.Policy,
                   search)
               || Contains(
                   document.Role,
                   search)
               || Contains(
                   document.ApprovalRule,
                   search)
               || Contains(
                   document.MaintenanceWindow,
                   search)
               || Contains(
                   document.Notification,
                   search)
               || Contains(
                   document.Integration,
                   search)
               || Contains(
                   document.GovernanceSetting,
                   search);
    }

    public static IReadOnlyList<WorkbenchSettingsSearchDocument>
        Search(
            IEnumerable<WorkbenchSettingsSearchDocument> documents,
            string? searchText)
    {
        ArgumentNullException.ThrowIfNull(documents);

        var results =
            new List<WorkbenchSettingsSearchDocument>();

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
