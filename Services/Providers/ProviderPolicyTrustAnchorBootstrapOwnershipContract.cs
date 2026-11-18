using System;

namespace TruthDoctor.Services.Providers;

/// <summary>
/// Declarative ownership contract for host policy
/// trust-anchor bootstrap responsibilities.
///
/// This contract classifies architectural concerns only.
///
/// It does not provision or authenticate trust roots,
/// validate signatures, authorize publishers, advance
/// anti-rollback state, approve releases, or permit
/// provider installation or execution.
///
/// A returned owner name is not proof that the
/// corresponding security capability exists.
/// </summary>
public sealed class ProviderPolicyTrustAnchorBootstrapOwnershipContract
{
    /// <summary>
    /// Returns the declared architectural owner for
    /// a specifically recognized concern.
    ///
    /// Unrecognized concerns fail explicitly and
    /// never default to host authorization.
    /// </summary>
    public string GetOwner(string concern)
    {
        ArgumentNullException.ThrowIfNull(concern);

        return concern switch
        {
            // Dedicated, host-controlled provider-security
            // responsibilities. These do not yet imply
            // that the controls are implemented.
            "PolicyRootCatalog" => "HostSecurity",

            "AuthorityToRootBinding" => "HostSecurity",

            "PolicyRootRotation" => "HostSecurity",

            "PolicySequenceHighWater" => "HostSecurity",

            // Application wiring belongs to the
            // composition boundary, not to provider input.
            "SecurityServiceComposition" =>
                "ApplicationComposition",

            // Preserve application entry ownership.
            "ApplicationEntryPoint" =>
                "ApplicationEntry",

            // These inputs and persistence mechanisms
            // cannot independently authorize trust roots.
            "ProviderPackageManifest" =>
                "NotAuthorized",

            "ProviderPackageRegistry" =>
                "NotAuthorized",

            "UserStore" =>
                "NotAuthorized",

            "EnvironmentVariables" =>
                "NotAuthorized",

            "SavedViewStorage" =>
                "NotAuthorized",

            _ => throw new ArgumentException(
                "The requested bootstrap ownership " +
                "concern is not defined.",
                nameof(concern))
        };
    }
}
