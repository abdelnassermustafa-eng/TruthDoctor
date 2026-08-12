using System;
using System.IO;
using TruthDoctor.Graph;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class
    TopologySavedViewStoragePathResolverTests :
        IDisposable
{
    private readonly string _root =
        Path.Combine(
            Path.GetTempPath(),
            "TruthDoctor.Tests",
            Guid.NewGuid().ToString("N"));

    [Fact]
    public void ExplicitRootProducesDeterministicPath()
    {
        var resolver =
            new TopologySavedViewStoragePathResolver(
                _root);

        Assert.Equal(
            Path.Combine(
                _root,
                "TruthDoctor"),
            resolver.DirectoryPath);

        Assert.Equal(
            Path.Combine(
                _root,
                "TruthDoctor",
                "topology-saved-views.json"),
            resolver.FilePath);
    }

    [Fact]
    public void ReturnedPathsAreAbsolute()
    {
        var resolver =
            new TopologySavedViewStoragePathResolver(
                _root);

        Assert.True(
            Path.IsPathFullyQualified(
                resolver.DirectoryPath));

        Assert.True(
            Path.IsPathFullyQualified(
                resolver.FilePath));
    }

    [Fact]
    public void ResolvingPathDoesNotCreateDirectory()
    {
        var resolver =
            new TopologySavedViewStoragePathResolver(
                _root);

        _ =
            resolver.FilePath;

        Assert.False(
            Directory.Exists(
                resolver.DirectoryPath));
    }

    [Fact]
    public void BlankExplicitRootIsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new TopologySavedViewStoragePathResolver(
                "   "));
    }

    [Fact]
    public void DefaultResolverUsesExpectedFileName()
    {
        var resolver =
            new TopologySavedViewStoragePathResolver();

        Assert.Equal(
            TopologySavedViewStoragePathResolver
                .SavedViewsFileName,
            Path.GetFileName(
                resolver.FilePath));

        Assert.Equal(
            TopologySavedViewStoragePathResolver
                .ApplicationDirectoryName,
            new DirectoryInfo(
                resolver.DirectoryPath)
                .Name);

        Assert.True(
            Path.IsPathFullyQualified(
                resolver.FilePath));
    }

    public void Dispose()
    {
        if (Directory.Exists(
                _root))
        {
            Directory.Delete(
                _root,
                recursive: true);
        }
    }
}
