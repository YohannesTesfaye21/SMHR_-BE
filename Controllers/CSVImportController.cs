using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMHFR_BE.DTOs;
using SMHFR_BE.Services;
using System.IO;

namespace SMHFR_BE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class CSVImportController : ControllerBase
{
    private readonly ICSVImportService _csvImportService;
    private readonly ILogger<CSVImportController> _logger;

    public CSVImportController(ICSVImportService csvImportService, ILogger<CSVImportController> logger)
    {
        _csvImportService = csvImportService;
        _logger = logger;
    }

    /// <summary>
    /// Import health facilities data from CSV file
    /// </summary>
    /// <param name="csvFilePath">Path to the CSV file</param>
    [HttpPost("import")]
    public async Task<IActionResult> ImportCSV([FromBody] ImportCSVRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CsvFilePath) || !System.IO.File.Exists(request.CsvFilePath))
        {
            return BadRequest(ApiResponse.ErrorResult("CSV file path is invalid or file does not exist"));
        }

        try
        {
            var result = await _csvImportService.ImportFromCSVAsync(request.CsvFilePath, request.UpdateExisting);

            var importResult = new
            {
                states = result.states,
                regions = result.regions,
                districts = result.districts,
                facilityTypes = result.facilityTypes,
                healthFacilities = result.facilities,
                updatedFacilities = result.updated,
                skippedCount = result.skippedRecords.Count,
                skippedRecords = result.skippedRecords.Take(100).ToList() // Return first 100 skipped records
            };

            var message = result.skippedRecords.Count > 0 
                ? $"CSV import completed successfully. {result.facilities} new facilities imported, {result.updated} facilities updated, {result.skippedRecords.Count} records skipped."
                : $"CSV import completed successfully. {result.facilities} new facilities imported, {result.updated} facilities updated.";

            return Ok(ApiResponse<object>.SuccessResult(importResult, message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing CSV file");
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while importing the CSV file", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Import health facilities data from uploaded CSV file
    /// </summary>
    /// <param name="file">CSV file to import</param>
    /// <param name="updateExisting">If true, update existing facilities instead of skipping them. Default: false</param>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadCSV(IFormFile file, [FromQuery] bool updateExisting = false)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(ApiResponse.ErrorResult("No file uploaded"));
        }

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(ApiResponse.ErrorResult("File must be a CSV file"));
        }

        var tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".csv");

        try
        {
            using (var stream = new FileStream(tempFilePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var result = await _csvImportService.ImportFromCSVAsync(tempFilePath, updateExisting);

            // Clean up temp file
            if (System.IO.File.Exists(tempFilePath))
            {
                System.IO.File.Delete(tempFilePath);
            }

            var importResult = new
            {
                states = result.states,
                regions = result.regions,
                districts = result.districts,
                facilityTypes = result.facilityTypes,
                healthFacilities = result.facilities,
                updatedFacilities = result.updated,
                skippedCount = result.skippedRecords.Count,
                skippedRecords = result.skippedRecords.Take(100).ToList() // Return first 100 skipped records
            };

            var message = result.skippedRecords.Count > 0 
                ? $"CSV import completed successfully. {result.facilities} new facilities imported, {result.updated} facilities updated, {result.skippedRecords.Count} records skipped."
                : $"CSV import completed successfully. {result.facilities} new facilities imported, {result.updated} facilities updated.";

            return Ok(ApiResponse<object>.SuccessResult(importResult, message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing CSV file");

            // Clean up temp file on error
            if (System.IO.File.Exists(tempFilePath))
            {
                System.IO.File.Delete(tempFilePath);
            }

            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while importing the CSV file", new List<string> { ex.Message }));
        }
    }
}

public class ImportCSVRequest
{
    public string CsvFilePath { get; set; } = string.Empty;
    public bool UpdateExisting { get; set; } = false;
}
