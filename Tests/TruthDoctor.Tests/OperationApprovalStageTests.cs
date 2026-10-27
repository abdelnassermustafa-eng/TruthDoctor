using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationApprovalStageTests
{
    private static OperationRoleIdentity CreateRole(
        string value = "change-manager")
    {
        return new OperationRoleIdentity(value);
    }

    [Fact]
    public void StoresExactStageIdentity()
    {
        var stage = new OperationApprovalStage(
            "stage-001", CreateRole());

        Assert.Equal("stage-001", stage.StageId);
    }

    [Fact]
    public void PreservesExactApproverRoleIdentity()
    {
        var role = CreateRole();

        var stage = new OperationApprovalStage(
            "stage-001", role);

        Assert.Equal(role, stage.ApproverRole);
    }

    [Fact]
    public void TrimsSurroundingStageIdWhitespace()
    {
        var stage = new OperationApprovalStage(
            "  stage-001  ", CreateRole());

        Assert.Equal("stage-001", stage.StageId);
    }

    [Fact]
    public void PreservesStageIdCaseAndInternalSpacing()
    {
        var stage = new OperationApprovalStage(
            "Stage  ONE", CreateRole());

        Assert.Equal("Stage  ONE", stage.StageId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void RejectsNullOrBlankStageId(string? stageId)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationApprovalStage(stageId!, CreateRole()));
    }

    [Theory]
    [InlineData("stage\none")]
    [InlineData("stage\rone")]
    [InlineData("stage\tone")]
    [InlineData("stage\0one")]
    public void RejectsEmbeddedControlCharacters(string stageId)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationApprovalStage(stageId, CreateRole()));
    }

    [Fact]
    public void RejectsDefaultApproverRoleIdentity()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationApprovalStage("stage-001", default));
    }

    [Fact]
    public void DifferentStageIdsRemainDistinct()
    {
        var first = new OperationApprovalStage(
            "stage-001", CreateRole());

        var second = new OperationApprovalStage(
            "stage-002", CreateRole());

        Assert.NotEqual(first.StageId, second.StageId);
    }

    [Fact]
    public void DifferentApproverRolesRemainDistinct()
    {
        var first = new OperationApprovalStage(
            "stage-001", CreateRole("change-manager"));

        var second = new OperationApprovalStage(
            "stage-001", CreateRole("security-operator"));

        Assert.NotEqual(first.ApproverRole, second.ApproverRole);
    }

    [Fact]
    public void AllowsSameApproverRoleForSeparateStageDeclarations()
    {
        var role = CreateRole();

        var first = new OperationApprovalStage(
            "stage-001", role);

        var second = new OperationApprovalStage(
            "stage-002", role);

        Assert.Equal(first.ApproverRole, second.ApproverRole);
        Assert.NotEqual(first.StageId, second.StageId);
    }

    [Fact]
    public void ExposesExactlyTwoGetterOnlyPublicProperties()
    {
        var properties = typeof(OperationApprovalStage)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance);

        Assert.Equal(2, properties.Length);

        Assert.Single(properties, property =>
            property.Name == "StageId" &&
            property.PropertyType == typeof(string) &&
            property.CanRead &&
            !property.CanWrite);

        Assert.Single(properties, property =>
            property.Name == "ApproverRole" &&
            property.PropertyType == typeof(OperationRoleIdentity) &&
            property.CanRead &&
            !property.CanWrite);
    }

    [Fact]
    public void HasOneTwoArgumentPublicConstructor()
    {
        var constructors = typeof(OperationApprovalStage)
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        var constructor = Assert.Single(constructors);
        var parameters = constructor.GetParameters();

        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal(
            typeof(OperationRoleIdentity),
            parameters[1].ParameterType);
    }

    [Fact]
    public void IsSealedAndNotAnApprovalService()
    {
        var type = typeof(OperationApprovalStage);

        Assert.True(type.IsSealed);
        Assert.False(type.IsAbstract);
        Assert.False(type.IsInterface);
    }

    [Fact]
    public void DoesNotExposeApprovalOrExecutionMethods()
    {
        var methods = typeof(OperationApprovalStage)
            .GetMethods(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .ToArray();

        Assert.Empty(methods);
    }
}
