using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationActorIdentityTests
{
    [Fact]
    public void ActorKindDefinesHumanAndService()
    {
        Assert.True(typeof(OperationActorKind).IsEnum);
        Assert.Equal(
            new[] { "Human", "Service" },
            Enum.GetNames<OperationActorKind>());
    }

    [Fact]
    public void IdentityIsPublicReadonlyValueType()
    {
        var type = typeof(OperationActorIdentity);

        Assert.True(type.IsPublic);
        Assert.True(type.IsValueType);
        Assert.True(type.IsDefined(
            typeof(System.Runtime.CompilerServices.IsReadOnlyAttribute),
            false));
        Assert.True(
            typeof(IEquatable<OperationActorIdentity>)
                .IsAssignableFrom(type));
    }

    [Fact]
    public void HasExactlyTwoGetterOnlyProperties()
    {
        var properties = typeof(OperationActorIdentity)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance);

        Assert.Equal(2, properties.Length);

        var kind = Assert.Single(properties,
            property => property.Name == "Kind");

        Assert.Equal(typeof(OperationActorKind), kind.PropertyType);
        Assert.NotNull(kind.GetMethod);
        Assert.Null(kind.SetMethod);

        var value = Assert.Single(properties,
            property => property.Name == "Value");

        Assert.Equal(typeof(string), value.PropertyType);
        Assert.NotNull(value.GetMethod);
        Assert.Null(value.SetMethod);
    }

    [Fact]
    public void HasExactlyOnePublicConstructor()
    {
        var constructor = Assert.Single(
            typeof(OperationActorIdentity).GetConstructors(
                BindingFlags.Public | BindingFlags.Instance));

        var parameters = constructor.GetParameters();

        Assert.Equal(2, parameters.Length);
        Assert.Equal("kind", parameters[0].Name);
        Assert.Equal(typeof(OperationActorKind),
            parameters[0].ParameterType);
        Assert.Equal("value", parameters[1].Name);
        Assert.Equal(typeof(string), parameters[1].ParameterType);
        Assert.All(parameters,
            parameter => Assert.False(parameter.IsOptional));
    }

    [Theory]
    [InlineData(OperationActorKind.Human, "user-123")]
    [InlineData(OperationActorKind.Service, "service-abc")]
    [InlineData(OperationActorKind.Human, "Directory/Team:A_7")]
    [InlineData(OperationActorKind.Service, "svc.cloud/ABC-42")]
    public void AcceptsOpaqueProviderNeutralIdentifiers(
        OperationActorKind kind,
        string value)
    {
        var actor = new OperationActorIdentity(kind, value);

        Assert.Equal(kind, actor.Kind);
        Assert.Equal(value, actor.Value);
        Assert.Equal(value, actor.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("\u2003")]
    public void RejectsNullEmptyAndWhitespaceOnly(
        string? value)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new OperationActorIdentity(
                OperationActorKind.Human, value!));
    }

    [Theory]
    [InlineData("actor\n123")]
    [InlineData("actor\r123")]
    [InlineData("actor\t123")]
    [InlineData("actor\u007f123")]
    public void RejectsControlCharacters(string value)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationActorIdentity(
                OperationActorKind.Human, value));
    }

    [Fact]
    public void TrimsOuterWhitespaceAndPreservesCase()
    {
        var actor = new OperationActorIdentity(
            OperationActorKind.Human, "  User-ABC-123  ");

        Assert.Equal("User-ABC-123", actor.Value);
        Assert.Equal("User-ABC-123", actor.ToString());
    }

    [Fact]
    public void PreservesInternalPrintableCharacters()
    {
        var actor = new OperationActorIdentity(
            OperationActorKind.Service,
            "Service / Group: A_B-7");

        Assert.Equal("Service / Group: A_B-7", actor.Value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    [InlineData(999)]
    public void RejectsUndefinedActorKinds(int rawKind)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationActorIdentity(
                (OperationActorKind)rawKind, "actor-123"));
    }

    [Fact]
    public void EqualKindsAndNormalizedValuesProduceEquality()
    {
        var first = new OperationActorIdentity(
            OperationActorKind.Human, "  User-123 ");
        var second = new OperationActorIdentity(
            OperationActorKind.Human, "User-123");

        Assert.Equal(first, second);
        Assert.True(first == second);
        Assert.False(first != second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void DifferentKindsRemainDifferentIdentities()
    {
        var human = new OperationActorIdentity(
            OperationActorKind.Human, "actor-123");
        var service = new OperationActorIdentity(
            OperationActorKind.Service, "actor-123");

        Assert.NotEqual(human, service);
    }

    [Fact]
    public void DifferentCaseRemainsDifferentIdentity()
    {
        var upper = new OperationActorIdentity(
            OperationActorKind.Human, "Actor-123");
        var lower = new OperationActorIdentity(
            OperationActorKind.Human, "actor-123");

        Assert.NotEqual(upper, lower);
    }

    [Fact]
    public void DifferentValuesRemainDifferentIdentities()
    {
        var first = new OperationActorIdentity(
            OperationActorKind.Service, "svc-123");
        var second = new OperationActorIdentity(
            OperationActorKind.Service, "svc-124");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void DefaultIdentityIsUninitialized()
    {
        var actor = default(OperationActorIdentity);

        Assert.Null(actor.Value);
        Assert.Equal(string.Empty, actor.ToString());
        Assert.NotEqual(
            new OperationActorIdentity(
                OperationActorKind.Human, "actor-123"),
            actor);
    }

    [Fact]
    public void DoesNotRequireProviderAccountOrCredentials()
    {
        var actor = new OperationActorIdentity(
            OperationActorKind.Service, "automation-agent-7");

        Assert.Equal("automation-agent-7", actor.Value);
        Assert.Equal(OperationActorKind.Service, actor.Kind);
    }

    [Fact]
    public void DoesNotExposeAuthorizationOrExecutionMethods()
    {
        var methods = typeof(OperationActorIdentity)
            .GetMethods(
                BindingFlags.Public |
                BindingFlags.Static |
                BindingFlags.Instance);

        var forbidden = new[]
        {
            "Authenticate", "Login", "Authorize",
            "Grant", "Deny", "Approve", "Reject",
            "Schedule", "Execute", "Dispatch",
            "Save", "Load"
        };

        Assert.DoesNotContain(methods,
            method => forbidden.Contains(method.Name));
    }
}
