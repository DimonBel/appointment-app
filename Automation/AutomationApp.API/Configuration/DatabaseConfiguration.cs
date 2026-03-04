using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using AutomationApp.Postgres.Data;

namespace AutomationApp.API.Configuration;

/// <summary>
/// Database configuration and initialization
/// </summary>
public static class DatabaseConfiguration
{
    /// <summary>
    /// Configure PostgreSQL database with dynamic JSON support
    /// </summary>
    public static IServiceCollection ConfigureDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        NpgsqlDataSourceBuilder dataSourceBuilder = new NpgsqlDataSourceBuilder(configuration.GetConnectionString("DefaultConnection"));
        dataSourceBuilder.EnableDynamicJson();
        var dataSource = dataSourceBuilder.Build();

        services.AddDbContext<AutomationDbContext>(options =>
            options.UseNpgsql(dataSource,
                b => b.MigrationsAssembly("AutomationApp.Postgres"))
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

        return services;
    }

    /// <summary>
    /// Ensure database is created and migrations are applied
    /// </summary>
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
        var dbContext = scope.ServiceProvider.GetRequiredService<AutomationDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}