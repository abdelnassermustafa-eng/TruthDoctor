using System;
using System.Linq;

namespace TruthDoctor.Graph;

/// <summary>
/// Converts live topology workspace values into the portable saved-view
/// contract. It has no UI, provider, or storage dependencies.
/// </summary>
public sealed class TopologySavedViewCaptureService
{
    private readonly TopologySavedViewValidator
        _validator =
            new();

    public TopologySavedView Capture(
        string id,
        string name,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        TopologySavedViewCaptureState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var view =
            new TopologySavedView
            {
                Id = id?.Trim() ?? "",
                Name = name?.Trim() ?? "",

                CreatedAtUtc =
                    createdAtUtc,

                UpdatedAtUtc =
                    updatedAtUtc,

                SelectedResourceId =
                    state.SelectedResourceId
                        ?.Trim() ??
                    "",

                Depth =
                    state.Depth,

                LayoutMode =
                    state.LayoutMode,

                SelectedDomainId =
                    TopologySavedViewDomainCodec
                        .ToStorage(
                            state.LiveSelectedDomainId),

                CollapsedDomainIds =
                    state.CollapsedDomainIds?
                        .Select(domainId =>
                            domainId?.Trim() ?? "")
                        .OrderBy(
                            domainId => domainId,
                            StringComparer.OrdinalIgnoreCase)
                        .ToArray()
                    ?? [],

                RelationshipFilters =
                    state.RelationshipFilters,

                Zoom =
                    state.Zoom,

                ScrollOffset =
                    state.ScrollOffset,

                IsMinimapVisible =
                    state.IsMinimapVisible,

                SearchText =
                    state.SearchText?.Trim() ?? ""
            };

        _validator.EnsureValid(view);

        return view;
    }
}
