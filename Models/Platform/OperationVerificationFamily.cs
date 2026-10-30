namespace TruthDoctor.Models.Platform;

/// <summary>
/// Provider-neutral categories of local verification comparisons.
/// </summary>
public enum OperationVerificationFamily
{
    ResourceState = 0,
    Health = 1,
    Dependency = 2,
    Reachability = 3,
    Service = 4
}
