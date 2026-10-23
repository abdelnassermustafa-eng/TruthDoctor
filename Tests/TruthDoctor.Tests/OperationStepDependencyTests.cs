using System;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationStepDependencyTests
{
    [Fact]
    public void StoresNormalizedDirectedStepReferences()
    {
        var dependency = new OperationStepDependency(
            " step-3 ", " step-1 ");

        Assert.Equal("step-3", dependency.StepId);
        Assert.Equal("step-1", dependency.PrerequisiteStepId);
    }

    [Fact]
    public void PreservesBothIdentifierCases()
    {
        var dependency = new OperationStepDependency(
            "Step-Three", "Step-One");

        Assert.Equal("Step-Three", dependency.StepId);
        Assert.Equal("Step-One", dependency.PrerequisiteStepId);
    }

    [Fact]
    public void RejectsIdenticalSelfDependency()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationStepDependency("step-1", "step-1"));
    }

    [Fact]
    public void RejectsCaseInsensitiveSelfDependency()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationStepDependency("Step-A", "step-a"));
    }

    [Fact]
    public void RejectsSelfDependencyAfterNormalization()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationStepDependency(" step-1 ", "step-1"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void RejectsBlankDependentStepIds(string? stepId)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationStepDependency(stepId!, "step-1"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void RejectsBlankPrerequisiteStepIds(string? prerequisite)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationStepDependency("step-2", prerequisite!));
    }

    [Theory]
    [InlineData("step\none")]
    [InlineData("step\rone")]
    [InlineData("step\0one")]
    public void RejectsEmbeddedControlsInDependentId(string stepId)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationStepDependency(stepId, "step-1"));
    }

    [Theory]
    [InlineData("step\none")]
    [InlineData("step\rone")]
    [InlineData("step\0one")]
    public void RejectsEmbeddedControlsInPrerequisiteId(
        string prerequisite)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationStepDependency("step-2", prerequisite));
    }

    [Fact]
    public void ExposesExactlyTwoReadOnlyPropertiesAndOneConstructor()
    {
        var type = typeof(OperationStepDependency);

        Assert.True(type.IsSealed);

        var properties = type.GetProperties(
            BindingFlags.Instance | BindingFlags.Public);

        Assert.Equal(2, properties.Length);

        foreach (var name in new[] { "StepId", "PrerequisiteStepId" })
        {
            var property = type.GetProperty(name);

            Assert.NotNull(property);
            Assert.Equal(typeof(string), property!.PropertyType);
            Assert.NotNull(property.GetMethod);
            Assert.Null(property.SetMethod);
        }

        var constructor = Assert.Single(type.GetConstructors());
        var parameters = constructor.GetParameters();

        Assert.Equal(2, parameters.Length);
        Assert.Equal("stepId", parameters[0].Name);
        Assert.Equal("prerequisiteStepId", parameters[1].Name);

        Assert.All(parameters, parameter =>
        {
            Assert.Equal(typeof(string), parameter.ParameterType);
            Assert.False(parameter.IsOptional);
        });
    }
}
