using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

/// <summary>
/// Test-first association of a history event with caller-reported approval
/// lifecycle context. Historical attribution is not authentication,
/// completed approval, or effective permission to execute.
/// </summary>
public sealed class OperationHistoryApprovalAttributionTests
{
    private static readonly DateTimeOffset Observed =
        new(2026, 10, 20, 9, 30, 0, TimeSpan.FromHours(-7));

    private static OperationHistoryEvent Event(
        string operationId = "approval-operation-001")
    {
        return new OperationHistoryEvent(
            "approval-history-event-001",
            new OperationCorrelationId(operationId),
            "ApprovalLifecycleReported");
    }

    private static OperationApprovalChain Chain()
    {
        var request = new OperationNativeApprovalRequest(
            new OperationCorrelationId("approval-operation-001"),
            new OperationActorIdentity(
                OperationActorKind.Human, "requester-001"),
            "Maintenance approval requested");

        var stage = new OperationApprovalStage(
            "review-001",
            new OperationRoleIdentity("operations-reviewer"));

        return new OperationApprovalChain(
            request, new[] { stage });
    }

    private static OperationApprovalLifecycle Lifecycle(
        OperationApprovalLifecycleStatus status =
            OperationApprovalLifecycleStatus.Unknown,
        DateTimeOffset? expiresAt = null,
        DateTimeOffset? revokedAt = null,
        OperationActorIdentity? revokedBy = null,
        DateTimeOffset? observedAt = null,
        OperationApprovalChain? chain = null)
    {
        return new OperationApprovalLifecycle(
            "approval-001",
            chain ?? Chain(),
            status,
            observedAt ?? Observed,
            expiresAt,
            revokedAt,
            revokedBy,
            "Caller-reported approval lifecycle state");
    }

    private static OperationHistoryApprovalAttribution Create(
        OperationApprovalLifecycle? lifecycle = null)
    {
        return new OperationHistoryApprovalAttribution(
            Event(), lifecycle ?? Lifecycle());
    }

    [Fact]
    public void PreservesExactHistoryEventReference()
    {
        var historyEvent = Event();

        var result = new OperationHistoryApprovalAttribution(
            historyEvent, Lifecycle());

        Assert.Same(historyEvent, result.Event);
    }

    [Fact]
    public void PreservesExactApprovalLifecycleReference()
    {
        var lifecycle = Lifecycle();

        var result = new OperationHistoryApprovalAttribution(
            Event(), lifecycle);

        Assert.Same(lifecycle, result.Lifecycle);
    }

    [Fact]
    public void PreservesHistoryEventIdentityAndKind()
    {
        var historyEvent = Event("different-operation-002");

        var result = new OperationHistoryApprovalAttribution(
            historyEvent, Lifecycle());

        Assert.Equal(
            "approval-history-event-001",
            result.Event.EventId);

        Assert.Equal(
            historyEvent.Operation,
            result.Event.Operation);

        Assert.Equal(
            "ApprovalLifecycleReported",
            result.Event.EventKind);

        Assert.Same(historyEvent, result.Event);
    }

    [Fact]
    public void PreservesApprovalIdAndChainReference()
    {
        var chain = Chain();
        var lifecycle = Lifecycle(chain: chain);
        var result = Create(lifecycle);

        Assert.Equal(
            "approval-001",
            result.Lifecycle.ApprovalId);

        Assert.Same(
            chain,
            result.Lifecycle.Chain);

        Assert.Same(
            chain.Request,
            result.Lifecycle.Chain.Request);

        Assert.Single(result.Lifecycle.Chain.Stages);
    }

    [Fact]
    public void PreservesUnknownLifecycleStatus()
    {
        var result = Create(Lifecycle(
            OperationApprovalLifecycleStatus.Unknown));

        Assert.Equal(
            OperationApprovalLifecycleStatus.Unknown,
            result.Lifecycle.Status);
    }

    [Fact]
    public void PreservesReportedActiveWithoutAuthorizationInference()
    {
        var result = Create(Lifecycle(
            OperationApprovalLifecycleStatus.ReportedActive));

        Assert.Equal(
            OperationApprovalLifecycleStatus.ReportedActive,
            result.Lifecycle.Status);

        var properties = typeof(OperationHistoryApprovalAttribution)
            .GetProperties(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly);

        foreach (var name in new[]
        {
            "Approved",
            "Authorized",
            "CanExecute",
            "PermissionGranted",
            "VerifiedApproval",
            "IsEffective",
            "ExecutionReady"
        })
        {
            Assert.DoesNotContain(
                properties,
                property => property.Name == name);
        }
    }

    [Fact]
    public void PreservesExpiredLifecycleWithoutClockEvaluation()
    {
        var expiry = Observed.AddDays(-2);

        var lifecycle = Lifecycle(
            OperationApprovalLifecycleStatus.Expired,
            expiresAt: expiry);

        var result = Create(lifecycle);

        Assert.Equal(
            OperationApprovalLifecycleStatus.Expired,
            result.Lifecycle.Status);

        Assert.Equal(
            expiry,
            result.Lifecycle.ExpiresAt);

        Assert.Same(lifecycle, result.Lifecycle);
    }

    [Fact]
    public void PreservesRevokedLifecycleMetadata()
    {
        var revoker = new OperationActorIdentity(
            OperationActorKind.Human, "reviewer-002");

        var revokedAt = Observed.AddHours(-1);

        var result = Create(Lifecycle(
            OperationApprovalLifecycleStatus.Revoked,
            revokedAt: revokedAt,
            revokedBy: revoker));

        Assert.Equal(
            OperationApprovalLifecycleStatus.Revoked,
            result.Lifecycle.Status);

        Assert.Equal(
            revokedAt,
            result.Lifecycle.RevokedAt);

        Assert.Equal(
            revoker,
            result.Lifecycle.RevokedBy);
    }

    [Fact]
    public void PreservesObservationTimestampOffset()
    {
        var supplied = new DateTimeOffset(
            2026, 10, 21, 13, 45, 0, TimeSpan.FromHours(3));

        var result = Create(Lifecycle(
            observedAt: supplied));

        Assert.Equal(
            supplied,
            result.Lifecycle.ObservedAt);

        Assert.Equal(
            supplied.Offset,
            result.Lifecycle.ObservedAt.Offset);
    }

    [Fact]
    public void PreservesOptionalExpirationMetadata()
    {
        var expiry = Observed.AddDays(2);

        var result = Create(Lifecycle(
            OperationApprovalLifecycleStatus.ReportedActive,
            expiresAt: expiry));

        Assert.Equal(
            expiry,
            result.Lifecycle.ExpiresAt);

        Assert.Equal(
            OperationApprovalLifecycleStatus.ReportedActive,
            result.Lifecycle.Status);
    }

    [Fact]
    public void RejectsNullHistoryEvent()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationHistoryApprovalAttribution(
                null!, Lifecycle()));
    }

    [Fact]
    public void RejectsNullApprovalLifecycle()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationHistoryApprovalAttribution(
                Event(), null!));
    }

    [Fact]
    public void DoesNotReinterpretExternalApprovalStatus()
    {
        var result = Create(Lifecycle(
            OperationApprovalLifecycleStatus.Unknown));

        Assert.Equal(
            OperationApprovalLifecycleStatus.Unknown,
            result.Lifecycle.Status);

        var properties = typeof(OperationHistoryApprovalAttribution)
            .GetProperties(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly);

        Assert.DoesNotContain(
            properties,
            property => property.PropertyType ==
                typeof(OperationExternalApprovalStatus));

        Assert.DoesNotContain(
            properties,
            property => property.PropertyType ==
                typeof(OperationExternalApprovalAdapter));
    }

    [Fact]
    public void IsPublicSealedProviderNeutralReferenceType()
    {
        var type = typeof(OperationHistoryApprovalAttribution);

        Assert.True(type.IsPublic);
        Assert.True(type.IsClass);
        Assert.True(type.IsSealed);

        Assert.Equal(
            "TruthDoctor.Models.Platform",
            type.Namespace);
    }

    [Fact]
    public void ExposesExactlyTwoGetterOnlyProperties()
    {
        var properties = typeof(OperationHistoryApprovalAttribution)
            .GetProperties(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly);

        Assert.Equal(2, properties.Length);

        var expected = new (string Name, Type Type)[]
        {
            ("Event", typeof(OperationHistoryEvent)),
            ("Lifecycle", typeof(OperationApprovalLifecycle))
        };

        Assert.Equal(
            expected.Select(item => item.Name).OrderBy(name => name),
            properties.Select(item => item.Name).OrderBy(name => name));

        foreach (var (name, expectedType) in expected)
        {
            var property = Assert.Single(
                properties,
                item => item.Name == name);

            Assert.Equal(
                expectedType,
                property.PropertyType);

            Assert.NotNull(property.GetMethod);
            Assert.Null(property.SetMethod);
        }
    }

    [Fact]
    public void HasOneConstructorAndNoOperationalMethods()
    {
        var type = typeof(OperationHistoryApprovalAttribution);

        var constructor = Assert.Single(
            type.GetConstructors(
                BindingFlags.Public |
                BindingFlags.Instance));

        var parameters = constructor.GetParameters();

        Assert.Equal(2, parameters.Length);

        Assert.Equal(
            "historyEvent",
            parameters[0].Name);

        Assert.Equal(
            typeof(OperationHistoryEvent),
            parameters[0].ParameterType);

        Assert.Equal(
            "lifecycle",
            parameters[1].Name);

        Assert.Equal(
            typeof(OperationApprovalLifecycle),
            parameters[1].ParameterType);

        Assert.All(
            parameters,
            parameter => Assert.False(parameter.IsOptional));

        var methods = type.GetMethods(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.Static |
                BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .ToArray();

        Assert.Empty(methods);
        Assert.Empty(type.GetInterfaces());
    }
}
