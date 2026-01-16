using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SMHFR_BE.Models;

namespace SMHFR_BE.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSets
    public DbSet<State> States { get; set; }
    public DbSet<Region> Regions { get; set; }
    public DbSet<District> Districts { get; set; }
    public DbSet<FacilityType> FacilityTypes { get; set; }
    public DbSet<HealthFacility> HealthFacilities { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure State
        modelBuilder.Entity<State>(entity =>
        {
            entity.HasIndex(e => e.StateCode).IsUnique();
        });

        // Configure Region
        modelBuilder.Entity<Region>(entity =>
        {
            entity.HasIndex(e => new { e.StateId, e.RegionName }).IsUnique();
        });

        // Configure District
        modelBuilder.Entity<District>(entity =>
        {
            entity.HasIndex(e => new { e.RegionId, e.DistrictName }).IsUnique();
        });

        // Configure FacilityType
        modelBuilder.Entity<FacilityType>(entity =>
        {
            entity.HasIndex(e => e.TypeName).IsUnique();
        });

        // Configure HealthFacility
        modelBuilder.Entity<HealthFacility>(entity =>
        {
            entity.HasIndex(e => e.FacilityId).IsUnique();
            entity.HasIndex(e => e.DistrictId);
            entity.HasIndex(e => e.FacilityTypeId);
            entity.HasIndex(e => e.Ownership);
            entity.HasIndex(e => e.OperationalStatus);
            entity.HasIndex(e => new { e.Latitude, e.Longitude });
        });
    }
}
