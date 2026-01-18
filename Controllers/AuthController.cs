using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SMHFR_BE.DTOs;
using SMHFR_BE.Models;
using SMHFR_BE.Services;
using Npgsql;

namespace SMHFR_BE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        IJwtService jwtService,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _jwtService = jwtService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register(RegisterRequest request)
    {
        try
        {
            if (await _userManager.FindByEmailAsync(request.Email) != null)
            {
                return BadRequest(ApiResponse<AuthResponse>.ErrorResult("Email is already registered"));
            }

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return BadRequest(ApiResponse<AuthResponse>.ErrorResult("User registration failed", errors));
            }

            // Assign default role (if you want to assign a role on registration)
            // await _userManager.AddToRoleAsync(user, "User");

            var roles = await _userManager.GetRolesAsync(user);
            var token = _jwtService.GenerateToken(user, roles);
            var refreshToken = _jwtService.GenerateRefreshToken();

            var authResponse = new AuthResponse
            {
                Token = token,
                RefreshToken = refreshToken,
                Expiration = DateTime.UtcNow.AddMinutes(60),
                User = new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Roles = roles.ToList()
                }
            };

            return Ok(ApiResponse<AuthResponse>.SuccessResult(authResponse, "User registered successfully"));
        }
        catch (Npgsql.PostgresException pgEx) when (pgEx.SqlState == "28P01")
        {
            _logger.LogError(pgEx, "❌ Database password authentication failed in Register");
            
            // Clear connection pool to force new connections
            try
            {
                NpgsqlConnection.ClearAllPools();
                _logger.LogInformation("Cleared Npgsql connection pool after authentication failure in Register");
            }
            catch (Exception clearEx)
            {
                _logger.LogError(clearEx, "Failed to clear connection pool");
            }
            
            return StatusCode(500, ApiResponse<AuthResponse>.ErrorResult(
                "Database authentication failed. Please check database credentials.",
                new List<string> 
                { 
                    "28P01: password authentication failed for user \"postgres\"",
                    "This error occurs when the PostgreSQL password in docker-compose.yml doesn't match the password stored in the database volume."
                }));
        }
        catch (Exception ex)
        {
            // Check for nested PostgreSQL authentication errors
            if (ex.InnerException is Npgsql.PostgresException innerPgEx && innerPgEx.SqlState == "28P01")
            {
                _logger.LogError(innerPgEx, "❌ Database password authentication failed in Register (inner exception)");
                
                try
                {
                    NpgsqlConnection.ClearAllPools();
                    _logger.LogInformation("Cleared Npgsql connection pool after authentication failure in Register (inner exception)");
                }
                catch (Exception clearEx)
                {
                    _logger.LogError(clearEx, "Failed to clear connection pool");
                }
                
                return StatusCode(500, ApiResponse<AuthResponse>.ErrorResult(
                    "Database authentication failed. Please check database credentials.",
                    new List<string> 
                    { 
                        "28P01: password authentication failed for user \"postgres\"",
                        "This error occurs when the PostgreSQL password in docker-compose.yml doesn't match the password stored in the database volume."
                    }));
            }
            
            _logger.LogError(ex, "Error registering user");
            return StatusCode(500, ApiResponse<AuthResponse>.ErrorResult("An error occurred while registering the user", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Login user
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(LoginRequest request)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !user.IsActive)
            {
                return Unauthorized(ApiResponse<AuthResponse>.ErrorResult("Invalid email or password"));
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                return Unauthorized(ApiResponse<AuthResponse>.ErrorResult("Invalid email or password"));
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            var roles = await _userManager.GetRolesAsync(user);
            var token = _jwtService.GenerateToken(user, roles);
            var refreshToken = _jwtService.GenerateRefreshToken();

            var authResponse = new AuthResponse
            {
                Token = token,
                RefreshToken = refreshToken,
                Expiration = DateTime.UtcNow.AddMinutes(60),
                User = new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Roles = roles.ToList()
                }
            };

            return Ok(ApiResponse<AuthResponse>.SuccessResult(authResponse, "Login successful"));
        }
        catch (Npgsql.PostgresException pgEx) when (pgEx.SqlState == "28P01")
        {
            _logger.LogError(pgEx, "❌ Database password authentication failed in Login");
            
            // Clear connection pool to force new connections
            try
            {
                NpgsqlConnection.ClearAllPools();
                _logger.LogInformation("Cleared Npgsql connection pool after authentication failure in Login");
            }
            catch (Exception clearEx)
            {
                _logger.LogError(clearEx, "Failed to clear connection pool");
            }
            
            return StatusCode(500, ApiResponse<AuthResponse>.ErrorResult(
                "Database authentication failed. Please check database credentials.",
                new List<string> 
                { 
                    "28P01: password authentication failed for user \"postgres\"",
                    "This error occurs when the PostgreSQL password in docker-compose.yml doesn't match the password stored in the database volume."
                }));
        }
        catch (Exception ex)
        {
            // Check for nested PostgreSQL authentication errors
            if (ex.InnerException is Npgsql.PostgresException innerPgEx && innerPgEx.SqlState == "28P01")
            {
                _logger.LogError(innerPgEx, "❌ Database password authentication failed in Login (inner exception)");
                
                try
                {
                    NpgsqlConnection.ClearAllPools();
                    _logger.LogInformation("Cleared Npgsql connection pool after authentication failure in Login (inner exception)");
                }
                catch (Exception clearEx)
                {
                    _logger.LogError(clearEx, "Failed to clear connection pool");
                }
                
                return StatusCode(500, ApiResponse<AuthResponse>.ErrorResult(
                    "Database authentication failed. Please check database credentials.",
                    new List<string> 
                    { 
                        "28P01: password authentication failed for user \"postgres\"",
                        "This error occurs when the PostgreSQL password in docker-compose.yml doesn't match the password stored in the database volume."
                    }));
            }
            
            _logger.LogError(ex, "Error during login");
            return StatusCode(500, ApiResponse<AuthResponse>.ErrorResult("An error occurred during login", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Get current user information
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserInfo>>> GetCurrentUser()
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<UserInfo>.ErrorResult("User not authenticated"));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(ApiResponse<UserInfo>.ErrorResult("User not found"));
            }

            var roles = await _userManager.GetRolesAsync(user);

            var userInfo = new UserInfo
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = roles.ToList()
            };

            return Ok(ApiResponse<UserInfo>.SuccessResult(userInfo, "User information retrieved successfully"));
        }
        catch (Npgsql.PostgresException pgEx) when (pgEx.SqlState == "28P01")
        {
            _logger.LogError(pgEx, "❌ Database password authentication failed in GetCurrentUser");
            
            // Clear connection pool to force new connections
            try
            {
                NpgsqlConnection.ClearAllPools();
                _logger.LogInformation("Cleared Npgsql connection pool after authentication failure in GetCurrentUser");
            }
            catch (Exception clearEx)
            {
                _logger.LogError(clearEx, "Failed to clear connection pool");
            }
            
            return StatusCode(500, ApiResponse<UserInfo>.ErrorResult(
                "Database authentication failed. Please check database credentials.",
                new List<string> 
                { 
                    "28P01: password authentication failed for user \"postgres\"",
                    "This error occurs when the PostgreSQL password in docker-compose.yml doesn't match the password stored in the database volume."
                }));
        }
        catch (Exception ex)
        {
            // Check for nested PostgreSQL authentication errors
            if (ex.InnerException is Npgsql.PostgresException innerPgEx && innerPgEx.SqlState == "28P01")
            {
                _logger.LogError(innerPgEx, "❌ Database password authentication failed in GetCurrentUser (inner exception)");
                
                try
                {
                    NpgsqlConnection.ClearAllPools();
                    _logger.LogInformation("Cleared Npgsql connection pool after authentication failure in GetCurrentUser (inner exception)");
                }
                catch (Exception clearEx)
                {
                    _logger.LogError(clearEx, "Failed to clear connection pool");
                }
                
                return StatusCode(500, ApiResponse<UserInfo>.ErrorResult(
                    "Database authentication failed. Please check database credentials.",
                    new List<string> 
                    { 
                        "28P01: password authentication failed for user \"postgres\"",
                        "This error occurs when the PostgreSQL password in docker-compose.yml doesn't match the password stored in the database volume."
                    }));
            }
            
            _logger.LogError(ex, "Error retrieving current user");
            return StatusCode(500, ApiResponse<UserInfo>.ErrorResult("An error occurred while retrieving user information", new List<string> { ex.Message }));
        }
    }
}
