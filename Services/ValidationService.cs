using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using SMHFR_BE.Data;
using SMHFR_BE.DTOs;
using SMHFR_BE.Models;
using ValidationResult = SMHFR_BE.DTOs.ValidationResult;

namespace SMHFR_BE.Services;

/// <summary>
/// Service for validating entities and DTOs before database operations
/// </summary>
public class ValidationService : IValidationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ValidationService> _logger;

    public ValidationService(ApplicationDbContext context, ILogger<ValidationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Validates a CreateHealthFacilityDTO
    /// </summary>
    public async Task<ValidationResult> ValidateCreateHealthFacilityAsync(CreateHealthFacilityDTO dto)
    {
        var result = ValidationResult.Success();

        // Validate data annotations
        var validationContext = new ValidationContext(dto);
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        
        if (!Validator.TryValidateObject(dto, validationContext, validationResults, true))
        {
            foreach (var validationResult in validationResults)
            {
                var fieldName = validationResult.MemberNames.FirstOrDefault() ?? "Unknown";
                result.AddError(fieldName, validationResult.ErrorMessage ?? "Invalid value");
            }
        }

        // Type and business logic validations
        result = await ValidateHealthFacilityCommonFieldsAsync(dto, result, null);

        return result;
    }

    /// <summary>
    /// Validates an UpdateHealthFacilityDTO for update
    /// </summary>
    public async Task<ValidationResult> ValidateUpdateHealthFacilityAsync(int id, UpdateHealthFacilityDTO dto)
    {
        var result = ValidationResult.Success();

        // Check if entity exists
        var exists = await _context.HealthFacilities.AnyAsync(hf => hf.HealthFacilityId == id);
        if (!exists)
        {
            result.AddError("Id", $"Health facility with ID {id} does not exist");
            return result; // Return early if doesn't exist
        }

        // Validate data annotations first
        var validationContext = new ValidationContext(dto);
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        
        if (!Validator.TryValidateObject(dto, validationContext, validationResults, true))
        {
            foreach (var validationResult in validationResults)
            {
                var fieldName = validationResult.MemberNames.FirstOrDefault() ?? "Unknown";
                result.AddError(fieldName, validationResult.ErrorMessage ?? "Invalid value");
            }
        }

        // Type and business logic validations
        result = await ValidateHealthFacilityCommonFieldsAsync(dto, result, id);

        return result;
    }

    /// <summary>
    /// Validates if a health facility exists for deletion
    /// </summary>
    public async Task<ValidationResult> ValidateDeleteHealthFacilityAsync(int id)
    {
        var result = ValidationResult.Success();

        // Validate ID is positive
        if (id <= 0)
        {
            result.AddError("Id", "Health facility ID must be greater than 0");
            return result;
        }

        // Check if entity exists
        var exists = await _context.HealthFacilities.AnyAsync(hf => hf.HealthFacilityId == id);
        if (!exists)
        {
            result.AddError("Id", $"Health facility with ID {id} does not exist");
        }

        return result;
    }

    /// <summary>
    /// Common validation logic for create operations
    /// </summary>
    private async Task<ValidationResult> ValidateHealthFacilityCommonFieldsAsync(
        CreateHealthFacilityDTO dto, 
        ValidationResult result, 
        int? excludeId = null)
    {
        // Validate string types and required fields
        if (string.IsNullOrWhiteSpace(dto.FacilityId))
        {
            result.AddError(nameof(dto.FacilityId), "Facility ID is required");
        }
        else
        {
            // Validate string type and length
            if (dto.FacilityId.Length > 50)
            {
                result.AddError(nameof(dto.FacilityId), "Facility ID cannot exceed 50 characters");
            }
            else
            {
                // Check for duplicate FacilityId (excluding current record for updates)
                var query = _context.HealthFacilities.Where(hf => hf.FacilityId == dto.FacilityId);
                if (excludeId.HasValue)
                {
                    query = query.Where(hf => hf.HealthFacilityId != excludeId.Value);
                }
                var exists = await query.AnyAsync();
                if (exists)
                {
                    result.AddError(nameof(dto.FacilityId), $"A health facility with Facility ID '{dto.FacilityId}' already exists.");
                }
            }
        }

        if (string.IsNullOrWhiteSpace(dto.HealthFacilityName))
        {
            result.AddError(nameof(dto.HealthFacilityName), "Health Facility Name is required");
        }
        else if (dto.HealthFacilityName.Length > 255)
        {
            result.AddError(nameof(dto.HealthFacilityName), "Health Facility Name cannot exceed 255 characters");
        }

        // Validate int types and foreign key existence
        if (dto.DistrictId <= 0)
        {
            result.AddError(nameof(dto.DistrictId), "District ID is required and must be greater than 0");
        }
        else
        {
            var districtExists = await _context.Districts.AnyAsync(d => d.DistrictId == dto.DistrictId);
            if (!districtExists)
            {
                result.AddError(nameof(dto.DistrictId), $"District with ID {dto.DistrictId} does not exist.");
            }
        }

        if (dto.FacilityTypeId <= 0)
        {
            result.AddError(nameof(dto.FacilityTypeId), "Facility Type ID is required and must be greater than 0");
        }
        else
        {
            var facilityTypeExists = await _context.FacilityTypes.AnyAsync(ft => ft.FacilityTypeId == dto.FacilityTypeId);
            if (!facilityTypeExists)
            {
                result.AddError(nameof(dto.FacilityTypeId), $"Facility Type with ID {dto.FacilityTypeId} does not exist.");
            }
        }

        // Validate OwnershipId
        if (dto.OwnershipId <= 0)
        {
            result.AddError(nameof(dto.OwnershipId), "Ownership ID is required and must be greater than 0");
        }
        else
        {
            var ownershipExists = await _context.Ownerships.AnyAsync(o => o.OwnershipId == dto.OwnershipId);
            if (!ownershipExists)
            {
                result.AddError(nameof(dto.OwnershipId), $"Ownership with ID {dto.OwnershipId} does not exist.");
            }
        }

        // Validate OperationalStatusId
        if (dto.OperationalStatusId <= 0)
        {
            result.AddError(nameof(dto.OperationalStatusId), "Operational Status ID is required and must be greater than 0");
        }
        else
        {
            var operationalStatusExists = await _context.OperationalStatuses.AnyAsync(os => os.OperationalStatusId == dto.OperationalStatusId);
            if (!operationalStatusExists)
            {
                result.AddError(nameof(dto.OperationalStatusId), $"Operational Status with ID {dto.OperationalStatusId} does not exist.");
            }
        }

        // Validate optional string fields (max length)
        if (!string.IsNullOrWhiteSpace(dto.NutritionClusterPartners) && dto.NutritionClusterPartners.Length > 255)
        {
            result.AddError(nameof(dto.NutritionClusterPartners), "Nutrition Cluster Partners cannot exceed 255 characters");
        }

        if (!string.IsNullOrWhiteSpace(dto.DamalCaafimaadPartner) && dto.DamalCaafimaadPartner.Length > 255)
        {
            result.AddError(nameof(dto.DamalCaafimaadPartner), "Damal Caafimaad Partner cannot exceed 255 characters");
        }

        if (!string.IsNullOrWhiteSpace(dto.BetterLifeProjectPartner) && dto.BetterLifeProjectPartner.Length > 255)
        {
            result.AddError(nameof(dto.BetterLifeProjectPartner), "Better Life Project Partner cannot exceed 255 characters");
        }

        if (!string.IsNullOrWhiteSpace(dto.CaafimaadPlusPartner) && dto.CaafimaadPlusPartner.Length > 255)
        {
            result.AddError(nameof(dto.CaafimaadPlusPartner), "Caafimaad Plus Partner cannot exceed 255 characters");
        }

        if (!string.IsNullOrWhiteSpace(dto.FacilityInChargeName) && dto.FacilityInChargeName.Length > 255)
        {
            result.AddError(nameof(dto.FacilityInChargeName), "Facility In-Charge Name cannot exceed 255 characters");
        }

        if (!string.IsNullOrWhiteSpace(dto.FacilityInChargeNumber) && dto.FacilityInChargeNumber.Length > 50)
        {
            result.AddError(nameof(dto.FacilityInChargeNumber), "Facility In-Charge Number cannot exceed 50 characters");
        }

        // Validate decimal types (latitude and longitude ranges)
        if (dto.Latitude.HasValue)
        {
            if (dto.Latitude < -90m || dto.Latitude > 90m)
            {
                result.AddError(nameof(dto.Latitude), "Latitude must be between -90 and 90");
            }
        }

        if (dto.Longitude.HasValue)
        {
            if (dto.Longitude < -180m || dto.Longitude > 180m)
            {
                result.AddError(nameof(dto.Longitude), "Longitude must be between -180 and 180");
            }
        }

        // DateTime types are nullable and don't need validation beyond type checking
        // (ASP.NET Core model binding handles DateTime parsing)

        return result;
    }

    /// <summary>
    /// Common validation logic for update operations
    /// </summary>
    private async Task<ValidationResult> ValidateHealthFacilityCommonFieldsAsync(
        UpdateHealthFacilityDTO dto, 
        ValidationResult result, 
        int? excludeId = null)
    {
        // Validate string types and required fields
        if (string.IsNullOrWhiteSpace(dto.FacilityId))
        {
            result.AddError(nameof(dto.FacilityId), "Facility ID is required");
        }
        else
        {
            // Validate string type and length
            if (dto.FacilityId.Length > 50)
            {
                result.AddError(nameof(dto.FacilityId), "Facility ID cannot exceed 50 characters");
            }
            else
            {
                // Check for duplicate FacilityId (excluding current record for updates)
                var query = _context.HealthFacilities.Where(hf => hf.FacilityId == dto.FacilityId);
                if (excludeId.HasValue)
                {
                    query = query.Where(hf => hf.HealthFacilityId != excludeId.Value);
                }
                var exists = await query.AnyAsync();
                if (exists)
                {
                    result.AddError(nameof(dto.FacilityId), $"A health facility with Facility ID '{dto.FacilityId}' already exists.");
                }
            }
        }

        if (string.IsNullOrWhiteSpace(dto.HealthFacilityName))
        {
            result.AddError(nameof(dto.HealthFacilityName), "Health Facility Name is required");
        }
        else if (dto.HealthFacilityName.Length > 255)
        {
            result.AddError(nameof(dto.HealthFacilityName), "Health Facility Name cannot exceed 255 characters");
        }

        // Validate int types and foreign key existence
        if (dto.DistrictId <= 0)
        {
            result.AddError(nameof(dto.DistrictId), "District ID is required and must be greater than 0");
        }
        else
        {
            var districtExists = await _context.Districts.AnyAsync(d => d.DistrictId == dto.DistrictId);
            if (!districtExists)
            {
                result.AddError(nameof(dto.DistrictId), $"District with ID {dto.DistrictId} does not exist.");
            }
        }

        if (dto.FacilityTypeId <= 0)
        {
            result.AddError(nameof(dto.FacilityTypeId), "Facility Type ID is required and must be greater than 0");
        }
        else
        {
            var facilityTypeExists = await _context.FacilityTypes.AnyAsync(ft => ft.FacilityTypeId == dto.FacilityTypeId);
            if (!facilityTypeExists)
            {
                result.AddError(nameof(dto.FacilityTypeId), $"Facility Type with ID {dto.FacilityTypeId} does not exist.");
            }
        }

        // Validate OwnershipId
        if (dto.OwnershipId <= 0)
        {
            result.AddError(nameof(dto.OwnershipId), "Ownership ID is required and must be greater than 0");
        }
        else
        {
            var ownershipExists = await _context.Ownerships.AnyAsync(o => o.OwnershipId == dto.OwnershipId);
            if (!ownershipExists)
            {
                result.AddError(nameof(dto.OwnershipId), $"Ownership with ID {dto.OwnershipId} does not exist.");
            }
        }

        // Validate OperationalStatusId
        if (dto.OperationalStatusId <= 0)
        {
            result.AddError(nameof(dto.OperationalStatusId), "Operational Status ID is required and must be greater than 0");
        }
        else
        {
            var operationalStatusExists = await _context.OperationalStatuses.AnyAsync(os => os.OperationalStatusId == dto.OperationalStatusId);
            if (!operationalStatusExists)
            {
                result.AddError(nameof(dto.OperationalStatusId), $"Operational Status with ID {dto.OperationalStatusId} does not exist.");
            }
        }

        // Validate optional string fields (max length)
        if (!string.IsNullOrWhiteSpace(dto.NutritionClusterPartners) && dto.NutritionClusterPartners.Length > 255)
        {
            result.AddError(nameof(dto.NutritionClusterPartners), "Nutrition Cluster Partners cannot exceed 255 characters");
        }

        if (!string.IsNullOrWhiteSpace(dto.DamalCaafimaadPartner) && dto.DamalCaafimaadPartner.Length > 255)
        {
            result.AddError(nameof(dto.DamalCaafimaadPartner), "Damal Caafimaad Partner cannot exceed 255 characters");
        }

        if (!string.IsNullOrWhiteSpace(dto.BetterLifeProjectPartner) && dto.BetterLifeProjectPartner.Length > 255)
        {
            result.AddError(nameof(dto.BetterLifeProjectPartner), "Better Life Project Partner cannot exceed 255 characters");
        }

        if (!string.IsNullOrWhiteSpace(dto.CaafimaadPlusPartner) && dto.CaafimaadPlusPartner.Length > 255)
        {
            result.AddError(nameof(dto.CaafimaadPlusPartner), "Caafimaad Plus Partner cannot exceed 255 characters");
        }

        if (!string.IsNullOrWhiteSpace(dto.FacilityInChargeName) && dto.FacilityInChargeName.Length > 255)
        {
            result.AddError(nameof(dto.FacilityInChargeName), "Facility In-Charge Name cannot exceed 255 characters");
        }

        if (!string.IsNullOrWhiteSpace(dto.FacilityInChargeNumber) && dto.FacilityInChargeNumber.Length > 50)
        {
            result.AddError(nameof(dto.FacilityInChargeNumber), "Facility In-Charge Number cannot exceed 50 characters");
        }

        // Validate decimal types (latitude and longitude ranges)
        if (dto.Latitude.HasValue)
        {
            if (dto.Latitude < -90m || dto.Latitude > 90m)
            {
                result.AddError(nameof(dto.Latitude), "Latitude must be between -90 and 90");
            }
        }

        if (dto.Longitude.HasValue)
        {
            if (dto.Longitude < -180m || dto.Longitude > 180m)
            {
                result.AddError(nameof(dto.Longitude), "Longitude must be between -180 and 180");
            }
        }

        // DateTime types are nullable and don't need validation beyond type checking
        // (ASP.NET Core model binding handles DateTime parsing)

        return result;
    }
}
