using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace TruthDoctor.Services.Standalone;

/// <summary>
/// Serializes and spaces CloudTrail requests per AWS account
/// and Region. Callers must share a single gate instance
/// across all CloudTrail lookups in the same application scope.
/// This component never calls AWS directly.
/// </summary>
public sealed class AwsCloudTrailRegionalRateGate
{
    private sealed class ScopeState
    {
        public SemaphoreSlim Lock { get; } = new(1, 1);

        public DateTimeOffset NextAllowedAt { get; set; }
    }

    private static readonly TimeSpan MinimumInterval =
        TimeSpan.FromMilliseconds(500);

    private readonly ConcurrentDictionary<
        (string Account, string Region),
        ScopeState> _scopes = new();

    private readonly Func<DateTimeOffset> _clock;

    private readonly Func<
        TimeSpan, CancellationToken, Task> _delay;

    public AwsCloudTrailRegionalRateGate(
        Func<DateTimeOffset>? clock = null,
        Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        _clock = clock ?? (() => DateTimeOffset.UtcNow);

        _delay = delay ?? ((wait, token) =>
            Task.Delay(wait, token));
    }

    public async Task<T> ExecuteAsync<T>(
        string accountId,
        string region,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        cancellationToken.ThrowIfCancellationRequested();

        if (accountId is null ||
            accountId.Length != 12 ||
            !ValidAccount(accountId) ||
            !ValidRegion(region))
        {
            throw new ArgumentException(
                "CloudTrail account or Region is invalid.");
        }

        ScopeState scope = _scopes.GetOrAdd(
            (accountId, region),
            static _ => new ScopeState());

        await scope.Lock.WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            DateTimeOffset now = _clock();

            TimeSpan wait = scope.NextAllowedAt - now;

            if (wait > TimeSpan.Zero)
            {
                await _delay(wait, cancellationToken)
                    .ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Never move a reserved slot backward,
            // even if wall-clock time moves backward.
            DateTimeOffset observed = _clock();

            DateTimeOffset slot =
                observed > scope.NextAllowedAt
                    ? observed
                    : scope.NextAllowedAt;

            scope.NextAllowedAt =
                slot + MinimumInterval;

            // A failed operation consumes its rate slot.
            // This prevents an immediate retry burst.
            return await operation(cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            scope.Lock.Release();
        }
    }

    private static bool ValidAccount(string value)
    {
        foreach (char c in value)
        {
            if (!char.IsAsciiDigit(c))
                return false;
        }

        return true;
    }

    private static bool ValidRegion(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.Length > 64)
        {
            return false;
        }

        foreach (char c in value)
        {
            // Explicit ASCII comparison is supported by .NET 10.
            bool asciiLower = c is >= 'a' and <= 'z';

            if (!asciiLower &&
                !char.IsAsciiDigit(c) &&
                c != '-')
            {
                return false;
            }
        }

        return true;
    }
}
