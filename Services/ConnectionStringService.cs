using System.Text;

namespace SMHFR_BE.Services;

public class ConnectionStringService : IConnectionStringService
{
    private readonly ILogger<ConnectionStringService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly string _persistenceFilePath;
    private string? _cachedConnectionString;

    public ConnectionStringService(
        ILogger<ConnectionStringService> logger, 
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _environment = environment;
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

        // Priority 1: Environment variable (always used in Docker/production)
        var envConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings:DefaultConnection");

        if (!string.IsNullOrWhiteSpace(envConnectionString))
        {
            _cachedConnectionString = envConnectionString;
            // Only persist in development - in production/Docker, always use environment variables
            // This prevents password drift when passwords change in .env
            if (_environment.IsDevelopment())
            {
                PersistConnectionString(envConnectionString);
            }
            return envConnectionString;
        }

        // Priority 2: Persisted file (only in development - production should use env vars)
        // In production/Docker, skip persisted file to prevent password drift
        if (_environment.IsDevelopment() && File.Exists(_persistenceFilePath))
        {
            try
            {
                var persisted = File.ReadAllText(_persistenceFilePath, Encoding.UTF8).Trim();
                if (!string.IsNullOrWhiteSpace(persisted))
                {
                    _cachedConnectionString = persisted;
                    return persisted;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read persisted connection string");
            }
        }

        // Priority 3: appsettings.json (local development fallback)
        var configConnectionString = _configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(configConnectionString))
        {
            _cachedConnectionString = configConnectionString;
            PersistConnectionString(configConnectionString);
            return configConnectionString;
        }

        throw new InvalidOperationException("Database connection string is missing");
    }

    public void PersistConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        try
        {
            var directory = Path.GetDirectoryName(_persistenceFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(_persistenceFilePath, connectionString, Encoding.UTF8);
            _cachedConnectionString = connectionString;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist connection string");
        }
    }

    public bool HasPersistedConnectionString()
    {
        return File.Exists(_persistenceFilePath) && 
               !string.IsNullOrWhiteSpace(File.ReadAllText(_persistenceFilePath, Encoding.UTF8).Trim());
    }
}
