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

var builder = WebApplication.CreateBuilder(args);

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

// Configure PostgreSQL connection
// Priority: Environment variable > appsettings.json
// Environment variable format: ConnectionStrings__DefaultConnection (double underscore for nested config)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found. Ensure ConnectionStrings__DefaultConnection environment variable is set.");
}

// Enhance connection string with better connection management settings
var enhancedConnectionString = connectionString;
var connectionParams = new List<string>();

// Add parameters only if not already present
if (!connectionString.Contains("Command Timeout", StringComparison.OrdinalIgnoreCase))
{
    connectionParams.Add("Command Timeout=30");
}
if (!connectionString.Contains("Connection Lifetime", StringComparison.OrdinalIgnoreCase))
{
    connectionParams.Add("Connection Lifetime=300");
}
if (!connectionString.Contains("Pooling", StringComparison.OrdinalIgnoreCase))
{
    connectionParams.Add("Pooling=true");
    connectionParams.Add("Minimum Pool Size=0");
    connectionParams.Add("Maximum Pool Size=100");
}

if (connectionParams.Count > 0)
{
    enhancedConnectionString = connectionString.TrimEnd(';') + ";" + string.Join(";", connectionParams);
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(enhancedConnectionString, npgsqlOptions =>
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 10,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: new[] { "57P01", "57P02", "57P03", "08003", "08006", "08001", "40001", "40P01" }))); // Connection and deadlock errors, but NOT password auth (28P01)

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

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SMHFR API V1");
    c.RoutePrefix = "swagger"; // Swagger UI at /swagger/index.html
});

// Only use HTTPS redirection when HTTPS port is explicitly configured
// This avoids warnings when running locally without HTTPS
var httpsPort = builder.Configuration["ASPNETCORE_HTTPS_PORT"];
if (httpsPort != null && !app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Enable CORS - must be before UseAuthentication and UseAuthorization
app.UseCors();

// Add database error handling middleware
app.UseMiddleware<SMHFR_BE.Middleware.DatabaseErrorMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Apply pending migrations and seed admin user on startup with connection validation
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    // Validate database connection with retries
    int retryCount = 0;
    const int maxRetries = 10;
    bool connectionValid = false;
    
    while (retryCount < maxRetries && !connectionValid)
    {
        try
        {
            logger.LogInformation("Attempting to connect to database (attempt {Attempt}/{MaxRetries})...", retryCount + 1, maxRetries);
            
            // Test connection - this will throw an exception if there's an error
            // CanConnect() can return false without throwing, so we force a query execution
            db.Database.ExecuteSqlRaw("SELECT 1");
            connectionValid = true;
            logger.LogInformation("✅ Database connection validated successfully");
            break;
        }
        catch (Npgsql.PostgresException pgEx) when (pgEx.SqlState == "28P01")
        {
            logger.LogError(pgEx, "❌ Database password authentication failed (attempt {Attempt}/{MaxRetries})", retryCount + 1, maxRetries);
            
            // Clear connection pool on authentication failure
            Npgsql.NpgsqlConnection.ClearAllPools();
            logger.LogInformation("Cleared Npgsql connection pool");
            
            if (retryCount >= maxRetries - 1)
            {
                logger.LogCritical("FATAL: Cannot connect to database after {MaxRetries} attempts. Password authentication is failing. Please check database credentials.", maxRetries);
                throw new InvalidOperationException($"Database password authentication failed after {maxRetries} attempts. Please verify database credentials in configuration.", pgEx);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Database connection attempt {Attempt}/{MaxRetries} failed: {ExceptionType} - {Message}", 
                retryCount + 1, maxRetries, ex.GetType().Name, ex.Message);
            
            // Log inner exception details if present
            if (ex.InnerException != null)
            {
                logger.LogError(ex.InnerException, "Inner exception: {InnerExceptionType} - {InnerMessage}", 
                    ex.InnerException.GetType().Name, ex.InnerException.Message);
            }
            
            // Log connection string (mask password)
            var connStr = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Not found";
            var maskedConnStr = connStr.Contains("Password=") 
                ? System.Text.RegularExpressions.Regex.Replace(connStr, @"Password=[^;]+", "Password=***") 
                : connStr;
            logger.LogError("Connection string being used: {ConnectionString}", maskedConnStr);
            
            if (retryCount >= maxRetries - 1)
            {
                logger.LogCritical(ex, "FATAL: Failed to connect to database after {MaxRetries} attempts. Last error: {ExceptionType} - {Message}", 
                    maxRetries, ex.GetType().Name, ex.Message);
                throw;
            }
        }
        
        retryCount++;
        if (!connectionValid)
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
    
    if (!connectionValid)
    {
        throw new InvalidOperationException($"Failed to establish database connection after {maxRetries} attempts");
    }
    
    // Apply migrations
    try
    {
        logger.LogInformation("Applying database migrations...");
        db.Database.Migrate();
        logger.LogInformation("✅ Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to apply database migrations");
        throw;
    }

    // Seed admin user
    try
    {
        logger.LogInformation("Seeding admin user...");
        var adminSeedService = scope.ServiceProvider.GetRequiredService<IAdminSeedService>();
        await adminSeedService.SeedAsync();
        logger.LogInformation("✅ Admin user seeding completed");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to seed admin user");
        throw;
    }
}

app.Run();
