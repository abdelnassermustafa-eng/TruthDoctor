using System.Collections.Generic;
using System.Linq;
using TruthDoctor.Models.Platform;

namespace TruthDoctor.Controllers.Workbench;

public static class InfrastructureResourceSearchResolver
{
    public static InfrastructureResource? ResolveUnique(
        IEnumerable<InfrastructureResource> resources,
        string? searchText)
    {
        var search =
            searchText?.Trim() ?? "";

        if (search.Length == 0)
        {
            return null;
        }

        var matches =
            resources
                .Where(resource =>
                    InfrastructureResourceSearch.Matches(
                        resource,
                        search))
                .Take(2)
                .ToList();

        return matches.Count == 1
            ? matches[0]
            : null;
    }
}
