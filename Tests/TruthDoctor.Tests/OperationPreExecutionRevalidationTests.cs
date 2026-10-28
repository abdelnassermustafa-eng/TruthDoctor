using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationPreExecutionRevalidationTests
{
    private static readonly DateTimeOffset Start =
        new(2026, 10, 28, 9, 0, 0, TimeSpan.FromHours(-7));

    private static OperationCorrelationId Operation(string id = "op-42") =>
        new(id);

    private static OperationExecutionSchedule Schedule(
        string operationId = "op-42",
        OperationMaintenanceWindow? window = null)
    {
        window ??= new OperationMaintenanceWindow(
            Start, Start.AddHours(2));

        return new OperationExecutionSchedule(
            "schedule-1",
            Operation(operationId),
            window,
            Start.AddMinutes(30),
            "Planned maintenance");
    }

    private static OperationApprovalChain Chain(
        string operationId = "op-42")
    {
        var requester = new OperationActorIdentity(
            OperationActorKind.Human, "requester-1");

        var request = new OperationNativeApprovalRequest(
            Operation(operationId), requester, "Maintenance request");

        var stage = new OperationApprovalStage(
            "stage-1", new OperationRoleIdentity("approver-role"));

        return new OperationApprovalChain(
            request, new[] { stage });
    }

    private static OperationMaintenanceWindowLifecycle Lifecycle(
        OperationExecutionSchedule schedule,
        string operationId = "op-42",
        OperationMaintenanceWindow? window = null,
        OperationMaintenanceWindowStatus status =
            OperationMaintenanceWindowStatus.Planned)
    {
        return new OperationMaintenanceWindowLifecycle(
            Operation(operationId),
            window ?? schedule.Window,
            status,
            Start.AddMinutes(5));
    }

    private static OperationPreExecutionRevalidation Create(
        string id = "revalidation-1",
        string finding = "Review required",
        string reason = "Pre-execution assessment")
    {
        var schedule = Schedule();

        return new OperationPreExecutionRevalidation(
            id,
            schedule,
            Chain(),
            Lifecycle(schedule),
            Start.AddMinutes(10),
            finding,
            reason);
    }

    [Fact]
    public void PreservesNormalizedTextAndCase()
    {
        var assessment = Create(
            "  Review-AbC  ",
            "  Needs Further Review  ",
            "  Change Request  ");

        Assert.Equal("Review-AbC", assessment.RevalidationId);
        Assert.Equal("Needs Further Review", assessment.Finding);
        Assert.Equal("Change Request", assessment.Reason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void RejectsNullOrBlankRevalidationId(string? value)
    {
        Assert.Throws<ArgumentException>(() =>
            Create(id: value!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void RejectsNullOrBlankFinding(string? value)
    {
        Assert.Throws<ArgumentException>(() =>
            Create(finding: value!));
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
    [InlineData("review\nid")]
    [InlineData("review\rid")]
    [InlineData("review\tid")]
    [InlineData("review\0id")]
    public void RejectsEmbeddedIdControlCharacters(string value)
    {
        Assert.Throws<ArgumentException>(() =>
            Create(id: value));
    }

    [Theory]
    [InlineData("finding\ntext")]
    [InlineData("finding\rtext")]
    [InlineData("finding\ttext")]
    [InlineData("finding\0text")]
    public void RejectsEmbeddedFindingControls(string value)
    {
        Assert.Throws<ArgumentException>(() =>
            Create(finding: value));
    }

    [Theory]
    [InlineData("reason\ntext")]
    [InlineData("reason\rtext")]
    [InlineData("reason\ttext")]
    [InlineData("reason\0text")]
    public void RejectsEmbeddedReasonControls(string value)
    {
        Assert.Throws<ArgumentException>(() =>
            Create(reason: value));
    }

    [Fact]
    public void PreservesOriginalReferences()
    {
        var schedule = Schedule();
        var chain = Chain();
        var lifecycle = Lifecycle(schedule);

        var assessment = new OperationPreExecutionRevalidation(
            "reval-1", schedule, chain, lifecycle,
            Start.AddMinutes(10), "Pending", "Review");

        Assert.Same(schedule, assessment.Schedule);
        Assert.Same(chain, assessment.ApprovalChain);
        Assert.Same(lifecycle, assessment.Lifecycle);
    }

    [Fact]
    public void RejectsNullSchedule()
    {
        var schedule = Schedule();

        Assert.Throws<ArgumentNullException>(() =>
            new OperationPreExecutionRevalidation(
                "reval-1", null!, Chain(), Lifecycle(schedule),
                Start, "Pending", "Review"));
    }

    [Fact]
    public void RejectsNullApprovalChain()
    {
        var schedule = Schedule();

        Assert.Throws<ArgumentNullException>(() =>
            new OperationPreExecutionRevalidation(
                "reval-1", schedule, null!, Lifecycle(schedule),
                Start, "Pending", "Review"));
    }

    [Fact]
    public void RejectsNullLifecycle()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationPreExecutionRevalidation(
                "reval-1", Schedule(), Chain(), null!,
                Start, "Pending", "Review"));
    }

    [Fact]
    public void RejectsApprovalChainForDifferentOperation()
    {
        var schedule = Schedule();

        Assert.Throws<ArgumentException>(() =>
            new OperationPreExecutionRevalidation(
                "reval-1", schedule, Chain("other-operation"),
                Lifecycle(schedule), Start, "Pending", "Review"));
    }

    [Fact]
    public void RejectsLifecycleForDifferentOperation()
    {
        var schedule = Schedule();

        Assert.Throws<ArgumentException>(() =>
            new OperationPreExecutionRevalidation(
                "reval-1", schedule, Chain(),
                Lifecycle(schedule, "other-operation"),
                Start, "Pending", "Review"));
    }

    [Fact]
    public void RejectsDifferentWindowReferenceEvenWithSameBoundaries()
    {
        var schedule = Schedule();

        var sameBounds = new OperationMaintenanceWindow(
            schedule.Window.Start,
            schedule.Window.End);

        Assert.NotSame(schedule.Window, sameBounds);

        Assert.Throws<ArgumentException>(() =>
            new OperationPreExecutionRevalidation(
                "reval-1", schedule, Chain(),
                Lifecycle(schedule, window: sameBounds),
                Start, "Pending", "Review"));
    }

    [Fact]
    public void AcceptsMatchingOperationAndWindowReferences()
    {
        var schedule = Schedule();
        var chain = Chain();
        var lifecycle = Lifecycle(schedule);

        var assessment = new OperationPreExecutionRevalidation(
            "reval-1", schedule, chain, lifecycle,
            Start, "Unverified", "Review");

        Assert.Same(schedule, assessment.Schedule);
        Assert.Same(chain, assessment.ApprovalChain);
        Assert.Same(lifecycle, assessment.Lifecycle);
    }

    [Fact]
    public void PreservesExplicitAssessmentOffset()
    {
        var schedule = Schedule();
        var assessedAt = Start.AddMinutes(20)
            .ToOffset(TimeSpan.FromHours(3));

        var assessment = new OperationPreExecutionRevalidation(
            "reval-1", schedule, Chain(), Lifecycle(schedule),
            assessedAt, "Pending", "Review");

        Assert.Equal(assessedAt, assessment.AssessedAt);
        Assert.Equal(assessedAt.Offset, assessment.AssessedAt.Offset);
    }

    [Fact]
    public void DoesNotInferTemporalFreshnessOrReadCurrentClock()
    {
        var schedule = Schedule();

        var assessedAt = Start.AddYears(-10);

        var assessment = new OperationPreExecutionRevalidation(
            "reval-1", schedule, Chain(), Lifecycle(schedule),
            assessedAt, "Historical", "Historical assessment");

        Assert.Equal(assessedAt, assessment.AssessedAt);
    }

    [Theory]
    [InlineData(OperationMaintenanceWindowStatus.Unknown)]
    [InlineData(OperationMaintenanceWindowStatus.Planned)]
    [InlineData(OperationMaintenanceWindowStatus.Active)]
    [InlineData(OperationMaintenanceWindowStatus.ClosedEarly)]
    [InlineData(OperationMaintenanceWindowStatus.Closed)]
    [InlineData(OperationMaintenanceWindowStatus.Expired)]
    [InlineData(OperationMaintenanceWindowStatus.Cancelled)]
    public void PreservesReportedStatusWithoutInferringAuthorization(
        OperationMaintenanceWindowStatus status)
    {
        var schedule = Schedule();
        var lifecycle = Lifecycle(schedule, status: status);

        var assessment = new OperationPreExecutionRevalidation(
            "reval-1", schedule, Chain(), lifecycle,
            Start, "Reported", "Requires independent verification");

        Assert.Same(lifecycle, assessment.Lifecycle);
        Assert.Equal(status, assessment.Lifecycle.Status);
    }

    [Fact]
    public void FindingDoesNotGrantPermission()
    {
        var assessment = Create(finding: "Authorized");

        Assert.Equal("Authorized", assessment.Finding);
        Assert.Null(
            typeof(OperationPreExecutionRevalidation).GetProperty(
                "IsAuthorized"));
        Assert.Null(
            typeof(OperationPreExecutionRevalidation).GetProperty(
                "CanExecute"));
    }

    [Fact]
    public void IsPublicSealedProviderNeutralReferenceType()
    {
        var type = typeof(OperationPreExecutionRevalidation);

        Assert.True(type.IsPublic);
        Assert.True(type.IsClass);
        Assert.True(type.IsSealed);
        Assert.Equal("TruthDoctor.Models.Platform", type.Namespace);
    }

    [Fact]
    public void ExposesExactlySevenGetterOnlyProperties()
    {
        var type = typeof(OperationPreExecutionRevalidation);

        var properties = type.GetProperties(
            BindingFlags.Public |
            BindingFlags.Instance |
            BindingFlags.DeclaredOnly);

        Assert.Equal(7, properties.Length);

        var expected = new (string Name, Type Type)[]
        {
            ("RevalidationId", typeof(string)),
            ("Schedule", typeof(OperationExecutionSchedule)),
            ("ApprovalChain", typeof(OperationApprovalChain)),
            ("Lifecycle", typeof(OperationMaintenanceWindowLifecycle)),
            ("AssessedAt", typeof(DateTimeOffset)),
            ("Finding", typeof(string)),
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
            typeof(OperationPreExecutionRevalidation)
                .GetConstructors(
                    BindingFlags.Public | BindingFlags.Instance));

        var parameters = ctor.GetParameters();

        var expectedNames = new[]
        {
            "revalidationId", "schedule", "approvalChain",
            "lifecycle", "assessedAt", "finding", "reason"
        };

        var expectedTypes = new[]
        {
            typeof(string),
            typeof(OperationExecutionSchedule),
            typeof(OperationApprovalChain),
            typeof(OperationMaintenanceWindowLifecycle),
            typeof(DateTimeOffset),
            typeof(string),
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
    public void HasNoPublicOperationalMethods()
    {
        var methods = typeof(OperationPreExecutionRevalidation)
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
    public void DoesNotDeclareIntegrationInterfaces()
    {
        Assert.Empty(
            typeof(OperationPreExecutionRevalidation).GetInterfaces());
    }
}
