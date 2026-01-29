using Xunit;

namespace Antital.Test.Integration;

/// <summary>
/// Collection definition for integration tests.
/// This ensures DatabaseFixture is shared across all integration test classes
/// and cleanup happens once after all tests complete (similar to Python's scope="session").
/// </summary>
[CollectionDefinition("IntegrationTests")]
public class IntegrationTestCollection : ICollectionFixture<DatabaseFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
