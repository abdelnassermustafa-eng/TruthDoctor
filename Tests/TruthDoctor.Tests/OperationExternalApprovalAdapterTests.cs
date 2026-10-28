using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationExternalApprovalAdapterTests
{
    private static readonly DateTimeOffset Observed =
        new(2026, 10, 28, 10, 0, 0, TimeSpan.FromHours(-7));

    private static OperationApprovalChain Chain()
    {
        var request = new OperationNativeApprovalRequest(
            new OperationCorrelationId("operation-42"),
            new OperationActorIdentity(
                OperationActorKind.Human, "requester"),
            "External approval review");

        var stage = new OperationApprovalStage(
            "stage-1",
            new OperationRoleIdentity("approver"));

        return new OperationApprovalChain(
            request, new[] { stage });
    }

    private static OperationExternalApprovalAdapter Create(
        string adapterId = "adapter-1",
        OperationExternalApprovalStatus status =
            OperationExternalApprovalStatus.Unknown,
        string externalSystem = "external-approval-system",
        string externalRequestId = "external-request-1",
        string reason = "Reported external approval status")
    {
        return new OperationExternalApprovalAdapter(
            adapterId,
            Chain(),
            externalSystem,
            externalRequestId,
            status,
            Observed,
            reason);
    }

    [Fact]
    public void PreservesTrimmedTextAndOriginalCase()
    {
        var adapter = Create(
            adapterId: "  AdApTeR-7  ",
            externalSystem: "  ServiceNow-Prod  ",
            externalRequestId: "  Req-ABC-42  ",
            reason: "  Manual Review  ");

        Assert.Equal("AdApTeR-7", adapter.AdapterId);
        Assert.Equal("ServiceNow-Prod", adapter.ExternalSystem);
        Assert.Equal("Req-ABC-42", adapter.ExternalRequestId);
        Assert.Equal("Manual Review", adapter.Reason);
    }

    [Theory]
    [InlineData("AdapterId", null)]
    [InlineData("AdapterId", "")]
    [InlineData("AdapterId", " ")]
    [InlineData("AdapterId", "\t")]
    [InlineData("ExternalSystem", null)]
    [InlineData("ExternalSystem", "")]
    [InlineData("ExternalSystem", " ")]
    [InlineData("ExternalSystem", "\t")]
    [InlineData("ExternalRequestId", null)]
    [InlineData("ExternalRequestId", "")]
    [InlineData("ExternalRequestId", " ")]
    [InlineData("ExternalRequestId", "\t")]
    [InlineData("Reason", null)]
    [InlineData("Reason", "")]
    [InlineData("Reason", " ")]
    [InlineData("Reason", "\t")]
    public void RejectsNullOrBlankText(
        string field, string? value)
    {
        Assert.Throws<ArgumentException>(() =>
        {
            _ = field switch
            {
                "AdapterId" => Create(adapterId: value!),
                "ExternalSystem" => Create(externalSystem: value!),
                "ExternalRequestId" =>
                    Create(externalRequestId: value!),
                "Reason" => Create(reason: value!),
                _ => throw new InvalidOperationException()
            };
        });
    }

    [Theory]
    [InlineData("AdapterId", "a\nb")]
    [InlineData("AdapterId", "a\tb")]
    [InlineData("AdapterId", "a\0b")]
    [InlineData("ExternalSystem", "a\nb")]
    [InlineData("ExternalSystem", "a\tb")]
    [InlineData("ExternalSystem", "a\0b")]
    [InlineData("ExternalRequestId", "a\nb")]
    [InlineData("ExternalRequestId", "a\tb")]
    [InlineData("ExternalRequestId", "a\0b")]
    [InlineData("Reason", "a\nb")]
    [InlineData("Reason", "a\tb")]
    [InlineData("Reason", "a\0b")]
    public void RejectsControlCharacters(
        string field, string value)
    {
        Assert.Throws<ArgumentException>(() =>
        {
            _ = field switch
            {
                "AdapterId" => Create(adapterId: value),
                "ExternalSystem" => Create(externalSystem: value),
                "ExternalRequestId" =>
                    Create(externalRequestId: value),
                "Reason" => Create(reason: value),
                _ => throw new InvalidOperationException()
            };
        });
    }

    [Fact]
    public void RejectsNullApprovalChain()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationExternalApprovalAdapter(
                "adapter-1",
                null!,
                "system-1",
                "request-1",
                OperationExternalApprovalStatus.Unknown,
                Observed,
                "Review"));
    }

    [Fact]
    public void PreservesExactApprovalChainReference()
    {
        var chain = Chain();

        var adapter = new OperationExternalApprovalAdapter(
            "adapter-1",
            chain,
            "system-1",
            "request-1",
            OperationExternalApprovalStatus.Pending,
            Observed,
            "Awaiting decision");

        Assert.Same(chain, adapter.Chain);
        Assert.Equal(
            "operation-42",
            adapter.Chain.Request.Operation.Value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    [InlineData(999)]
    public void RejectsUndefinedStatuses(int raw)
    {
        Assert.Throws<ArgumentException>(() =>
            Create(status: (OperationExternalApprovalStatus)raw));
    }

    [Theory]
    [InlineData(OperationExternalApprovalStatus.Unknown)]
    [InlineData(OperationExternalApprovalStatus.Pending)]
    [InlineData(OperationExternalApprovalStatus.ReportedApproved)]
    [InlineData(OperationExternalApprovalStatus.ReportedDenied)]
    [InlineData(OperationExternalApprovalStatus.Unavailable)]
    public void PreservesAllFiveReportedStatuses(
        OperationExternalApprovalStatus status)
    {
        var adapter = Create(status: status);
        Assert.Equal(status, adapter.Status);
    }

    [Fact]
    public void ReportedApprovedDoesNotGrantAuthorization()
    {
        var adapter = Create(
            status: OperationExternalApprovalStatus.ReportedApproved);

        Assert.Equal(
            OperationExternalApprovalStatus.ReportedApproved,
            adapter.Status);

        Assert.Null(
            typeof(OperationExternalApprovalAdapter)
                .GetProperty("IsAuthorized"));

        Assert.Null(
            typeof(OperationExternalApprovalAdapter)
                .GetProperty("CanExecute"));
    }

    [Fact]
    public void UnavailableDoesNotDefaultToApproved()
    {
        var adapter = Create(
            status: OperationExternalApprovalStatus.Unavailable);

        Assert.Equal(
            OperationExternalApprovalStatus.Unavailable,
            adapter.Status);
    }

    [Fact]
    public void PendingRemainsPendingWithoutExternalEvaluation()
    {
        var adapter = Create(
            status: OperationExternalApprovalStatus.Pending);

        Assert.Equal(
            OperationExternalApprovalStatus.Pending,
            adapter.Status);
    }

    [Fact]
    public void UnknownRemainsUnknownWithoutProviderDiscovery()
    {
        var adapter = Create();

        Assert.Equal(
            OperationExternalApprovalStatus.Unknown,
            adapter.Status);
    }

    [Fact]
    public void PreservesSuppliedObservationAndOffset()
    {
        var observed = Observed.ToOffset(
            TimeSpan.FromHours(5));

        var adapter = new OperationExternalApprovalAdapter(
            "adapter-1",
            Chain(),
            "system-1",
            "request-1",
            OperationExternalApprovalStatus.Unknown,
            observed,
            "Review");

        Assert.Equal(observed, adapter.ObservedAt);
        Assert.Equal(observed.Offset, adapter.ObservedAt.Offset);
    }

    [Fact]
    public void PreservesCallerSuppliedExternalRequestReference()
    {
        var adapter = Create(
            externalRequestId: "Ticket-1234");

        Assert.Equal("Ticket-1234", adapter.ExternalRequestId);
    }

    [Fact]
    public void DoesNotRequireLifecycleSnapshot()
    {
        var adapter = Create();

        Assert.NotNull(adapter.Chain);
        Assert.Null(
            typeof(OperationExternalApprovalAdapter)
                .GetProperty("Lifecycle"));
    }

    [Fact]
    public void IsPublicSealedProviderNeutralClass()
    {
        var type = typeof(OperationExternalApprovalAdapter);

        Assert.True(type.IsPublic);
        Assert.True(type.IsClass);
        Assert.True(type.IsSealed);
        Assert.Equal("TruthDoctor.Models.Platform", type.Namespace);
    }

    [Fact]
    public void ExposesExactlySevenGetterOnlyProperties()
    {
        var type = typeof(OperationExternalApprovalAdapter);

        var properties = type.GetProperties(
            BindingFlags.Public |
            BindingFlags.Instance |
            BindingFlags.DeclaredOnly);

        Assert.Equal(7, properties.Length);

        var expected = new (string Name, Type Type)[]
        {
            ("AdapterId", typeof(string)),
            ("Chain", typeof(OperationApprovalChain)),
            ("ExternalSystem", typeof(string)),
            ("ExternalRequestId", typeof(string)),
            ("Status", typeof(OperationExternalApprovalStatus)),
            ("ObservedAt", typeof(DateTimeOffset)),
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
    public void HasExactlyOneSevenArgumentConstructor()
    {
        var ctor = Assert.Single(
            typeof(OperationExternalApprovalAdapter)
                .GetConstructors(
                    BindingFlags.Public |
                    BindingFlags.Instance));

        var parameters = ctor.GetParameters();

        var expectedNames = new[]
        {
            "adapterId", "chain", "externalSystem",
            "externalRequestId", "status", "observedAt",
            "reason"
        };

        var expectedTypes = new[]
        {
            typeof(string),
            typeof(OperationApprovalChain),
            typeof(string),
            typeof(string),
            typeof(OperationExternalApprovalStatus),
            typeof(DateTimeOffset),
            typeof(string)
        };

        Assert.Equal(7, parameters.Length);

        for (var i = 0; i < parameters.Length; i++)
        {
            Assert.Equal(expectedNames[i], parameters[i].Name);
            Assert.Equal(expectedTypes[i], parameters[i].ParameterType);
            Assert.False(parameters[i].IsOptional);
        }
    }

    [Fact]
    public void DeclaresNoOperationalPublicMethods()
    {
        var methods = typeof(OperationExternalApprovalAdapter)
            .GetMethods(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.Static |
                BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .ToArray();

        Assert.Empty(methods);
    }

    [Fact]
    public void DoesNotImplementExternalIntegrationInterfaces()
    {
        Assert.Empty(
            typeof(OperationExternalApprovalAdapter)
                .GetInterfaces());
    }

    [Fact]
    public void StatusEnumHasExactlyFiveValues()
    {
        Assert.Equal(
            new[]
            {
                "Unknown",
                "Pending",
                "ReportedApproved",
                "ReportedDenied",
                "Unavailable"
            },
            Enum.GetNames<OperationExternalApprovalStatus>());
    }

    [Fact]
    public void StatusEnumNumericValuesAreStable()
    {
        Assert.Equal(0, (int)OperationExternalApprovalStatus.Unknown);
        Assert.Equal(1, (int)OperationExternalApprovalStatus.Pending);
        Assert.Equal(2, (int)OperationExternalApprovalStatus.ReportedApproved);
        Assert.Equal(3, (int)OperationExternalApprovalStatus.ReportedDenied);
        Assert.Equal(4, (int)OperationExternalApprovalStatus.Unavailable);
    }
}
