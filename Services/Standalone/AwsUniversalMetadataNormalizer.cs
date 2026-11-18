using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TruthDoctor.Services.Standalone;

/// <summary>
/// Service-independent metadata extraction from an individual
/// AWS discovery JSON resource. No AWS calls or state mutations.
/// </summary>
public static class AwsUniversalMetadataNormalizer
{
    public const int MaxDepth = 10;
    public const int MaxArrayItems = 64;
    public const int MaxProperties = 2048;
    public const int MaxValueLength = 4096;

    private static readonly string[] SensitiveNames =
    [
        "password",
        "passwd",
        "secret",
        "credential",
        "accesstoken",
        "refreshtoken",
        "sessiontoken",
        "clienttoken",
        "authorization",
        "privatekey",
        "accesskey",
        "apikey",
        "userdata",
        "keymaterial",
        "connectionstring",
        "sensitive"
    ];

    public static AwsNormalizedMetadata Normalize(
        JsonElement resource)
    {
        if (resource.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException(
                "AWS resource metadata must be a JSON object.",
                nameof(resource));
        }

        var properties = new Dictionary<string, string>(
            StringComparer.Ordinal);

        var tags = new Dictionary<string, string>(
            StringComparer.Ordinal);

        foreach (JsonProperty property
            in resource.EnumerateObject())
        {
            if (string.Equals(
                property.Name, "Tags",
                StringComparison.OrdinalIgnoreCase))
            {
                ReadTags(property.Value, tags);
                continue;
            }

            Flatten(
                property.Value,
                property.Name,
                properties,
                0);
        }

        return new AwsNormalizedMetadata(
            properties, tags);
    }

    private static void Flatten(
        JsonElement value,
        string path,
        Dictionary<string, string> output,
        int depth)
    {
        if (output.Count >= MaxProperties
            || depth > MaxDepth
            || Sensitive(path)
            || SensitiveContainer(path))
        {
            return;
        }

        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (JsonProperty child
                    in value.EnumerateObject())
                {
                    Flatten(
                        child.Value,
                        path + "." + child.Name,
                        output,
                        depth + 1);
                }
                break;

            case JsonValueKind.Array:
                output.TryAdd(
                    path + ".Count",
                    value.GetArrayLength().ToString(
                        CultureInfo.InvariantCulture));

                int index = 0;

                foreach (JsonElement child
                    in value.EnumerateArray())
                {
                    if (index >= MaxArrayItems)
                    {
                        break;
                    }

                    Flatten(
                        child,
                        path + "[" + index.ToString(
                            CultureInfo.InvariantCulture) + "]",
                        output,
                        depth + 1);

                    index++;
                }
                break;

            case JsonValueKind.String:
                AddValue(
                    output, path,
                    value.GetString() ?? "");
                break;

            case JsonValueKind.Number:
                AddValue(
                    output, path,
                    value.GetRawText());
                break;

            case JsonValueKind.True:
                AddValue(output, path, "true");
                break;

            case JsonValueKind.False:
                AddValue(output, path, "false");
                break;

            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                break;
        }
    }

    private static void AddValue(
        Dictionary<string, string> target,
        string key,
        string value)
    {
        if (target.Count >= MaxProperties
            || Sensitive(key)
            || SensitiveValue(value))
        {
            return;
        }

        target.TryAdd(
            key,
            value.Length <= MaxValueLength
                ? value
                : value[..MaxValueLength] + " [truncated]");
    }

    private static void ReadTags(
        JsonElement value,
        Dictionary<string, string> output)
    {
        if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement entry
                in value.EnumerateArray())
            {
                if (output.Count >= MaxProperties)
                {
                    break;
                }

                if (entry.ValueKind != JsonValueKind.Object
                    || !entry.TryGetProperty(
                        "Key", out JsonElement key)
                    || key.ValueKind != JsonValueKind.String
                    || !entry.TryGetProperty(
                        "Value", out JsonElement tagValue))
                {
                    continue;
                }

                string? name = key.GetString();

                if (string.IsNullOrWhiteSpace(name)
                    || Sensitive(name))
                {
                    continue;
                }

                if (tagValue.ValueKind ==
                    JsonValueKind.String)
                {
                    AddValue(
                        output,
                        name,
                        tagValue.GetString() ?? "");
                }
            }

            return;
        }

        if (value.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty tag
                in value.EnumerateObject())
            {
                if (tag.Value.ValueKind ==
                    JsonValueKind.String)
                {
                    AddValue(
                        output,
                        tag.Name,
                        tag.Value.GetString() ?? "");
                }
            }
        }
    }

    private static bool SensitiveContainer(string path)
    {
        string[] parts = path.Split('.');

        for (int index = 0; index < parts.Length; index++)
        {
            string part = parts[index];

            if (string.Equals(
                part, "EnvironmentVariables",
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (index > 0
                && string.Equals(
                    parts[index - 1],
                    "Environment",
                    StringComparison.OrdinalIgnoreCase)
                && string.Equals(
                    part, "Variables",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool SensitiveValue(string value)
    {
        if (value.Contains(
            "-----BEGIN PRIVATE KEY-----",
            StringComparison.OrdinalIgnoreCase)
            || value.Contains(
                "-----BEGIN RSA PRIVATE KEY-----",
                StringComparison.OrdinalIgnoreCase)
            || value.Contains(
                "-----BEGIN OPENSSH PRIVATE KEY-----",
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (Regex.IsMatch(
            value,
            @"\b(?:AKIA|ASIA)[A-Z0-9]{16}\b",
            RegexOptions.CultureInvariant))
        {
            return true;
        }

        if (Uri.TryCreate(
            value, UriKind.Absolute, out Uri? uri)
            && !string.IsNullOrEmpty(uri.UserInfo))
        {
            return true;
        }

        return false;
    }

    private static bool Sensitive(string name)
    {
        // Normalize separators and punctuation, but retain
        // property-name text for simple sensitive-field checks.
        Span<char> characters =
            stackalloc char[name.Length];

        int count = 0;

        foreach (char character in name)
        {
            if (char.IsLetterOrDigit(character))
            {
                characters[count++] =
                    char.ToLowerInvariant(character);
            }
        }

        string normalized =
            new(characters[..count]);

        foreach (string sensitive in SensitiveNames)
        {
            if (normalized.Contains(
                sensitive, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>
/// A detached, read-only view of normalized discovery metadata.
/// </summary>
public sealed class AwsNormalizedMetadata
{
    public IReadOnlyDictionary<string, string> Properties
    {
        get;
    }

    public IReadOnlyDictionary<string, string> Tags
    {
        get;
    }

    public AwsNormalizedMetadata(
        IReadOnlyDictionary<string, string> properties,
        IReadOnlyDictionary<string, string> tags)
    {
        ArgumentNullException.ThrowIfNull(properties);
        ArgumentNullException.ThrowIfNull(tags);

        Properties = new Dictionary<string, string>(
            properties, StringComparer.Ordinal);

        Tags = new Dictionary<string, string>(
            tags, StringComparer.Ordinal);
    }
}
