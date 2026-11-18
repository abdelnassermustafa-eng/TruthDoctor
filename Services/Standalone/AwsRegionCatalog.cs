using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace TruthDoctor.Services.Standalone;

/// <summary>
/// Parses the available AWS Regions returned by EC2 DescribeRegions.
/// This is a catalog operation, not infrastructure discovery.
/// </summary>
public static class AwsRegionCatalog
{
    public static IReadOnlyList<string> Parse(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (!root.TryGetProperty("Regions", out var regions)
            || regions.ValueKind != JsonValueKind.Array)
        {
            throw new FormatException(
                "AWS Region response is missing Regions.");
        }

        var available = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var region in regions.EnumerateArray())
        {
            if (!region.TryGetProperty(
                "RegionName", out var nameElement)
                || nameElement.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            string? name = nameElement.GetString();

            if (string.IsNullOrWhiteSpace(name))
                continue;

            if (region.TryGetProperty(
                "OptInStatus", out var statusElement)
                && statusElement.ValueKind == JsonValueKind.String
                && string.Equals(
                    statusElement.GetString(),
                    "not-opted-in",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            available.Add(name.Trim());
        }

        return available
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
    }
}
