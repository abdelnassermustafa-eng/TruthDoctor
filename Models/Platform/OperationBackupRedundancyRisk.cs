using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral backup and redundancy risk declaration.
/// Descriptions are supplied independently, not discovered or verified.
/// Does not infer restoration, calculate risk, authorize, or execute.
/// </summary>
public sealed class OperationBackupRedundancyRisk
{
    public string? BackupDescription { get; }

    public string? RedundancyDescription { get; }

    public OperationRiskLevel? DeclaredRiskLevel { get; }

    public OperationBackupRedundancyRisk(
        string? backupDescription = null,
        string? redundancyDescription = null,
        OperationRiskLevel? declaredRiskLevel = null)
    {
        if (declaredRiskLevel.HasValue &&
            !Enum.IsDefined(declaredRiskLevel.Value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(declaredRiskLevel),
                declaredRiskLevel,
                "Undefined operation risk level.");
        }

        BackupDescription = NormalizeOptionalDescription(
            backupDescription, nameof(backupDescription));

        RedundancyDescription = NormalizeOptionalDescription(
            redundancyDescription, nameof(redundancyDescription));

        DeclaredRiskLevel = declaredRiskLevel;
    }

    private static string? NormalizeOptionalDescription(
        string? description,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var normalized = description.Trim();

        foreach (var character in normalized)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "Backup and redundancy descriptions cannot contain control characters.",
                    parameterName);
            }
        }

        return normalized;
    }
}
