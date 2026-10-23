using System;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationOutcomeConditionTests
{
    [Fact]
    public void StoresNormalizedKeyAndExpectedValue()
    {
        var condition = new OperationOutcomeCondition(
            " resource.state ", " Running ");

        Assert.Equal("resource.state", condition.Key);
        Assert.Equal("Running", condition.ExpectedValue);
    }

    [Fact]
    public void PreservesOriginalCase()
    {
        var condition = new OperationOutcomeCondition(
            "Resource.State", "Running");

        Assert.Equal("Resource.State", condition.Key);
        Assert.Equal("Running", condition.ExpectedValue);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void RejectsNullOrBlankKeys(string? key)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationOutcomeCondition(key!, "running"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void RejectsNullOrBlankExpectedValues(string? value)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationOutcomeCondition("resource.state", value!));
    }

    [Theory]
    [InlineData("resource.\nstate")]
    [InlineData("resource.\rstate")]
    [InlineData("resource.\0state")]
    public void RejectsEmbeddedControlCharactersInKeys(string key)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationOutcomeCondition(key, "running"));
    }

    [Theory]
    [InlineData("run\nning")]
    [InlineData("run\rning")]
    [InlineData("run\0ning")]
    public void RejectsEmbeddedControlCharactersInValues(string value)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationOutcomeCondition("resource.state", value));
    }

    [Fact]
    public void ExposesExactlyTwoReadOnlyProperties()
    {
        var type = typeof(OperationOutcomeCondition);

        Assert.True(type.IsSealed);

        var properties = type.GetProperties(
            BindingFlags.Instance | BindingFlags.Public);

        Assert.Equal(2, properties.Length);

        foreach (var name in new[] { "Key", "ExpectedValue" })
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
        Assert.Equal("key", parameters[0].Name);
        Assert.Equal("expectedValue", parameters[1].Name);
        Assert.All(parameters, parameter =>
        {
            Assert.Equal(typeof(string), parameter.ParameterType);
            Assert.False(parameter.IsOptional);
        });
    }
}
