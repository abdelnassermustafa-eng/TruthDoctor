using System;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A2StandaloneBackendContractTests
{
    [Fact]
    public void BackendRuntimeContractMustExist()
    {
        Type contract = typeof(IStandaloneBackendRuntime);

        Assert.True(contract.IsInterface);
    }

    [Fact]
    public void ProviderConnectionContractMustExist()
    {
        Type contract = typeof(IProviderConnectionManager);

        Assert.True(contract.IsInterface);
    }

    [Fact]
    public void ProviderResourceDiscoveryContractMustExist()
    {
        Type contract = typeof(IProviderResourceDiscovery);

        Assert.True(contract.IsInterface);
    }

    [Fact]
    public void BackendContractsMustBeOwnedByTruthDoctor()
    {
        Type[] contracts =
        {
            typeof(IStandaloneBackendRuntime),
            typeof(IProviderConnectionManager),
            typeof(IProviderResourceDiscovery)
        };

        foreach (Type contract in contracts)
        {
            Assert.Equal(
                "TruthDoctor.Services.Standalone",
                contract.Namespace
            );

            Assert.Equal(
                "TruthDoctor",
                contract.Assembly.GetName().Name
            );
        }
    }
}
