using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationRoleIdentityTests
{
    [Fact]
    public void IdentityIsPublicReadonlyValueType()
    {
        var type = typeof(OperationRoleIdentity);

        Assert.True(type.IsPublic);
        Assert.True(type.IsValueType);
        Assert.True(type.IsDefined(
            typeof(System.Runtime.CompilerServices.IsReadOnlyAttribute),
            false));
        Assert.True(
            typeof(IEquatable<OperationRoleIdentity>)
                .IsAssignableFrom(type));
    }

    [Fact]
    public void HasExactlyOneGetterOnlyProperty()
    {
        var properties = typeof(OperationRoleIdentity)
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
            typeof(OperationRoleIdentity).GetConstructors(
                BindingFlags.Public | BindingFlags.Instance));

        var parameter = Assert.Single(constructor.GetParameters());

        Assert.Equal("value", parameter.Name);
        Assert.Equal(typeof(string), parameter.ParameterType);
        Assert.False(parameter.IsOptional);
    }

    [Theory]
    [InlineData("viewer")]
    [InlineData("operator")]
    [InlineData("security-reviewer")]
    [InlineData("platform-admin")]
    [InlineData("cloud-engineer")]
    [InlineData("role2")]
    [InlineData("ops-team-v2")]
    public void AcceptsCanonicalRoleIdentifiers(string value)
    {
        var role = new OperationRoleIdentity(value);

        Assert.Equal(value, role.Value);
        Assert.Equal(value, role.ToString());
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
            new OperationRoleIdentity(value!));
    }

    [Theory]
    [InlineData("Viewer")]
    [InlineData("PLATFORM-ADMIN")]
    [InlineData("platform-Admin")]
    [InlineData("2viewer")]
    [InlineData("-viewer")]
    [InlineData("viewer-")]
    [InlineData("viewer--admin")]
    [InlineData("viewer_admin")]
    [InlineData("viewer.admin")]
    [InlineData("viewer/admin")]
    [InlineData("viewer:admin")]
    [InlineData("viewer.*")]
    [InlineData("*")]
    [InlineData("all*")]
    [InlineData(" viewer")]
    [InlineData("viewer ")]
    [InlineData("viewer admin")]
    [InlineData("viewer\nadmin")]
    [InlineData("viewer\tadmin")]
    public void RejectsNoncanonicalRoleIdentifiers(string value)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationRoleIdentity(value));
    }

    [Fact]
    public void PreservesCanonicalRoleIdentifier()
    {
        var role = new OperationRoleIdentity("security-reviewer");

        Assert.Equal("security-reviewer", role.Value);
        Assert.Equal("security-reviewer", role.ToString());
    }

    [Fact]
    public void EqualValuesProduceValueEquality()
    {
        var first = new OperationRoleIdentity("platform-admin");
        var second = new OperationRoleIdentity("platform-admin");

        Assert.Equal(first, second);
        Assert.True(first == second);
        Assert.False(first != second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void DifferentValuesRemainDifferent()
    {
        var first = new OperationRoleIdentity("viewer");
        var second = new OperationRoleIdentity("operator");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void DefaultIdentityIsUninitialized()
    {
        var role = default(OperationRoleIdentity);

        Assert.Null(role.Value);
        Assert.Equal(string.Empty, role.ToString());
        Assert.NotEqual(new OperationRoleIdentity("viewer"), role);
    }

    [Fact]
    public void AdministratorLikeNameDoesNotGrantAuthority()
    {
        var role = new OperationRoleIdentity("platform-admin");

        Assert.Equal("platform-admin", role.Value);

        var forbidden = new[]
        {
            "IsAdmin", "IsAdministrator", "IsAuthorized",
            "CanExecute", "HasPermission", "Permissions",
            "Grants", "Privileges"
        };

        var properties = typeof(OperationRoleIdentity)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance);

        Assert.DoesNotContain(properties,
            property => forbidden.Contains(property.Name));
    }

    [Fact]
    public void DoesNotExposeAuthorizationOrExecutionMethods()
    {
        var methods = typeof(OperationRoleIdentity)
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
