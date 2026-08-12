using System;
using System.Collections.Generic;
using System.Linq;

namespace TruthDoctor.Graph;

/// <summary>
/// In-memory collection of validated topology saved views.
/// This class has no UI, provider, or filesystem dependencies.
/// </summary>
public sealed class TopologySavedViewCatalog
{
    private readonly Dictionary<
        string,
        TopologySavedView> _views =
            new(
                StringComparer.OrdinalIgnoreCase);

    private readonly TopologySavedViewValidator
        _validator =
            new();

    public IReadOnlyList<TopologySavedView> All =>
        _views.Values
            .OrderBy(
                view => view.Name,
                StringComparer.OrdinalIgnoreCase)
            .ThenBy(
                view => view.Id,
                StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public TopologySavedView? Find(
        string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return _views.TryGetValue(
                id.Trim(),
                out var view)
            ? view
            : null;
    }

    public void Add(
        TopologySavedView view)
    {
        ArgumentNullException.ThrowIfNull(view);

        _validator.EnsureValid(view);

        if (_views.ContainsKey(view.Id))
        {
            throw new InvalidOperationException(
                $"A saved view with ID '{view.Id}' " +
                "already exists.");
        }

        EnsureUniqueName(
            view.Name);

        _views.Add(
            view.Id,
            view);
    }

    public TopologySavedView Rename(
        string id,
        string newName,
        DateTimeOffset updatedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(newName);

        var existing =
            Find(id)
            ?? throw new KeyNotFoundException(
                $"Saved view '{id}' was not found.");

        var normalizedName =
            newName.Trim();

        EnsureUniqueName(
            normalizedName,
            existing.Id);

        var renamed =
            Copy(
                existing,
                normalizedName,
                updatedAtUtc);

        _validator.EnsureValid(renamed);

        _views[existing.Id] =
            renamed;

        return renamed;
    }

    public bool Delete(
        string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        return _views.Remove(id.Trim());
    }

    public void ReplaceAll(
        IEnumerable<TopologySavedView> views)
    {
        ArgumentNullException.ThrowIfNull(views);

        var replacements =
            views.ToArray();

        var candidate =
            new TopologySavedViewCatalog();

        foreach (var view in replacements)
        {
            candidate.Add(view);
        }

        _views.Clear();

        foreach (var view in candidate._views)
        {
            _views.Add(
                view.Key,
                view.Value);
        }
    }

    private void EnsureUniqueName(
        string name,
        string? excludedId = null)
    {
        var duplicate =
            _views.Values.Any(view =>
                !view.Id.Equals(
                    excludedId,
                    StringComparison.OrdinalIgnoreCase) &&
                view.Name.Equals(
                    name,
                    StringComparison.OrdinalIgnoreCase));

        if (duplicate)
        {
            throw new InvalidOperationException(
                $"A saved view named '{name}' " +
                "already exists.");
        }
    }

    private static TopologySavedView Copy(
        TopologySavedView source,
        string name,
        DateTimeOffset updatedAtUtc)
    {
        return new TopologySavedView
        {
            SchemaVersion =
                source.SchemaVersion,

            Id = source.Id,
            Name = name,

            CreatedAtUtc =
                source.CreatedAtUtc,

            UpdatedAtUtc =
                updatedAtUtc,

            SelectedResourceId =
                source.SelectedResourceId,

            Depth = source.Depth,

            LayoutMode =
                source.LayoutMode,

            SelectedDomainId =
                source.SelectedDomainId,

            CollapsedDomainIds =
                [.. source.CollapsedDomainIds],

            RelationshipFilters =
                source.RelationshipFilters,

            Zoom = source.Zoom,

            ScrollOffset =
                source.ScrollOffset,

            IsMinimapVisible =
                source.IsMinimapVisible,

            SearchText =
                source.SearchText
        };
    }
}
