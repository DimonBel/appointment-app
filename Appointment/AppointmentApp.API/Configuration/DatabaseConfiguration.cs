using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using AppointmentApp.Postgres.Data;

namespace AppointmentApp.API.Configuration;

/// <summary>
/// Database configuration and initialization helper
/// Configures PostgreSQL with JSON support and manages database creation/migration on startup
/// </summary>
public static class DatabaseConfiguration
{
    /// <summary>
    /// Configures PostgreSQL database with JSON support for the application
    /// Sets up DbContext with PostgreSQL provider and applies migrations assembly configuration
    /// </summary>
    /// <param name="services">Service collection to add database services to</param>
    /// <param name="configuration">Application configuration containing connection strings</param>
    /// <returns>Service collection with database services configured</returns>
    public static IServiceCollection ConfigureDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(configuration.GetConnectionString("DefaultConnection"));
        dataSourceBuilder.EnableDynamicJson();
        var dataSource = dataSourceBuilder.Build();

        services.AddDbContext<AppointmentDbContext>(options =>
            options.UseNpgsql(dataSource,
                b => b.MigrationsAssembly("AppointmentApp.Postgres"))
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

        return services;
    }

    /// <summary>
    /// Ensures the database exists and all migrations are applied
    /// Creates the database if it doesn't exist and runs EF Core migrations
    /// </summary>
    /// <param name="services">Service provider for resolving DbContext</param>
    /// <param name="configuration">Configuration containing connection strings</param>
    /// <exception cref="InvalidOperationException">Connection string is missing or invalid</exception>
    public static async Task EnsureDatabaseCreatedAndMigratedAsync(IServiceProvider services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");
        }

        var csb = new NpgsqlConnectionStringBuilder(connectionString);
        var databaseName = csb.Database;
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidOperationException("Database name is missing in 'DefaultConnection'.");
        }

        var adminCsb = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Database = "postgres"
        };

        await using (var adminConnection = new NpgsqlConnection(adminCsb.ConnectionString))
        {
            await adminConnection.OpenAsync();

            await using (var existsCmd = new NpgsqlCommand(
                             "SELECT 1 FROM pg_database WHERE datname = @name;",
                             adminConnection))
            {
                existsCmd.Parameters.AddWithValue("name", databaseName);
                var exists = await existsCmd.ExecuteScalarAsync() != null;
                if (!exists)
                {
                    await using var createCmd =
                        new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\";", adminConnection);
                    await createCmd.ExecuteNonQueryAsync();
                }
            }
        }

        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppointmentDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}