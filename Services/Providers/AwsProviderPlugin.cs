namespace TruthDoctor.Services.Providers;

public sealed class AwsProviderPlugin : IProviderPlugin
{
    public string ProviderId => "aws";

    public ProviderRenderDescriptor ProviderVisual { get; } =
        new()
        {
            ProviderId = "aws",
            DisplayName = "Amazon Web Services",
            IconKey = "aws",
            AccentKey = "orange"
        };
}
