using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A6V3DistributionEvidenceRequirementCatalogTests
{
    private const string CatalogName =
        "TruthDoctorV3DistributionEvidenceRequirementCatalog";

    private static Type RequireCatalog()
    {
        Assembly assembly = Assembly.Load("TruthDoctor");

        Type? catalog = assembly.GetTypes()
            .SingleOrDefault(type => type.Name == CatalogName);

        Assert.True(
            catalog is not null,
            "RED: the non-authorizing catalog has not been implemented.");

        return catalog!;
    }

    private static string[] ExpectedIds()
    {
        return Enumerable.Range(1, 4)
            .SelectMany(stage => Enumerable.Range(1, 4)
                .Select(index => $"D{stage:00}-R{index:00}"))
            .ToArray();
    }

    private static string[] GetIds()
    {
        Type type = RequireCatalog();

        MethodInfo? method = type.GetMethod(
            "GetAllRequirementIds",
            BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull(method);

        object? result = method!.Invoke(null, null);

        Assert.IsAssignableFrom<IEnumerable<string>>(result);

        return ((IEnumerable<string>)result!).ToArray();
    }

    private static string GetStage(string identifier)
    {
        Type type = RequireCatalog();

        MethodInfo? method = type.GetMethod(
            "GetStage",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(string) },
            modifiers: null);

        Assert.NotNull(method);

        object? result = method!.Invoke(
            null, new object?[] { identifier });

        Assert.IsType<string>(result);

        return (string)result!;
    }

    [Fact]
    public void CatalogTypeMustExist()
    {
        Type type = RequireCatalog();
        Assert.True(type.IsClass);
        Assert.True(type.IsPublic);
    }

    [Fact]
    public void CatalogMustContainSixteenUniqueRequirements()
    {
        string[] identifiers = GetIds();

        Assert.Equal(16, identifiers.Length);
        Assert.Equal(16, identifiers.Distinct().Count());
    }

    [Fact]
    public void RequirementIdsMustPreserveCanonicalOrder()
    {
        Assert.Equal(ExpectedIds(), GetIds());
    }

    [Fact]
    public void RequirementsMustPreserveFourDistributionStages()
    {
        string[] stages = {
            "publisher_identity",
            "artifact_integrity",
            "anchor_binding",
            "first_run_review"
        };

        string[] identifiers = ExpectedIds();

        for (int i = 0; i < identifiers.Length; i++)
        {
            Assert.Equal(
                stages[i / 4],
                GetStage(identifiers[i]));
        }
    }

    [Fact]
    public void UnknownRequirementIdentityMustBeRejected()
    {
        Type type = RequireCatalog();

        MethodInfo? method = type.GetMethod(
            "GetStage",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(string) },
            modifiers: null);

        Assert.NotNull(method);

        TargetInvocationException exception =
            Assert.Throws<TargetInvocationException>(
                () => method!.Invoke(
                    null, new object?[] { "D99-R99" }));

        Assert.IsAssignableFrom<ArgumentException>(
            exception.InnerException);
    }

    [Fact]
    public void CatalogMustExposeNoSecurityAuthorizationApi()
    {
        Type type = RequireCatalog();

        string[] forbidden = {
            "VerifySignature",
            "AuthenticateRoot",
            "EnrollRoot",
            "WriteProtectedState",
            "AuthorizeRelease",
            "InstallProvider",
            "ExecuteProvider"
        };

        string[] names = type
            .GetMethods(
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Static |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly)
            .Select(method => method.Name)
            .ToArray();

        foreach (string name in forbidden)
        {
            Assert.DoesNotContain(name, names);
        }
    }
}
