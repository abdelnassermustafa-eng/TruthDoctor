using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationPermissionIdentityTests
{
    [Fact]
    public void IdentityIsPublicReadonlyValueType()
    {
        var type = typeof(OperationPermissionIdentity);

        Assert.True(type.IsPublic);
        Assert.True(type.IsValueType);
        Assert.True(type.IsDefined(
            typeof(System.Runtime.CompilerServices.IsReadOnlyAttribute),
            false));
        Assert.True(
            typeof(IEquatable<OperationPermissionIdentity>)
                .IsAssignableFrom(type));
    }

    [Fact]
    public void HasExactlyOneGetterOnlyProperty()
    {
        var properties = typeof(OperationPermissionIdentity)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var property = Assert.Single(properties);

        Assert.Equal("Value", property.Name);
        Assert.Equal(typeof(string), property.PropertyType);
        Assert.NotNull(property.GetMethod);
        Assert.Null(property.SetMethod);
    }

    [Fact]
    public void HasExactlyOnePublicConstructor()
    {
        var constructor = Assert.Single(
            typeof(OperationPermissionIdentity).GetConstructors(
                BindingFlags.Public | BindingFlags.Instance));

        var parameter = Assert.Single(constructor.GetParameters());

        Assert.Equal("value", parameter.Name);
        Assert.Equal(typeof(string), parameter.ParameterType);
        Assert.False(parameter.IsOptional);
    }

    [Theory]
    [InlineData("resources.read")]
    [InlineData("resources.modify")]
    [InlineData("topology.read")]
    [InlineData("operations.submit")]
    [InlineData("operations.review")]
    [InlineData("network.route-table.read")]
    [InlineData("platform.v2.read")]
    public void AcceptsCanonicalPermissionIdentifiers(string value)
    {
        var permission = new OperationPermissionIdentity(value);

        Assert.Equal(value, permission.Value);
        Assert.Equal(value, permission.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("\u2003")]
    public void RejectsNullEmptyAndWhitespaceOnly(string? value)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new OperationPermissionIdentity(value!));
    }

    [Theory]
    [InlineData("resources")]
    [InlineData(".resources.read")]
    [InlineData("resources.read.")]
    [InlineData("resources..read")]
    [InlineData("Resources.read")]
    [InlineData("resources.Read")]
    [InlineData("resources.READ")]
    [InlineData("1resources.read")]
    [InlineData("resources.1read")]
    [InlineData("resources.-read")]
    [InlineData("resources.read-")]
    [InlineData("resources_read")]
    [InlineData("resources/read")]
    [InlineData("resources:read")]
    [InlineData("resources.*")]
    [InlineData("*")]
    [InlineData("resources.read ")]
    [InlineData(" resources.read")]
    [InlineData("resources.\nread")]
    [InlineData("resources.\tread")]
    public void RejectsNoncanonicalPermissionIdentifiers(string value)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationPermissionIdentity(value));
    }

    [Fact]
    public void PreservesCanonicalIdentifierWithoutNormalization()
    {
        var permission = new OperationPermissionIdentity(
            "network.route-table.read");

        Assert.Equal("network.route-table.read", permission.Value);
    }

    [Fact]
    public void EqualValuesProduceValueEquality()
    {
        var first = new OperationPermissionIdentity("resources.read");
        var second = new OperationPermissionIdentity("resources.read");

        Assert.Equal(first, second);
        Assert.True(first == second);
        Assert.False(first != second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void DifferentValuesRemainDifferent()
    {
        var first = new OperationPermissionIdentity("resources.read");
        var second = new OperationPermissionIdentity("resources.modify");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void DefaultIdentityIsUninitialized()
    {
        var permission = default(OperationPermissionIdentity);

        Assert.Null(permission.Value);
        Assert.Equal(string.Empty, permission.ToString());
        Assert.NotEqual(
            new OperationPermissionIdentity("resources.read"),
            permission);
    }

    [Fact]
    public void DoesNotExposeAuthorizationOrExecutionMethods()
    {
        var methods = typeof(OperationPermissionIdentity)
            .GetMethods(
                BindingFlags.Public |
                BindingFlags.Static |
                BindingFlags.Instance);

        var forbidden = new[]
        {
            "Authenticate", "Login", "Authorize",
            "IsAuthorized", "HasPermission",
            "Grant", "Deny", "Approve", "Reject",
            "Schedule", "Execute", "Dispatch",
            "Save", "Load"
        };

        Assert.DoesNotContain(methods,
            method => forbidden.Contains(method.Name));
    }
}
