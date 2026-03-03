using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using DocumentApp.Postgres.Data;

namespace DocumentApp.API.Configuration;

/// <summary>
/// Database configuration and initialization
/// </summary>
public static class DatabaseConfiguration
{
    /// <summary>
    /// Configure PostgreSQL database
    /// </summary>
    public static IServiceCollection ConfigureDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<DocumentDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions => npgsqlOptions.EnableRetryOnFailure()));

        return services;
    }

    /// <summary>
    /// Ensure database is created and migrations are applied
    /// </summary>
    public static async Task EnsureDatabaseCreatedAndMigratedAsync(IServiceProvider services, IConfiguration configuration, ILogger logger)
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

        // Check if database exists
        var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
        var dbName = builder.Database;
        builder.Database = "postgres";

        using var connection = new Npgsql.NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync();

        var checkDbCommand = connection.CreateCommand();
        checkDbCommand.CommandText = $"SELECT 1 FROM pg_database WHERE datname = '{dbName}'";
        var dbExists = await checkDbCommand.ExecuteScalarAsync() != null;

        if (!dbExists)
        {
            var createDbCommand = connection.CreateCommand();
            createDbCommand.CommandText = $"CREATE DATABASE \"{dbName}\"";
            await createDbCommand.ExecuteNonQueryAsync();
            logger.LogInformation("Created database: {DatabaseName}", dbName);
        }

        await connection.CloseAsync();

        // Apply migrations
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DocumentDbContext>();
        await context.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully");
    }
}