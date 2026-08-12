using System;
using System.Collections.Generic;
using System.Linq;

namespace TruthDoctor.Graph;

/// <summary>
/// Coordinates the in-memory saved-view catalog with its persistent store.
///
/// Every mutation is prepared in a separate candidate catalog, written to
/// persistent storage, and only then published to the live catalog. A failed
/// write therefore leaves the live catalog unchanged.
/// </summary>
public sealed class TopologySavedViewCatalogService
{
    private readonly TopologySavedViewFileStore
        _store;

    private readonly TopologySavedViewCatalog
        _catalog =
            new();

    public TopologySavedViewCatalogService(
        TopologySavedViewFileStore store)
    {
        ArgumentNullException.ThrowIfNull(store);

        _store =
            store;
    }

    public TopologySavedViewCatalog Catalog =>
        _catalog;

    public IReadOnlyList<TopologySavedView> All =>
        _catalog.All;

    public TopologySavedViewStoreLoadResult Load()
    {
        var result =
            _store.TryLoad();

        if (!result.IsSuccess)
        {
            return result;
        }

        _catalog.ReplaceAll(
            result.Views);

        return result;
    }

    public TopologySavedView? Find(
        string id)
    {
        return _catalog.Find(
            id);
    }

    public void Add(
        TopologySavedView view)
    {
        ArgumentNullException.ThrowIfNull(view);

        PublishCandidate(candidate =>
            candidate.Add(
                view));
    }

    public TopologySavedView Rename(
        string id,
        string newName,
        DateTimeOffset updatedAtUtc)
    {
        TopologySavedView? renamed =
            null;

        PublishCandidate(candidate =>
        {
            renamed =
                candidate.Rename(
                    id,
                    newName,
                    updatedAtUtc);
        });

        return renamed
            ?? throw new InvalidOperationException(
                "The renamed saved view was not produced.");
    }

    public bool Delete(
        string id)
    {
        if (_catalog.Find(id) is null)
        {
            return false;
        }

        var deleted =
            false;

        PublishCandidate(candidate =>
        {
            deleted =
                candidate.Delete(
                    id);
        });

        return deleted;
    }

    public void ReplaceAll(
        IEnumerable<TopologySavedView> views)
    {
        ArgumentNullException.ThrowIfNull(views);

        var replacement =
            views.ToArray();

        PublishCandidate(candidate =>
            candidate.ReplaceAll(
                replacement));
    }

    private void PublishCandidate(
        Action<TopologySavedViewCatalog> mutation)
    {
        ArgumentNullException.ThrowIfNull(
            mutation);

        var candidate =
            new TopologySavedViewCatalog();

        candidate.ReplaceAll(
            _catalog.All);

        mutation(
            candidate);

        _store.Save(
            candidate.All);

        _catalog.ReplaceAll(
            candidate.All);
    }
}
