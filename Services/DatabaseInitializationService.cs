using Microsoft.EntityFrameworkCore;
using Npgsql;
using SMHFR_BE.Data;

namespace SMHFR_BE.Services;

public class DatabaseInitializationService : IDatabaseInitializationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseInitializationService> _logger;

    public DatabaseInitializationService(
        ApplicationDbContext context,
        ILogger<DatabaseInitializationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        _logger.LogInformation("Initializing database...");

        // Test connection with a simple SQL query - more robust than CanConnect()
        // EF Core's retry logic (configured in Program.cs) will handle transient failures
        try
        {
            await _context.Database.ExecuteSqlRawAsync("SELECT 1");
            _logger.LogInformation("✅ Database connection verified");
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "❌ Database connection failed!");
            _logger.LogError("   SQL State: {SqlState}", ex.SqlState ?? "Unknown");
            _logger.LogError("   Error Code: {ErrorCode}", ex.ErrorCode);
            _logger.LogError("   Message: {Message}", ex.Message);

            if (ex.SqlState == "28P01")
            {
                _logger.LogError("   ❌ PASSWORD AUTHENTICATION FAILED!");
                _logger.LogError("   The password in the connection string does not match PostgreSQL.");
                _logger.LogError("   Please verify ConnectionStrings__DefaultConnection environment variable.");
            }
            else if (ex.SqlState == "3D000")
            {
                _logger.LogError("   ❌ DATABASE DOES NOT EXIST!");
                _logger.LogError("   The database 'smhfr_db' might not have been created.");
            }
            else if (ex.Message.Contains("could not translate host name") || ex.Message.Contains("Name or service not known"))
            {
                _logger.LogError("   ❌ CANNOT RESOLVE DATABASE HOST!");
                _logger.LogError("   The hostname 'postgres' cannot be resolved.");
                _logger.LogError("   This might indicate a Docker network issue.");
            }
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Database connection test failed!");
            _logger.LogError("   Exception type: {ExceptionType}", ex.GetType().Name);
            _logger.LogError("   Message: {Message}", ex.Message);
            if (ex.InnerException != null)
            {
                _logger.LogError("   Inner exception: {InnerException}", ex.InnerException.Message);
            }
            throw;
        }

        // Run migrations - EF Core retry logic will handle transient failures automatically
        try
        {
            await _context.Database.MigrateAsync();
            _logger.LogInformation("✅ Database migrations applied successfully");
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "28P01")
        {
            _logger.LogError(ex, "❌ Database password authentication failed during migration!");
            _logger.LogError("   SQL State: {SqlState}", ex.SqlState);
            _logger.LogError("   This indicates the password in the connection string does not match PostgreSQL.");
            _logger.LogError("   Please verify ConnectionStrings__DefaultConnection environment variable.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to apply database migrations");
            _logger.LogError("   Exception type: {ExceptionType}", ex.GetType().Name);
            if (ex.InnerException != null)
            {
                _logger.LogError("   Inner exception: {InnerException}", ex.InnerException.Message);
            }
            throw;
        }
    }

    public async Task VerifyConnectionAsync()
    {
        _logger.LogInformation("Verifying database connection...");

        // Test connection with a simple SQL query
        try
        {
            await _context.Database.ExecuteSqlRawAsync("SELECT 1");
            _logger.LogInformation("✅ Database connection verified");
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "❌ Database connection failed!");
            _logger.LogError("   SQL State: {SqlState}", ex.SqlState ?? "Unknown");
            _logger.LogError("   Error Code: {ErrorCode}", ex.ErrorCode);
            _logger.LogError("   Message: {Message}", ex.Message);

            if (ex.SqlState == "28P01")
            {
                _logger.LogError("   ❌ PASSWORD AUTHENTICATION FAILED!");
                _logger.LogError("   The password in the connection string does not match PostgreSQL.");
                _logger.LogError("   Please verify ConnectionStrings__DefaultConnection environment variable.");
            }
            else if (ex.SqlState == "3D000")
            {
                _logger.LogError("   ❌ DATABASE DOES NOT EXIST!");
                _logger.LogError("   The database 'smhfr_db' might not have been created.");
            }
            else if (ex.Message.Contains("could not translate host name") || ex.Message.Contains("Name or service not known"))
            {
                _logger.LogError("   ❌ CANNOT RESOLVE DATABASE HOST!");
                _logger.LogError("   The hostname 'postgres' cannot be resolved.");
                _logger.LogError("   This might indicate a Docker network issue.");
            }
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Database connection test failed!");
            _logger.LogError("   Exception type: {ExceptionType}", ex.GetType().Name);
            _logger.LogError("   Message: {Message}", ex.Message);
            if (ex.InnerException != null)
            {
                _logger.LogError("   Inner exception: {InnerException}", ex.InnerException.Message);
            }
            throw;
        }
    }
}
