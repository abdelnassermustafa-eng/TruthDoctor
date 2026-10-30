using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable normalized HTTP/HTTPS endpoint identity.
/// Parsing does not perform DNS resolution or network access.
/// </summary>
public sealed class OperationServiceEndpoint
{
    public string Uri { get; }

    public OperationServiceEndpoint(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            throw new ArgumentException(
                "An absolute HTTP endpoint is required.",
                nameof(uri));
        }

        var value = uri.Trim();

        foreach (var character in value)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "Endpoint cannot contain control characters.",
                    nameof(uri));
            }
        }

        if (!System.Uri.TryCreate(
                value, UriKind.Absolute, out var parsed) ||
            (parsed.Scheme != System.Uri.UriSchemeHttp &&
             parsed.Scheme != System.Uri.UriSchemeHttps) ||
            string.IsNullOrWhiteSpace(parsed.Host) ||
            parsed.UserInfo.Length != 0 ||
            parsed.Fragment.Length != 0)
        {
            throw new ArgumentException(
                "Endpoint must be an absolute HTTP/HTTPS URI " +
                "without credentials or fragments.",
                nameof(uri));
        }

        // AbsoluteUri provides deterministic URI normalization,
        // including host/scheme case and default-port handling.
        // No request is made and no host is resolved.
        Uri = parsed.AbsoluteUri;
    }
}
