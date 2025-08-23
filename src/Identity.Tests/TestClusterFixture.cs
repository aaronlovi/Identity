using System;
using Microsoft.Extensions.Configuration;
using Orleans.TestingHost;

namespace Identity.Grains.Tests;

public sealed class TestClusterFixture : IDisposable {
    public TestClusterFixture() {
        var builder = new TestClusterBuilder();
        _ = builder.ConfigureHostConfiguration(config => {
            _ = config.
                AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).
                AddJsonFile("appsettings.Test.json", optional: true, reloadOnChange: true).
                AddEnvironmentVariables();
        });
        _ = builder.AddSiloBuilderConfigurator<TestSiloConfigurations>();
        _ = builder.AddClientBuilderConfigurator<TestClientConfigurations>();
        Cluster = builder.Build();
        Cluster.Deploy();
    }

    public TestCluster Cluster { get; }

    public void Dispose() {
        Cluster.StopAllSilos();
        Cluster.Dispose();
    }
}
