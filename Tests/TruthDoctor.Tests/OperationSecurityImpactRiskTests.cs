using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationSecurityImpactRiskTests
{
    [Fact]
    public void TypeIsPublicAndSealed()
    {
        var type = typeof(OperationSecurityImpactRisk);
        Assert.True(type.IsPublic);
        Assert.True(type.IsSealed);
    }

    [Fact]
    public void PublicApiHasExactlyTwoGetterOnlyProperties()
    {
        var type = typeof(OperationSecurityImpactRisk);
        var properties = type.GetProperties(
            BindingFlags.Public | BindingFlags.Instance);

        Assert.Equal(
            new[] { "DeclaredRiskLevel", "SecurityImpactDescription" },
            properties.Select(p => p.Name).OrderBy(n => n).ToArray());

        Assert.All(properties, property =>
        {
            Assert.NotNull(property.GetMethod);
            Assert.Null(property.SetMethod);
        });

        Assert.Equal(typeof(string),
            type.GetProperty("SecurityImpactDescription")!.PropertyType);
        Assert.Equal(typeof(OperationRiskLevel?),
            type.GetProperty("DeclaredRiskLevel")!.PropertyType);
    }

    [Fact]
    public void ConstructorHasExactlyTwoOptionalParameters()
    {
        var constructor = Assert.Single(
            typeof(OperationSecurityImpactRisk).GetConstructors());

        var parameters = constructor.GetParameters();

        Assert.Equal(2, parameters.Length);
        Assert.Equal("securityImpactDescription", parameters[0].Name);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal("declaredRiskLevel", parameters[1].Name);
        Assert.Equal(typeof(OperationRiskLevel?),
            parameters[1].ParameterType);

        Assert.All(parameters, parameter =>
        {
            Assert.True(parameter.IsOptional);
            Assert.Null(parameter.DefaultValue);
        });
    }

    [Fact]
    public void DefaultDoesNotInventImpactOrRisk()
    {
        var risk = new OperationSecurityImpactRisk();

        Assert.Null(risk.SecurityImpactDescription);
        Assert.Null(risk.DeclaredRiskLevel);
    }

    [Fact]
    public void SecurityDescriptionAndRiskAreIndependent()
    {
        var first = new OperationSecurityImpactRisk(
            "  Potential Confidentiality Exposure  ",
            OperationRiskLevel.R0);

        var second = new OperationSecurityImpactRisk(
            "Access Control Change",
            OperationRiskLevel.R5);

        var unassessed = new OperationSecurityImpactRisk(
            "Potential Integrity Impact");

        Assert.Equal(
            "Potential Confidentiality Exposure",
            first.SecurityImpactDescription);
        Assert.Equal(
            OperationRiskLevel.R0,
            first.DeclaredRiskLevel);

        Assert.Equal(
            "Access Control Change",
            second.SecurityImpactDescription);
        Assert.Equal(
            OperationRiskLevel.R5,
            second.DeclaredRiskLevel);

        Assert.Equal(
            "Potential Integrity Impact",
            unassessed.SecurityImpactDescription);
        Assert.Null(unassessed.DeclaredRiskLevel);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(" \t ")]
    public void BlankDescriptionsNormalizeToNull(string? description)
    {
        var risk = new OperationSecurityImpactRisk(
            description,
            OperationRiskLevel.R2);

        Assert.Null(risk.SecurityImpactDescription);
        Assert.Equal(OperationRiskLevel.R2, risk.DeclaredRiskLevel);
    }

    [Theory]
    [InlineData("security\nimpact")]
    [InlineData("security\rimpact")]
    [InlineData("security\0impact")]
    public void EmbeddedControlCharactersAreRejected(string description)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationSecurityImpactRisk(description));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void AllApprovedRiskLevelsMayBeDeclared(int value)
    {
        var level = (OperationRiskLevel)value;

        var risk = new OperationSecurityImpactRisk(
            declaredRiskLevel: level);

        Assert.Null(risk.SecurityImpactDescription);
        Assert.Equal(level, risk.DeclaredRiskLevel);
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
            new OperationSecurityImpactRisk(
                declaredRiskLevel: (OperationRiskLevel)value));
    }
}
