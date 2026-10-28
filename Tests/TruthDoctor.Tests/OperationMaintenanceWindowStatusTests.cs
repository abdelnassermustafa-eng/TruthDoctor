using System;
using System.Linq;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationMaintenanceWindowStatusTests
{
    [Fact]
    public void IsDedicatedProviderNeutralEnum()
    {
        var type = typeof(OperationMaintenanceWindowStatus);

        Assert.True(type.IsPublic);
        Assert.True(type.IsEnum);
        Assert.Equal(
            "TruthDoctor.Models.Platform",
            type.Namespace);
        Assert.Equal(typeof(int), Enum.GetUnderlyingType(type));
    }

    [Fact]
    public void UnknownHasZeroValue()
    {
        Assert.Equal(
            0, (int)OperationMaintenanceWindowStatus.Unknown);
    }

    [Fact]
    public void PlannedHasOneValue()
    {
        Assert.Equal(
            1, (int)OperationMaintenanceWindowStatus.Planned);
    }

    [Fact]
    public void ActiveHasTwoValue()
    {
        Assert.Equal(
            2, (int)OperationMaintenanceWindowStatus.Active);
    }

    [Fact]
    public void ClosedEarlyHasThreeValue()
    {
        Assert.Equal(
            3, (int)OperationMaintenanceWindowStatus.ClosedEarly);
    }

    [Fact]
    public void ClosedHasFourValue()
    {
        Assert.Equal(
            4, (int)OperationMaintenanceWindowStatus.Closed);
    }

    [Fact]
    public void ExpiredHasFiveValue()
    {
        Assert.Equal(
            5, (int)OperationMaintenanceWindowStatus.Expired);
    }

    [Fact]
    public void CancelledHasSixValue()
    {
        Assert.Equal(
            6, (int)OperationMaintenanceWindowStatus.Cancelled);
    }

    [Fact]
    public void DefinesExactlyApprovedLifecycleVocabulary()
    {
        var names = Enum.GetNames<OperationMaintenanceWindowStatus>();

        Assert.Equal(
            new[]
            {
                "Unknown",
                "Planned",
                "Active",
                "ClosedEarly",
                "Closed",
                "Expired",
                "Cancelled"
            }.OrderBy(name => name, StringComparer.Ordinal),
            names.OrderBy(name => name, StringComparer.Ordinal));
    }

    [Fact]
    public void DoesNotAliasNumericLifecycleValues()
    {
        var values = Enum.GetValues<OperationMaintenanceWindowStatus>()
            .Select(value => (int)value)
            .ToArray();

        Assert.Equal(7, values.Length);
        Assert.Equal(7, values.Distinct().Count());
    }

    [Fact]
    public void IsNotAFlagsEnumeration()
    {
        Assert.False(
            Attribute.IsDefined(
                typeof(OperationMaintenanceWindowStatus),
                typeof(FlagsAttribute)));
    }
}
