using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationBackupRedundancyRiskTests
{
    [Fact]
    public void TypeIsPublicAndSealed()
    {
        var type = typeof(OperationBackupRedundancyRisk);
        Assert.True(type.IsPublic);
        Assert.True(type.IsSealed);
    }

    [Fact]
    public void PublicApiHasExactlyThreeGetterOnlyProperties()
    {
        var type = typeof(OperationBackupRedundancyRisk);
        var properties = type.GetProperties(
            BindingFlags.Public | BindingFlags.Instance);

        Assert.Equal(
            new[]
            {
                "BackupDescription",
                "DeclaredRiskLevel",
                "RedundancyDescription"
            },
            properties.Select(p => p.Name).OrderBy(n => n).ToArray());

        Assert.All(properties, property =>
        {
            Assert.NotNull(property.GetMethod);
            Assert.Null(property.SetMethod);
        });

        Assert.Equal(typeof(string),
            type.GetProperty("BackupDescription")!.PropertyType);
        Assert.Equal(typeof(string),
            type.GetProperty("RedundancyDescription")!.PropertyType);
        Assert.Equal(typeof(OperationRiskLevel?),
            type.GetProperty("DeclaredRiskLevel")!.PropertyType);
    }

    [Fact]
    public void ConstructorHasExactlyThreeOptionalParameters()
    {
        var constructor = Assert.Single(
            typeof(OperationBackupRedundancyRisk).GetConstructors());

        var parameters = constructor.GetParameters();

        Assert.Equal(3, parameters.Length);
        Assert.Equal("backupDescription", parameters[0].Name);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal("redundancyDescription", parameters[1].Name);
        Assert.Equal(typeof(string), parameters[1].ParameterType);
        Assert.Equal("declaredRiskLevel", parameters[2].Name);
        Assert.Equal(typeof(OperationRiskLevel?),
            parameters[2].ParameterType);

        Assert.All(parameters, parameter =>
        {
            Assert.True(parameter.IsOptional);
            Assert.Null(parameter.DefaultValue);
        });
    }

    [Fact]
    public void DefaultDoesNotInventBackupRedundancyOrRisk()
    {
        var risk = new OperationBackupRedundancyRisk();

        Assert.Null(risk.BackupDescription);
        Assert.Null(risk.RedundancyDescription);
        Assert.Null(risk.DeclaredRiskLevel);
    }

    [Fact]
    public void BackupRedundancyAndDeclaredRiskRemainIndependent()
    {
        var backupOnly = new OperationBackupRedundancyRisk(
            backupDescription: "  Backup Status Not Verified  ");

        var redundancyOnly = new OperationBackupRedundancyRisk(
            redundancyDescription: "  Replica Status Unknown  ",
            declaredRiskLevel: OperationRiskLevel.R5);

        var both = new OperationBackupRedundancyRisk(
            "  Backup Restoration Not Tested  ",
            "  Failover Readiness Not Verified  ",
            OperationRiskLevel.R0);

        Assert.Equal(
            "Backup Status Not Verified",
            backupOnly.BackupDescription);
        Assert.Null(backupOnly.RedundancyDescription);
        Assert.Null(backupOnly.DeclaredRiskLevel);

        Assert.Null(redundancyOnly.BackupDescription);
        Assert.Equal(
            "Replica Status Unknown",
            redundancyOnly.RedundancyDescription);
        Assert.Equal(
            OperationRiskLevel.R5,
            redundancyOnly.DeclaredRiskLevel);

        Assert.Equal(
            "Backup Restoration Not Tested",
            both.BackupDescription);
        Assert.Equal(
            "Failover Readiness Not Verified",
            both.RedundancyDescription);
        Assert.Equal(
            OperationRiskLevel.R0,
            both.DeclaredRiskLevel);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(" \t ")]
    public void BlankBackupDescriptionsNormalizeToNull(string? description)
    {
        var risk = new OperationBackupRedundancyRisk(
            backupDescription: description,
            redundancyDescription: "Replica Status Unknown",
            declaredRiskLevel: OperationRiskLevel.R2);

        Assert.Null(risk.BackupDescription);
        Assert.Equal(
            "Replica Status Unknown",
            risk.RedundancyDescription);
        Assert.Equal(OperationRiskLevel.R2, risk.DeclaredRiskLevel);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(" \t ")]
    public void BlankRedundancyDescriptionsNormalizeToNull(string? description)
    {
        var risk = new OperationBackupRedundancyRisk(
            backupDescription: "Backup Status Unknown",
            redundancyDescription: description,
            declaredRiskLevel: OperationRiskLevel.R3);

        Assert.Equal("Backup Status Unknown", risk.BackupDescription);
        Assert.Null(risk.RedundancyDescription);
        Assert.Equal(OperationRiskLevel.R3, risk.DeclaredRiskLevel);
    }

    [Theory]
    [InlineData("backup\nstatus")]
    [InlineData("backup\rstatus")]
    [InlineData("backup\0status")]
    public void EmbeddedBackupControlsAreRejected(string description)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationBackupRedundancyRisk(
                backupDescription: description));
    }

    [Theory]
    [InlineData("replica\nstatus")]
    [InlineData("replica\rstatus")]
    [InlineData("replica\0status")]
    public void EmbeddedRedundancyControlsAreRejected(string description)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationBackupRedundancyRisk(
                redundancyDescription: description));
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

        var risk = new OperationBackupRedundancyRisk(
            declaredRiskLevel: level);

        Assert.Null(risk.BackupDescription);
        Assert.Null(risk.RedundancyDescription);
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
            new OperationBackupRedundancyRisk(
                declaredRiskLevel: (OperationRiskLevel)value));
    }
}
