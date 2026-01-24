using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SMHFR_BE.Data;
using SMHFR_BE.Models;
using SMHFR_BE.Services;
using Npgsql;
using System.Security.Claims;
using System.Text;

// IMPORTANT: Check certificate BEFORE WebApplication.CreateBuilder
// Kestrel configuration is loaded during CreateBuilder, so we must clear env var before that
var certPathEnv = Environment.GetEnvironmentVariable("ASPNETCORE_Kestrel__Certificates__Default__Path");
var certPasswordEnv = Environment.GetEnvironmentVariable("ASPNETCORE_Kestrel__Certificates__Default__Password");

if (!string.IsNullOrEmpty(certPathEnv))
{
    Console.WriteLine($"[HTTPS Config] Certificate path from env: {certPathEnv}");
    if (!File.Exists(certPathEnv))
    {
        // Clear the environment variable IMMEDIATELY to prevent Kestrel from trying to load it
        Environment.SetEnvironmentVariable("ASPNETCORE_Kestrel__Certificates__Default__Path", null);
        Environment.SetEnvironmentVariable("ASPNETCORE_Kestrel__Certificates__Default__Password", null);
        Console.WriteLine($"⚠️  Certificate file not found: {certPathEnv}. HTTPS disabled. Using HTTP only.");
    }
    else
    {
        // Verify certificate can be loaded (password check)
        try
        {
            var certPassword = certPasswordEnv ?? string.Empty;
            using var cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(certPathEnv, certPassword);
            Console.WriteLine($"✅ Certificate file found and password is correct: {certPathEnv}");
        }
        catch (System.Security.Cryptography.CryptographicException ex) when (ex.Message.Contains("password", StringComparison.OrdinalIgnoreCase))
        {
            // Password is wrong - clear env vars and disable HTTPS
            Environment.SetEnvironmentVariable("ASPNETCORE_Kestrel__Certificates__Default__Path", null);
            Environment.SetEnvironmentVariable("ASPNETCORE_Kestrel__Certificates__Default__Password", null);
            Console.WriteLine($"⚠️  Certificate password is incorrect. HTTPS disabled. Using HTTP only.");
            Console.WriteLine($"   Set CERT_PASSWORD=dev-cert-password in .env file to enable HTTPS.");
        }
        catch (Exception ex)
        {
            // Other certificate errors - clear env vars and disable HTTPS
            Environment.SetEnvironmentVariable("ASPNETCORE_Kestrel__Certificates__Default__Path", null);
            Environment.SetEnvironmentVariable("ASPNETCORE_Kestrel__Certificates__Default__Password", null);
            Console.WriteLine($"⚠️  Certificate error: {ex.Message}. HTTPS disabled. Using HTTP only.");
        }
    }
}

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel for HTTPS (optional)
builder.WebHost.ConfigureKestrel(options =>
{
    // HTTP endpoint (always available)
    options.ListenAnyIP(8080);
    
    // HTTPS endpoint (only if certificate is available)
    var certPath = Environment.GetEnvironmentVariable("ASPNETCORE_Kestrel__Certificates__Default__Path")
        ?? builder.Configuration["Kestrel:Certificates:Default:Path"];
    var certPassword = Environment.GetEnvironmentVariable("ASPNETCORE_Kestrel__Certificates__Default__Password")
        ?? builder.Configuration["Kestrel:Certificates:Default:Password"];
    
    // Only configure HTTPS if certificate file actually exists
    if (!string.IsNullOrEmpty(certPath) && File.Exists(certPath))
    {
        try
        {
            // Use provided certificate
            options.ListenAnyIP(8443, listenOptions =>
            {
                if (!string.IsNullOrEmpty(certPassword))
                {
                    listenOptions.UseHttps(certPath, certPassword);
                }
                else
                {
                    listenOptions.UseHttps(certPath);
                }
            });
            Console.WriteLine($"✅ HTTPS enabled with certificate: {certPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Error loading certificate: {ex.Message}. HTTPS disabled. Using HTTP only.");
        }
    }
    else
    {
        Console.WriteLine("[HTTPS Config] No certificate path configured. HTTPS disabled. Using HTTP only.");
    }
    
    // Log all configured endpoints
    Console.WriteLine("[Kestrel Config] Configured endpoints:");
    Console.WriteLine("  - HTTP: http://0.0.0.0:8080");
    if (!string.IsNullOrEmpty(certPath) && File.Exists(certPath))
    {
        Console.WriteLine("  - HTTPS: https://0.0.0.0:8443");
    }
    else
    {
        Console.WriteLine("  - HTTPS: Not configured");
    }
});

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SMHFR API",
        Version = "v1",
        Description = "API documentation for SMHFR Backend"
    });

    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Register connection string service (must be singleton to persist across requests)
builder.Services.AddSingleton<IConnectionStringService, ConnectionStringService>();

// Get connection string using factory pattern to avoid BuildServiceProvider warning
string connectionString;
string connectionStringSource;

// Use a factory to get connection string without building service provider early
{
    // Create service manually to avoid BuildServiceProvider warning
    var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
    var logger = loggerFactory.CreateLogger<ConnectionStringService>();
    var connectionStringService = new ConnectionStringService(
        logger,
        builder.Environment,
        builder.Configuration
    );
    
    try
    {
        connectionString = connectionStringService.GetConnectionString();
        connectionStringSource = connectionStringService.HasPersistedConnectionString() 
            ? "Persisted File" 
            : (Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") != null
                ? "Environment Variable"
                : "appsettings.json");
        
        // Log connection string source and details (without password for security)
        var connectionStringForLogging = connectionString;
        if (connectionString.Contains("Password="))
        {
            var passwordIndex = connectionString.IndexOf("Password=");
            var passwordEnd = connectionString.IndexOf(";", passwordIndex);
            if (passwordEnd == -1) passwordEnd = connectionString.Length;
            connectionStringForLogging = connectionString.Substring(0, passwordIndex + 9) + "***" + connectionString.Substring(passwordEnd);
        }
        
        Console.WriteLine($"[DB Config] Connection string source: {connectionStringSource}");
        Console.WriteLine($"[DB Config] Connection string: {connectionStringForLogging}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Failed to get connection string: {ex.Message}");
        throw;
    }
    finally
    {
        loggerFactory.Dispose();
    }
}

// Validate connection string doesn't contain placeholder password
if (connectionString.Contains("Password=CHANGEME") || connectionString.Contains("Password=;") || connectionString.Contains("Password=\"\""))
{
    throw new InvalidOperationException(
        $"❌ Invalid database password detected in connection string!\n" +
        $"   The connection string contains a placeholder or empty password.\n" +
        $"   Please ensure ConnectionStrings__DefaultConnection environment variable is set correctly in docker-compose.yml");
}

// Extract password for validation (check if it's reasonable length)
if (connectionString.Contains("Password="))
{
    var passwordStart = connectionString.IndexOf("Password=") + 9;
    var passwordEnd = connectionString.IndexOf(";", passwordStart);
    if (passwordEnd == -1) passwordEnd = connectionString.Length;
    var password = connectionString.Substring(passwordStart, passwordEnd - passwordStart);
    
    if (password.Length < 3)
    {
        throw new InvalidOperationException(
            $"❌ Database password is too short or invalid!\n" +
            $"   Password length: {password.Length}\n" +
            $"   Please ensure ConnectionStrings__DefaultConnection environment variable has a valid password.");
    }
}

// Database connection verification is handled by:
// 1. Docker health checks (docker-compose.yml) - ensures postgres is ready before API starts
// 2. CI/CD pipeline - verifies database connectivity during deployment
// 3. EF Core retry logic - handles transient connection failures gracefully
// 4. Application health checks - /api/health endpoint for monitoring

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        // Enable connection retry on transient failures
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
    }));

// Configure Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
var issuer = jwtSettings["Issuer"] ?? "SMHFR_API";
var audience = jwtSettings["Audience"] ?? "SMHFR_Client";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        {
            KeyId = "SMHFR_JWT_KEY"
        },
        ClockSkew = TimeSpan.Zero,
        RoleClaimType = ClaimTypes.Role,
        RequireSignedTokens = true,
        ValidateActor = false
    };
    
    // Add event handlers for debugging
    options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(context.Exception, "JWT Authentication failed");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("JWT Token validated successfully for user: {UserId}", context.Principal?.Identity?.Name);
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("JWT Challenge triggered. Error: {Error}, ErrorDescription: {ErrorDescription}", context.Error, context.ErrorDescription);
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// Configure CORS to allow all origins
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add services
builder.Services.AddScoped<IHealthCheckService, HealthCheckService>();
builder.Services.AddScoped<ICSVImportService, CSVImportService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAdminSeedService, AdminSeedService>();
builder.Services.AddScoped<IValidationService, ValidationService>();
builder.Services.AddScoped<IDatabaseInitializationService, DatabaseInitializationService>();
// ConnectionStringService is already registered as Singleton above

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SMHFR API V1");
    c.RoutePrefix = "swagger"; // Swagger UI at /swagger (not /swagger/index.html)
    c.DisplayRequestDuration();
    c.EnableDeepLinking();
    c.EnableFilter();
    c.ShowExtensions();
});

// Don't use HTTPS redirection - it causes issues when both HTTP and HTTPS are enabled
// Users can access either port directly: HTTP on 8080, HTTPS on 8443

// Enable CORS - must be before UseAuthentication and UseAuthorization
app.UseCors();

// Add database error handling middleware
app.UseMiddleware<SMHFR_BE.Middleware.DatabaseErrorMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Log startup information
app.Lifetime.ApplicationStarted.Register(() =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("🚀 Application started!");
    logger.LogInformation("📡 Listening endpoints:");
    logger.LogInformation("   - HTTP: http://0.0.0.0:8080");
    var httpsCertPath = Environment.GetEnvironmentVariable("ASPNETCORE_Kestrel__Certificates__Default__Path")
        ?? builder.Configuration["Kestrel:Certificates:Default:Path"];
    if (!string.IsNullOrEmpty(httpsCertPath) && File.Exists(httpsCertPath))
    {
        logger.LogInformation("   - HTTPS: https://0.0.0.0:8443 ✅");
    }
    else
    {
        logger.LogInformation("   - HTTPS: Not configured");
    }
});

// Initialize database and seed admin user on startup
// Note: Migrations are run in CI/CD pipeline, not here
// This only seeds the admin user and verifies connection
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // Clear connection pool on startup to ensure no stale connections with wrong password
        // This prevents the 5-10 minute password mismatch issue
        Npgsql.NpgsqlConnection.ClearAllPools();
        logger.LogInformation("✅ Connection pool cleared on startup");
        
        // Verify database connection (migrations are handled by CI/CD)
        var dbInitService = scope.ServiceProvider.GetRequiredService<IDatabaseInitializationService>();
        await dbInitService.VerifyConnectionAsync();
        
        // Seed admin user
        var adminSeedService = scope.ServiceProvider.GetRequiredService<IAdminSeedService>();
        await adminSeedService.SeedAsync();
        
        logger.LogInformation("✅ Database connection verified and admin user seeded");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Failed to initialize database");
        logger.LogError("   Exception: {ExceptionType}", ex.GetType().Name);
        logger.LogError("   Message: {Message}", ex.Message);
        if (ex.InnerException != null)
        {
            logger.LogError("   Inner Exception: {InnerException}", ex.InnerException.Message);
        }
        // Don't throw - allow API to start even if seeding fails
        // Migrations should have been run by CI/CD pipeline
        logger.LogWarning("⚠️  API will start but database seeding may have failed");
    }
}

app.Run();
