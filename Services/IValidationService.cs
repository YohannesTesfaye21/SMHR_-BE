using SMHFR_BE.DTOs;
using SMHFR_BE.Models;
using ValidationResult = SMHFR_BE.DTOs.ValidationResult;

namespace SMHFR_BE.Services;

/// <summary>
/// Service for validating entities and DTOs before database operations
/// </summary>
public interface IValidationService
{
    /// <summary>
    /// Validates a CreateHealthFacilityDTO
    /// </summary>
    Task<ValidationResult> ValidateCreateHealthFacilityAsync(CreateHealthFacilityDTO dto);

    /// <summary>
    /// Validates an UpdateHealthFacilityDTO for update
    /// </summary>
    Task<ValidationResult> ValidateUpdateHealthFacilityAsync(int id, UpdateHealthFacilityDTO dto);

    /// <summary>
    /// Validates if a health facility exists for deletion
    /// </summary>
    Task<ValidationResult> ValidateDeleteHealthFacilityAsync(int id);
}
