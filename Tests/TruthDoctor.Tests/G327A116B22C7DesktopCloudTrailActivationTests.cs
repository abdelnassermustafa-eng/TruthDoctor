using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A116B22C7DesktopCloudTrailActivationTests
{
    [Fact]
    public void DesktopSessionUsesExplicitOptInWithoutExecutingAws()
    {
        int runnerCalls = 0;

        Task<string> Runner(
            IReadOnlyList<string> arguments,
            CancellationToken cancellationToken)
        {
            runnerCalls++;
            return Task.FromResult("{}");
        }

        string? environmentValue =
            Environment.GetEnvironmentVariable(
                "TRUTHDOCTOR_AWS_CLOUDTRAIL");

        bool expectedEnabled = string.Equals(
            environmentValue,
            "1",
            StringComparison.Ordinal);

        var defaultSession = new StandaloneAwsDesktopSession(
            "synthetic-profile",
            new[] { "us-east-1" },
            Runner);

        var catalogSession = new StandaloneAwsDesktopSession(
            "synthetic-profile",
            new[] { "us-east-1" },
            Runner,
            enableRegionCatalog: true);

        Assert.Equal(
            expectedEnabled,
            ReadCloudTrailFlag(defaultSession));

        Assert.Equal(
            expectedEnabled,
            ReadCloudTrailFlag(catalogSession));

        var directAdapter = new AwsCliMultiServiceAdapter(
            "synthetic-connection",
            "synthetic-profile",
            new[] { "us-east-1" },
            Runner);

        Assert.False(
            ReadAdapterCloudTrailFlag(directAdapter));

        Assert.Equal(0, runnerCalls);
    }

    private static bool ReadCloudTrailFlag(
        StandaloneAwsDesktopSession session)
    {
        FieldInfo? adapterField =
            typeof(StandaloneAwsDesktopSession).GetField(
                "_adapter",
                BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(adapterField);

        var adapter = Assert.IsType<AwsCliMultiServiceAdapter>(
            adapterField!.GetValue(session));

        return ReadAdapterCloudTrailFlag(adapter);
    }

    private static bool ReadAdapterCloudTrailFlag(
        AwsCliMultiServiceAdapter adapter)
    {
        FieldInfo? flagField =
            typeof(AwsCliMultiServiceAdapter).GetField(
                "_enableCloudTrailCreationEvidence",
                BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(flagField);

        return Assert.IsType<bool>(
            flagField!.GetValue(adapter));
    }
}
