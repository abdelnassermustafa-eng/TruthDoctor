using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationAuthorizationConditionTests
{
    [Fact]
    public void StoresExactConditionValues()
    {
        var condition = new OperationAuthorizationCondition(
            "environment.name",
            "Production");

        Assert.Equal("environment.name", condition.Key);
        Assert.Equal("Production", condition.ExpectedValue);
    }

    [Fact]
    public void TrimsSurroundingWhitespace()
    {
        var condition = new OperationAuthorizationCondition(
            "  environment.name  ",
            "  Production  ");

        Assert.Equal("environment.name", condition.Key);
        Assert.Equal("Production", condition.ExpectedValue);
    }

    [Fact]
    public void PreservesOriginalCase()
    {
        var condition = new OperationAuthorizationCondition(
            "Environment.Name",
            "ProDUCTION");

        Assert.Equal("Environment.Name", condition.Key);
        Assert.Equal("ProDUCTION", condition.ExpectedValue);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void RejectsNullOrBlankKeys(string? key)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationAuthorizationCondition(
                key!,
                "Production"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void RejectsNullOrBlankExpectedValues(string? value)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationAuthorizationCondition(
                "environment.name",
                value!));
    }

    [Theory]
    [InlineData("environment\nname")]
    [InlineData("environment\rname")]
    [InlineData("environment\0name")]
    [InlineData("environment\tname")]
    public void RejectsEmbeddedControlCharactersInKeys(string key)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationAuthorizationCondition(
                key,
                "Production"));
    }

    [Theory]
    [InlineData("Prod\nuction")]
    [InlineData("Prod\ruction")]
    [InlineData("Prod\0uction")]
    [InlineData("Prod\tuction")]
    public void RejectsEmbeddedControlCharactersInValues(string value)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationAuthorizationCondition(
                "environment.name",
                value));
    }

    [Fact]
    public void DifferentKeysRemainDistinct()
    {
        var first = new OperationAuthorizationCondition(
            "environment.name",
            "Production");

        var second = new OperationAuthorizationCondition(
            "environment.region",
            "Production");

        Assert.NotEqual(first.Key, second.Key);
    }

    [Fact]
    public void DifferentExpectedValuesRemainDistinct()
    {
        var first = new OperationAuthorizationCondition(
            "environment.name",
            "Production");

        var second = new OperationAuthorizationCondition(
            "environment.name",
            "Staging");

        Assert.NotEqual(
            first.ExpectedValue,
            second.ExpectedValue);
    }

    [Fact]
    public void ExposesExactlyTwoGetterOnlyProperties()
    {
        var type = typeof(OperationAuthorizationCondition);

        Assert.True(type.IsPublic);
        Assert.True(type.IsSealed);

        var properties = type.GetProperties(
            BindingFlags.Public | BindingFlags.Instance);

        Assert.Equal(2, properties.Length);

        Assert.Equal(
            new[] { "ExpectedValue", "Key" },
            properties.Select(p => p.Name)
                .OrderBy(n => n)
                .ToArray());

        foreach (var property in properties)
        {
            Assert.Equal(typeof(string), property.PropertyType);
            Assert.True(property.CanRead);
            Assert.False(property.CanWrite);
        }
    }

    [Fact]
    public void HasOneTwoArgumentConstructor()
    {
        var constructors = typeof(OperationAuthorizationCondition)
            .GetConstructors(
                BindingFlags.Public | BindingFlags.Instance);

        var constructor = Assert.Single(constructors);

        var parameters = constructor.GetParameters();

        Assert.Equal(2, parameters.Length);

        Assert.Equal("key", parameters[0].Name);
        Assert.Equal(typeof(string), parameters[0].ParameterType);

        Assert.Equal("expectedValue", parameters[1].Name);
        Assert.Equal(typeof(string), parameters[1].ParameterType);
    }

    [Fact]
    public void DoesNotExposeAuthorizationOrExecutionMethods()
    {
        var methods = typeof(OperationAuthorizationCondition)
            .GetMethods(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName);

        Assert.Empty(methods);
    }
}
