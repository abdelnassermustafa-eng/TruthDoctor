using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using TruthDoctor.State;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327AAwsDesktopConnectionTests
{
    private static string Response(
        IReadOnlyList<string> arguments)
    {
        string command = string.Join(" ", arguments);

        if (command.Contains("get-caller-identity"))
        {
            return """
                {"Account":"123456789012","Arn":"arn:aws:iam::123456789012:role/desktop-test"}
                """;
        }

        if (command.Contains("describe-vpcs"))
        {
            return """
                {"Vpcs":[{"VpcId":"vpc-desktop-001"}]}
                """;
        }

        if (command.Contains("describe-subnets"))
        {
            return """
                {"Subnets":[{"SubnetId":"subnet-desktop-001"}]}
                """;
        }

        if (command.Contains("describe-instances"))
        {
            return """{"Reservations":[]}""";
        }

        if (command.Contains("describe-db-instances"))
        {
            return """{"DBInstances":[]}""";
        }

        if (command.Contains("list-functions"))
        {
            return """{"Functions":[]}""";
        }

        if (command.Contains("list-buckets"))
        {
            return """{"Buckets":[]}""";
        }

        throw new InvalidOperationException(
            "Unrecognized test command");
    }

    private static StandaloneAwsDesktopSession Create(
        Func<IReadOnlyList<string>,
            CancellationToken, Task<string>> runner) =>
        new(
            "unit-test-profile",
            ["us-east-1"],
            runner);

    [Fact]
    public void ConstructingSessionDoesNotContactAws()
    {
        int calls = 0;

        _ = Create((_, _) =>
        {
            calls++;
            throw new InvalidOperationException(
                "No call was expected");
        });

        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task DiscoversResourcesIntoRealWorkbench()
    {
        var calls = new List<string>();

        var session = Create((args, _) =>
        {
            calls.Add(string.Join(" ", args));
            return Task.FromResult(Response(args));
        });

        var workbench = new WorkbenchState();

        await session.DiscoverAsync(workbench);

        Assert.NotNull(workbench.PlatformState);
        Assert.NotNull(workbench.InfrastructureGraph);
        Assert.Equal(
            2,
            workbench.PlatformState!.Resources.Count);

        Assert.Equal(
            7,
            calls.Count);

        Assert.Contains(
            calls,
            call => call.Contains("sts get-caller-identity"));

        Assert.Contains(
            calls,
            call => call.Contains("ec2 describe-vpcs"));

        Assert.Contains(
            calls,
            call => call.Contains("ec2 describe-subnets"));
    }

    [Fact]
    public async Task IncompleteAwsScanDoesNotReplaceWorkbench()
    {
        var session = Create((args, _) =>
        {
            if (string.Join(" ", args).Contains(
                "describe-vpcs"))
            {
                throw new AwsDiscoveryAccessDeniedException();
            }

            return Task.FromResult(Response(args));
        });

        var workbench = new WorkbenchState();

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await session.DiscoverAsync(workbench));

        Assert.Null(workbench.PlatformState);
        Assert.Null(workbench.InfrastructureGraph);
    }
}
