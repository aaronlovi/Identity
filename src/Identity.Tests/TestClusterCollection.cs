using Xunit;

namespace Identity.Grains.Tests;

[CollectionDefinition("TestClusterCollection")]
public class TestClusterCollection : ICollectionFixture<TestClusterFixture> { }
