using System;
using TruthDoctor.Models.Platform;
using TruthDoctor.State;

namespace TruthDoctor.Controllers.Workbench;

public sealed class WorkbenchSelectionController
{
    private readonly WorkbenchState _state;

    public WorkbenchSelectionController(
        WorkbenchState state)
    {
        _state = state;
    }

    public event EventHandler? SelectionChanged;

    public void SelectProvider(string? providerId)
    {
        _state.SelectedProviderId =
            providerId?.Trim() ?? "";

        NotifyChanged();
    }

    public void SelectAccount(string? accountId)
    {
        _state.SelectedAccountId =
            accountId?.Trim() ?? "";

        NotifyChanged();
    }

    public void SelectLocation(string? location)
    {
        _state.SelectedLocation =
            location?.Trim() ?? "";

        NotifyChanged();
    }

    public void SelectDomain(string? domainId)
    {
        _state.SelectedDomainId =
            domainId?.Trim() ?? "";

        NotifyChanged();
    }

    public void SelectResource(
        InfrastructureResource? resource)
    {
        _state.SelectedResource = resource;

        if (resource is not null)
        {
            _state.SelectedProviderId =
                resource.ProviderId;

            _state.SelectedAccountId =
                resource.AccountId;

            _state.SelectedLocation =
                resource.Location;

            _state.SelectedDomainId =
                resource.DomainId;
        }

        NotifyChanged();
    }

    public void ClearResource()
    {
        _state.SelectedResource = null;

        NotifyChanged();
    }

    public void ClearAll()
    {
        _state.SelectedProviderId = "";
        _state.SelectedAccountId = "";
        _state.SelectedLocation = "";
        _state.SelectedDomainId = "";
        _state.SelectedResource = null;

        NotifyChanged();
    }

    private void NotifyChanged()
    {
        _state.NotifyChanged();

        SelectionChanged?.Invoke(
            this,
            EventArgs.Empty);
    }
}
