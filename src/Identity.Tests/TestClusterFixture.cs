using System;
using Orleans.TestingHost;

namespace Identity.Tests;

public sealed class TestClusterFixture : IDisposable {
    public TestClusterFixture() {
        var builder = new TestClusterBuilder();
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
