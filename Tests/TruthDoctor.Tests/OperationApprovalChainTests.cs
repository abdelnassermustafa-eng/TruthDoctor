using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationApprovalChainTests
{
    private static OperationNativeApprovalRequest CreateRequest()
    {
        var actorKind = Enum.GetValues<OperationActorKind>()[0];

        return new OperationNativeApprovalRequest(
            new OperationCorrelationId("operation-001"),
            new OperationActorIdentity(actorKind, "requester-001"),
            "Infrastructure change requires approval");
    }

    private static OperationApprovalStage CreateStage(
        string stageId = "stage-001",
        string role = "change-manager")
    {
        return new OperationApprovalStage(
            stageId,
            new OperationRoleIdentity(role));
    }

    [Fact]
    public void PreservesOriginalNativeApprovalRequestReference()
    {
        var request = CreateRequest();

        var chain = new OperationApprovalChain(
            request,
            new[] { CreateStage() });

        Assert.Same(request, chain.Request);
    }

    [Fact]
    public void StoresStagesInDeclaredOrder()
    {
        var first = CreateStage("stage-001");
        var second = CreateStage("stage-002", "security-operator");
        var third = CreateStage("stage-003");

        var chain = new OperationApprovalChain(
            CreateRequest(),
            new[] { first, second, third });

        Assert.Equal(3, chain.Stages.Count);
        Assert.Same(first, chain.Stages[0]);
        Assert.Same(second, chain.Stages[1]);
        Assert.Same(third, chain.Stages[2]);
    }

    [Fact]
    public void PreservesOriginalApprovalStageReferences()
    {
        var stage = CreateStage();

        var chain = new OperationApprovalChain(
            CreateRequest(),
            new[] { stage });

        Assert.Same(stage, Assert.Single(chain.Stages));
    }

    [Fact]
    public void AllowsSameApproverRoleInDifferentStages()
    {
        var first = CreateStage("stage-001", "change-manager");
        var second = CreateStage("stage-002", "change-manager");

        var chain = new OperationApprovalChain(
            CreateRequest(),
            new[] { first, second });

        Assert.Equal(2, chain.Stages.Count);
        Assert.Equal(
            chain.Stages[0].ApproverRole,
            chain.Stages[1].ApproverRole);
    }

    [Fact]
    public void RejectsNullNativeApprovalRequest()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationApprovalChain(
                null!,
                new[] { CreateStage() }));
    }

    [Fact]
    public void RejectsNullStagesCollection()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationApprovalChain(
                CreateRequest(),
                null!));
    }

    [Fact]
    public void RejectsEmptyStagesCollection()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationApprovalChain(
                CreateRequest(),
                Array.Empty<OperationApprovalStage>()));
    }

    [Fact]
    public void RejectsNullStageEntry()
    {
        var stages = new OperationApprovalStage[]
        {
            CreateStage(),
            null!
        };

        Assert.Throws<ArgumentException>(() =>
            new OperationApprovalChain(CreateRequest(), stages));
    }

    [Theory]
    [InlineData("stage-001", "stage-001")]
    [InlineData("stage-001", "STAGE-001")]
    [InlineData("Stage-One", "stage-one")]
    [InlineData("STAGE-one", "stage-ONE")]
    public void RejectsDuplicateStageIdsIgnoringCase(
        string firstId,
        string secondId)
    {
        var stages = new[]
        {
            CreateStage(firstId),
            CreateStage(secondId)
        };

        Assert.Throws<ArgumentException>(() =>
            new OperationApprovalChain(CreateRequest(), stages));
    }

    [Fact]
    public void RejectsDuplicateStageIdsAfterTrimming()
    {
        var stages = new[]
        {
            CreateStage("  stage-001  "),
            CreateStage("STAGE-001")
        };

        Assert.Throws<ArgumentException>(() =>
            new OperationApprovalChain(CreateRequest(), stages));
    }

    [Fact]
    public void SnapshotsMutableInputCollection()
    {
        var original = CreateStage("stage-001");
        var input = new List<OperationApprovalStage> { original };

        var chain = new OperationApprovalChain(CreateRequest(), input);

        input[0] = CreateStage("stage-002");
        input.Add(CreateStage("stage-003"));
        input.Clear();

        Assert.Same(original, Assert.Single(chain.Stages));
    }

    [Fact]
    public void ExposesReadOnlyStageCollection()
    {
        var chain = new OperationApprovalChain(
            CreateRequest(),
            new[] { CreateStage() });

        var collection =
            Assert.IsAssignableFrom<IList<OperationApprovalStage>>(
                chain.Stages);

        Assert.True(collection.IsReadOnly);

        Assert.Throws<NotSupportedException>(() =>
            collection.Add(CreateStage("stage-002")));

        Assert.Throws<NotSupportedException>(() =>
            collection[0] = CreateStage("stage-003"));
    }

    [Fact]
    public void SeparateChainsPreserveIndependentStageDeclarations()
    {
        var first = CreateStage("first-stage");
        var second = CreateStage("second-stage");

        var firstChain = new OperationApprovalChain(
            CreateRequest(),
            new[] { first });

        var secondChain = new OperationApprovalChain(
            CreateRequest(),
            new[] { second });

        Assert.Same(first, Assert.Single(firstChain.Stages));
        Assert.Same(second, Assert.Single(secondChain.Stages));
    }

    [Fact]
    public void ExposesExactlyTwoGetterOnlyPublicProperties()
    {
        var properties = typeof(OperationApprovalChain)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance);

        Assert.Equal(2, properties.Length);

        Assert.Single(properties, property =>
            property.Name == "Request" &&
            property.PropertyType ==
                typeof(OperationNativeApprovalRequest) &&
            property.CanRead &&
            !property.CanWrite);

        Assert.Single(properties, property =>
            property.Name == "Stages" &&
            property.PropertyType ==
                typeof(IReadOnlyList<OperationApprovalStage>) &&
            property.CanRead &&
            !property.CanWrite);
    }

    [Fact]
    public void HasOneTwoArgumentPublicConstructor()
    {
        var constructor = Assert.Single(
            typeof(OperationApprovalChain)
                .GetConstructors(
                    BindingFlags.Public | BindingFlags.Instance));

        var parameters = constructor.GetParameters();

        Assert.Equal(2, parameters.Length);
        Assert.Equal(
            typeof(OperationNativeApprovalRequest),
            parameters[0].ParameterType);
        Assert.Equal(
            typeof(IEnumerable<OperationApprovalStage>),
            parameters[1].ParameterType);
    }

    [Fact]
    public void IsSealedAndNotAnApprovalService()
    {
        var type = typeof(OperationApprovalChain);

        Assert.True(type.IsSealed);
        Assert.False(type.IsAbstract);
        Assert.False(type.IsInterface);
    }

    [Fact]
    public void DoesNotExposeApprovalOrExecutionMethods()
    {
        var methods = typeof(OperationApprovalChain)
            .GetMethods(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .ToArray();

        Assert.Empty(methods);
    }
}
