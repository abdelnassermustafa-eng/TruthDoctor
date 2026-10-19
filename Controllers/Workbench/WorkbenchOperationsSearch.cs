using System;
using System.Collections.Generic;
using TruthDoctor.Models.Platform;

namespace TruthDoctor.Controllers.Workbench;

public enum WorkbenchOperationsSearchMatchKind
{
    Resource,
    Capability,
    Action,
    Runbook,
    OperationRequest,
    ApprovalState,
    ExecutionState
}

public sealed class WorkbenchOperationsSearchMatch
{
    public required WorkbenchOperationsSearchMatchKind Kind
    {
        get;
        init;
    }

    public InfrastructureResource? Resource
    {
        get;
        init;
    }

    public string DisplayText
    {
        get;
        init;
    } = "";
}

public static class WorkbenchOperationsSearch
{
    public static WorkbenchOperationsSearchMatch? Match(
        InfrastructureResource resource,
        string? searchText)
    {
        ArgumentNullException.ThrowIfNull(resource);

        var search =
            searchText?.Trim() ?? "";

        if (search.Length == 0)
        {
            return new WorkbenchOperationsSearchMatch
            {
                Kind =
                    WorkbenchOperationsSearchMatchKind.Resource,

                Resource =
                    resource,

                DisplayText =
                    resource.DisplayName
            };
        }

        if (InfrastructureResourceSearch.Matches(
                resource,
                search))
        {
            return new WorkbenchOperationsSearchMatch
            {
                Kind =
                    WorkbenchOperationsSearchMatchKind.Resource,

                Resource =
                    resource,

                DisplayText =
                    resource.DisplayName
            };
        }

        if (resource.Capabilities is not null)
        {
            foreach (var capability in
                     resource.Capabilities)
            {
                if (capability?.Contains(
                        search,
                        StringComparison.OrdinalIgnoreCase)
                    == true)
                {
                    return new WorkbenchOperationsSearchMatch
                    {
                        Kind =
                            WorkbenchOperationsSearchMatchKind
                                .Capability,

                        Resource =
                            resource,

                        DisplayText =
                            capability
                    };
                }
            }
        }

        return null;
    }

    public static IReadOnlyList<WorkbenchOperationsSearchMatch>
        Search(
            IEnumerable<InfrastructureResource> resources,
            string? searchText)
    {
        ArgumentNullException.ThrowIfNull(resources);

        var results =
            new List<WorkbenchOperationsSearchMatch>();

        foreach (var resource in resources)
        {
            var match =
                Match(
                    resource,
                    searchText);

            if (match is not null)
            {
                results.Add(match);
            }
        }

        return results;
    }
}
