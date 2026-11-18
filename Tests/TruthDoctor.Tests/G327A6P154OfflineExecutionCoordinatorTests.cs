using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Controllers.Workbench;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A6P154OfflineExecutionCoordinatorTests
{
    private static async Task<string> ExecuteAsync(
        bool inProcessRequested,
        bool offlineLocalDemoAuthorized,
        Func<CancellationToken, Task> offlineDiscovery,
        CancellationToken cancellationToken = default)
    {
        Assembly assembly =
            typeof(DesktopInProcessExecutionPolicy).Assembly;

        Type? coordinator = assembly.GetType(
            "TruthDoctor.Controllers.Workbench."
            + "DesktopOfflineExecutionCoordinator",
            throwOnError: false);

        if (coordinator is null)
        {
            Assert.Fail(
                "RED: offline desktop execution coordinator "
                + "is not implemented.");
        }

        MethodInfo? execute = coordinator.GetMethod(
            "ExecuteAsync",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types:
            [
                typeof(bool),
                typeof(bool),
                typeof(Func<CancellationToken, Task>),
                typeof(CancellationToken)
            ],
            modifiers: null);

        Assert.NotNull(execute);

        object? invocation = execute.Invoke(
            null,
            [
                inProcessRequested,
                offlineLocalDemoAuthorized,
                offlineDiscovery,
                cancellationToken
            ]);

        Task task = Assert.IsAssignableFrom<Task>(invocation);

        await task;

        PropertyInfo? resultProperty =
            task.GetType().GetProperty("Result");

        Assert.NotNull(resultProperty);

        object? result = resultProperty.GetValue(task);

        Assert.NotNull(result);

        Assert.True(
            result.GetType().IsEnum,
            "Coordinator must return a typed enum outcome.");

        string? name = Enum.GetName(
            result.GetType(),
            result);

        Assert.NotNull(name);

        return name;
    }

    [Theory]
    [InlineData(false, false, "LegacyHttp", 0)]
    [InlineData(false, true, "LegacyHttp", 0)]
    [InlineData(true, false, "FailClosed", 0)]
    [InlineData(true, true, "OfflineLocalDemo", 1)]
    public async Task ResolvesAuthorizationAndDispatch(
        bool inProcessRequested,
        bool offlineLocalDemoAuthorized,
        string expectedOutcome,
        int expectedCalls)
    {
        int calls = 0;

        string actual = await ExecuteAsync(
            inProcessRequested,
            offlineLocalDemoAuthorized,
            _ =>
            {
                calls++;
                return Task.CompletedTask;
            });

        Assert.Equal(expectedOutcome, actual);
        Assert.Equal(expectedCalls, calls);
    }

    [Fact]
    public async Task OfflineDelegateRunsExactlyOnce()
    {
        int calls = 0;

        string actual = await ExecuteAsync(
            true,
            true,
            _ =>
            {
                calls++;
                return Task.CompletedTask;
            });

        Assert.Equal("OfflineLocalDemo", actual);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task OfflineFailurePropagatesWithoutRetry()
    {
        int calls = 0;

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () =>
                {
                    await ExecuteAsync(
                        true,
                        true,
                        _ =>
                        {
                            calls++;

                            return Task.FromException(
                                new InvalidOperationException(
                                    "synthetic offline discovery failed"));
                        });
                });

        Assert.Equal(
            "synthetic offline discovery failed",
            exception.Message);

        Assert.Equal(1, calls);
    }
}
