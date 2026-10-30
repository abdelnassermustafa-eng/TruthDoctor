using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable declared service-check specification.
/// Only GET and HEAD are supported as initial HTTP methods.
/// This type does not execute application requests.
/// </summary>
public sealed class OperationServiceCheckSpecification
{
    public OperationServiceProtocol Protocol { get; }

    public OperationServiceEndpoint Endpoint { get; }

    public string Method { get; }

    public OperationServiceResponseCriteria Criteria { get; }

    public OperationServiceCheckSpecification(
        OperationServiceProtocol protocol,
        OperationServiceEndpoint endpoint,
        string method,
        OperationServiceResponseCriteria criteria)
    {
        ArgumentNullException.ThrowIfNull(protocol);
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(criteria);

        if (string.IsNullOrWhiteSpace(method))
        {
            throw new ArgumentException(
                "HTTP method is required.",
                nameof(method));
        }

        var normalized = method.Trim().ToUpperInvariant();

        if (!string.Equals(normalized, "GET", StringComparison.Ordinal) &&
            !string.Equals(normalized, "HEAD", StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Only GET and HEAD are supported.",
                nameof(method));
        }

        Protocol = protocol;
        Endpoint = endpoint;
        Method = normalized;
        Criteria = criteria;
    }
}
