using System;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationIntentTests
{
    private static UniversalCapabilityIdentity Start =>
        new("compute.instance.start");

    private static UniversalCapabilityIdentity Stop =>
        new("compute.instance.stop");

    [Fact]
    public void StoresCapabilityDescriptionAndReason()
    {
        var intent = new OperationIntent(
            Start, "Start instance", "Scheduled maintenance");

        Assert.Equal(Start, intent.Capability);
        Assert.Equal("Start instance", intent.Description);
        Assert.Equal("Scheduled maintenance", intent.Reason);
    }

    [Fact]
    public void RejectsDefaultCapabilityIdentity()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationIntent(default, "Start instance"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RejectsNullOrBlankDescription(string? description)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationIntent(Start, description!));
    }

    [Fact]
    public void TrimsDescriptionAndPreservesCase()
    {
        var intent = new OperationIntent(
            Start, "  Start EC2 Instance  ");

        Assert.Equal("Start EC2 Instance", intent.Description);
    }

    [Theory]
    [InlineData("Start\ninstance")]
    [InlineData("Start\tinstance")]
    [InlineData("Start\0instance")]
    public void RejectsEmbeddedDescriptionControlCharacters(
        string description)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationIntent(Start, description));
    }

    [Fact]
    public void AllowsOmittedReason()
    {
        var intent = new OperationIntent(Start, "Start instance");

        Assert.Null(intent.Reason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizesBlankReasonToNull(string? reason)
    {
        var intent = new OperationIntent(
            Start, "Start instance", reason);

        Assert.Null(intent.Reason);
    }

    [Fact]
    public void TrimsReasonAndPreservesCase()
    {
        var intent = new OperationIntent(
            Start, "Start instance", "  Planned MAINTENANCE  ");

        Assert.Equal("Planned MAINTENANCE", intent.Reason);
    }

    [Theory]
    [InlineData("Needs\napproval")]
    [InlineData("Needs\tapproval")]
    [InlineData("Needs\0approval")]
    public void RejectsEmbeddedReasonControlCharacters(string reason)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationIntent(Start, "Start instance", reason));
    }

    [Fact]
    public void PreservesDistinctCapabilityIdentities()
    {
        var start = new OperationIntent(Start, "Start instance");
        var stop = new OperationIntent(Stop, "Stop instance");

        Assert.NotEqual(start.Capability, stop.Capability);
        Assert.Equal(Start, start.Capability);
        Assert.Equal(Stop, stop.Capability);
    }

    [Fact]
    public void ExposesReadOnlyProperties()
    {
        var type = typeof(OperationIntent);

        Assert.True(type.IsSealed);

        foreach (var name in new[]
        {
            "Capability", "Description", "Reason"
        })
        {
            var property = type.GetProperty(
                name, BindingFlags.Instance | BindingFlags.Public);

            Assert.NotNull(property);
            Assert.NotNull(property!.GetMethod);
            Assert.Null(property.SetMethod);
        }

        var properties = type.GetProperties(
            BindingFlags.Instance | BindingFlags.Public);

        Assert.Equal(3, properties.Length);
    }

    [Fact]
    public void DoesNotRequireProviderOrResourceTarget()
    {
        var intent = new OperationIntent(
            new UniversalCapabilityIdentity("storage.volume.snapshot"),
            "Create a volume snapshot");

        Assert.Equal(
            "storage.volume.snapshot",
            intent.Capability.Value);
        Assert.Equal(
            "Create a volume snapshot",
            intent.Description);

        var constructor = Assert.Single(
            typeof(OperationIntent).GetConstructors());

        var parameters = constructor.GetParameters();

        Assert.Equal(3, parameters.Length);
        Assert.Equal("capability", parameters[0].Name);
        Assert.Equal("description", parameters[1].Name);
        Assert.Equal("reason", parameters[2].Name);
        Assert.True(parameters[2].IsOptional);
    }
}
