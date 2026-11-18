using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327AAwsMultiServiceAdapterTests
{
    private const string Connection = "aws-test-connection";
    private const string Account = "123456789012";

    private static readonly string Identity =
        """{"Account":"123456789012","Arn":"arn:aws:iam::123456789012:role/test"}""";

    private static string Response(
        IReadOnlyList<string> args,
        string vpc = "vpc-001")
    {
        string command = string.Join(" ", args);

        if (command.Contains("get-caller-identity"))
            return Identity;

        if (command.Contains("describe-vpcs"))
            return "{\"Vpcs\":[{\"VpcId\":\""
                + vpc
                + "\",\"Tags\":[{\"Key\":\"Name\",\"Value\":\"Main Network\"}]}]}";

        if (command.Contains("describe-subnets"))
            return """{"Subnets":[{"SubnetId":"subnet-001"}]}""";

        if (command.Contains("describe-instances"))
            return """{"Reservations":[{"Instances":[{"InstanceId":"i-001"}]}]}""";

        if (command.Contains("describe-db-instances"))
            return """{"DBInstances":[{"DBInstanceIdentifier":"database-001"}]}""";

        if (command.Contains("list-functions"))
            return """{"Functions":[{"FunctionName":"function-001"}]}""";

        if (command.Contains("list-buckets"))
            return """{"Buckets":[{"Name":"bucket-001"}]}""";

        throw new InvalidOperationException(
            "Unexpected command: " + command);
    }

    private static AwsCliMultiServiceAdapter Create(
        Func<IReadOnlyList<string>, CancellationToken, Task<string>>
            runner,
        params string[] regions) =>
        new(
            Connection,
            "unit-test-profile",
            regions.Length == 0
                ? ["us-east-1"]
                : regions,
            runner);

    private static async Task Authenticate(
        AwsCliMultiServiceAdapter adapter) =>
        await adapter.AuthenticateAsync(
            Connection, CancellationToken.None);

    [Fact]
    public async Task AuthenticatesWithoutCredentialStorage()
    {
        var calls = new List<string>();

        var adapter = Create((args, _) =>
        {
            calls.Add(string.Join(" ", args));
            return Task.FromResult(Response(args));
        });

        await Authenticate(adapter);

        Assert.Single(calls);
        Assert.Contains("sts get-caller-identity", calls[0]);
        Assert.Contains("--profile unit-test-profile", calls[0]);
        Assert.Contains("--region us-east-1", calls[0]);
    }

    [Fact]
    public async Task DiscoversSixResourceFamilies()
    {
        var adapter = Create(
            (args, _) => Task.FromResult(Response(args)));

        await Authenticate(adapter);

        var result = await adapter.DiscoverInventoryAsync(
            Connection,
            AwsDiscoveryInventorySnapshot.Empty);

        Assert.Equal(6, result.Snapshot.Resources.Count);
        Assert.Equal(6, result.Snapshot.Coverage.Count);
        Assert.Equal(6, result.Added.Count);

        Assert.All(result.Snapshot.Coverage, scope =>
            Assert.Equal(
                AwsDiscoveryScanStatus.Success,
                scope.Status));

        Assert.Contains(
            result.Snapshot.Resources,
            x => x.Key.ResourceType == "compute-instance");

        Assert.Contains(
            result.Snapshot.Resources,
            x => x.Key.ResourceType == "database-instance");

        Assert.Contains(
            result.Snapshot.Resources,
            x => x.Key.ResourceType == "function");

        Assert.Contains(
            result.Snapshot.Resources,
            x => x.Key.ResourceType == "storage-bucket");
    }

    [Fact]
    public async Task ScansRegionalServicesPerRegionAndS3Once()
    {
        var calls = new List<string>();

        var adapter = Create(
            (args, _) =>
            {
                calls.Add(string.Join(" ", args));
                return Task.FromResult(Response(args));
            },
            "us-east-1",
            "us-west-2");

        await Authenticate(adapter);

        var result = await adapter.DiscoverInventoryAsync(
            Connection,
            AwsDiscoveryInventorySnapshot.Empty);

        Assert.Equal(11, result.Snapshot.Coverage.Count);

        Assert.Single(calls.Where(
            x => x.Contains("s3api list-buckets")));

        Assert.Equal(2, calls.Count(
            x => x.Contains("ec2 describe-vpcs")));

        Assert.Contains(
            result.Snapshot.Resources,
            x => x.Key.Scope.Region == "global");
    }

    [Fact]
    public async Task RebuiltResourceIdentityProducesRemoveAndAdd()
    {
        bool rebuilt = false;

        var adapter = Create((args, _) =>
            Task.FromResult(Response(
                args,
                rebuilt ? "vpc-new" : "vpc-old")));

        await Authenticate(adapter);

        var first = await adapter.DiscoverInventoryAsync(
            Connection,
            AwsDiscoveryInventorySnapshot.Empty);

        rebuilt = true;

        var second = await adapter.DiscoverInventoryAsync(
            Connection,
            first.Snapshot);

        Assert.Contains(
            second.Removed,
            x => x.Key.ResourceId == "vpc-old");

        Assert.Contains(
            second.Added,
            x => x.Key.ResourceId == "vpc-new");

        Assert.DoesNotContain(
            second.Snapshot.Resources,
            x => x.Key.ResourceId == "vpc-old");
    }

    [Fact]
    public async Task AccessDeniedPreservesPriorScope()
    {
        bool denied = false;

        var adapter = Create((args, _) =>
        {
            string command = string.Join(" ", args);

            if (denied && command.Contains("describe-vpcs"))
                throw new AwsDiscoveryAccessDeniedException();

            return Task.FromResult(Response(args));
        });

        await Authenticate(adapter);

        var first = await adapter.DiscoverInventoryAsync(
            Connection,
            AwsDiscoveryInventorySnapshot.Empty);

        denied = true;

        var second = await adapter.DiscoverInventoryAsync(
            Connection,
            first.Snapshot);

        Assert.Contains(
            second.Snapshot.Resources,
            x => x.Key.ResourceId == "vpc-001");

        Assert.DoesNotContain(
            second.Removed,
            x => x.Key.ResourceId == "vpc-001");

        Assert.Contains(
            second.Snapshot.Coverage,
            x => x.Scope.Service == "ec2:vpc"
                && x.Status == AwsDiscoveryScanStatus.AccessDenied);
    }

    [Fact]
    public async Task NeutralDiscoveryFailsClosedOnPartialScan()
    {
        var adapter = Create((args, _) =>
        {
            if (string.Join(" ", args).Contains("list-functions"))
                throw new AwsDiscoveryAccessDeniedException();

            return Task.FromResult(Response(args));
        });

        await Authenticate(adapter);

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await adapter.DiscoverAsync(
                Connection, CancellationToken.None));

        Assert.NotNull(adapter.LastTransition);
        Assert.Contains(
            adapter.LastTransition!.Snapshot.Coverage,
            x => x.Status == AwsDiscoveryScanStatus.AccessDenied);
    }

    [Fact]
    public async Task RejectsUnknownConnectionAndUnauthenticatedDiscovery()
    {
        int calls = 0;

        var adapter = Create((args, _) =>
        {
            calls++;
            return Task.FromResult(Response(args));
        });

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await adapter.DiscoverInventoryAsync(
                Connection,
                AwsDiscoveryInventorySnapshot.Empty));

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await adapter.AuthenticateAsync(
                "another-connection",
                CancellationToken.None));

        Assert.Equal(0, calls);
    }
}
