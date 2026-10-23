using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationCorrelationIdTests
{
    [Fact]
    public void TypeIsPublicReadonlyValueType()
    {
        var type = typeof(OperationCorrelationId);

        Assert.True(type.IsPublic);
        Assert.True(type.IsValueType);
        Assert.True(type.IsDefined(
            typeof(System.Runtime.CompilerServices.IsReadOnlyAttribute),
            false));
        Assert.True(typeof(IEquatable<OperationCorrelationId>)
            .IsAssignableFrom(type));
    }

    [Fact]
    public void HasExactlyOnePublicGetterOnlyProperty()
    {
        var properties = typeof(OperationCorrelationId)
            .GetProperties(
                BindingFlags.Public | BindingFlags.Instance);

        var property = Assert.Single(properties);

        Assert.Equal("Value", property.Name);
        Assert.Equal(typeof(string), property.PropertyType);
        Assert.NotNull(property.GetMethod);
        Assert.Null(property.SetMethod);
    }

    [Fact]
    public void HasExactlyOnePublicStringConstructor()
    {
        var constructor = Assert.Single(
            typeof(OperationCorrelationId).GetConstructors(
                BindingFlags.Public | BindingFlags.Instance));

        var parameter = Assert.Single(constructor.GetParameters());

        Assert.Equal("value", parameter.Name);
        Assert.Equal(typeof(string), parameter.ParameterType);
        Assert.False(parameter.IsOptional);
    }

    [Theory]
    [InlineData("corr-123")]
    [InlineData("A/B:C")]
    [InlineData("550e8400-e29b-41d4-a716-446655440000")]
    [InlineData("origin_REQUEST-42")]
    [InlineData("equipe/42")]
    [InlineData("x.y_z")]
    public void AcceptsOpaqueProviderNeutralValues(string value)
    {
        var identity = new OperationCorrelationId(value);

        Assert.Equal(value, identity.Value);
        Assert.Equal(value, identity.ToString());
    }

    [Fact]
    public void ConstructedValueAndToStringAgree()
    {
        const string value = "corr-ABC-123";
        var identity = new OperationCorrelationId(value);

        Assert.Equal(value, identity.Value);
        Assert.Equal(value, identity.ToString());
    }

    [Fact]
    public void TrimsOuterWhitespacePreservingCase()
    {
        var identity = new OperationCorrelationId(
            "  Corr-ABC-123  ");

        Assert.Equal("Corr-ABC-123", identity.Value);
        Assert.Equal("Corr-ABC-123", identity.ToString());
    }

    [Fact]
    public void PreservesInternalPrintableCharacters()
    {
        var identity = new OperationCorrelationId(
            "Corr / Group: A_B-7");

        Assert.Equal("Corr / Group: A_B-7", identity.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("\u2003")]
    public void RejectsNullEmptyOrWhitespaceOnly(string? value)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new OperationCorrelationId(value!));
    }

    [Theory]
    [InlineData("corr\n123")]
    [InlineData("corr\r123")]
    [InlineData("corr\t123")]
    [InlineData("corr\0123")]
    [InlineData("corr\u007f123")]
    public void RejectsEmbeddedControlCharacters(string value)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationCorrelationId(value));
    }

    [Fact]
    public void IdenticalValuesProduceEqualIdentitiesAndHashes()
    {
        var first = new OperationCorrelationId("corr-123");
        var second = new OperationCorrelationId("corr-123");

        Assert.Equal(first, second);
        Assert.True(first == second);
        Assert.False(first != second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void NormalizedValuesProduceEqualIdentities()
    {
        var first = new OperationCorrelationId("  Corr-123 ");
        var second = new OperationCorrelationId("Corr-123");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void DifferentCaseValuesRemainDifferentIdentities()
    {
        var upper = new OperationCorrelationId("Corr-123");
        var lower = new OperationCorrelationId("corr-123");

        Assert.NotEqual(upper, lower);
        Assert.True(upper != lower);
    }

    [Fact]
    public void DifferentValuesRemainDifferentIdentities()
    {
        var first = new OperationCorrelationId("corr-123");
        var second = new OperationCorrelationId("corr-124");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void DefaultStructIsUninitializedAndFormatsEmpty()
    {
        var identity = default(OperationCorrelationId);

        Assert.Null(identity.Value);
        Assert.Equal(string.Empty, identity.ToString());
        Assert.NotEqual(
            new OperationCorrelationId("corr-123"),
            identity);
    }

    [Fact]
    public void IdentityDoesNotRequireProviderOrGuid()
    {
        var identity = new OperationCorrelationId("external-abc");

        Assert.Equal("external-abc", identity.Value);
    }

    [Fact]
    public void DoesNotExposeGenerationOrOperationalMethods()
    {
        var methods = typeof(OperationCorrelationId)
            .GetMethods(
                BindingFlags.Public |
                BindingFlags.Static |
                BindingFlags.Instance);

        var forbidden = new[]
        {
            "New", "NewId", "Create", "Generate",
            "Approve", "Authorize", "Execute", "Dispatch",
            "Save", "Load", "Serialize"
        };

        Assert.DoesNotContain(methods,
            method => forbidden.Contains(method.Name));
    }
}
