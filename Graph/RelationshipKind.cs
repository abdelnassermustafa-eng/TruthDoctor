namespace TruthDoctor.Graph;

public enum RelationshipKind
{
    Unknown = 0,

    Contains,
    MemberOf,

    AttachedTo,
    HostedOn,

    DependsOn,
    Uses,

    ConnectedTo,
    RoutesThrough,

    SecuredBy,

    Serves,
    Targets,

    AssociatedWith
}
