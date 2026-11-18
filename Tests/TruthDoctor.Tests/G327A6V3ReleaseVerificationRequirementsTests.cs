using System;
using TruthDoctor.Services.Providers;
using Xunit;

namespace TruthDoctor.Tests;

/// <summary>
/// TDD contract for proposed TruthDoctor V3 release
/// verification requirements.
///
/// This suite does not claim any verification gate
/// is implemented or has passed acceptance.
///
/// It does not authenticate application releases,
/// provision trust roots, verify provider-policy
/// authority, or authorize executable providers.
///
/// The older TruthApi build-release.sh is not part
/// of the V3 release verification authority.
/// </summary>
public sealed class G327A6V3ReleaseVerificationRequirementsTests
{
    private static TruthDoctorV3ReleaseVerificationRequirementsContract
        CreateContract()
    {
        return new TruthDoctorV3ReleaseVerificationRequirementsContract();
    }

    [Theory]
    [InlineData("V3-REL-001", "ReleaseEngineering")]
    [InlineData("V3-REL-002", "ReleaseEngineering")]
    [InlineData("V3-REL-003", "ReleaseEngineering")]
    [InlineData("V3-REL-004", "ReleaseEngineering")]
    [InlineData("V3-REL-005", "ReleaseSecurity")]
    [InlineData("V3-REL-006", "ReleaseSecurity")]
    [InlineData("V3-REL-007", "ReleaseSecurity")]
    [InlineData("V3-REL-008", "HostSecurity")]
    [InlineData("V3-REL-009", "HostSecurity")]
    [InlineData("V3-REL-010", "ProviderSecurity")]
    [InlineData("V3-REL-011", "ReleaseGovernance")]
    [InlineData("V3-REL-012", "QualityEngineering")]
    public void ProposedRequirementHasOwnerButNoImplementedAuthority(
        string requirementId,
        string expectedOwner)
    {
        var requirement = CreateContract()
            .GetRequirement(requirementId);

        Assert.Equal(requirementId, requirement.Id);
        Assert.Equal(expectedOwner, requirement.Owner);

        // P53 explicitly classified every control as
        // proposed and not yet implemented.
        Assert.False(requirement.ImplementationVerified);
        Assert.False(requirement.AcceptanceGateImplemented);
    }

    [Fact]
    public void UndefinedRequirementFailsExplicitly()
    {
        Action action = () =>
        {
            _ = CreateContract()
                .GetRequirement("V3-REL-999");
        };

        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void NullRequirementFailsExplicitly()
    {
        Action action = () =>
        {
            _ = CreateContract()
                .GetRequirement(null!);
        };

        Assert.Throws<ArgumentNullException>(action);
    }
}
