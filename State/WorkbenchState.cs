using System;
using System.Collections.Generic;
using TruthDoctor.Models.Platform;

namespace TruthDoctor.State;

public sealed class WorkbenchState
{
    public PlatformState? PlatformState { get; private set; }

    public InfrastructureResource? SelectedResource { get; set; }

    public string CurrentView { get; set; } = "dashboard";

    public string SelectedProviderId { get; set; } = "";

    public string SelectedAccountId { get; set; } = "";

    public string SelectedLocation { get; set; } = "";

    public string SelectedDomainId { get; set; } = "";

    public string SearchText { get; set; } = "";

    public string SelectedStateFilter { get; set; } = "";

    public IReadOnlyList<InfrastructureResource> VisibleResources
    { get; private set; } =
        Array.Empty<InfrastructureResource>();

    public bool IsDiscovering { get; set; }

    public string LastError { get; set; } = "";

    public event EventHandler? Changed;

    public void SetPlatformState(PlatformState platformState)
    {
        ArgumentNullException.ThrowIfNull(platformState);

        PlatformState = platformState;

        SelectedProviderId =
            platformState.Context.ProviderId;

        SelectedAccountId =
            platformState.Context.AccountId;

        SelectedLocation =
            platformState.Context.DefaultLocation;

        VisibleResources =
            platformState.Resources;

        LastError = "";

        NotifyChanged();
    }

    public void SetVisibleResources(
        IReadOnlyList<InfrastructureResource> resources)
    {
        VisibleResources =
            resources ?? Array.Empty<InfrastructureResource>();

        NotifyChanged();
    }

    public void SetFailure(string message)
    {
        LastError = message ?? "";
        IsDiscovering = false;

        NotifyChanged();
    }

    public void NotifyChanged()
    {
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
