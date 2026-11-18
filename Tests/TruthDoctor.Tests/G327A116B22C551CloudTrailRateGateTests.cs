using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A116B22C551CloudTrailRateGateTests
{
    private sealed class FakeClock
    {
        public DateTimeOffset Now { get; private set; } =
            new(2026, 11, 16, 12, 0, 0, TimeSpan.Zero);

        public List<TimeSpan> Waits { get; } = new();

        public Task Delay(
            TimeSpan span,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Waits.Add(span);
            Now += span;

            return Task.CompletedTask;
        }

        public AwsCloudTrailRegionalRateGate CreateGate()
            => new(() => Now, Delay);
    }

    private static Task<string> Success(
        CancellationToken _)
        => Task.FromResult("OK");

    [Fact]
    public async Task FirstRequestRunsWithoutDelay()
    {
        var clock = new FakeClock();
        var gate = clock.CreateGate();

        string result = await gate.ExecuteAsync(
            "111122223333",
            "us-east-1",
            Success);

        Assert.Equal("OK", result);
        Assert.Empty(clock.Waits);
    }

    [Fact]
    public async Task ConsecutiveSameScopeCallsAreSpaced()
    {
        var clock = new FakeClock();
        var gate = clock.CreateGate();

        await gate.ExecuteAsync(
            "111122223333", "us-east-1", Success);

        await gate.ExecuteAsync(
            "111122223333", "us-east-1", Success);

        await gate.ExecuteAsync(
            "111122223333", "us-east-1", Success);

        Assert.Equal(2, clock.Waits.Count);

        Assert.All(
            clock.Waits,
            value => Assert.Equal(
                TimeSpan.FromMilliseconds(500),
                value));
    }

    [Fact]
    public async Task DifferentAccountsAndRegionsHaveSeparateBudgets()
    {
        var clock = new FakeClock();
        var gate = clock.CreateGate();

        await gate.ExecuteAsync(
            "111122223333", "us-east-1", Success);

        await gate.ExecuteAsync(
            "111122223333", "us-west-2", Success);

        await gate.ExecuteAsync(
            "999988887777", "us-east-1", Success);

        Assert.Empty(clock.Waits);
    }

    [Fact]
    public async Task FailedRequestStillConsumesItsRateSlot()
    {
        var clock = new FakeClock();
        var gate = clock.CreateGate();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => gate.ExecuteAsync<string>(
                "111122223333",
                "us-east-1",
                _ => throw new InvalidOperationException(
                    "Synthetic failure")));

        await gate.ExecuteAsync(
            "111122223333",
            "us-east-1",
            Success);

        Assert.Single(clock.Waits);

        Assert.Equal(
            TimeSpan.FromMilliseconds(500),
            clock.Waits[0]);
    }

    [Fact]
    public async Task CancellationStopsOperationBeforeInvocation()
    {
        var clock = new FakeClock();
        var gate = clock.CreateGate();

        int calls = 0;

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => gate.ExecuteAsync(
                "111122223333",
                "us-east-1",
                _ =>
                {
                    calls++;
                    return Task.FromResult("UNEXPECTED");
                },
                cts.Token));

        Assert.Equal(0, calls);
        Assert.Empty(clock.Waits);
    }

    [Fact]
    public async Task SameScopeOperationsAreSerialized()
    {
        var clock = new FakeClock();
        var gate = clock.CreateGate();

        int calls = 0;

        var release = new TaskCompletionSource<string>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        Task<string> first = gate.ExecuteAsync(
            "111122223333",
            "us-east-1",
            _ =>
            {
                calls++;
                return release.Task;
            });

        Task<string> second = gate.ExecuteAsync(
            "111122223333",
            "us-east-1",
            _ =>
            {
                calls++;
                return Task.FromResult("SECOND");
            });

        Assert.Equal(1, calls);
        Assert.False(second.IsCompleted);

        release.SetResult("FIRST");

        Assert.Equal("FIRST", await first);
        Assert.Equal("SECOND", await second);

        Assert.Equal(2, calls);
        Assert.Single(clock.Waits);
    }

    [Fact]
    public async Task InvalidIdentityIsRejectedWithoutCallingRunner()
    {
        var clock = new FakeClock();
        var gate = clock.CreateGate();

        int calls = 0;

        Task<string> Operation(CancellationToken _)
        {
            calls++;
            return Task.FromResult("UNEXPECTED");
        }

        await Assert.ThrowsAsync<ArgumentException>(
            () => gate.ExecuteAsync(
                "invalid",
                "us-east-1",
                Operation));

        await Assert.ThrowsAsync<ArgumentException>(
            () => gate.ExecuteAsync(
                "111122223333",
                "us east 1",
                Operation));

        await Assert.ThrowsAsync<ArgumentException>(
            () => gate.ExecuteAsync(
                "111122223333",
                "US-EAST-1",
                Operation));

        Assert.Equal(0, calls);
    }
}
