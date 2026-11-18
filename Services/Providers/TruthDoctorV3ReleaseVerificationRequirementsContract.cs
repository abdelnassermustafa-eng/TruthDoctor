using System;

namespace TruthDoctor.Services.Providers;

/// <summary>
/// Data-only descriptor of a proposed TruthDoctor V3
/// release verification requirement.
///
/// A requirement descriptor is not proof of an
/// implemented or accepted security control.
/// </summary>
public sealed class TruthDoctorV3ReleaseVerificationRequirement
{
    public string Id { get; }

    public string Owner { get; }

    public bool ImplementationVerified { get; }

    public bool AcceptanceGateImplemented { get; }

    internal TruthDoctorV3ReleaseVerificationRequirement(
        string id,
        string owner)
    {
        Id = id;
        Owner = owner;

        // No release verification authority exists
        // merely because a requirement is defined.
        ImplementationVerified = false;
        AcceptanceGateImplemented = false;
    }
}

/// <summary>
/// Non-authorizing TruthDoctor V3 release requirements.
///
/// This type does not build, sign, verify or publish
/// executable application artifacts.
///
/// It does not provision trusted signing identities,
/// authenticate policy roots, authorize providers,
/// modify the legacy TruthApi release workflow,
/// or enable provider installation.
///
/// Unknown requirement identifiers fail explicitly.
/// </summary>
public sealed class TruthDoctorV3ReleaseVerificationRequirementsContract
{
    public TruthDoctorV3ReleaseVerificationRequirement
        GetRequirement(string requirementId)
    {
        ArgumentNullException.ThrowIfNull(requirementId);

        var owner = requirementId switch
        {
            // V3-specific release construction.
            "V3-REL-001" => "ReleaseEngineering",
            "V3-REL-002" => "ReleaseEngineering",
            "V3-REL-003" => "ReleaseEngineering",
            "V3-REL-004" => "ReleaseEngineering",

            // Application publisher and artifact
            // authenticity requirements.
            "V3-REL-005" => "ReleaseSecurity",
            "V3-REL-006" => "ReleaseSecurity",
            "V3-REL-007" => "ReleaseSecurity",

            // Independent host policy-root security.
            "V3-REL-008" => "HostSecurity",
            "V3-REL-009" => "HostSecurity",

            // Provider release authorization is separate.
            "V3-REL-010" => "ProviderSecurity",

            // Production release approval and governance.
            "V3-REL-011" => "ReleaseGovernance",

            // Cross-platform installation acceptance.
            "V3-REL-012" => "QualityEngineering",

            _ => throw new ArgumentException(
                "Unknown TruthDoctor V3 release " +
                "verification requirement.",
                nameof(requirementId))
        };

        return new TruthDoctorV3ReleaseVerificationRequirement(
            requirementId,
            owner);
    }
}
