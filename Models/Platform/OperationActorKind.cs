namespace TruthDoctor.Models.Platform;

/// <summary>
/// Describes the category of an operation actor.
/// Classification does not establish authentication or authorization.
/// </summary>
public enum OperationActorKind
{
    Human = 0,
    Service = 1
}
