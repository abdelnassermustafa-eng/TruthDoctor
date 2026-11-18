using System;
using System.Collections.Generic;

namespace TruthDoctor.Services.Providers;

/// <summary>
/// Static reference catalog for the sixteen provisional
/// authenticated-distribution evidence obligations.
///
/// This class does not authenticate a publisher, verify a
/// signature, enroll a root, approve a release, install a
/// provider, or grant execution authority.
/// </summary>
public static class TruthDoctorV3DistributionEvidenceRequirementCatalog
{
    private static readonly string[] RequirementIds =
    {
        "D01-R01",
        "D01-R02",
        "D01-R03",
        "D01-R04",
        "D02-R01",
        "D02-R02",
        "D02-R03",
        "D02-R04",
        "D03-R01",
        "D03-R02",
        "D03-R03",
        "D03-R04",
        "D04-R01",
        "D04-R02",
        "D04-R03",
        "D04-R04"
    };

    /// <summary>
    /// Returns a defensive copy of the accepted requirement
    /// identifier sequence. No trust decision is made.
    /// </summary>
    public static IEnumerable<string> GetAllRequirementIds()
    {
        return (string[])RequirementIds.Clone();
    }

    /// <summary>
    /// Returns the reference evidence stage for an exact known
    /// requirement identifier. Unknown identifiers are rejected.
    /// No root or provider authorization is performed.
    /// </summary>
    public static string GetStage(string identifier)
    {
        ArgumentNullException.ThrowIfNull(identifier);

        for (int i = 0; i < RequirementIds.Length; i++)
        {
            if (string.Equals(
                    RequirementIds[i],
                    identifier,
                    StringComparison.Ordinal))
            {
                return i switch
                {
                    < 4 => "publisher_identity",
                    < 8 => "artifact_integrity",
                    < 12 => "anchor_binding",
                    _ => "first_run_review"
                };
            }
        }

        throw new ArgumentException(
            "Unknown distribution evidence requirement identifier.",
            nameof(identifier));
    }
}
