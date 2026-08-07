namespace TruthDoctor.Services.Visuals;

public sealed class AwsRenderContributor :
    IRenderContributor
{
    public bool TryResolve(
        string providerId,
        string domainId,
        string resourceType,
        string iconKey,
        string accentKey,
        out RenderDescriptor visual)
    {
        visual = null!;

        if (!providerId.Equals(
                "aws",
                System.StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return false;
    }
}
