namespace TruthDoctor.Services.Visuals;

public interface IRenderContributor
{
    bool TryResolve(
        string providerId,
        string domainId,
        string resourceType,
        string iconKey,
        string accentKey,
        out RenderDescriptor visual);
}
