using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationDependencyVerificationTests
{
    private static readonly DateTimeOffset Timestamp =
        new(2026, 10, 29, 14, 0, 0, TimeSpan.Zero);

    private static OperationTarget Target(
        string nativeId,
        int change = -1)
    {
        var fields = new[]
        {
            "provider", "account-01", "us-east-1",
            "compute", "instance", nativeId
        };

        if (change >= 0)
        {
            fields[change] += "-different";
        }

        return new OperationTarget(
            fields[0], fields[1], fields[2],
            fields[3], fields[4], fields[5]);
    }

    private static OperationTarget Subject(int change = -1) =>
        Target("subject-42", change);

    private static OperationTarget Dependency(int change = -1) =>
        Target("dependency-73", change);

    private static OperationDependencyObservation Observation(
        OperationDependencyObservationDisposition disposition =
            OperationDependencyObservationDisposition.Observed,
        OperationTarget? subject = null,
        OperationTarget? dependency = null,
        string relationship = "depends-on")
    {
        return new OperationDependencyObservation(
            new OperationCorrelationId("operation-42"),
            subject ?? Subject(),
            dependency ?? Dependency(),
            relationship,
            Timestamp,
            disposition);
    }

    private static OperationDependencyComparison Compare(
        string key = "dependency.relationship",
        string expected = "present",
        OperationDependencyObservationDisposition disposition =
            OperationDependencyObservationDisposition.Observed,
        OperationTarget? subject = null,
        OperationTarget? dependency = null,
        string relationship = "depends-on",
        string declaredRelationship = "depends-on")
    {
        return OperationDependencyComparison.Evaluate(
            new OperationOutcomeCondition(key, expected),
            new OperationResourceScope(Subject()),
            Dependency(),
            declaredRelationship,
            Observation(disposition, subject, dependency, relationship));
    }

    [Fact]
    public void ObservationDispositionHasExplicitNumericValues()
    {
        Assert.Equal(0, (int)OperationDependencyObservationDisposition.Unknown);
        Assert.Equal(1, (int)OperationDependencyObservationDisposition.Observed);
        Assert.Equal(2, (int)OperationDependencyObservationDisposition.Absent);
        Assert.Equal(3, (int)OperationDependencyObservationDisposition.Unavailable);
        Assert.Equal(4, (int)OperationDependencyObservationDisposition.NotAssessed);
        Assert.Equal(5,
            Enum.GetValues<OperationDependencyObservationDisposition>().Length);
    }

    [Fact]
    public void ComparisonDispositionHasExplicitNumericValues()
    {
        Assert.Equal(0, (int)OperationDependencyComparisonDisposition.Unknown);
        Assert.Equal(1, (int)OperationDependencyComparisonDisposition.Matched);
        Assert.Equal(2, (int)OperationDependencyComparisonDisposition.Mismatched);
        Assert.Equal(3, (int)OperationDependencyComparisonDisposition.SubjectMismatch);
        Assert.Equal(4, (int)OperationDependencyComparisonDisposition.DependencyMismatch);
        Assert.Equal(5, (int)OperationDependencyComparisonDisposition.RelationshipMismatch);
        Assert.Equal(6, (int)OperationDependencyComparisonDisposition.UnsupportedCondition);
        Assert.Equal(7, (int)OperationDependencyComparisonDisposition.Absent);
        Assert.Equal(8, (int)OperationDependencyComparisonDisposition.Unavailable);
        Assert.Equal(9, (int)OperationDependencyComparisonDisposition.NoObservation);
        Assert.Equal(10, (int)OperationDependencyComparisonDisposition.NotAssessed);
        Assert.Equal(11,
            Enum.GetValues<OperationDependencyComparisonDisposition>().Length);
    }

    [Theory]
    [InlineData(OperationDependencyObservationDisposition.Unknown)]
    [InlineData(OperationDependencyObservationDisposition.Observed)]
    [InlineData(OperationDependencyObservationDisposition.Absent)]
    [InlineData(OperationDependencyObservationDisposition.Unavailable)]
    [InlineData(OperationDependencyObservationDisposition.NotAssessed)]
    public void ObservationPreservesExplicitDisposition(
        OperationDependencyObservationDisposition disposition)
    {
        var observed = Observation(disposition);
        Assert.Equal(disposition, observed.Disposition);
        Assert.Equal("depends-on", observed.Relationship);
        Assert.Equal(Timestamp, observed.ObservedAt);
    }

    [Fact]
    public void ObservationPreservesOperationAndBothTargets()
    {
        var operation = new OperationCorrelationId("operation-42");
        var subject = Subject();
        var dependency = Dependency();

        var observed = new OperationDependencyObservation(
            operation, subject, dependency,
            "depends-on", Timestamp,
            OperationDependencyObservationDisposition.Observed);

        Assert.Equal(operation, observed.Operation);
        Assert.Same(subject, observed.Subject);
        Assert.Same(dependency, observed.Dependency);
    }

    [Theory]
    [InlineData("present",
        OperationDependencyObservationDisposition.Observed,
        OperationDependencyComparisonDisposition.Matched)]
    [InlineData("present",
        OperationDependencyObservationDisposition.Absent,
        OperationDependencyComparisonDisposition.Absent)]
    [InlineData("absent",
        OperationDependencyObservationDisposition.Absent,
        OperationDependencyComparisonDisposition.Matched)]
    [InlineData("absent",
        OperationDependencyObservationDisposition.Observed,
        OperationDependencyComparisonDisposition.Mismatched)]
    public void PresentAndAbsentAreExpectationAware(
        string expected,
        OperationDependencyObservationDisposition observation,
        OperationDependencyComparisonDisposition result)
    {
        Assert.Equal(result,
            Compare(expected: expected, disposition: observation)
                .Disposition);
    }

    [Theory]
    [InlineData(OperationDependencyObservationDisposition.Unknown,
        OperationDependencyComparisonDisposition.NoObservation)]
    [InlineData(OperationDependencyObservationDisposition.Unavailable,
        OperationDependencyComparisonDisposition.Unavailable)]
    [InlineData(OperationDependencyObservationDisposition.NotAssessed,
        OperationDependencyComparisonDisposition.NotAssessed)]
    public void MissingOrUnavailableObservationsNeverMatch(
        OperationDependencyObservationDisposition observation,
        OperationDependencyComparisonDisposition result)
    {
        Assert.Equal(result,
            Compare(expected: "absent", disposition: observation)
                .Disposition);
    }

    [Fact]
    public void ExpectedConditionTokensAreCaseInsensitive()
    {
        Assert.Equal(
            OperationDependencyComparisonDisposition.Matched,
            Compare(key: "DEPENDENCY.RELATIONSHIP",
                expected: "PRESENT").Disposition);
    }

    [Theory]
    [InlineData("resource.state", "present")]
    [InlineData("dependency.relationship", "healthy")]
    [InlineData("dependency.relationship", "exists")]
    public void UnsupportedConditionsNeverMatch(
        string key, string expected)
    {
        Assert.Equal(
            OperationDependencyComparisonDisposition.UnsupportedCondition,
            Compare(key: key, expected: expected).Disposition);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void EverySubjectIdentityDimensionIsStrict(int dimension)
    {
        Assert.Equal(
            OperationDependencyComparisonDisposition.SubjectMismatch,
            Compare(subject: Subject(dimension)).Disposition);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void EveryDependencyIdentityDimensionIsStrict(int dimension)
    {
        Assert.Equal(
            OperationDependencyComparisonDisposition.DependencyMismatch,
            Compare(dependency: Dependency(dimension)).Disposition);
    }

    [Fact]
    public void ReversingDirectedRelationshipDoesNotMatch()
    {
        Assert.Equal(
            OperationDependencyComparisonDisposition.SubjectMismatch,
            Compare(subject: Dependency(), dependency: Subject())
                .Disposition);
    }

    [Fact]
    public void RelationshipKindMismatchDoesNotMatch()
    {
        Assert.Equal(
            OperationDependencyComparisonDisposition.RelationshipMismatch,
            Compare(relationship: "uses").Disposition);
    }

    [Fact]
    public void DeclaredRelationshipMismatchDoesNotMatch()
    {
        Assert.Equal(
            OperationDependencyComparisonDisposition.RelationshipMismatch,
            Compare(declaredRelationship: "uses").Disposition);
    }

    [Fact]
    public void SubjectMismatchPrecedesUnsupportedCondition()
    {
        Assert.Equal(
            OperationDependencyComparisonDisposition.SubjectMismatch,
            Compare(key: "invalid",
                subject: Subject(5)).Disposition);
    }

    [Fact]
    public void DependencyMismatchPrecedesUnsupportedCondition()
    {
        Assert.Equal(
            OperationDependencyComparisonDisposition.DependencyMismatch,
            Compare(key: "invalid",
                dependency: Dependency(5)).Disposition);
    }

    [Fact]
    public void UnsupportedConditionPrecedesUnavailableObservation()
    {
        Assert.Equal(
            OperationDependencyComparisonDisposition.UnsupportedCondition,
            Compare(expected: "invalid",
                disposition:
                    OperationDependencyObservationDisposition.Unavailable)
                .Disposition);
    }

    [Fact]
    public void ComparisonPreservesExactInputs()
    {
        var condition =
            new OperationOutcomeCondition("dependency.relationship", "present");
        var scope = new OperationResourceScope(Subject());
        var dependency = Dependency();
        var observation = Observation();

        var comparison = OperationDependencyComparison.Evaluate(
            condition, scope, dependency, "depends-on", observation);

        Assert.Same(condition, comparison.Condition);
        Assert.Same(scope, comparison.Scope);
        Assert.Same(dependency, comparison.Dependency);
        Assert.Same(observation, comparison.Observation);
        Assert.Equal("depends-on", comparison.Relationship);
        Assert.Equal(OperationDependencyComparisonDisposition.Matched,
            comparison.Disposition);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void ObservationRejectsInvalidInputs(int which)
    {
        var operation = new OperationCorrelationId("operation-42");
        var subject = Subject();
        var dependency = Dependency();
        var timestamp = Timestamp;
        var relationship = "depends-on";

        Assert.ThrowsAny<ArgumentException>(() =>
            new OperationDependencyObservation(
                which == 0 ? default : operation,
                which == 1 ? null! : subject,
                which == 2 ? null! : dependency,
                which == 3 ? null! : relationship,
                which == 4 ? default : timestamp,
                OperationDependencyObservationDisposition.Observed));
    }

    [Fact]
    public void ObservationRejectsUndefinedDisposition()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OperationDependencyObservation(
                new OperationCorrelationId("operation-42"),
                Subject(), Dependency(), "depends-on", Timestamp,
                (OperationDependencyObservationDisposition)99));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void ComparisonRejectsNullOrBlankInputs(int which)
    {
        var condition =
            new OperationOutcomeCondition("dependency.relationship", "present");
        var scope = new OperationResourceScope(Subject());
        var dependency = Dependency();
        var observation = Observation();

        Assert.ThrowsAny<ArgumentException>(() =>
            OperationDependencyComparison.Evaluate(
                which == 0 ? null! : condition,
                which == 1 ? null! : scope,
                which == 2 ? null! : dependency,
                which == 3 ? null! : "depends-on",
                which == 4 ? null! : observation));
    }

    [Fact]
    public void ObservationPublicShapeIsImmutable()
    {
        var type = typeof(OperationDependencyObservation);
        Assert.True(type.IsPublic);
        Assert.True(type.IsSealed);

        var properties = type.GetProperties(
            BindingFlags.Public | BindingFlags.Instance);

        Assert.Equal(new[]
        {
            "Dependency", "Disposition", "ObservedAt",
            "Operation", "Relationship", "Subject"
        }, properties.Select(p => p.Name).OrderBy(n => n));

        Assert.All(properties, property =>
        {
            Assert.NotNull(property.GetMethod);
            Assert.Null(property.SetMethod);
        });
    }

    [Fact]
    public void ComparisonPublicShapeIsNarrowAndImmutable()
    {
        var type = typeof(OperationDependencyComparison);
        Assert.True(type.IsPublic);
        Assert.True(type.IsSealed);
        Assert.Empty(type.GetConstructors());

        var properties = type.GetProperties(
            BindingFlags.Public | BindingFlags.Instance);

        Assert.Equal(new[]
        {
            "Condition", "Dependency", "Disposition",
            "Observation", "Relationship", "Scope"
        }, properties.Select(p => p.Name).OrderBy(n => n));

        Assert.All(properties, property =>
        {
            Assert.NotNull(property.GetMethod);
            Assert.Null(property.SetMethod);
        });

        var methods = type.GetMethods(
            BindingFlags.Public | BindingFlags.DeclaredOnly |
            BindingFlags.Static | BindingFlags.Instance);

        var method = Assert.Single(
            methods, candidate => !candidate.IsSpecialName);

        Assert.Equal("Evaluate", method.Name);
        Assert.True(method.IsStatic);
        Assert.Equal(type, method.ReturnType);
        Assert.Equal(new[]
        {
            typeof(OperationOutcomeCondition),
            typeof(OperationResourceScope),
            typeof(OperationTarget),
            typeof(string),
            typeof(OperationDependencyObservation)
        }, method.GetParameters().Select(p => p.ParameterType));
    }

    [Fact]
    public void ComparisonDoesNotAssertVerifiedSuccess()
    {
        var type = typeof(OperationDependencyComparison);
        Assert.Null(type.GetProperty("IsVerified"));
        Assert.Null(type.GetProperty("IsSuccessful"));
        Assert.Null(type.GetProperty("VerifiedSuccess"));
        Assert.Null(type.GetProperty("ExecutionSucceeded"));
        Assert.Null(type.GetProperty("Evidence"));
    }
}
