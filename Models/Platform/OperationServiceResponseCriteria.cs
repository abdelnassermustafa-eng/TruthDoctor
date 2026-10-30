using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable exact HTTP response-status expectation.
/// No status family is implicitly treated as success.
/// </summary>
public sealed class OperationServiceResponseCriteria
{
    public int ExpectedHttpStatusCode { get; }

    public OperationServiceResponseCriteria(int expectedHttpStatusCode)
    {
        if (expectedHttpStatusCode < 100 ||
            expectedHttpStatusCode > 599)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expectedHttpStatusCode),
                expectedHttpStatusCode,
                "HTTP status must be between 100 and 599.");
        }

        ExpectedHttpStatusCode = expectedHttpStatusCode;
    }
}
