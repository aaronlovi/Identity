using Xunit;

namespace Identity.Tests;

[CollectionDefinition("TestClusterCollection")]
public class TestClusterCollection : ICollectionFixture<TestClusterFixture> { }
