using System;
using Identity.Grains;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Hosting;
using Orleans.TestingHost;

namespace Identity.Tests;

public sealed class TestSiloConfigurations : ISiloConfigurator {
    public void Configure(ISiloBuilder siloBuilder) {
        _ = siloBuilder.
            AddMemoryGrainStorageAsDefault().
            UseInMemoryReminderService();
    }
}
