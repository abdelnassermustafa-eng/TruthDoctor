using System;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationPlannedStepTests
{
    private static UniversalCapabilityIdentity Start =>
        new("compute.instance.start");

    private static OperationTarget SampleTarget() =>
        new("aws", "account-1", "us-east-1",
            "compute", "instance", "i-123");

    [Fact]
    public void StoresNormalizedStepIdentityCapabilityAndDescription()
    {
        var step = new OperationPlannedStep(
            " step-1 ", Start, " Start instance ");

        Assert.Equal("step-1", step.StepId);
        Assert.Equal(Start, step.Capability);
        Assert.Equal("Start instance", step.Description);
        Assert.Null(step.Target);
    }

    [Fact]
    public void PreservesStepIdentityAndDescriptionCase()
    {
        var step = new OperationPlannedStep(
            "Step-A", Start, "Start Instance");

        Assert.Equal("Step-A", step.StepId);
        Assert.Equal("Start Instance", step.Description);
    }

    [Fact]
    public void AllowsOmittedTarget()
    {
        var step = new OperationPlannedStep(
            "step-1", Start, "Start instance");

        Assert.Null(step.Target);
    }

    [Fact]
    public void PreservesSuppliedTargetReference()
    {
        var target = SampleTarget();

        var step = new OperationPlannedStep(
            "step-1", Start, "Start instance", target);

        Assert.Same(target, step.Target);
        Assert.Equal("i-123", step.Target!.NativeId);
    }

    [Fact]
    public void RejectsDefaultCapabilityIdentity()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationPlannedStep(
                "step-1", default, "Start instance"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void RejectsNullOrBlankStepIds(string? stepId)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationPlannedStep(
                stepId!, Start, "Start instance"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void RejectsNullOrBlankDescriptions(string? description)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationPlannedStep(
                "step-1", Start, description!));
    }

    [Theory]
    [InlineData("step\none")]
    [InlineData("step\rone")]
    [InlineData("step\0one")]
    public void RejectsEmbeddedControlCharactersInStepIds(string stepId)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationPlannedStep(
                stepId, Start, "Start instance"));
    }

    [Theory]
    [InlineData("Start\ninstance")]
    [InlineData("Start\rinstance")]
    [InlineData("Start\0instance")]
    public void RejectsEmbeddedControlCharactersInDescriptions(
        string description)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationPlannedStep(
                "step-1", Start, description));
    }

    [Fact]
    public void ExposesExactlyFourReadOnlyPropertiesAndOneConstructor()
    {
        var type = typeof(OperationPlannedStep);

        Assert.True(type.IsSealed);

        var properties = type.GetProperties(
            BindingFlags.Instance | BindingFlags.Public);

        Assert.Equal(4, properties.Length);

        foreach (var (name, expectedType) in new[]
        {
            ("StepId", typeof(string)),
            ("Capability", typeof(UniversalCapabilityIdentity)),
            ("Description", typeof(string)),
            ("Target", typeof(OperationTarget))
        })
        {
            var property = type.GetProperty(name);

            Assert.NotNull(property);
            Assert.Equal(expectedType, property!.PropertyType);
            Assert.NotNull(property.GetMethod);
            Assert.Null(property.SetMethod);
        }

        var constructor = Assert.Single(type.GetConstructors());
        var parameters = constructor.GetParameters();

        Assert.Equal(4, parameters.Length);
        Assert.Equal("stepId", parameters[0].Name);
        Assert.Equal("capability", parameters[1].Name);
        Assert.Equal("description", parameters[2].Name);
        Assert.Equal("target", parameters[3].Name);

        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal(
            typeof(UniversalCapabilityIdentity),
            parameters[1].ParameterType);
        Assert.Equal(typeof(string), parameters[2].ParameterType);
        Assert.Equal(typeof(OperationTarget), parameters[3].ParameterType);

        Assert.All(parameters[..3], parameter =>
            Assert.False(parameter.IsOptional));

        Assert.True(parameters[3].IsOptional);
        Assert.Null(parameters[3].DefaultValue);
    }
}
