using System.Text;

namespace SMHFR_BE.Services;

public class ConnectionStringService : IConnectionStringService
{
    private readonly ILogger<ConnectionStringService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _persistenceFilePath;
    private string? _cachedConnectionString;

    public ConnectionStringService(
        ILogger<ConnectionStringService> logger, 
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        // Store in a persistent location (works in Docker volumes)
        _persistenceFilePath = Path.Combine(
            environment.ContentRootPath,
            "data",
            ".connectionstring"
        );
        
        // Ensure directory exists
        var directory = Path.GetDirectoryName(_persistenceFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public string GetConnectionString()
    {
        // Return cached value if available
        if (_cachedConnectionString != null)
        {
            return _cachedConnectionString;
        }

        // Try to load from persisted file first
        if (File.Exists(_persistenceFilePath))
        {
            try
            {
                var persisted = File.ReadAllText(_persistenceFilePath, Encoding.UTF8).Trim();
                if (!string.IsNullOrWhiteSpace(persisted))
                {
                    _logger.LogInformation("✅ Loaded connection string from persisted file");
                    _cachedConnectionString = persisted;
                    return persisted;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️  Failed to read persisted connection string, falling back to environment variable");
            }
        }

        // Fallback to environment variable
        var envConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings:DefaultConnection");

        if (!string.IsNullOrWhiteSpace(envConnectionString))
        {
            _logger.LogInformation("✅ Using connection string from environment variable");
            _cachedConnectionString = envConnectionString;
            
            // Persist it for next time
            try
            {
                PersistConnectionString(envConnectionString);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️  Failed to persist connection string from environment variable");
            }
            
            return envConnectionString;
        }

        // Fallback to appsettings.json (for local development)
        var configConnectionString = _configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(configConnectionString))
        {
            _logger.LogWarning("⚠️  Using connection string from appsettings.json (not recommended for production)");
            _cachedConnectionString = configConnectionString;
            
            // Persist it for next time
            try
            {
                PersistConnectionString(configConnectionString);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️  Failed to persist connection string from appsettings.json");
            }
            
            return configConnectionString;
        }

        throw new InvalidOperationException(
            "❌ Database connection string is missing!\n" +
            "   No persisted connection string found, ConnectionStrings__DefaultConnection environment variable is not set, and appsettings.json doesn't have DefaultConnection.\n" +
            $"   Persistence file location: {_persistenceFilePath}\n" +
            "   For local development, add ConnectionStrings.DefaultConnection to appsettings.json or appsettings.Development.json");
    }

    public void PersistConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));
        }

        try
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(_persistenceFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Write to file with restricted permissions (owner read/write only)
            File.WriteAllText(_persistenceFilePath, connectionString, Encoding.UTF8);
            
            // Try to set file permissions (Unix/Linux only, ignored on Windows)
            try
            {
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    System.Diagnostics.Process.Start("chmod", $"600 {_persistenceFilePath}")?.WaitForExit();
                }
            }
            catch
            {
                // Ignore permission setting failures
            }

            _cachedConnectionString = connectionString;
            _logger.LogInformation("✅ Connection string persisted to {FilePath}", _persistenceFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to persist connection string to {FilePath}", _persistenceFilePath);
            throw;
        }
    }

    public bool HasPersistedConnectionString()
    {
        return File.Exists(_persistenceFilePath) && 
               !string.IsNullOrWhiteSpace(File.ReadAllText(_persistenceFilePath, Encoding.UTF8).Trim());
    }
}
