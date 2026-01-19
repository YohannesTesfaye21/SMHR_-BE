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
// IMPORTANT: We ONLY use environment variable - never appsettings.json for connection string
// This ensures single source of truth and prevents password mismatches

// Check environment variable first (this is what docker-compose.yml sets)
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") 
    ?? Environment.GetEnvironmentVariable("ConnectionStrings:DefaultConnection"); // Fallback for different formats

// If not in environment, check configuration (but this should not happen in production)
if (string.IsNullOrWhiteSpace(connectionString))
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        Console.WriteLine("⚠️  WARNING: Using connection string from appsettings.json instead of environment variable!");
        Console.WriteLine("⚠️  This is NOT recommended for production. Please set ConnectionStrings__DefaultConnection environment variable.");
    }
}

// If connection string is still empty or null, throw a clear error
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "❌ Database connection string is missing or empty!\n" +
        "   Please set the ConnectionStrings__DefaultConnection environment variable.\n" +
        "   In Docker, this should be set via docker-compose.yml environment section.\n" +
        "   Current environment variables:\n" +
        $"   - ConnectionStrings__DefaultConnection: {(Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") != null ? "SET" : "NOT SET")}\n" +
        $"   - ConnectionStrings:DefaultConnection: {(Environment.GetEnvironmentVariable("ConnectionStrings:DefaultConnection") != null ? "SET" : "NOT SET")}");
}

// Determine connection string source for logging
var connectionStringSource = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") != null 
    ? "Environment Variable (ConnectionStrings__DefaultConnection)" 
    : Environment.GetEnvironmentVariable("ConnectionStrings:DefaultConnection") != null
    ? "Environment Variable (ConnectionStrings:DefaultConnection)"
    : "Configuration (appsettings.json - NOT RECOMMENDED)";

// Log connection string source and details (without password for security)
var connectionStringForLogging = connectionString;
if (connectionString.Contains("Password="))
{
    var passwordIndex = connectionString.IndexOf("Password=");
    var passwordEnd = connectionString.IndexOf(";", passwordIndex);
    if (passwordEnd == -1) passwordEnd = connectionString.Length;
    var passwordLength = passwordEnd - passwordIndex - 9; // "Password=" is 9 chars
    connectionStringForLogging = connectionString.Substring(0, passwordIndex + 9) + "***" + connectionString.Substring(passwordEnd);
}

Console.WriteLine($"[DB Config] Connection string source: {connectionStringSource}");
Console.WriteLine($"[DB Config] Connection string: {connectionStringForLogging}");

// Validate connection string doesn't contain placeholder password
if (connectionString.Contains("Password=CHANGEME") || connectionString.Contains("Password=;") || connectionString.Contains("Password=\"\""))
{
    throw new InvalidOperationException(
        $"❌ Invalid database password detected in connection string!\n" +
        $"   Source: {connectionStringSource}\n" +
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
            $"   Source: {connectionStringSource}\n" +
            $"   Password length: {password.Length}\n" +
            $"   Please ensure ConnectionStrings__DefaultConnection environment variable has a valid password.");
    }
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

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

// Apply pending migrations and seed admin user on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    // Apply migrations
    try
    {
        logger.LogInformation("Applying database migrations...");
        
        // Test connection before migrating - catch actual exceptions
        try
        {
            var canConnect = db.Database.CanConnect();
            if (!canConnect)
            {
                logger.LogError("❌ Cannot connect to database before migrations!");
                logger.LogError("   This usually indicates a connection string or authentication issue.");
                throw new InvalidOperationException("Cannot connect to database. Check connection string and credentials.");
            }
            logger.LogInformation("✅ Database connection test successful");
        }
        catch (Npgsql.NpgsqlException ex)
        {
            logger.LogError(ex, "❌ Database connection failed with NpgsqlException!");
            logger.LogError("   SQL State: {SqlState}", ex.SqlState ?? "Unknown");
            logger.LogError("   Error Code: {ErrorCode}", ex.ErrorCode);
            logger.LogError("   Message: {Message}", ex.Message);
            
            if (ex.SqlState == "28P01")
            {
                logger.LogError("   ❌ PASSWORD AUTHENTICATION FAILED!");
                logger.LogError("   The password in the connection string does not match PostgreSQL.");
                logger.LogError("   Please verify ConnectionStrings__DefaultConnection environment variable.");
            }
            else if (ex.SqlState == "3D000")
            {
                logger.LogError("   ❌ DATABASE DOES NOT EXIST!");
                logger.LogError("   The database 'smhfr_db' might not have been created.");
            }
            else if (ex.Message.Contains("could not translate host name") || ex.Message.Contains("Name or service not known"))
            {
                logger.LogError("   ❌ CANNOT RESOLVE DATABASE HOST!");
                logger.LogError("   The hostname 'postgres' cannot be resolved.");
                logger.LogError("   This might indicate a Docker network issue.");
            }
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Database connection test failed!");
            logger.LogError("   Exception type: {ExceptionType}", ex.GetType().Name);
            logger.LogError("   Message: {Message}", ex.Message);
            if (ex.InnerException != null)
            {
                logger.LogError("   Inner exception: {InnerException}", ex.InnerException.Message);
            }
            throw;
        }
        
        db.Database.Migrate();
        logger.LogInformation("✅ Database migrations applied successfully");
    }
    catch (Npgsql.NpgsqlException ex) when (ex.SqlState == "28P01")
    {
        logger.LogError(ex, "❌ Database password authentication failed during migration!");
        logger.LogError("   SQL State: {SqlState}", ex.SqlState);
        logger.LogError("   This indicates the password in the connection string does not match PostgreSQL.");
        logger.LogError("   Please verify ConnectionStrings__DefaultConnection environment variable.");
        throw;
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Failed to apply database migrations");
        logger.LogError("   Exception type: {ExceptionType}", ex.GetType().Name);
        if (ex.InnerException != null)
        {
            logger.LogError("   Inner exception: {InnerException}", ex.InnerException.Message);
        }
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
