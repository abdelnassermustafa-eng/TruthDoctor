using System;
using System.Collections.Generic;

namespace TruthDoctor.Graph;

/// <summary>
/// Non-throwing result of loading the persistent saved-view catalog.
/// </summary>
public sealed class TopologySavedViewStoreLoadResult
{
    public bool IsSuccess { get; init; }

    public bool FileWasMissing { get; init; }

    public IReadOnlyList<TopologySavedView> Views { get; init; } =
        Array.Empty<TopologySavedView>();

    public string ErrorMessage { get; init; } = "";

    public static TopologySavedViewStoreLoadResult Success(
        IReadOnlyList<TopologySavedView> views,
        bool fileWasMissing = false)
    {
        ArgumentNullException.ThrowIfNull(views);

        return new TopologySavedViewStoreLoadResult
        {
            IsSuccess = true,
            FileWasMissing = fileWasMissing,
            Views = views
        };
    }

    public static TopologySavedViewStoreLoadResult Failure(
        string errorMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            errorMessage);

        return new TopologySavedViewStoreLoadResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}
