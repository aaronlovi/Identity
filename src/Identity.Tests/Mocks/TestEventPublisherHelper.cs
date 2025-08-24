using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Identity.Infrastructure.Firebase;
using Microsoft.Extensions.DependencyInjection;
using Orleans.TestingHost;

namespace Identity.Grains.Tests.Mocks;

/// <summary>
/// Helper class to access the MockEventPublisher from test cluster.
/// </summary>
public static class TestEventPublisherHelper {
    /// <summary>
    /// Gets the MockEventPublisher instance from the test cluster.
    /// </summary>
    public static MockEventPublisher GetMockEventPublisher(this TestCluster cluster) {
        // Access the service from the silo's service provider
        var siloServiceProvider = cluster.GetSiloServiceProvider();
        var eventPublisher = siloServiceProvider.GetRequiredService<IEventPublisher>();
        return (MockEventPublisher)eventPublisher;
    }

    /// <summary>
    /// Clears all published events from the mock event publisher.
    /// </summary>
    public static void ClearPublishedEvents(this TestCluster cluster) {
        cluster.GetMockEventPublisher().ClearEvents();
    }
}
