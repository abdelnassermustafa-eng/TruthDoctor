using System;
using System.Collections.Generic;
using TruthDoctor.Models.Platform;

namespace TruthDoctor.Controllers.Workbench;

public static class InfrastructureResourceSearch
{
    public static bool Matches(
        InfrastructureResource resource,
        string? searchText)
    {
        ArgumentNullException.ThrowIfNull(resource);

        var search =
            searchText?.Trim() ?? "";

        if (search.Length == 0)
        {
            return true;
        }

        return Contains(
                   resource.DisplayName,
                   search) ||
               Contains(
                   resource.NativeId,
                   search) ||
               Contains(
                   resource.ResourceId,
                   search) ||
               Contains(
                   resource.ResourceType,
                   search) ||
               Contains(
                   resource.DomainId,
                   search) ||
               Contains(
                   resource.State,
                   search) ||
               Contains(
                   resource.Location,
                   search) ||
               Contains(
                   resource.AccountId,
                   search) ||
               MatchesDictionary(
                   resource.Properties,
                   search) ||
               MatchesDictionary(
                   resource.Tags,
                   search);
    }

    private static bool MatchesDictionary(
        IReadOnlyDictionary<string, string>? values,
        string search)
    {
        if (values is null)
        {
            return false;
        }

        foreach (var item in values)
        {
            if (Contains(item.Key, search) ||
                Contains(item.Value, search))
            {
                return true;
            }
        }

        return false;
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
