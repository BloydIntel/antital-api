using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Antital.API;
using Antital.Infrastructure;
using BuildingBlocks.Infrastructure.Implementations;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Antital.Test.Integration;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<Program> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set environment FIRST before configuration is loaded
        builder.UseEnvironment("Testing");
        
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Override connection string to use Docker SQL Server test database
            // Make sure Docker container is running: docker-compose up antitaldb
            // Port is mapped to 8600:1433, so use localhost,8600 from host machine
            // Password is injected via environment variable TEST_DB_PASSWORD to avoid hardcoding secrets
            var testDbPassword = Environment.GetEnvironmentVariable("TEST_DB_PASSWORD")
                ?? throw new InvalidOperationException("TEST_DB_PASSWORD must be set for integration tests.");

            var testConnectionString = Environment.GetEnvironmentVariable("TEST_DB_CONNECTION_STRING") 
                ?? $"Server=localhost,8600;Database=AntitalDB_Test;User Id=sa;Password={testDbPassword};TrustServerCertificate=True;";
            
            // Add test configuration LAST so it overrides appsettings
            // Use null instead of empty string to ensure Elasticsearch sink is skipped
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", testConnectionString },
                { "ElasticSearch:Uri", null! }, // Null to skip Elasticsearch sink
                { "Jwt:Key", "BehzadDaraSecurityKey@#@BehzadDaraSecurityKey" }, // JWT key for testing
                { "Jwt:Issuer", "http://localhost:28747/" },
                { "Jwt:Audience", "http://localhost:28747/" }
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the real database context registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AntitalDBContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Remove DBContext registration
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DBContext));

            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            // Add Docker SQL Server database for testing (matches production)
            // Port is mapped to 8600:1433, so use localhost,8600 from host machine
            // Password is injected via environment variable TEST_DB_PASSWORD to avoid hardcoding secrets
            var testDbPassword = Environment.GetEnvironmentVariable("TEST_DB_PASSWORD")
                ?? throw new InvalidOperationException("TEST_DB_PASSWORD must be set for integration tests.");

            var testConnectionString = Environment.GetEnvironmentVariable("TEST_DB_CONNECTION_STRING") 
                ?? $"Server=localhost,8600;Database=AntitalDB_Test;User Id=sa;Password={testDbPassword};TrustServerCertificate=True;";
            
            services.AddDbContext<AntitalDBContext>(options =>
            {
                options.UseSqlServer(testConnectionString, sqlOptions => 
                    sqlOptions.MigrationsAssembly("Antital.Infrastructure"));
            });

            services.AddScoped<DBContext>(provider => provider.GetService<AntitalDBContext>()!);
        });
        
        // Workaround: Configure Kestrel to use a real server instead of TestServer
        // This avoids the PipeWriter.UnflushedBytes issue observed with TestServer.
        // Limitation: Kestrel binds to an ephemeral port; WebApplicationFactory injects the
        // correct BaseAddress for HttpClient, but external tools can't rely on a fixed port.
        // If the upstream TestServer issue is resolved, we can revert to the in-memory server.
        builder.UseKestrel(options =>
        {
            options.ListenLocalhost(0); // Use random available port
        });
    }
}
