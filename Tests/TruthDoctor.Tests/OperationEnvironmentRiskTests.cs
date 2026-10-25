using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationEnvironmentRiskTests
{
    [Fact]
    public void TypeIsPublicSealedAndHasExactlyTwoGetterOnlyProperties()
    {
        var type = typeof(OperationEnvironmentRisk);

        Assert.True(type.IsPublic);
        Assert.True(type.IsSealed);

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        Assert.Equal(
            new[] { "DeclaredRiskLevel", "EnvironmentName" },
            properties.Select(p => p.Name).OrderBy(n => n).ToArray());

        Assert.All(properties, property =>
        {
            Assert.NotNull(property.GetMethod);
            Assert.Null(property.SetMethod);
        });

        Assert.Equal(
            typeof(string),
            type.GetProperty("EnvironmentName")!.PropertyType);
        Assert.Equal(
            typeof(OperationRiskLevel?),
            type.GetProperty("DeclaredRiskLevel")!.PropertyType);
    }

    [Fact]
    public void ConstructorHasExactlyTwoOptionalParameters()
    {
        var constructor = Assert.Single(
            typeof(OperationEnvironmentRisk).GetConstructors());

        var parameters = constructor.GetParameters();
        Assert.Equal(2, parameters.Length);

        Assert.Equal("environmentName", parameters[0].Name);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.True(parameters[0].IsOptional);
        Assert.Null(parameters[0].DefaultValue);

        Assert.Equal("declaredRiskLevel", parameters[1].Name);
        Assert.Equal(typeof(OperationRiskLevel?), parameters[1].ParameterType);
        Assert.True(parameters[1].IsOptional);
        Assert.Null(parameters[1].DefaultValue);
    }

    [Fact]
    public void DefaultsDoNotCreateAnImplicitRiskAssessment()
    {
        var risk = new OperationEnvironmentRisk();

        Assert.Null(risk.EnvironmentName);
        Assert.Null(risk.DeclaredRiskLevel);
    }

    [Fact]
    public void ExplicitR0IsDifferentFromUnassessed()
    {
        var unassessed = new OperationEnvironmentRisk();
        var assessed = new OperationEnvironmentRisk(
            declaredRiskLevel: OperationRiskLevel.R0);

        Assert.Null(unassessed.DeclaredRiskLevel);
        Assert.Equal(OperationRiskLevel.R0, assessed.DeclaredRiskLevel);
    }

    [Fact]
    public void EnvironmentLabelDoesNotInferRisk()
    {
        var risk = new OperationEnvironmentRisk("Production");

        Assert.Equal("Production", risk.EnvironmentName);
        Assert.Null(risk.DeclaredRiskLevel);
    }

    [Fact]
    public void EnvironmentNameIsTrimmedWithoutChangingCase()
    {
        var risk = new OperationEnvironmentRisk("  ProDUCTION  ");

        Assert.Equal("ProDUCTION", risk.EnvironmentName);
        Assert.Null(risk.DeclaredRiskLevel);
    }

    [Fact]
    public void EnvironmentNameAndDeclaredRiskRemainIndependent()
    {
        var first = new OperationEnvironmentRisk(
            "Production", OperationRiskLevel.R1);
        var second = new OperationEnvironmentRisk(
            "Staging", OperationRiskLevel.R5);

        Assert.Equal("Production", first.EnvironmentName);
        Assert.Equal(OperationRiskLevel.R1, first.DeclaredRiskLevel);

        Assert.Equal("Staging", second.EnvironmentName);
        Assert.Equal(OperationRiskLevel.R5, second.DeclaredRiskLevel);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(" \t ")]
    public void BlankEnvironmentNamesNormalizeToNull(string? name)
    {
        var risk = new OperationEnvironmentRisk(
            name, OperationRiskLevel.R2);

        Assert.Null(risk.EnvironmentName);
        Assert.Equal(OperationRiskLevel.R2, risk.DeclaredRiskLevel);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void AllApprovedRiskLevelsArePreserved(int level)
    {
        var expected = (OperationRiskLevel)level;
        var risk = new OperationEnvironmentRisk(
            declaredRiskLevel: expected);

        Assert.Null(risk.EnvironmentName);
        Assert.Equal(expected, risk.DeclaredRiskLevel);
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
            new OperationEnvironmentRisk(
                "Production", (OperationRiskLevel)value));
    }

    [Theory]
    [InlineData("prod\nname")]
    [InlineData("prod\rname")]
    [InlineData("prod\0name")]
    public void EmbeddedControlCharactersAreRejected(string name)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationEnvironmentRisk(name));
    }
}
