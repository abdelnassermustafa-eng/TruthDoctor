using System;
using System.IO;

namespace TruthDoctor.Graph;

/// <summary>
/// Resolves the per-user location of the topology saved-view catalog.
/// The default root follows the operating system's local application-data
/// convention.
/// </summary>
public sealed class TopologySavedViewStoragePathResolver
{
    public const string ApplicationDirectoryName =
        "TruthDoctor";

    public const string SavedViewsFileName =
        "topology-saved-views.json";

    private readonly string _localApplicationDataRoot;

    public TopologySavedViewStoragePathResolver()
        : this(
            ResolveDefaultRoot())
    {
    }

    public TopologySavedViewStoragePathResolver(
        string localApplicationDataRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            localApplicationDataRoot);

        _localApplicationDataRoot =
            Path.GetFullPath(
                localApplicationDataRoot.Trim());
    }

    public string DirectoryPath =>
        Path.Combine(
            _localApplicationDataRoot,
            ApplicationDirectoryName);

    public string FilePath =>
        Path.Combine(
            DirectoryPath,
            SavedViewsFileName);

    private static string ResolveDefaultRoot()
    {
        var localApplicationData =
            Environment.GetFolderPath(
                Environment.SpecialFolder
                    .LocalApplicationData);

        if (!string.IsNullOrWhiteSpace(
                localApplicationData))
        {
            return localApplicationData;
        }

        var userProfile =
            Environment.GetFolderPath(
                Environment.SpecialFolder
                    .UserProfile);

        if (!string.IsNullOrWhiteSpace(
                userProfile))
        {
            return Path.Combine(
                userProfile,
                ".local",
                "share");
        }

        throw new InvalidOperationException(
            "A per-user application-data location " +
            "could not be resolved.");
    }
}
