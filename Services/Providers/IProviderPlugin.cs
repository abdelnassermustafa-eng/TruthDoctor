namespace TruthDoctor.Services.Providers;

public interface IProviderPlugin
{
    string ProviderId { get; }

    ProviderRenderDescriptor ProviderVisual { get; }
}
