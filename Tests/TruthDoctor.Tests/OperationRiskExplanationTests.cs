using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationRiskExplanationTests
{
    [Fact]
    public void TypeIsPublicAndSealed()
    {
        var type = typeof(OperationRiskExplanation);

        Assert.True(type.IsPublic);
        Assert.True(type.IsSealed);
        Assert.Equal("TruthDoctor.Models.Platform", type.Namespace);
    }

    [Fact]
    public void PublicApiHasExactlyTwoGetterOnlyProperties()
    {
        var type = typeof(OperationRiskExplanation);
        var properties = type.GetProperties(
            BindingFlags.Public | BindingFlags.Instance);

        Assert.Equal(
            new[] { "DeclaredRiskLevel", "Explanation" },
            properties.Select(p => p.Name).OrderBy(n => n));

        Assert.All(properties, property =>
        {
            Assert.NotNull(property.GetMethod);
            Assert.Null(property.SetMethod);
        });

        Assert.Equal(
            typeof(string),
            type.GetProperty("Explanation")!.PropertyType);

        Assert.Equal(
            typeof(OperationRiskLevel?),
            type.GetProperty("DeclaredRiskLevel")!.PropertyType);
    }

    [Fact]
    public void ConstructorHasExactlyTwoOptionalParameters()
    {
        var constructor = Assert.Single(
            typeof(OperationRiskExplanation).GetConstructors());

        var parameters = constructor.GetParameters();

        Assert.Equal(2, parameters.Length);

        Assert.Equal("explanation", parameters[0].Name);
        Assert.Equal(typeof(string), parameters[0].ParameterType);

        Assert.Equal("declaredRiskLevel", parameters[1].Name);
        Assert.Equal(
            typeof(OperationRiskLevel?),
            parameters[1].ParameterType);

        Assert.All(parameters, parameter =>
        {
            Assert.True(parameter.IsOptional);
            Assert.Null(parameter.DefaultValue);
        });
    }

    [Fact]
    public void DefaultDoesNotInventExplanationOrRisk()
    {
        var explanation = new OperationRiskExplanation();

        Assert.Null(explanation.Explanation);
        Assert.Null(explanation.DeclaredRiskLevel);
    }

    [Fact]
    public void ExplanationAndDeclaredRiskRemainIndependent()
    {
        var textOnly = new OperationRiskExplanation(
            explanation: "  Requires Manual Review  ");

        var riskOnly = new OperationRiskExplanation(
            declaredRiskLevel: OperationRiskLevel.R5);

        var both = new OperationRiskExplanation(
            explanation: "  Backup Evidence Not Verified  ",
            declaredRiskLevel: OperationRiskLevel.R0);

        Assert.Equal(
            "Requires Manual Review",
            textOnly.Explanation);
        Assert.Null(textOnly.DeclaredRiskLevel);

        Assert.Null(riskOnly.Explanation);
        Assert.Equal(
            OperationRiskLevel.R5,
            riskOnly.DeclaredRiskLevel);

        Assert.Equal(
            "Backup Evidence Not Verified",
            both.Explanation);
        Assert.Equal(
            OperationRiskLevel.R0,
            both.DeclaredRiskLevel);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(" \t ")]
    public void BlankExplanationsNormalizeToNull(string? value)
    {
        var explanation = new OperationRiskExplanation(
            explanation: value,
            declaredRiskLevel: OperationRiskLevel.R2);

        Assert.Null(explanation.Explanation);
        Assert.Equal(
            OperationRiskLevel.R2,
            explanation.DeclaredRiskLevel);
    }

    [Theory]
    [InlineData("risk\nreason")]
    [InlineData("risk\rreason")]
    [InlineData("risk\treason")]
    [InlineData("risk\0reason")]
    public void EmbeddedControlCharactersAreRejected(string value)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationRiskExplanation(explanation: value));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void AllR0ToR5LevelsMayBeDeclared(int value)
    {
        var level = (OperationRiskLevel)value;

        var explanation = new OperationRiskExplanation(
            declaredRiskLevel: level);

        Assert.Null(explanation.Explanation);
        Assert.Equal(level, explanation.DeclaredRiskLevel);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    [InlineData(99)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void UndefinedRiskLevelsAreRejected(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OperationRiskExplanation(
                declaredRiskLevel: (OperationRiskLevel)value));
    }
}
