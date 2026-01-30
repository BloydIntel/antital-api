using Microsoft.EntityFrameworkCore;
using Antital.Infrastructure;

namespace Antital.Test.Integration;

/// <summary>
/// Database fixture that ensures the test database exists and is cleaned up after all tests.
/// Uses PostgreSQL for testing.
/// Connection string comes from TEST_DB_CONNECTION_STRING (or defaults to localhost:5432).
/// </summary>
public class DatabaseFixture : IDisposable
{
    private readonly AntitalDBContext _context;
    private readonly string _connectionString;

    public DatabaseFixture()
    {
        _connectionString = Environment.GetEnvironmentVariable("TEST_DB_CONNECTION_STRING") 
            ?? "Host=localhost;Port=5432;Database=antitaldb_test;Username=crownedprinz;Password=";
        
        var options = new DbContextOptionsBuilder<AntitalDBContext>()
            .UseNpgsql(_connectionString, npgsqlOptions => 
                npgsqlOptions.MigrationsAssembly("Antital.Infrastructure"))
            .Options;

        _context = new AntitalDBContext(options);

        // Ensure database is created and migrations are applied (PostgreSQL)
        try
        {
            _context.Database.Migrate();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to connect to test database. Check connection string and Postgres availability. " +
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
