using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationApprovalLifecycleTests
{
    private static readonly DateTimeOffset Observed =
        new(2026, 10, 28, 10, 0, 0, TimeSpan.FromHours(-7));

    private static OperationApprovalChain Chain()
    {
        var request = new OperationNativeApprovalRequest(
            new OperationCorrelationId("operation-42"),
            new OperationActorIdentity(
                OperationActorKind.Human, "requester"),
            "Scheduled infrastructure maintenance");

        var stage = new OperationApprovalStage(
            "stage-1",
            new OperationRoleIdentity("approver"));

        return new OperationApprovalChain(
            request, new[] { stage });
    }

    private static OperationActorIdentity Revoker() =>
        new(OperationActorKind.Human, "reviewer-1");

    private static OperationApprovalLifecycle Create(
        string approvalId = "approval-1",
        OperationApprovalLifecycleStatus status =
            OperationApprovalLifecycleStatus.Unknown,
        DateTimeOffset? expiresAt = null,
        DateTimeOffset? revokedAt = null,
        OperationActorIdentity? revokedBy = null,
        string reason = "Reported approval lifecycle")
    {
        return new OperationApprovalLifecycle(
            approvalId,
            Chain(),
            status,
            Observed,
            expiresAt,
            revokedAt,
            revokedBy,
            reason);
    }

    [Fact]
    public void PreservesNormalizedTextAndCase()
    {
        var lifecycle = Create(
            approvalId: "  ApPrOvAl-7  ",
            reason: "  Manual Review  ");

        Assert.Equal("ApPrOvAl-7", lifecycle.ApprovalId);
        Assert.Equal("Manual Review", lifecycle.Reason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void RejectsNullOrBlankApprovalId(string? value)
    {
        Assert.Throws<ArgumentException>(() =>
            Create(approvalId: value!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void RejectsNullOrBlankReason(string? value)
    {
        Assert.Throws<ArgumentException>(() =>
            Create(reason: value!));
    }

    [Theory]
    [InlineData("a\nb")]
    [InlineData("a\rb")]
    [InlineData("a\tb")]
    [InlineData("a\0b")]
    public void RejectsApprovalIdControls(string value)
    {
        Assert.Throws<ArgumentException>(() =>
            Create(approvalId: value));
    }

    [Theory]
    [InlineData("a\nb")]
    [InlineData("a\rb")]
    [InlineData("a\tb")]
    [InlineData("a\0b")]
    public void RejectsReasonControls(string value)
    {
        Assert.Throws<ArgumentException>(() =>
            Create(reason: value));
    }

    [Fact]
    public void RejectsNullApprovalChain()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationApprovalLifecycle(
                "approval-1", null!,
                OperationApprovalLifecycleStatus.Unknown,
                Observed, null, null, null, "Review"));
    }

    [Fact]
    public void PreservesExactChainReference()
    {
        var chain = Chain();
        var lifecycle = new OperationApprovalLifecycle(
            "approval-1", chain,
            OperationApprovalLifecycleStatus.Unknown,
            Observed, null, null, null, "Review");

        Assert.Same(chain, lifecycle.Chain);
        Assert.Equal(
            "operation-42",
            lifecycle.Chain.Request.Operation.Value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    [InlineData(999)]
    public void RejectsUndefinedStatuses(int raw)
    {
        Assert.Throws<ArgumentException>(() =>
            Create(status: (OperationApprovalLifecycleStatus)raw));
    }

    [Theory]
    [InlineData(OperationApprovalLifecycleStatus.Unknown)]
    [InlineData(OperationApprovalLifecycleStatus.ReportedActive)]
    [InlineData(OperationApprovalLifecycleStatus.Expired)]
    [InlineData(OperationApprovalLifecycleStatus.Revoked)]
    public void PreservesDefinedReportedStatus(
        OperationApprovalLifecycleStatus status)
    {
        var expiresAt = status == OperationApprovalLifecycleStatus.Expired
            ? Observed.AddDays(-1)
            : (DateTimeOffset?)null;

        var revokedAt = status == OperationApprovalLifecycleStatus.Revoked
            ? Observed.AddMinutes(-1)
            : (DateTimeOffset?)null;

        var revokedBy = status == OperationApprovalLifecycleStatus.Revoked
            ? Revoker()
            : (OperationActorIdentity?)null;

        var lifecycle = Create(
            status: status,
            expiresAt: expiresAt,
            revokedAt: revokedAt,
            revokedBy: revokedBy);

        Assert.Equal(status, lifecycle.Status);
    }

    [Fact]
    public void ExpiredRequiresExplicitExpirationTimestamp()
    {
        Assert.Throws<ArgumentException>(() =>
            Create(status: OperationApprovalLifecycleStatus.Expired));
    }

    [Fact]
    public void ExpiredPreservesExplicitExpirationTimestamp()
    {
        var expiry = Observed.AddHours(-3)
            .ToOffset(TimeSpan.FromHours(3));

        var lifecycle = Create(
            status: OperationApprovalLifecycleStatus.Expired,
            expiresAt: expiry);

        Assert.Equal(expiry, lifecycle.ExpiresAt);
        Assert.Equal(expiry.Offset, lifecycle.ExpiresAt!.Value.Offset);
    }

    [Fact]
    public void RevokedRequiresRevocationTime()
    {
        Assert.Throws<ArgumentException>(() =>
            Create(
                status: OperationApprovalLifecycleStatus.Revoked,
                revokedBy: Revoker()));
    }

    [Fact]
    public void RevokedRequiresRevokingActor()
    {
        Assert.Throws<ArgumentException>(() =>
            Create(
                status: OperationApprovalLifecycleStatus.Revoked,
                revokedAt: Observed));
    }

    [Fact]
    public void RevokedRejectsUninitializedActor()
    {
        Assert.Throws<ArgumentException>(() =>
            Create(
                status: OperationApprovalLifecycleStatus.Revoked,
                revokedAt: Observed,
                revokedBy: default(OperationActorIdentity)));
    }

    [Fact]
    public void RevokedPreservesActorAndTimestampOffset()
    {
        var actor = Revoker();
        var time = Observed.ToOffset(TimeSpan.FromHours(2));

        var lifecycle = Create(
            status: OperationApprovalLifecycleStatus.Revoked,
            revokedAt: time,
            revokedBy: actor);

        Assert.Equal(actor, lifecycle.RevokedBy);
        Assert.Equal(time, lifecycle.RevokedAt);
        Assert.Equal(time.Offset, lifecycle.RevokedAt!.Value.Offset);
    }

    [Theory]
    [InlineData(OperationApprovalLifecycleStatus.Unknown)]
    [InlineData(OperationApprovalLifecycleStatus.ReportedActive)]
    [InlineData(OperationApprovalLifecycleStatus.Expired)]
    public void NonRevokedStatusRejectsRevocationMetadata(
        OperationApprovalLifecycleStatus status)
    {
        var expiry = status == OperationApprovalLifecycleStatus.Expired
            ? Observed.AddDays(-1)
            : (DateTimeOffset?)null;

        Assert.Throws<ArgumentException>(() =>
            Create(
                status: status,
                expiresAt: expiry,
                revokedAt: Observed,
                revokedBy: Revoker()));
    }

    [Fact]
    public void NonRevokedStatusRejectsRevocationTimeAlone()
    {
        Assert.Throws<ArgumentException>(() =>
            Create(revokedAt: Observed));
    }

    [Fact]
    public void NonRevokedStatusRejectsRevokingActorAlone()
    {
        Assert.Throws<ArgumentException>(() =>
            Create(revokedBy: Revoker()));
    }

    [Fact]
    public void ReportedActiveWithoutExpiryIsNotProofOfAuthorization()
    {
        var lifecycle = Create(
            status: OperationApprovalLifecycleStatus.ReportedActive);

        Assert.Equal(
            OperationApprovalLifecycleStatus.ReportedActive,
            lifecycle.Status);

        Assert.Null(lifecycle.ExpiresAt);
        Assert.Null(
            typeof(OperationApprovalLifecycle).GetProperty("IsAuthorized"));
        Assert.Null(
            typeof(OperationApprovalLifecycle).GetProperty("CanExecute"));
    }

    [Fact]
    public void DoesNotCalculateExpirationFromObservation()
    {
        var expiry = Observed.AddYears(-10);

        var lifecycle = Create(
            status: OperationApprovalLifecycleStatus.ReportedActive,
            expiresAt: expiry);

        Assert.Equal(
            OperationApprovalLifecycleStatus.ReportedActive,
            lifecycle.Status);
        Assert.Equal(expiry, lifecycle.ExpiresAt);
    }

    [Fact]
    public void PreservesOriginalObservationOffset()
    {
        var observed = Observed.ToOffset(TimeSpan.FromHours(5));

        var lifecycle = new OperationApprovalLifecycle(
            "approval-1", Chain(),
            OperationApprovalLifecycleStatus.Unknown,
            observed, null, null, null, "Review");

        Assert.Equal(observed, lifecycle.ObservedAt);
        Assert.Equal(observed.Offset, lifecycle.ObservedAt.Offset);
    }

    [Fact]
    public void AllowsExplicitRevocationAndExpiryTogether()
    {
        var expiry = Observed.AddDays(1);
        var revocation = Observed.AddMinutes(-2);

        var lifecycle = Create(
            status: OperationApprovalLifecycleStatus.Revoked,
            expiresAt: expiry,
            revokedAt: revocation,
            revokedBy: Revoker());

        Assert.Equal(expiry, lifecycle.ExpiresAt);
        Assert.Equal(revocation, lifecycle.RevokedAt);
    }

    [Fact]
    public void LifecycleIsPublicSealedProviderNeutralClass()
    {
        var type = typeof(OperationApprovalLifecycle);

        Assert.True(type.IsPublic);
        Assert.True(type.IsClass);
        Assert.True(type.IsSealed);
        Assert.Equal("TruthDoctor.Models.Platform", type.Namespace);
    }

    [Fact]
    public void ExposesExactlyEightGetterOnlyProperties()
    {
        var type = typeof(OperationApprovalLifecycle);

        var properties = type.GetProperties(
            BindingFlags.Public |
            BindingFlags.Instance |
            BindingFlags.DeclaredOnly);

        Assert.Equal(8, properties.Length);

        var expected = new (string Name, Type Type)[]
        {
            ("ApprovalId", typeof(string)),
            ("Chain", typeof(OperationApprovalChain)),
            ("Status", typeof(OperationApprovalLifecycleStatus)),
            ("ObservedAt", typeof(DateTimeOffset)),
            ("ExpiresAt", typeof(DateTimeOffset?)),
            ("RevokedAt", typeof(DateTimeOffset?)),
            ("RevokedBy", typeof(OperationActorIdentity?)),
            ("Reason", typeof(string))
        };

        foreach (var (name, propertyType) in expected)
        {
            var property = type.GetProperty(name);
            Assert.NotNull(property);
            Assert.Equal(propertyType, property.PropertyType);
            Assert.NotNull(property.GetMethod);
            Assert.Null(property.SetMethod);
        }
    }

    [Fact]
    public void HasExactlyOneEightArgumentConstructor()
    {
        var ctor = Assert.Single(
            typeof(OperationApprovalLifecycle).GetConstructors(
                BindingFlags.Public | BindingFlags.Instance));

        var parameters = ctor.GetParameters();
        var expectedNames = new[]
        {
            "approvalId", "chain", "status", "observedAt",
            "expiresAt", "revokedAt", "revokedBy", "reason"
        };

        var expectedTypes = new[]
        {
            typeof(string),
            typeof(OperationApprovalChain),
            typeof(OperationApprovalLifecycleStatus),
            typeof(DateTimeOffset),
            typeof(DateTimeOffset?),
            typeof(DateTimeOffset?),
            typeof(OperationActorIdentity?),
            typeof(string)
        };

        Assert.Equal(8, parameters.Length);

        for (var i = 0; i < parameters.Length; i++)
        {
            Assert.Equal(expectedNames[i], parameters[i].Name);
            Assert.Equal(expectedTypes[i], parameters[i].ParameterType);
            Assert.False(parameters[i].IsOptional);
        }
    }

    [Fact]
    public void HasNoPublicOperationalMethods()
    {
        var methods = typeof(OperationApprovalLifecycle).GetMethods(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.Static |
                BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .ToArray();

        Assert.Empty(methods);
    }

    [Fact]
    public void DoesNotDeclareIntegrationInterfaces()
    {
        Assert.Empty(typeof(OperationApprovalLifecycle).GetInterfaces());
    }

    [Fact]
    public void StatusEnumHasExactlyFourNamedValues()
    {
        var names = Enum.GetNames<OperationApprovalLifecycleStatus>();

        Assert.Equal(
            new[] { "Unknown", "ReportedActive", "Expired", "Revoked" },
            names);
    }

    [Fact]
    public void StatusEnumHasStableNumericValues()
    {
        Assert.Equal(0, (int)OperationApprovalLifecycleStatus.Unknown);
        Assert.Equal(1, (int)OperationApprovalLifecycleStatus.ReportedActive);
        Assert.Equal(2, (int)OperationApprovalLifecycleStatus.Expired);
        Assert.Equal(3, (int)OperationApprovalLifecycleStatus.Revoked);
    }
}
