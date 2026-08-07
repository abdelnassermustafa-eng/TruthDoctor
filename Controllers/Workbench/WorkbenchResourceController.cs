using System;
using System.Collections.Generic;
using System.Linq;
using TruthDoctor.Models.Platform;
using TruthDoctor.State;

namespace TruthDoctor.Controllers.Workbench;

public sealed class WorkbenchResourceController
{
    private readonly WorkbenchState _state;

    public WorkbenchResourceController(
        WorkbenchState state)
    {
        _state = state;
    }

    public IReadOnlyList<InfrastructureResource> ApplyFilters(
        string? searchText,
        string? domainDisplayName,
        string? stateFilter)
    {
        var platformState = _state.PlatformState;

        if (platformState is null)
        {
            _state.SetVisibleResources(
                Array.Empty<InfrastructureResource>());

            return _state.VisibleResources;
        }

        _state.SearchText = searchText?.Trim() ?? "";
        _state.SelectedStateFilter = stateFilter ?? "";

        var query = platformState.Resources.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(_state.SearchText))
        {
            query = query.Where(resource =>
                Contains(resource.DisplayName, _state.SearchText) ||
                Contains(resource.NativeId, _state.SearchText) ||
                Contains(resource.ResourceType, _state.SearchText) ||
                Contains(resource.DomainId, _state.SearchText) ||
                Contains(resource.State, _state.SearchText) ||
                Contains(resource.Location, _state.SearchText) ||
                Contains(resource.AccountId, _state.SearchText));
        }

        if (!string.IsNullOrWhiteSpace(domainDisplayName) &&
            !domainDisplayName.Equals(
                "All domains",
                StringComparison.OrdinalIgnoreCase))
        {
            var domain = platformState.Domains.FirstOrDefault(
                item => item.DisplayName.Equals(
                    domainDisplayName,
                    StringComparison.OrdinalIgnoreCase));

            if (domain is not null)
            {
                _state.SelectedDomainId = domain.Id;

                query = query.Where(resource =>
                    resource.DomainId.Equals(
                        domain.Id,
                        StringComparison.OrdinalIgnoreCase));
            }
        }
        else
        {
            _state.SelectedDomainId = "";
        }

        if (!string.IsNullOrWhiteSpace(stateFilter) &&
            !stateFilter.Equals(
                "All states",
                StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(resource =>
                resource.State.Equals(
                    stateFilter,
                    StringComparison.OrdinalIgnoreCase));
        }

        var resources = query
            .OrderBy(resource => resource.DomainId)
            .ThenBy(resource => resource.ResourceType)
            .ThenBy(resource => resource.DisplayName)
            .ToList();

        _state.SetVisibleResources(resources);

        return resources;
    }

    public IReadOnlyList<InfrastructureResource> SelectDomain(
        string domainId)
    {
        var platformState = _state.PlatformState;

        if (platformState is null)
        {
            return Array.Empty<InfrastructureResource>();
        }

        var domain = platformState.Domains.FirstOrDefault(
            item => item.Id.Equals(
                domainId,
                StringComparison.OrdinalIgnoreCase));

        if (domain is null)
        {
            return _state.VisibleResources;
        }

        return ApplyFilters(
            _state.SearchText,
            domain.DisplayName,
            _state.SelectedStateFilter);
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
