using System;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationStepRecoveryTests
{
    private static UniversalCapabilityIdentity Stop =>
        new("compute.instance.stop");

    [Fact]
    public void StatusEnumHasExactValues()
    {
        Assert.Equal(0, (int)OperationRecoveryStatus.Unknown);
        Assert.Equal(1, (int)OperationRecoveryStatus.Unsupported);
        Assert.Equal(2, (int)OperationRecoveryStatus.Planned);
        Assert.Equal(3, Enum.GetValues<OperationRecoveryStatus>().Length);
    }

    [Fact]
    public void KindEnumHasExactValues()
    {
        Assert.Equal(0, (int)OperationRecoveryKind.None);
        Assert.Equal(1, (int)OperationRecoveryKind.Rollback);
        Assert.Equal(2, (int)OperationRecoveryKind.Compensation);
        Assert.Equal(3, Enum.GetValues<OperationRecoveryKind>().Length);
    }

    [Fact]
    public void UnknownCanHaveNoRecoveryCapability()
    {
        var recovery = new OperationStepRecovery(
            " step-a ", OperationRecoveryStatus.Unknown,
            OperationRecoveryKind.None);

        Assert.Equal("step-a", recovery.StepId);
        Assert.Equal(OperationRecoveryStatus.Unknown, recovery.Status);
        Assert.Equal(OperationRecoveryKind.None, recovery.Kind);
        Assert.Null(recovery.RecoveryCapability);
        Assert.Null(recovery.Limitations);
    }

    [Fact]
    public void UnsupportedCanDescribeLimitations()
    {
        var recovery = new OperationStepRecovery(
            "Step-A", OperationRecoveryStatus.Unsupported,
            OperationRecoveryKind.None, limitations: " No reversal ");

        Assert.Equal("Step-A", recovery.StepId);
        Assert.Equal("No reversal", recovery.Limitations);
        Assert.Null(recovery.RecoveryCapability);
    }

    [Fact]
    public void PlannedRollbackRequiresAndPreservesCapability()
    {
        var recovery = new OperationStepRecovery(
            "step-a", OperationRecoveryStatus.Planned,
            OperationRecoveryKind.Rollback, Stop);

        Assert.Equal(OperationRecoveryKind.Rollback, recovery.Kind);
        Assert.Equal(Stop, recovery.RecoveryCapability);
    }

    [Fact]
    public void PlannedCompensationRequiresAndPreservesCapability()
    {
        var recovery = new OperationStepRecovery(
            "step-a", OperationRecoveryStatus.Planned,
            OperationRecoveryKind.Compensation, Stop);

        Assert.Equal(OperationRecoveryKind.Compensation, recovery.Kind);
        Assert.Equal(Stop, recovery.RecoveryCapability);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void RejectsBlankStepId(string? stepId)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationStepRecovery(
                stepId!, OperationRecoveryStatus.Unknown,
                OperationRecoveryKind.None));
    }

    [Theory]
    [InlineData("step\nx")]
    [InlineData("step\rx")]
    [InlineData("step\0x")]
    public void RejectsEmbeddedControlsInStepId(string stepId)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationStepRecovery(
                stepId, OperationRecoveryStatus.Unknown,
                OperationRecoveryKind.None));
    }

    [Theory]
    [InlineData("one\nx")]
    [InlineData("one\rx")]
    [InlineData("one\0x")]
    public void RejectsEmbeddedControlsInLimitations(string limitations)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationStepRecovery(
                "step-a", OperationRecoveryStatus.Unknown,
                OperationRecoveryKind.None, limitations: limitations));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t ")]
    public void BlankLimitationsNormalizeToNull(string? limitations)
    {
        var recovery = new OperationStepRecovery(
            "step-a", OperationRecoveryStatus.Unknown,
            OperationRecoveryKind.None, limitations: limitations);

        Assert.Null(recovery.Limitations);
    }

    [Theory]
    [InlineData(OperationRecoveryStatus.Unknown)]
    [InlineData(OperationRecoveryStatus.Unsupported)]
    public void NonplannedStatusRejectsRollbackKind(
        OperationRecoveryStatus status)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationStepRecovery(
                "step-a", status,
                OperationRecoveryKind.Rollback, Stop));
    }

    [Theory]
    [InlineData(OperationRecoveryStatus.Unknown)]
    [InlineData(OperationRecoveryStatus.Unsupported)]
    public void NonplannedStatusRejectsCompensationKind(
        OperationRecoveryStatus status)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationStepRecovery(
                "step-a", status,
                OperationRecoveryKind.Compensation, Stop));
    }

    [Theory]
    [InlineData(OperationRecoveryStatus.Unknown)]
    [InlineData(OperationRecoveryStatus.Unsupported)]
    public void NonplannedStatusRejectsRecoveryCapability(
        OperationRecoveryStatus status)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationStepRecovery(
                "step-a", status,
                OperationRecoveryKind.None, Stop));
    }

    [Theory]
    [InlineData(OperationRecoveryKind.Rollback)]
    [InlineData(OperationRecoveryKind.Compensation)]
    public void PlannedRecoveryRejectsAbsentCapability(
        OperationRecoveryKind kind)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationStepRecovery(
                "step-a", OperationRecoveryStatus.Planned, kind));
    }

    [Theory]
    [InlineData(OperationRecoveryKind.Rollback)]
    [InlineData(OperationRecoveryKind.Compensation)]
    public void PlannedRecoveryRejectsDefaultCapability(
        OperationRecoveryKind kind)
    {
        UniversalCapabilityIdentity? invalid =
            default(UniversalCapabilityIdentity);

        Assert.Throws<ArgumentException>(() =>
            new OperationStepRecovery(
                "step-a", OperationRecoveryStatus.Planned,
                kind, invalid));
    }

    [Fact]
    public void PlannedRecoveryRejectsNoneKind()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationStepRecovery(
                "step-a", OperationRecoveryStatus.Planned,
                OperationRecoveryKind.None, Stop));
    }

    [Fact]
    public void RejectsUnknownStatusEnumValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OperationStepRecovery(
                "step-a", (OperationRecoveryStatus)99,
                OperationRecoveryKind.None));
    }

    [Fact]
    public void RejectsUnknownKindEnumValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OperationStepRecovery(
                "step-a", OperationRecoveryStatus.Unknown,
                (OperationRecoveryKind)99));
    }

    [Fact]
    public void PreservesCaseAndTrimsOptionalLimitations()
    {
        var recovery = new OperationStepRecovery(
            " StEp-A ", OperationRecoveryStatus.Planned,
            OperationRecoveryKind.Compensation, Stop,
            " Manual Approval ");

        Assert.Equal("StEp-A", recovery.StepId);
        Assert.Equal("Manual Approval", recovery.Limitations);
    }

    [Fact]
    public void HasExactImmutablePublicApi()
    {
        var type = typeof(OperationStepRecovery);
        Assert.True(type.IsSealed);

        var properties = type.GetProperties(
            BindingFlags.Instance | BindingFlags.Public);
        Assert.Equal(5, properties.Length);

        foreach (var property in properties)
        {
            Assert.NotNull(property.GetMethod);
            Assert.Null(property.SetMethod);
        }

        Assert.Equal(typeof(string),
            type.GetProperty("StepId")!.PropertyType);
        Assert.Equal(typeof(OperationRecoveryStatus),
            type.GetProperty("Status")!.PropertyType);
        Assert.Equal(typeof(OperationRecoveryKind),
            type.GetProperty("Kind")!.PropertyType);
        Assert.Equal(typeof(UniversalCapabilityIdentity?),
            type.GetProperty("RecoveryCapability")!.PropertyType);
        Assert.Equal(typeof(string),
            type.GetProperty("Limitations")!.PropertyType);

        var ctor = Assert.Single(type.GetConstructors());
        var parameters = ctor.GetParameters();
        Assert.Equal(5, parameters.Length);

        Assert.Equal("stepId", parameters[0].Name);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal("status", parameters[1].Name);
        Assert.Equal(typeof(OperationRecoveryStatus),
            parameters[1].ParameterType);
        Assert.Equal("kind", parameters[2].Name);
        Assert.Equal(typeof(OperationRecoveryKind),
            parameters[2].ParameterType);
        Assert.Equal("recoveryCapability", parameters[3].Name);
        Assert.Equal(typeof(UniversalCapabilityIdentity?),
            parameters[3].ParameterType);
        Assert.True(parameters[3].IsOptional);
        Assert.Equal("limitations", parameters[4].Name);
        Assert.Equal(typeof(string), parameters[4].ParameterType);
        Assert.True(parameters[4].IsOptional);
    }
}
