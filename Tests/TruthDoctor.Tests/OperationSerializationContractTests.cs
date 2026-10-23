using System;
using System.Linq;
using System.Text.Json;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

/// <summary>
/// Tests JSON representations with explicit model reconstruction.
/// This is not a direct-model System.Text.Json deserialization contract
/// and does not introduce a production persistence implementation.
/// </summary>
public sealed class OperationSerializationContractTests
{
    private static UniversalCapabilityIdentity Capability() =>
        new("compute.instance.start");

    private static OperationTarget Target() =>
        new("provider", "account", "location", "compute",
            "instance", "resource-01");

    private static OperationIntent Intent() =>
        new(Capability(), "Start the instance", "Maintenance");

    private static OperationDesiredOutcome Outcome() =>
        new(new[]
        {
            new OperationOutcomeCondition("state", "running")
        });

    private static OperationPlannedStep Step(string id) =>
        new(id, Capability(), "Start the instance", Target());

    private static OperationPlannedSteps Steps() =>
        new(new[] { Step("step-a"), Step("step-b") });

    private static OperationPlanReview Plan(
        bool populated = false,
        bool empty = false)
    {
        if (!populated && !empty)
        {
            return new OperationPlanReview(
                "Review instance start", Intent(), Target(),
                Outcome(), Steps());
        }

        var preconditions = new OperationPreconditions(
            empty
                ? Array.Empty<OperationPrecondition>()
                : new[] { new OperationPrecondition("state", "stopped") });

        var dependencies = new OperationStepDependencies(
            empty
                ? Array.Empty<OperationStepDependency>()
                : new[]
                {
                    new OperationStepDependency("step-b", "step-a")
                });

        var recovery = new OperationRecoveryPlan(
            empty
                ? Array.Empty<OperationStepRecovery>()
                : new[]
                {
                    new OperationStepRecovery(
                        "step-a",
                        OperationRecoveryStatus.Unsupported,
                        OperationRecoveryKind.None,
                        limitations: "Manual recovery")
                });

        var impact = new OperationEstimatedImpact(
            empty
                ? Array.Empty<OperationStepImpact>()
                : new[]
                {
                    new OperationStepImpact(
                        "step-a",
                        OperationImpactSeverity.Moderate,
                        OperationImpactConfidence.High,
                        estimatedAffectedResourceCount: 2,
                        availabilityDisruptionExpected: true,
                        rationale: "Brief outage")
                });

        var evidence = new OperationEvidenceRequirements(
            empty
                ? Array.Empty<OperationEvidenceRequirement>()
                : new[]
                {
                    new OperationEvidenceRequirement(
                        "ev-1", "step-a",
                        "observation", "Capture final state",
                        sourceHint: "status")
                });

        return new OperationPlanReview(
            "Review instance start", Intent(), Target(),
            Outcome(), Steps(), preconditions, dependencies,
            recovery, impact, evidence);
    }

    [Fact]
    public void SerializationUsesExpectedJsonOptions()
    {
        var options = OperationSerializationTestBridge.Options;
        Assert.Equal(JsonNamingPolicy.CamelCase,
            options.PropertyNamingPolicy);
        Assert.True(options.PropertyNameCaseInsensitive);
        Assert.Equal(
            System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
            options.UnmappedMemberHandling);

        var json = JsonSerializer.Serialize(
            OperationRecoveryKind.Rollback, options);
        Assert.Equal("\"rollback\"", json);
    }

    [Fact]
    public void CapabilityIdentityRoundTripPreservesValue()
    {
        var source = Capability();
        var restored =
            OperationSerializationTestBridge.RoundTrip(source);

        Assert.Equal(source, restored);
        Assert.Equal("compute.instance.start", restored.Value);
    }

    [Fact]
    public void CorrelationIdentityRoundTripPreservesCase()
    {
        var source = new OperationCorrelationId("Corr-ID-AbC");
        var restored =
            OperationSerializationTestBridge.RoundTrip(source);

        Assert.Equal(source, restored);
        Assert.Equal("Corr-ID-AbC", restored.Value);
        Assert.NotEqual(new OperationCorrelationId("corr-id-abc"), restored);
    }

    [Fact]
    public void CorrelationIdentityNormalizationIsPreserved()
    {
        var source = new OperationCorrelationId("  Corr-A1  ");
        var restored =
            OperationSerializationTestBridge.RoundTrip(source);

        Assert.Equal("Corr-A1", restored.Value);
        Assert.Equal(source, restored);
    }

    [Fact]
    public void RequiredPlanRepresentationRoundTrips()
    {
        var original = Plan();
        var restored =
            OperationSerializationTestBridge.RoundTrip(original);

        Assert.NotSame(original, restored);
        Assert.Equal(original.Summary, restored.Summary);
        Assert.Equal(original.Intent.Capability,
            restored.Intent.Capability);
        Assert.Equal(original.Intent.Description,
            restored.Intent.Description);
        Assert.Equal(original.Intent.Reason,
            restored.Intent.Reason);
        Assert.Equal(original.Target.NativeId,
            restored.Target.NativeId);
        Assert.Equal(original.Target.ProviderId,
            restored.Target.ProviderId);
        Assert.Equal(original.DesiredOutcome.Conditions[0].ExpectedValue,
            restored.DesiredOutcome.Conditions[0].ExpectedValue);
        Assert.Equal(original.PlannedSteps.Steps.Count,
            restored.PlannedSteps.Steps.Count);
        Assert.NotSame(original.PlannedSteps,
            restored.PlannedSteps);
    }

    [Fact]
    public void OptionalNullDeclarationsRoundTrip()
    {
        var restored =
            OperationSerializationTestBridge.RoundTrip(Plan());

        Assert.Null(restored.Preconditions);
        Assert.Null(restored.Dependencies);
        Assert.Null(restored.RecoveryPlan);
        Assert.Null(restored.EstimatedImpact);
        Assert.Null(restored.EvidenceRequirements);
    }

    [Fact]
    public void PopulatedOptionalDeclarationsRoundTrip()
    {
        var restored =
            OperationSerializationTestBridge.RoundTrip(
                Plan(populated: true));

        Assert.Equal("stopped",
            Assert.Single(restored.Preconditions!.Conditions).ExpectedValue);
        Assert.Equal("step-a",
            Assert.Single(restored.Dependencies!.Dependencies)
                .PrerequisiteStepId);
        Assert.Equal(OperationRecoveryStatus.Unsupported,
            Assert.Single(restored.RecoveryPlan!.Recoveries).Status);
        Assert.Equal(OperationImpactSeverity.Moderate,
            Assert.Single(restored.EstimatedImpact!.Impacts).Severity);
        Assert.Equal("ev-1",
            Assert.Single(restored.EvidenceRequirements!.Requirements)
                .RequirementId);
    }

    [Fact]
    public void EmptyOptionalCollectionsRemainDistinctFromNull()
    {
        var restored =
            OperationSerializationTestBridge.RoundTrip(
                Plan(empty: true));

        Assert.NotNull(restored.Preconditions);
        Assert.NotNull(restored.Dependencies);
        Assert.NotNull(restored.RecoveryPlan);
        Assert.NotNull(restored.EstimatedImpact);
        Assert.NotNull(restored.EvidenceRequirements);

        Assert.Empty(restored.Preconditions!.Conditions);
        Assert.Empty(restored.Dependencies!.Dependencies);
        Assert.Empty(restored.RecoveryPlan!.Recoveries);
        Assert.Empty(restored.EstimatedImpact!.Impacts);
        Assert.Empty(restored.EvidenceRequirements!.Requirements);
    }

    [Fact]
    public void NestedStepOrderingIsPreserved()
    {
        var restored =
            OperationSerializationTestBridge.RoundTrip(Plan());

        Assert.Equal(
            new[] { "step-a", "step-b" },
            restored.PlannedSteps.Steps.Select(x => x.StepId));
        Assert.Equal(
            new[] { "resource-01", "resource-01" },
            restored.PlannedSteps.Steps.Select(x => x.Target!.NativeId));
    }

    [Fact]
    public void DependencyOrderingIsPreserved()
    {
        var source = new OperationStepDependencies(new[]
        {
            new OperationStepDependency("step-c", "step-b"),
            new OperationStepDependency("step-b", "step-a")
        });

        var restored =
            OperationSerializationTestBridge.RoundTrip(source);

        Assert.Equal(
            new[] { "step-c", "step-b" },
            restored.Dependencies.Select(x => x.StepId));
        Assert.Equal(
            new[] { "step-b", "step-a" },
            restored.Dependencies.Select(x => x.PrerequisiteStepId));
    }

    [Fact]
    public void EnumValuesRoundTripWithoutNumericFallback()
    {
        var options = OperationSerializationTestBridge.Options;
        var json = JsonSerializer.Serialize(
            OperationImpactConfidence.High, options);

        Assert.Equal("\"high\"", json);
        Assert.Equal(OperationImpactConfidence.High,
            JsonSerializer.Deserialize<OperationImpactConfidence>(
                json, options));
        Assert.Throws<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<OperationImpactConfidence>(
                "99", options);
        });
    }

    [Fact]
    public void MalformedJsonIsRejected()
    {
        var rejected = false;
        try
        {
            OperationSerializationTestBridge.Restore<OperationPlanReview>(
                "{ invalid-json }");
        }
        catch (JsonException)
        {
            rejected = true;
        }

        Assert.True(rejected, "Malformed JSON must be rejected.");
    }

    [Fact]
    public void InvalidIdentityIsRejectedDuringReconstruction()
    {
        var invalidCapability =
            "{\"value\":\"Invalid-Capability\"}";
        var invalidCorrelation =
            "{\"value\":\"   \"}";

        Assert.Throws<ArgumentException>(() =>
            OperationSerializationTestBridge
                .Restore<UniversalCapabilityIdentity>(
                    invalidCapability));

        Assert.Throws<ArgumentException>(() =>
            OperationSerializationTestBridge
                .Restore<OperationCorrelationId>(
                    invalidCorrelation));
    }

    [Fact]
    public void InvalidModelStateIsRejectedDuringReconstruction()
    {
        var invalidStep =
            "{\"stepId\":\"   \",\"capability\":"
            + "{\"value\":\"compute.instance.start\"},"
            + "\"description\":\"Start instance\",\"target\":null}";

        Assert.Throws<ArgumentException>(() =>
            OperationSerializationTestBridge
                .Restore<OperationPlannedStep>(invalidStep));
    }

    [Theory]
    [InlineData("OperationDesiredOutcome")]
    [InlineData("OperationEstimatedImpact")]
    [InlineData("OperationEvidenceRequirements")]
    [InlineData("OperationPlannedSteps")]
    [InlineData("OperationPreconditions")]
    [InlineData("OperationRecoveryPlan")]
    [InlineData("OperationStepDependencies")]
    public void CollectionRepresentationRoundTripsThroughConstructors(
        string model)
    {
        switch (model)
        {
            case "OperationDesiredOutcome":
            {
                var restored =
                    OperationSerializationTestBridge.RoundTrip(Outcome());
                Assert.Equal("state",
                    Assert.Single(restored.Conditions).Key);
                break;
            }
            case "OperationEstimatedImpact":
            {
                var source = new OperationEstimatedImpact(new[]
                {
                    new OperationStepImpact(
                        "step-a", OperationImpactSeverity.High,
                        OperationImpactConfidence.Medium,
                        estimatedAffectedResourceCount: 3)
                });
                var restored =
                    OperationSerializationTestBridge.RoundTrip(source);
                Assert.Equal(3,
                    Assert.Single(restored.Impacts)
                        .EstimatedAffectedResourceCount);
                break;
            }
            case "OperationEvidenceRequirements":
            {
                var source = new OperationEvidenceRequirements(new[]
                {
                    new OperationEvidenceRequirement(
                        "ev-1", "step-a", "log", "Collect log")
                });
                var restored =
                    OperationSerializationTestBridge.RoundTrip(source);
                Assert.Equal("ev-1",
                    Assert.Single(restored.Requirements).RequirementId);
                break;
            }
            case "OperationPlannedSteps":
            {
                var restored =
                    OperationSerializationTestBridge.RoundTrip(Steps());
                Assert.Equal(2, restored.Steps.Count);
                Assert.Equal("step-b", restored.Steps[1].StepId);
                break;
            }
            case "OperationPreconditions":
            {
                var source = new OperationPreconditions(new[]
                {
                    new OperationPrecondition("state", "stopped")
                });
                var restored =
                    OperationSerializationTestBridge.RoundTrip(source);
                Assert.Equal("stopped",
                    Assert.Single(restored.Conditions).ExpectedValue);
                break;
            }
            case "OperationRecoveryPlan":
            {
                var source = new OperationRecoveryPlan(new[]
                {
                    new OperationStepRecovery(
                        "step-a", OperationRecoveryStatus.Unknown,
                        OperationRecoveryKind.None)
                });
                var restored =
                    OperationSerializationTestBridge.RoundTrip(source);
                Assert.Equal(OperationRecoveryKind.None,
                    Assert.Single(restored.Recoveries).Kind);
                break;
            }
            case "OperationStepDependencies":
            {
                var source = new OperationStepDependencies(new[]
                {
                    new OperationStepDependency("step-b", "step-a")
                });
                var restored =
                    OperationSerializationTestBridge.RoundTrip(source);
                Assert.Equal("step-a",
                    Assert.Single(restored.Dependencies)
                        .PrerequisiteStepId);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(model), model,
                    "Unknown collection wrapper case.");
        }
    }
}
/// <summary>
/// Test-only JSON transport and explicit constructor reconstruction.
/// This is NOT a production persistence API and does NOT establish
/// direct System.Text.Json deserialization support for platform models.
/// </summary>
internal static class OperationSerializationTestBridge
{
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            UnmappedMemberHandling =
                System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow
        };

        options.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter(
                JsonNamingPolicy.CamelCase,
                allowIntegerValues: false));

        return options;
    }

    public static T RoundTrip<T>(T source)
    {
        ArgumentNullException.ThrowIfNull(source);

        // Serialize the actual model to JSON. No reference reuse.
        string json = JsonSerializer.Serialize(source, Options);

        // Parse JSON and rebuild through platform constructors.
        return Restore<T>(json);
    }

    public static T Restore<T>(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        using var document = JsonDocument.Parse(json);

        return (T)Construct(typeof(T), document.RootElement);
    }

    private static string Required(JsonElement element, string name)
    {
        return element.GetProperty(name).GetString()!;
    }

    private static string? OptionalText(
        JsonElement element,
        string name)
    {
        if (!element.TryGetProperty(name, out var property) ||
            property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return property.GetString();
    }

    private static int? OptionalInt(
        JsonElement element,
        string name)
    {
        if (!element.TryGetProperty(name, out var property) ||
            property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return property.GetInt32();
    }

    private static bool? OptionalBool(
        JsonElement element,
        string name)
    {
        if (!element.TryGetProperty(name, out var property) ||
            property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return property.GetBoolean();
    }

    private static T EnumValue<T>(
        JsonElement element,
        string name)
        where T : struct, Enum
    {
        return JsonSerializer.Deserialize<T>(
            element.GetProperty(name).GetRawText(),
            Options);
    }

    private static T Model<T>(
        JsonElement element,
        string name)
    {
        return (T)Construct(typeof(T), element.GetProperty(name));
    }

    private static T? OptionalModel<T>(
        JsonElement element,
        string name)
        where T : class
    {
        if (!element.TryGetProperty(name, out var property) ||
            property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return (T)Construct(typeof(T), property);
    }

    private static UniversalCapabilityIdentity? OptionalCapability(
        JsonElement element,
        string name)
    {
        if (!element.TryGetProperty(name, out var property) ||
            property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return (UniversalCapabilityIdentity)Construct(
            typeof(UniversalCapabilityIdentity), property);
    }

    private static T[] Models<T>(
        JsonElement element,
        string name)
    {
        return element.GetProperty(name)
            .EnumerateArray()
            .Select(item => (T)Construct(typeof(T), item))
            .ToArray();
    }

    private static object Construct(Type type, JsonElement element)
    {
        if (type == typeof(UniversalCapabilityIdentity))
        {
            return new UniversalCapabilityIdentity(
                Required(element, "value"));
        }

        if (type == typeof(OperationCorrelationId))
        {
            return new OperationCorrelationId(
                Required(element, "value"));
        }

        if (type == typeof(OperationIntent))
        {
            return new OperationIntent(
                Model<UniversalCapabilityIdentity>(
                    element, "capability"),
                Required(element, "description"),
                OptionalText(element, "reason"));
        }

        if (type == typeof(OperationTarget))
        {
            return new OperationTarget(
                Required(element, "providerId"),
                Required(element, "accountId"),
                Required(element, "location"),
                Required(element, "domainId"),
                Required(element, "resourceType"),
                Required(element, "nativeId"));
        }

        if (type == typeof(OperationOutcomeCondition))
        {
            return new OperationOutcomeCondition(
                Required(element, "key"),
                Required(element, "expectedValue"));
        }

        if (type == typeof(OperationDesiredOutcome))
        {
            return new OperationDesiredOutcome(
                Models<OperationOutcomeCondition>(
                    element, "conditions"));
        }

        if (type == typeof(OperationPlannedStep))
        {
            return new OperationPlannedStep(
                Required(element, "stepId"),
                Model<UniversalCapabilityIdentity>(
                    element, "capability"),
                Required(element, "description"),
                OptionalModel<OperationTarget>(
                    element, "target"));
        }

        if (type == typeof(OperationPlannedSteps))
        {
            return new OperationPlannedSteps(
                Models<OperationPlannedStep>(
                    element, "steps"));
        }

        if (type == typeof(OperationPrecondition))
        {
            return new OperationPrecondition(
                Required(element, "key"),
                Required(element, "expectedValue"));
        }

        if (type == typeof(OperationPreconditions))
        {
            return new OperationPreconditions(
                Models<OperationPrecondition>(
                    element, "conditions"));
        }

        if (type == typeof(OperationStepDependency))
        {
            return new OperationStepDependency(
                Required(element, "stepId"),
                Required(element, "prerequisiteStepId"));
        }

        if (type == typeof(OperationStepDependencies))
        {
            return new OperationStepDependencies(
                Models<OperationStepDependency>(
                    element, "dependencies"));
        }

        if (type == typeof(OperationStepRecovery))
        {
            return new OperationStepRecovery(
                Required(element, "stepId"),
                EnumValue<OperationRecoveryStatus>(
                    element, "status"),
                EnumValue<OperationRecoveryKind>(
                    element, "kind"),
                OptionalCapability(
                    element, "recoveryCapability"),
                OptionalText(element, "limitations"));
        }

        if (type == typeof(OperationRecoveryPlan))
        {
            return new OperationRecoveryPlan(
                Models<OperationStepRecovery>(
                    element, "recoveries"));
        }

        if (type == typeof(OperationStepImpact))
        {
            return new OperationStepImpact(
                Required(element, "stepId"),
                EnumValue<OperationImpactSeverity>(
                    element, "severity"),
                EnumValue<OperationImpactConfidence>(
                    element, "confidence"),
                OptionalInt(
                    element, "estimatedAffectedResourceCount"),
                OptionalBool(
                    element, "availabilityDisruptionExpected"),
                OptionalText(element, "rationale"));
        }

        if (type == typeof(OperationEstimatedImpact))
        {
            return new OperationEstimatedImpact(
                Models<OperationStepImpact>(
                    element, "impacts"));
        }

        if (type == typeof(OperationEvidenceRequirement))
        {
            return new OperationEvidenceRequirement(
                Required(element, "requirementId"),
                Required(element, "stepId"),
                Required(element, "category"),
                Required(element, "description"),
                element.GetProperty("isMandatory").GetBoolean(),
                OptionalText(element, "sourceHint"));
        }

        if (type == typeof(OperationEvidenceRequirements))
        {
            return new OperationEvidenceRequirements(
                Models<OperationEvidenceRequirement>(
                    element, "requirements"));
        }

        if (type == typeof(OperationPlanReview))
        {
            return new OperationPlanReview(
                Required(element, "summary"),
                Model<OperationIntent>(element, "intent"),
                Model<OperationTarget>(element, "target"),
                Model<OperationDesiredOutcome>(
                    element, "desiredOutcome"),
                Model<OperationPlannedSteps>(
                    element, "plannedSteps"),
                OptionalModel<OperationPreconditions>(
                    element, "preconditions"),
                OptionalModel<OperationStepDependencies>(
                    element, "dependencies"),
                OptionalModel<OperationRecoveryPlan>(
                    element, "recoveryPlan"),
                OptionalModel<OperationEstimatedImpact>(
                    element, "estimatedImpact"),
                OptionalModel<OperationEvidenceRequirements>(
                    element, "evidenceRequirements"));
        }

        throw new NotSupportedException(
            "No test-local reconstruction defined for " +
            type.FullName);
    }
}
