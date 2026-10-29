using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationProviderResponseEvidenceTests
{
    private static readonly DateTimeOffset RecordedTime =
        new(2026, 10, 29, 12, 30, 0, TimeSpan.Zero);

    private static OperationProviderResponseEvidence Create(
        OperationProviderResponseDisposition disposition =
            OperationProviderResponseDisposition.Unknown,
        string stepId = "step-01",
        string providerId = "provider",
        DateTimeOffset? recordedAt = null,
        OperationCorrelationId? operation = null)
    {
        return new OperationProviderResponseEvidence(
            operation ?? new OperationCorrelationId("operation-42"),
            stepId,
            providerId,
            recordedAt ?? RecordedTime,
            disposition);
    }

    [Fact]
    public void PreservesOperationStepProviderAndRecordedInstant()
    {
        var operation = new OperationCorrelationId("Op-ABC");
        var recordedAt = new DateTimeOffset(
            2026, 10, 29, 5, 30, 0, TimeSpan.FromHours(-7));

        var evidence = Create(
            OperationProviderResponseDisposition.Acknowledged,
            operation: operation,
            recordedAt: recordedAt);

        Assert.Equal(operation, evidence.Operation);
        Assert.Equal("step-01", evidence.StepId);
        Assert.Equal("provider", evidence.ProviderId);
        Assert.Equal(recordedAt, evidence.RecordedAt);
        Assert.Equal(
            OperationProviderResponseDisposition.Acknowledged,
            evidence.Disposition);
    }

    [Theory]
    [InlineData(OperationProviderResponseDisposition.Unknown)]
    [InlineData(OperationProviderResponseDisposition.Acknowledged)]
    [InlineData(OperationProviderResponseDisposition.Rejected)]
    public void PreservesReportedDispositionWithoutInferringSuccess(
        OperationProviderResponseDisposition disposition)
    {
        var evidence = Create(disposition);

        Assert.Equal(disposition, evidence.Disposition);
    }

    [Fact]
    public void UnknownIsTheDefaultDisposition()
    {
        Assert.Equal(
            OperationProviderResponseDisposition.Unknown,
            default(OperationProviderResponseDisposition));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(999)]
    public void RejectsUnknownNumericDispositions(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Create((OperationProviderResponseDisposition)value));
    }

    [Fact]
    public void RejectsDefaultOperationCorrelationIdentity()
    {
        Assert.Throws<ArgumentException>(() =>
            Create(operation: default(OperationCorrelationId)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void RejectsBlankStepIdentifiers(string? value)
    {
        Assert.Throws<ArgumentException>(() =>
            Create(stepId: value!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void RejectsBlankProviderIdentifiers(string? value)
    {
        Assert.Throws<ArgumentException>(() =>
            Create(providerId: value!));
    }

    [Theory]
    [InlineData("step\n01")]
    [InlineData("step\t01")]
    [InlineData("step\001")]
    public void RejectsControlCharactersInStepIdentifiers(string value)
    {
        Assert.Throws<ArgumentException>(() =>
            Create(stepId: value));
    }

    [Theory]
    [InlineData("provider\none")]
    [InlineData("provider\tone")]
    [InlineData("provider\001")]
    public void RejectsControlCharactersInProviderIdentifiers(
        string value)
    {
        Assert.Throws<ArgumentException>(() =>
            Create(providerId: value));
    }

    [Fact]
    public void NormalizesTextButPreservesCase()
    {
        var evidence = Create(
            stepId: "  Step-ABC  ",
            providerId: "  Provider-X  ");

        Assert.Equal("Step-ABC", evidence.StepId);
        Assert.Equal("Provider-X", evidence.ProviderId);
    }

    [Fact]
    public void RejectsDefaultRecordingTimestamp()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OperationProviderResponseEvidence(
                new OperationCorrelationId("operation-42"),
                "step-01",
                "provider",
                default,
                OperationProviderResponseDisposition.Unknown));
    }

    [Fact]
    public void DeclaresOnlyFiveGetterOnlyProperties()
    {
        var type = typeof(OperationProviderResponseEvidence);

        Assert.True(type.IsPublic);
        Assert.True(type.IsSealed);
        Assert.Equal("TruthDoctor.Models.Platform", type.Namespace);

        var properties = type.GetProperties(
            BindingFlags.Public | BindingFlags.Instance);

        Assert.Equal(5, properties.Length);

        Assert.Equal(
            new[]
            {
                "Disposition", "Operation", "ProviderId",
                "RecordedAt", "StepId"
            },
            properties.Select(p => p.Name).OrderBy(n => n));

        Assert.All(properties, property =>
        {
            Assert.NotNull(property.GetMethod);
            Assert.Null(property.SetMethod);
        });

        Assert.Equal(typeof(OperationCorrelationId),
            type.GetProperty("Operation")!.PropertyType);
        Assert.Equal(typeof(string),
            type.GetProperty("StepId")!.PropertyType);
        Assert.Equal(typeof(string),
            type.GetProperty("ProviderId")!.PropertyType);
        Assert.Equal(typeof(DateTimeOffset),
            type.GetProperty("RecordedAt")!.PropertyType);
        Assert.Equal(typeof(OperationProviderResponseDisposition),
            type.GetProperty("Disposition")!.PropertyType);

        var constructor = Assert.Single(type.GetConstructors());
        var parameters = constructor.GetParameters();

        Assert.Equal(
            new[]
            {
                typeof(OperationCorrelationId),
                typeof(string),
                typeof(string),
                typeof(DateTimeOffset),
                typeof(OperationProviderResponseDisposition)
            },
            parameters.Select(p => p.ParameterType));
        Assert.All(parameters, p => Assert.False(p.IsOptional));
    }

    [Fact]
    public void ExposesNoVerifiedOutcomeOrExecutionAPI()
    {
        var type = typeof(OperationProviderResponseEvidence);

        var properties = type.GetProperties(
            BindingFlags.Public | BindingFlags.Instance);

        foreach (var forbidden in new[]
        {
            "IsSuccessful", "IsVerified", "ExecutionStatus",
            "VerificationResult", "RawResponse", "Payload",
            "AccessToken", "Secret"
        })
        {
            Assert.DoesNotContain(properties,
                property => property.Name == forbidden);
        }

        var declaredMethods = type.GetMethods(
            BindingFlags.DeclaredOnly |
            BindingFlags.Public |
            BindingFlags.Instance |
            BindingFlags.Static);

        Assert.DoesNotContain(declaredMethods,
            method => method.Name is
                "Execute" or "Verify" or "Authorize" or "Approve"
                or "Collect" or "Dispatch");
    }
}
