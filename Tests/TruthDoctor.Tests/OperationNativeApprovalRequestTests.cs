using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationNativeApprovalRequestTests
{
    private static OperationActorKind ValidActorKind =>
        Enum.GetValues<OperationActorKind>()[0];

    private static OperationCorrelationId CreateOperation(
        string value = "operation-001")
    {
        return new OperationCorrelationId(value);
    }

    private static OperationActorIdentity CreateRequester(
        string value = "requester-001")
    {
        return new OperationActorIdentity(ValidActorKind, value);
    }

    [Fact]
    public void PreservesOperationCorrelationIdentity()
    {
        var operation = CreateOperation();

        var request = new OperationNativeApprovalRequest(
            operation, CreateRequester(), "Change requires approval");

        Assert.Equal(operation, request.Operation);
    }

    [Fact]
    public void PreservesRequesterIdentity()
    {
        var requester = CreateRequester();

        var request = new OperationNativeApprovalRequest(
            CreateOperation(), requester, "Change requires approval");

        Assert.Equal(requester, request.Requester);
    }

    [Fact]
    public void PreservesApprovalReason()
    {
        var request = new OperationNativeApprovalRequest(
            CreateOperation(), CreateRequester(), "Review production change");

        Assert.Equal("Review production change", request.Reason);
    }

    [Fact]
    public void TrimsApprovalReason()
    {
        var request = new OperationNativeApprovalRequest(
            CreateOperation(), CreateRequester(), "  Review change  ");

        Assert.Equal("Review change", request.Reason);
    }

    [Fact]
    public void PreservesReasonCaseAndInternalSpacing()
    {
        var request = new OperationNativeApprovalRequest(
            CreateOperation(), CreateRequester(), "Review  PROD Change");

        Assert.Equal("Review  PROD Change", request.Reason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void RejectsNullOrBlankReason(string? reason)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationNativeApprovalRequest(
                CreateOperation(), CreateRequester(), reason!));
    }

    [Theory]
    [InlineData("review\nchange")]
    [InlineData("review\rchange")]
    [InlineData("review\tchange")]
    [InlineData("review\0change")]
    public void RejectsEmbeddedControlCharacters(string reason)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationNativeApprovalRequest(
                CreateOperation(), CreateRequester(), reason));
    }

    [Fact]
    public void RejectsDefaultOperationCorrelationIdentity()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationNativeApprovalRequest(
                default, CreateRequester(), "Review change"));
    }

    [Fact]
    public void RejectsDefaultRequesterIdentity()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationNativeApprovalRequest(
                CreateOperation(), default, "Review change"));
    }

    [Fact]
    public void RejectsRequesterWithUndefinedActorKind()
    {
        var invalidKind = (OperationActorKind)(-1);

        var invalidRequester = new OperationActorIdentity(
            ValidActorKind, "requester-001");

        // The existing actor identity constructor must reject an
        // undefined kind before any approval request can be constructed.
        Assert.Throws<ArgumentException>(() =>
            new OperationActorIdentity(invalidKind, invalidRequester.Value));
    }

    [Fact]
    public void DifferentOperationIdentitiesRemainDistinct()
    {
        var requester = CreateRequester();

        var first = new OperationNativeApprovalRequest(
            CreateOperation("operation-A"), requester, "Review change");

        var second = new OperationNativeApprovalRequest(
            CreateOperation("operation-B"), requester, "Review change");

        Assert.NotEqual(first.Operation, second.Operation);
    }

    [Fact]
    public void DifferentRequesterIdentitiesRemainDistinct()
    {
        var operation = CreateOperation();

        var first = new OperationNativeApprovalRequest(
            operation, CreateRequester("requester-A"), "Review change");

        var second = new OperationNativeApprovalRequest(
            operation, CreateRequester("requester-B"), "Review change");

        Assert.NotEqual(first.Requester, second.Requester);
    }

    [Fact]
    public void DifferentReasonsRemainDistinct()
    {
        var operation = CreateOperation();
        var requester = CreateRequester();

        var first = new OperationNativeApprovalRequest(
            operation, requester, "Reason A");

        var second = new OperationNativeApprovalRequest(
            operation, requester, "Reason B");

        Assert.NotEqual(first.Reason, second.Reason);
    }

    [Fact]
    public void ExposesExactlyThreeGetterOnlyPublicProperties()
    {
        var properties = typeof(OperationNativeApprovalRequest)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance);

        Assert.Equal(3, properties.Length);

        Assert.Single(properties, p =>
            p.Name == "Operation" &&
            p.PropertyType == typeof(OperationCorrelationId) &&
            p.CanRead &&
            !p.CanWrite);

        Assert.Single(properties, p =>
            p.Name == "Requester" &&
            p.PropertyType == typeof(OperationActorIdentity) &&
            p.CanRead &&
            !p.CanWrite);

        Assert.Single(properties, p =>
            p.Name == "Reason" &&
            p.PropertyType == typeof(string) &&
            p.CanRead &&
            !p.CanWrite);
    }

    [Fact]
    public void HasOneThreeArgumentPublicConstructor()
    {
        var constructors = typeof(OperationNativeApprovalRequest)
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        var constructor = Assert.Single(constructors);
        var parameters = constructor.GetParameters();

        Assert.Equal(3, parameters.Length);
        Assert.Equal(typeof(OperationCorrelationId),
            parameters[0].ParameterType);
        Assert.Equal(typeof(OperationActorIdentity),
            parameters[1].ParameterType);
        Assert.Equal(typeof(string),
            parameters[2].ParameterType);
    }

    [Fact]
    public void IsSealedAndIsNotAnAuthorizationService()
    {
        var type = typeof(OperationNativeApprovalRequest);

        Assert.True(type.IsSealed);
        Assert.False(type.IsAbstract);
        Assert.False(type.IsInterface);
    }

    [Fact]
    public void DoesNotExposeApprovalAuthorizationOrExecutionMethods()
    {
        var methods = typeof(OperationNativeApprovalRequest)
            .GetMethods(BindingFlags.Public |
                        BindingFlags.Instance |
                        BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .ToArray();

        Assert.Empty(methods);
    }
}
