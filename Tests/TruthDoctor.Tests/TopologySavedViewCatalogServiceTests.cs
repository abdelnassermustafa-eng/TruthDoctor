using System;
using System.IO;
using System.Linq;
using TruthDoctor.Graph;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class
    TopologySavedViewCatalogServiceTests :
        IDisposable
{
    private readonly string _directory;

    private readonly string _filePath;

    private readonly TopologySavedViewFileStore
        _store;

    private readonly TopologySavedViewCatalogService
        _service;

    public TopologySavedViewCatalogServiceTests()
    {
        _directory =
            Path.Combine(
                Path.GetTempPath(),
                "TruthDoctor.Tests",
                Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(
            _directory);

        _filePath =
            Path.Combine(
                _directory,
                "topology-saved-views.json");

        _store =
            new TopologySavedViewFileStore(
                _filePath);

        _service =
            new TopologySavedViewCatalogService(
                _store);
    }

    [Fact]
    public void MissingFileLoadsAsEmptyCatalog()
    {
        var result =
            _service.Load();

        Assert.True(
            result.IsSuccess);

        Assert.True(
            result.FileWasMissing);

        Assert.Empty(
            _service.All);
    }

    [Fact]
    public void ExistingPersistentViewsLoadIntoCatalog()
    {
        _store.Save(
        [
            CreateView(
                "zulu",
                "Zulu"),
            CreateView(
                "alpha",
                "Alpha")
        ]);

        var result =
            _service.Load();

        Assert.True(
            result.IsSuccess);

        Assert.False(
            result.FileWasMissing);

        Assert.Equal(
            ["Alpha", "Zulu"],
            _service.All.Select(view =>
                view.Name));
    }

    [Fact]
    public void CorruptReloadLeavesLiveCatalogUnchanged()
    {
        _service.Add(
            CreateView(
                "existing",
                "Existing"));

        File.WriteAllText(
            _filePath,
            "{ invalid json");

        var result =
            _service.Load();

        Assert.False(
            result.IsSuccess);

        var remaining =
            Assert.Single(
                _service.All);

        Assert.Equal(
            "existing",
            remaining.Id);
    }

    [Fact]
    public void AddPersistsAndPublishesView()
    {
        _service.Add(
            CreateView(
                "one",
                "One"));

        Assert.NotNull(
            _service.Find(
                "ONE"));

        var persisted =
            Assert.Single(
                _store.Load());

        Assert.Equal(
            "one",
            persisted.Id);
    }

    [Fact]
    public void RenamePersistsAndPublishesNewName()
    {
        _service.Add(
            CreateView(
                "one",
                "Before"));

        var updatedAt =
            new DateTimeOffset(
                2026,
                8,
                12,
                11,
                30,
                0,
                TimeSpan.Zero);

        var renamed =
            _service.Rename(
                "one",
                "After",
                updatedAt);

        Assert.Equal(
            "After",
            renamed.Name);

        Assert.Equal(
            updatedAt,
            renamed.UpdatedAtUtc);

        var persisted =
            Assert.Single(
                _store.Load());

        Assert.Equal(
            "After",
            persisted.Name);
    }

    [Fact]
    public void DeletePersistsAndPublishesRemoval()
    {
        _service.Add(
            CreateView(
                "one",
                "One"));

        Assert.True(
            _service.Delete(
                "ONE"));

        Assert.Empty(
            _service.All);

        Assert.Empty(
            _store.Load());

        Assert.False(
            _service.Delete(
                "one"));
    }

    [Fact]
    public void FailedWriteLeavesLiveCatalogUnchanged()
    {
        var destinationDirectory =
            Path.Combine(
                _directory,
                "destination-is-a-directory");

        Directory.CreateDirectory(
            destinationDirectory);

        var failingService =
            new TopologySavedViewCatalogService(
                new TopologySavedViewFileStore(
                    destinationDirectory));

        Assert.ThrowsAny<IOException>(() =>
            failingService.Add(
                CreateView(
                    "one",
                    "One")));

        Assert.Empty(
            failingService.All);

        Assert.True(
            Directory.Exists(
                destinationDirectory));
    }

    [Fact]
    public void ReplaceAllPersistsOneAtomicCatalog()
    {
        _service.Add(
            CreateView(
                "old",
                "Old"));

        _service.ReplaceAll(
        [
            CreateView(
                "zulu",
                "Zulu"),
            CreateView(
                "alpha",
                "Alpha")
        ]);

        Assert.Equal(
            ["Alpha", "Zulu"],
            _service.All.Select(view =>
                view.Name));

        Assert.Equal(
            ["Alpha", "Zulu"],
            _store.Load().Select(view =>
                view.Name));

        Assert.Null(
            _service.Find(
                "old"));
    }

    public void Dispose()
    {
        if (Directory.Exists(
                _directory))
        {
            Directory.Delete(
                _directory,
                recursive: true);
        }
    }

    private static TopologySavedView CreateView(
        string id,
        string name)
    {
        var timestamp =
            new DateTimeOffset(
                2026,
                8,
                12,
                11,
                0,
                0,
                TimeSpan.Zero);

        return new TopologySavedView
        {
            Id = id,
            Name = name,

            CreatedAtUtc =
                timestamp,

            UpdatedAtUtc =
                timestamp,

            SelectedResourceId =
                "provider-neutral-resource",

            Depth = 2,

            LayoutMode =
                TopologyLayoutMode.Domain,

            SelectedDomainId =
                TopologySavedViewDomainCodec
                    .AllDomainsStorageId,

            CollapsedDomainIds =
            [
                "compute"
            ],

            RelationshipFilters =
                new TopologyRelationshipFilterState
                {
                    Containment = true,
                    Placement = true,
                    Dependency = true,
                    Connectivity = true,
                    Security = true,
                    Traffic = true,
                    Association = true,
                    Other = true
                },

            Zoom = 1.10,

            ScrollOffset =
                new TopologyScrollOffset(
                    80,
                    40),

            IsMinimapVisible =
                true,

            SearchText =
                "gateway"
        };
    }
}
