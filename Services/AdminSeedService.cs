using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SMHFR_BE.Data;
using SMHFR_BE.Models;

namespace SMHFR_BE.Services;

public interface IAdminSeedService
{
    Task SeedAsync();
}

public class AdminSeedService : IAdminSeedService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminSeedService> _logger;

    public AdminSeedService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration,
        ILogger<AdminSeedService> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            // Create roles
            await CreateRolesAsync();

            // Create admin user
            await CreateAdminUserAsync();

            _logger.LogInformation("Admin seed completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding admin user");
            throw;
        }
    }

    private async Task CreateRolesAsync()
    {
        var roles = new[] { "Admin", "User" };

        foreach (var roleName in roles)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new IdentityRole(roleName));
                _logger.LogInformation("Created role: {RoleName}", roleName);
            }
        }
    }

    private async Task CreateAdminUserAsync()
    {
        var adminSettings = _configuration.GetSection("AdminSettings");
        var adminEmail = adminSettings["Email"] ?? "admin@smhfr.com";
        var adminPassword = adminSettings["Password"] ?? "Admin@123";
        var adminFirstName = adminSettings["FirstName"] ?? "Admin";
        var adminLastName = adminSettings["LastName"] ?? "User";

        var adminUser = await _userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = adminFirstName,
                LastName = adminLastName,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(adminUser, adminPassword);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(adminUser, "Admin");
                _logger.LogInformation("Admin user created successfully: {Email}", adminEmail);
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to create admin user: {Errors}", errors);
                throw new Exception($"Failed to create admin user: {errors}");
            }
        }
        else
        {
            // Ensure admin user has Admin role
            if (!await _userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                await _userManager.AddToRoleAsync(adminUser, "Admin");
                _logger.LogInformation("Admin role assigned to existing user: {Email}", adminEmail);
            }
        }
    }
}
