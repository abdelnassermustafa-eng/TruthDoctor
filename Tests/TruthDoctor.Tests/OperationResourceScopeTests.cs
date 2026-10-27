using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationResourceScopeTests
{
    private static OperationTarget CreateTarget(
        string providerId = "aws",
        string accountId = "123456789012",
        string location = "us-east-1",
        string domainId = "compute",
        string resourceType = "instance",
        string nativeId = "i-AbC123")
    {
        return new OperationTarget(
            providerId,
            accountId,
            location,
            domainId,
            resourceType,
            nativeId);
    }

    [Fact]
    public void ConstructsFromAnExistingOperationTarget()
    {
        var target = CreateTarget();
        var scope = new OperationResourceScope(target);

        Assert.Same(target, scope.Target);
    }

    [Fact]
    public void RejectsNullTarget()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationResourceScope(null!));
    }

    [Fact]
    public void MatchesItsDeclaredTarget()
    {
        var target = CreateTarget();
        var scope = new OperationResourceScope(target);

        Assert.True(scope.Matches(target));
    }

    [Fact]
    public void MatchesEquivalentTargetIdentity()
    {
        var scope = new OperationResourceScope(CreateTarget());

        Assert.True(scope.Matches(CreateTarget()));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void RejectsMismatchInAnyIdentityDimension(int dimension)
    {
        var scope = new OperationResourceScope(CreateTarget());

        var values = new[]
        {
            "aws",
            "123456789012",
            "us-east-1",
            "compute",
            "instance",
            "i-AbC123"
        };

        values[dimension] += "-other";

        var candidate = new OperationTarget(
            values[0],
            values[1],
            values[2],
            values[3],
            values[4],
            values[5]);

        Assert.False(scope.Matches(candidate));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void DoesNotIgnoreIdentifierCase(int dimension)
    {
        var scope = new OperationResourceScope(CreateTarget());

        var values = new[]
        {
            "aws",
            "Account-AbC123",
            "us-east-1",
            "compute",
            "instance",
            "i-AbC123"
        };

        values[dimension] = values[dimension].ToUpperInvariant();

        var candidate = new OperationTarget(
            values[0],
            values[1],
            values[2],
            values[3],
            values[4],
            values[5]);

        Assert.False(scope.Matches(candidate));
    }

    [Fact]
    public void RejectsNullCandidate()
    {
        var scope = new OperationResourceScope(CreateTarget());

        Assert.False(scope.Matches(null));
    }

    [Fact]
    public void IsSealedAndExposesReadOnlyTarget()
    {
        var type = typeof(OperationResourceScope);

        Assert.True(type.IsSealed);

        var targetProperty = type.GetProperty(
            "Target",
            BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(targetProperty);
        Assert.Equal(
            typeof(OperationTarget),
            targetProperty!.PropertyType);
        Assert.Null(targetProperty.SetMethod);

        Assert.Single(
            type.GetProperties(
                BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void DoesNotExposeAuthorizationOrExecutionMethods()
    {
        var methods = typeof(OperationResourceScope)
            .GetMethods(
                BindingFlags.Instance |
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.DeclaredOnly);

        Assert.DoesNotContain(
            methods,
            method =>
                method.Name.Contains(
                    "Authorize",
                    StringComparison.OrdinalIgnoreCase) ||
                method.Name.Contains(
                    "Execute",
                    StringComparison.OrdinalIgnoreCase) ||
                method.Name.Contains(
                    "Grant",
                    StringComparison.OrdinalIgnoreCase) ||
                method.Name.Contains(
                    "Approve",
                    StringComparison.OrdinalIgnoreCase));
    }
}
