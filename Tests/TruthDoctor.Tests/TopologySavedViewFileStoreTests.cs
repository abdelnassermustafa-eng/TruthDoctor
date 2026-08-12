using System;
using System.IO;
using System.Linq;
using TruthDoctor.Graph;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class TopologySavedViewFileStoreTests :
    IDisposable
{
    private readonly string _directory;

    private readonly string _filePath;

    private readonly TopologySavedViewFileStore
        _store;

    public TopologySavedViewFileStoreTests()
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
    }

    [Fact]
    public void RoundTripPreservesViewsInCatalogOrder()
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

        var loaded =
            _store.Load();

        Assert.Equal(
            ["Alpha", "Zulu"],
            loaded.Select(view =>
                view.Name));

        Assert.Equal(
            "networking",
            loaded[0].SelectedDomainId);

        Assert.Equal(
            TopologyLayoutMode.Domain,
            loaded[0].LayoutMode);

        Assert.Equal(
            ["compute"],
            loaded[0].CollapsedDomainIds);
    }

    [Fact]
    public void MissingFileReturnsSuccessfulEmptyResult()
    {
        var result =
            _store.TryLoad();

        Assert.True(
            result.IsSuccess);

        Assert.True(
            result.FileWasMissing);

        Assert.Empty(
            result.Views);

        Assert.Empty(
            result.ErrorMessage);
    }

    [Fact]
    public void CorruptJsonReturnsFailureWithoutThrowing()
    {
        File.WriteAllText(
            _filePath,
            "{ this is not valid json");

        var result =
            _store.TryLoad();

        Assert.False(
            result.IsSuccess);

        Assert.False(
            result.FileWasMissing);

        Assert.Empty(
            result.Views);

        Assert.NotEmpty(
            result.ErrorMessage);
    }

    [Fact]
    public void UnsupportedStoreSchemaIsRejectedSafely()
    {
        File.WriteAllText(
            _filePath,
            """
            {
              "schemaVersion": 99,
              "views": []
            }
            """);

        var result =
            _store.TryLoad();

        Assert.False(
            result.IsSuccess);

        Assert.Contains(
            "schema",
            result.ErrorMessage
                .ToLowerInvariant());
    }

    [Fact]
    public void UnknownStorePropertyIsRejectedSafely()
    {
        File.WriteAllText(
            _filePath,
            """
            {
              "schemaVersion": 1,
              "views": [],
              "unexpected": true
            }
            """);

        var result =
            _store.TryLoad();

        Assert.False(
            result.IsSuccess);

        Assert.Contains(
            "unexpected",
            result.ErrorMessage
                .ToLowerInvariant());
    }

    [Fact]
    public void InvalidReplacementLeavesExistingFileUnchanged()
    {
        _store.Save(
        [
            CreateView(
                "existing",
                "Existing")
        ]);

        var originalJson =
            File.ReadAllText(
                _filePath);

        Assert.Throws<InvalidOperationException>(() =>
            _store.Save(
            [
                CreateView(
                    "one",
                    "Duplicate"),
                CreateView(
                    "two",
                    "DUPLICATE")
            ]));

        Assert.Equal(
            originalJson,
            File.ReadAllText(
                _filePath));

        var remaining =
            Assert.Single(
                _store.Load());

        Assert.Equal(
            "existing",
            remaining.Id);
    }

    [Fact]
    public void LaterSaveAtomicallyReplacesCatalog()
    {
        _store.Save(
        [
            CreateView(
                "first",
                "First")
        ]);

        _store.Save(
        [
            CreateView(
                "second",
                "Second")
        ]);

        var loaded =
            Assert.Single(
                _store.Load());

        Assert.Equal(
            "second",
            loaded.Id);

        Assert.Equal(
            "Second",
            loaded.Name);
    }

    [Fact]
    public void SuccessfulSaveLeavesNoTemporaryFiles()
    {
        _store.Save(
        [
            CreateView(
                "one",
                "One")
        ]);

        Assert.True(
            File.Exists(
                _filePath));

        Assert.Empty(
            Directory.GetFiles(
                _directory,
                "*.tmp",
                SearchOption.TopDirectoryOnly));
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
                10,
                30,
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
                "networking",

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

            Zoom = 1.15,

            ScrollOffset =
                new TopologyScrollOffset(
                    125,
                    80),

            IsMinimapVisible =
                true,

            SearchText =
                "gateway"
        };
    }
}
