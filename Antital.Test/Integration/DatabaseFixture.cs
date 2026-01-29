using Microsoft.EntityFrameworkCore;
using Antital.Infrastructure;

namespace Antital.Test.Integration;

/// <summary>
/// Database fixture that ensures the test database exists and is cleaned up after all tests.
/// Similar to the Python project's conftest.py db fixture.
/// Uses Docker SQL Server (Azure SQL Edge) for testing - matches production environment.
/// Connection string assumes Docker container is running: docker-compose up antitaldb
/// </summary>
public class DatabaseFixture : IDisposable
{
    private readonly AntitalDBContext _context;
    private readonly string _connectionString;

    public DatabaseFixture()
    {
        // Use Docker SQL Server (Azure SQL Edge) for testing
        // Make sure Docker container is running: docker-compose up antitaldb
        // Port is mapped to 8600:1433, so use localhost,8600 from host machine
        // Password from docker-compose.override.yml: Admin1234!!
        _connectionString = Environment.GetEnvironmentVariable("TEST_DB_CONNECTION_STRING") 
            ?? "Server=localhost,8600;Database=AntitalDB_Test;User Id=sa;Password=Admin1234!!;TrustServerCertificate=True;";
        
        var options = new DbContextOptionsBuilder<AntitalDBContext>()
            .UseSqlServer(_connectionString, sqlOptions => 
                sqlOptions.MigrationsAssembly("Antital.Infrastructure"))
            .Options;

        _context = new AntitalDBContext(options);

        // Ensure database exists and schema is created
        // Using EnsureCreated instead of Migrate to avoid conflicts with WebApplicationFactory migrations
        // EnsureCreated creates the schema directly without running migrations (faster for tests)
        try
        {
            // Check if database can be connected
            if (!_context.Database.CanConnect())
            {
                // Database doesn't exist, create it
                _context.Database.EnsureCreated();
            }
            else
            {
                // Database exists, ensure schema is up to date
                // If tables don't exist, create them
                if (!_context.Database.GetPendingMigrations().Any())
                {
                    // No pending migrations, but ensure schema exists
                    _context.Database.EnsureCreated();
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to connect to test database. Make sure Docker container is running: docker-compose up antitaldb. " +
                $"Error: {ex.Message}", ex);
        }
    }

    public AntitalDBContext GetContext()
    {
        return _context;
    }

    /// <summary>
    /// Cleans up all data from the test database.
    /// Called after each test class execution (similar to Python's conftest.py cleanup).
    /// </summary>
    public void Cleanup()
    {
        try
        {
            // Delete all data in correct order (child tables first if any foreign keys exist)
            // For now, we only have Users table, so we can delete all users
            _context.Users.RemoveRange(_context.Users);
            _context.SaveChanges();
        }
        catch (Exception ex)
        {
            // Log error but don't fail - database might be in inconsistent state
            System.Diagnostics.Debug.WriteLine($"Error during cleanup: {ex.Message}");
        }
    }

    public void Dispose()
    {
        Cleanup();
        _context.Dispose();
    }
}
