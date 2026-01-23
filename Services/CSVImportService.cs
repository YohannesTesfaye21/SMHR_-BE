using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using SMHFR_BE.Data;
using SMHFR_BE.Models;
using System.Globalization;

namespace SMHFR_BE.Services;

public class CSVImportService : ICSVImportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CSVImportService> _logger;

    public CSVImportService(ApplicationDbContext context, ILogger<CSVImportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(int states, int regions, int districts, int facilityTypes, int facilities)> ImportFromCSVAsync(string csvFilePath)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            HeaderValidated = null,
            MissingFieldFound = null
        };

        var csvRecords = new List<CSVRecord>();
        
        using (var reader = new StreamReader(csvFilePath))
        using (var csv = new CsvReader(reader, config))
        {
            csv.Context.RegisterClassMap<CSVRecordMap>();
            csvRecords = csv.GetRecords<CSVRecord>().ToList();
        }

        // Step 1: Extract and create lookup tables
        var statesDict = new Dictionary<string, State>();
        var regionsDict = new Dictionary<string, Region>();
        var districtsDict = new Dictionary<string, District>();
        var facilityTypesDict = new Dictionary<string, FacilityType>();
        var operationalStatusesDict = new Dictionary<string, OperationalStatus>();
        var ownershipsDict = new Dictionary<string, Ownership>();

        // Extract unique values from CSV - FIRST PASS: Build lookup dictionaries
        foreach (var record in csvRecords)
        {
            // States - accept all values as they are in CSV (customer data)
            var stateValue = record.State?.Trim();
            if (!string.IsNullOrWhiteSpace(stateValue) && !statesDict.ContainsKey(stateValue))
            {
                statesDict[stateValue] = new State
                {
                    StateCode = stateValue,
                    StateName = stateValue,
                    CreatedAt = DateTime.UtcNow
                };
            }

            // Regions - accept all values as they are in CSV
            var stateValueForRegion = record.State?.Trim();
            if (!string.IsNullOrWhiteSpace(record.Region) && !string.IsNullOrWhiteSpace(stateValueForRegion))
            {
                var regionKey = $"{stateValueForRegion}|{record.Region.Trim()}";
                if (!regionsDict.ContainsKey(regionKey))
                {
                    regionsDict[regionKey] = new Region
                    {
                        RegionName = record.Region.Trim(),
                        CreatedAt = DateTime.UtcNow
                    };
                }
            }

            // Districts - accept all values as they are in CSV
            var stateValueForDistrict = record.State?.Trim();
            if (!string.IsNullOrWhiteSpace(record.District) && 
                !string.IsNullOrWhiteSpace(record.Region) && 
                !string.IsNullOrWhiteSpace(stateValueForDistrict))
            {
                var districtKey = $"{stateValueForDistrict}|{record.Region.Trim()}|{record.District.Trim()}";
                if (!districtsDict.ContainsKey(districtKey))
                {
                    districtsDict[districtKey] = new District
                    {
                        DistrictName = record.District.Trim(),
                        CreatedAt = DateTime.UtcNow
                    };
                }
            }

            // Facility Types
            if (!string.IsNullOrWhiteSpace(record.HealthFacilityType) && !facilityTypesDict.ContainsKey(record.HealthFacilityType.Trim()))
            {
                facilityTypesDict[record.HealthFacilityType.Trim()] = new FacilityType
                {
                    TypeName = record.HealthFacilityType.Trim(),
                    CreatedAt = DateTime.UtcNow
                };
            }

            // Operational Statuses
            if (!string.IsNullOrWhiteSpace(record.OperationalStatus) && !operationalStatusesDict.ContainsKey(record.OperationalStatus.Trim()))
            {
                operationalStatusesDict[record.OperationalStatus.Trim()] = new OperationalStatus
                {
                    StatusName = record.OperationalStatus.Trim(),
                    CreatedAt = DateTime.UtcNow
                };
            }

            // Ownerships
            if (!string.IsNullOrWhiteSpace(record.Ownership) && !ownershipsDict.ContainsKey(record.Ownership.Trim()))
            {
                ownershipsDict[record.Ownership.Trim()] = new Ownership
                {
                    OwnershipType = record.Ownership.Trim(),
                    CreatedAt = DateTime.UtcNow
                };
            }
        }

        // Step 2: Save States first - Register all states from CSV
        try
        {
            foreach (var state in statesDict.Values)
            {
                var existing = await _context.States.FirstOrDefaultAsync(s => s.StateCode == state.StateCode);
                if (existing == null)
                {
                    _context.States.Add(state);
                }
                else
                {
                    statesDict[state.StateCode] = existing;
                }
            }
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            var longStateCode = statesDict.Values.FirstOrDefault(s => s.StateCode.Length > 255);
            if (longStateCode != null)
            {
                throw new InvalidOperationException($"StateCode '{longStateCode.StateCode}' is {longStateCode.StateCode.Length} characters long, which exceeds the maximum allowed length of 255 characters. Please check the CSV data.", ex);
            }
            throw new InvalidOperationException($"Error saving States to database: {ex.Message}", ex);
        }

        // Step 3: Save Regions (with State references) - Register all regions from CSV
        try
        {
            foreach (var kvp in regionsDict)
            {
                var parts = kvp.Key.Split('|');
                var stateCode = parts[0];
                
                if (!statesDict.TryGetValue(stateCode, out var state))
                {
                    _logger.LogWarning("Skipping region '{RegionName}' because parent state '{StateCode}' was not found in States lookup table.", kvp.Value.RegionName, stateCode);
                    continue;
                }
                
                var region = kvp.Value;
                region.StateId = state.StateId;

                var existing = await _context.Regions.FirstOrDefaultAsync(r => 
                    r.StateId == region.StateId && r.RegionName == region.RegionName);
                if (existing == null)
                {
                    _context.Regions.Add(region);
                }
                else
                {
                    regionsDict[kvp.Key] = existing;
                }
            }
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error saving Regions to database: {ex.Message}", ex);
        }

        // Step 4: Save Districts (with Region references) - Register all districts from CSV
        try
        {
            foreach (var kvp in districtsDict)
            {
                var parts = kvp.Key.Split('|');
                var stateCode = parts[0];
                var regionName = parts[1];
                
                var regionKey = $"{stateCode}|{regionName}";
                if (!regionsDict.TryGetValue(regionKey, out var region))
                {
                    _logger.LogWarning("Skipping district '{DistrictName}' because parent region '{RegionName}' in state '{StateCode}' was not found in Regions lookup table.", kvp.Value.DistrictName, regionName, stateCode);
                    continue;
                }
                
                var district = kvp.Value;
                district.RegionId = region.RegionId;

                var existing = await _context.Districts.FirstOrDefaultAsync(d => 
                    d.RegionId == district.RegionId && d.DistrictName == district.DistrictName);
                if (existing == null)
                {
                    _context.Districts.Add(district);
                }
                else
                {
                    districtsDict[kvp.Key] = existing;
                }
            }
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error saving Districts to database: {ex.Message}", ex);
        }

        // Step 5: Save FacilityTypes
        foreach (var facilityType in facilityTypesDict.Values)
        {
            var existing = await _context.FacilityTypes.FirstOrDefaultAsync(ft => ft.TypeName == facilityType.TypeName);
            if (existing == null)
            {
                _context.FacilityTypes.Add(facilityType);
            }
            else
            {
                facilityTypesDict[facilityType.TypeName] = existing;
            }
        }
        await _context.SaveChangesAsync();

        // Step 6: Save OperationalStatuses
        foreach (var operationalStatus in operationalStatusesDict.Values)
        {
            var existing = await _context.OperationalStatuses.FirstOrDefaultAsync(os => os.StatusName == operationalStatus.StatusName);
            if (existing == null)
            {
                _context.OperationalStatuses.Add(operationalStatus);
            }
            else
            {
                operationalStatusesDict[operationalStatus.StatusName] = existing;
            }
        }
        await _context.SaveChangesAsync();

        // Step 7: Save Ownerships
        foreach (var ownership in ownershipsDict.Values)
        {
            var existing = await _context.Ownerships.FirstOrDefaultAsync(o => o.OwnershipType == ownership.OwnershipType);
            if (existing == null)
            {
                _context.Ownerships.Add(ownership);
            }
            else
            {
                ownershipsDict[ownership.OwnershipType] = existing;
            }
        }
        await _context.SaveChangesAsync();

        // Step 8: Create Health Facilities - Insert facilities using registered lookup tables
        int facilitiesCount = 0;
        var skippedRecords = new List<string>();
        var processedFacilityIds = new HashSet<string>(); // Track FacilityIds processed in this import session
        int totalCSVRecords = csvRecords.Count; // Track total records in CSV
        
        try
        {
            int recordNumber = 0;
            foreach (var record in csvRecords)
            {
                recordNumber++;
                
                // Skip if FacilityId is empty
                var facilityId = record.NewFacilityId?.Trim();
                if (string.IsNullOrWhiteSpace(facilityId))
                {
                    var skipReason = "FacilityId is empty";
                    _logger.LogWarning("Skipping record {RecordNumber}: {Reason}", recordNumber, skipReason);
                    skippedRecords.Add($"Row {recordNumber}: {skipReason}");
                    continue;
                }

                // Check if we've already processed this FacilityId in this import session
                if (processedFacilityIds.Contains(facilityId))
                {
                    var skipReason = $"Duplicate FacilityId '{facilityId}' found in CSV (already processed in this import session)";
                    _logger.LogWarning("Skipping: {Reason}", skipReason);
                    skippedRecords.Add($"FacilityId '{facilityId}': {skipReason}");
                    continue;
                }

                // Check if facility already exists in database
                var existingFacility = await _context.HealthFacilities
                    .FirstOrDefaultAsync(hf => hf.FacilityId == facilityId);
                
                if (existingFacility != null)
                {
                    var skipReason = $"FacilityId '{facilityId}' already exists in database";
                    _logger.LogInformation("Skipping: {Reason}", skipReason);
                    skippedRecords.Add($"FacilityId '{facilityId}': {skipReason}");
                    continue;
                }
                
                // Mark this FacilityId as processed
                processedFacilityIds.Add(facilityId);

                // Get lookup IDs from registered lookup tables - accept all values as they are in CSV
                var stateCode = record.State?.Trim();
            if (string.IsNullOrWhiteSpace(stateCode))
            {
                var skipReason = $"State is empty for FacilityId '{facilityId}'";
                _logger.LogWarning("Skipping: {Reason}", skipReason);
                skippedRecords.Add($"FacilityId '{facilityId}': {skipReason}");
                continue;
            }

                if (!statesDict.TryGetValue(stateCode, out var state))
                {
                    var skipReason = $"State '{stateCode}' was not found in registered States lookup table for FacilityId '{facilityId}'";
                    _logger.LogWarning("Skipping: {Reason}", skipReason);
                    skippedRecords.Add($"FacilityId '{facilityId}': {skipReason}");
                    continue;
                }

                var regionName = record.Region?.Trim();
                if (string.IsNullOrWhiteSpace(regionName))
                {
                    var skipReason = $"Region is empty for FacilityId '{facilityId}'";
                    _logger.LogWarning("Skipping: {Reason}", skipReason);
                    skippedRecords.Add($"FacilityId '{facilityId}': {skipReason}");
                    continue;
                }

                var districtName = record.District?.Trim();
                if (string.IsNullOrWhiteSpace(districtName))
                {
                    var skipReason = $"District is empty for FacilityId '{facilityId}'";
                    _logger.LogWarning("Skipping: {Reason}", skipReason);
                    skippedRecords.Add($"FacilityId '{facilityId}': {skipReason}");
                    continue;
                }

                var regionKey = $"{stateCode}|{regionName}";
                if (!regionsDict.TryGetValue(regionKey, out var regionObj))
                {
                    var skipReason = $"Region '{regionName}' in State '{stateCode}' was not found in registered Regions lookup table";
                    _logger.LogWarning("Skipping health facility '{FacilityId}': {Reason}", facilityId, skipReason);
                    skippedRecords.Add($"FacilityId '{facilityId}': {skipReason}");
                    continue;
                }

                var districtKey = $"{stateCode}|{regionName}|{districtName}";
                if (!districtsDict.TryGetValue(districtKey, out var district))
                {
                    var skipReason = $"District '{districtName}' in Region '{regionName}' was not found in registered Districts lookup table";
                    _logger.LogWarning("Skipping health facility '{FacilityId}': {Reason}", facilityId, skipReason);
                    skippedRecords.Add($"FacilityId '{facilityId}': {skipReason}");
                    continue;
                }

                var facilityTypeName = record.HealthFacilityType?.Trim();
                if (string.IsNullOrWhiteSpace(facilityTypeName))
                {
                    var skipReason = $"HealthFacilityType is empty for FacilityId '{facilityId}'";
                    _logger.LogWarning("Skipping: {Reason}", skipReason);
                    skippedRecords.Add($"FacilityId '{facilityId}': {skipReason}");
                    continue;
                }
                
                if (!facilityTypesDict.TryGetValue(facilityTypeName, out var facilityType))
                {
                    var skipReason = $"FacilityType '{facilityTypeName}' was not found in registered FacilityTypes lookup table for FacilityId '{facilityId}'";
                    _logger.LogWarning("Skipping: {Reason}", skipReason);
                    skippedRecords.Add($"FacilityId '{facilityId}': {skipReason}");
                    continue;
                }

                // Get OperationalStatus
                var operationalStatusName = record.OperationalStatus?.Trim();
                if (string.IsNullOrWhiteSpace(operationalStatusName))
                {
                    var skipReason = $"OperationalStatus is empty for FacilityId '{facilityId}'";
                    _logger.LogWarning("Skipping: {Reason}", skipReason);
                    skippedRecords.Add($"FacilityId '{facilityId}': {skipReason}");
                    continue;
                }

                if (!operationalStatusesDict.TryGetValue(operationalStatusName, out var operationalStatus))
                {
                    var skipReason = $"OperationalStatus '{operationalStatusName}' was not found in registered OperationalStatuses lookup table for FacilityId '{facilityId}'";
                    _logger.LogWarning("Skipping: {Reason}", skipReason);
                    skippedRecords.Add($"FacilityId '{facilityId}': {skipReason}");
                    continue;
                }

                // Get Ownership
                var ownershipType = record.Ownership?.Trim();
                if (string.IsNullOrWhiteSpace(ownershipType))
                {
                    var skipReason = $"Ownership is empty for FacilityId '{facilityId}'";
                    _logger.LogWarning("Skipping: {Reason}", skipReason);
                    skippedRecords.Add($"FacilityId '{facilityId}': {skipReason}");
                    continue;
                }

                if (!ownershipsDict.TryGetValue(ownershipType, out var ownership))
                {
                    var skipReason = $"Ownership '{ownershipType}' was not found in registered Ownerships lookup table for FacilityId '{facilityId}'";
                    _logger.LogWarning("Skipping: {Reason}", skipReason);
                    skippedRecords.Add($"FacilityId '{facilityId}': {skipReason}");
                    continue;
                }

                    // Parse coordinates with validation for decimal(10,7) constraint
                // decimal(10,7) means: 3 digits before decimal, 7 after = max 999.9999999
                // But valid ranges are: Latitude -90 to 90, Longitude -180 to 180
                // So we need to validate and truncate/round to fit the constraint
                decimal? latitude = null;
                decimal? longitude = null;
                
                if (!string.IsNullOrWhiteSpace(record.Latitude) && 
                record.Latitude.Trim().ToLower() != "missing")
            {
                var latStr = record.Latitude.Trim();
                if (decimal.TryParse(latStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var lat))
                {
                    // Validate latitude range: -90 to 90
                    if (lat >= -90m && lat <= 90m)
                    {
                        // Truncate to 7 decimal places to fit decimal(10,7)
                        // Round to nearest value with max 7 decimal places
                        latitude = Math.Round(lat, 7, MidpointRounding.AwayFromZero);
                    }
                    else
                    {
                        _logger.LogWarning("Invalid latitude value '{Latitude}' for FacilityId '{FacilityId}'. Latitude must be between -90 and 90. Setting to null.", lat, facilityId);
                    }
                }
                else
                {
                    _logger.LogWarning("Could not parse latitude value '{Latitude}' for FacilityId '{FacilityId}'. Setting to null.", latStr, facilityId);
                }
            }

            if (!string.IsNullOrWhiteSpace(record.Longitude) && 
                record.Longitude.Trim().ToLower() != "missing")
            {
                var lngStr = record.Longitude.Trim();
                if (decimal.TryParse(lngStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var lng))
                {
                    // Validate longitude range: -180 to 180
                    if (lng >= -180m && lng <= 180m)
                    {
                        // Truncate to 7 decimal places to fit decimal(10,7)
                        // Round to nearest value with max 7 decimal places
                        longitude = Math.Round(lng, 7, MidpointRounding.AwayFromZero);
                    }
                    else
                    {
                        _logger.LogWarning("Invalid longitude value '{Longitude}' for FacilityId '{FacilityId}'. Longitude must be between -180 and 180. Setting to null.", lng, facilityId);
                    }
                }
                else
                {
                    _logger.LogWarning("Could not parse longitude value '{Longitude}' for FacilityId '{FacilityId}'. Setting to null.", lngStr, facilityId);
                }
            }

                // Parse dates - ensure UTC for PostgreSQL compatibility
                DateTime? ParseDate(string? dateStr)
            {
                if (string.IsNullOrWhiteSpace(dateStr) || 
                    dateStr.Trim().ToLower() == "no" || 
                    dateStr.Trim().ToLower() == "missing" ||
                    dateStr.Trim().ToLower() == "n/a")
                    return null;

                // Try parsing with different formats
                string[] formats = { "M/d/yyyy", "MM/dd/yyyy", "yyyy-MM-dd", "d-MMM-yy", "dd-MM-yyyy", "MMM d, yyyy", "M/d/yyyy H:mm:ss", "MM/dd/yyyy H:mm:ss" };
                
                if (DateTime.TryParseExact(dateStr.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    // Ensure the date is in UTC - if it's Unspecified, treat it as UTC
                    if (date.Kind == DateTimeKind.Unspecified)
                    {
                        return DateTime.SpecifyKind(date, DateTimeKind.Utc);
                    }
                    // If it's already Local or UTC, convert to UTC
                    return date.ToUniversalTime();
                }

                // Fallback to regular parsing
                if (DateTime.TryParse(dateStr.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                {
                    if (parsedDate.Kind == DateTimeKind.Unspecified)
                    {
                        return DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
                    }
                    return parsedDate.ToUniversalTime();
                }

                return null;
            }

                // Handle "No" values for partners
                string? HandleNoValue(string? value)
            {
                if (string.IsNullOrWhiteSpace(value) || 
                    value.Trim().ToLower() == "no" ||
                    value.Trim().ToLower() == "n/a")
                    return null;
                return value.Trim();
            }

                // Handle Facility In-Charge Number - accept all values as-is from CSV
                // Accepts: phone numbers (with/without spaces, with leading zeros), text values like "Closed"
                string? HandleFacilityInChargeNumber(string? value)
            {
                if (string.IsNullOrWhiteSpace(value))
                    return null;
                
                var trimmed = value.Trim();
                
                // Handle common "no value" indicators (case-insensitive)
                if (trimmed.Equals("no", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.Equals("n/a", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.Equals("missing", StringComparison.OrdinalIgnoreCase))
                    return null;
                
                // Return as-is (preserves leading zeros, spaces, and text values like "Closed")
                // No length restriction - accepts customer data as-is
                return trimmed;
            }

                var healthFacility = new HealthFacility
            {
                FacilityId = facilityId,
                HealthFacilityName = record.HealthFacilityName?.Trim() ?? string.Empty,
                Latitude = latitude,
                Longitude = longitude,
                DistrictId = district.DistrictId,
                FacilityTypeId = facilityType.FacilityTypeId,
                OwnershipId = ownership.OwnershipId,
                OperationalStatusId = operationalStatus.OperationalStatusId,
                HCPartners = HandleNoValue(record.HCPartners),
                HCProjectEndDate = ParseDate(record.HCProjectEndDate),
                NutritionClusterPartners = HandleNoValue(record.NutritionClusterPartners),
                DamalCaafimaadPartner = HandleNoValue(record.DamalCaafimaadPartner),
                DamalCaafimaadProjectEndDate = ParseDate(record.DamalCaafimaadProjectEndDate),
                BetterLifeProjectPartner = HandleNoValue(record.BetterLifeProjectPartner),
                BetterLifeProjectEndDate = ParseDate(record.BetterLifeProjectEndDate),
                CaafimaadPlusPartner = HandleNoValue(record.CaafimaadPlusPartner),
                CaafimaadPlusProjectEndDate = ParseDate(record.CaafimaadPlusProjectEndDate),
                FacilityInChargeName = HandleNoValue(record.FacilityInChargeName),
                FacilityInChargeNumber = HandleFacilityInChargeNumber(record.FacilityInChargeNumber),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.HealthFacilities.Add(healthFacility);
            facilitiesCount++;

                // Batch save every 100 records
                if (facilitiesCount % 100 == 0)
                {
                    await _context.SaveChangesAsync();
                }
            }
            
            await _context.SaveChangesAsync();
            
            // Log summary of import
            int skippedCount = skippedRecords.Count;
            int processedCount = totalCSVRecords - skippedCount;
            _logger.LogInformation("CSV import completed: {TotalRecords} total CSV records, {ProcessedCount} processed, {SkippedCount} skipped, {InsertedCount} new facilities inserted", 
                totalCSVRecords, processedCount, skippedCount, facilitiesCount);
            
            if (skippedRecords.Count > 0)
            {
                _logger.LogWarning("Skipped records ({SkippedCount}): {SkippedReasons}", skippedCount, string.Join("; ", skippedRecords.Take(10)));
                if (skippedRecords.Count > 10)
                {
                    _logger.LogWarning("... and {MoreCount} more skipped records. Check full logs for details.", skippedRecords.Count - 10);
                }
            }
        }
        catch (DbUpdateException dbEx)
        {
            var innerException = dbEx.InnerException;
            if (innerException?.Message?.Contains("value too long") == true)
            {
                // Extract the specific field and length from the error
                var match = System.Text.RegularExpressions.Regex.Match(innerException.Message, @"value too long for type (character varying|varchar)\((\d+)\)");
                if (match.Success)
                {
                    var maxLength = match.Groups[2].Value;
                    throw new InvalidOperationException($"A field value exceeds the maximum allowed length of {maxLength} characters. Database error: {innerException.Message}. Please check the CSV data for fields that may be too long.", dbEx);
                }
                throw new InvalidOperationException($"A field value exceeds the maximum allowed length. Database error: {innerException.Message}. Please check the CSV data.", dbEx);
            }
            if (innerException?.Message?.Contains("numeric field overflow") == true || 
                innerException?.Message?.Contains("numeric overflow") == true ||
                (innerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "22003"))
            {
                throw new InvalidOperationException($"Numeric field overflow error: A numeric value exceeds the allowed range. This usually occurs with Latitude/Longitude coordinates that exceed decimal(10,7) format (max 999.9999999). Check CSV coordinates for FacilityIds near the error location. Database error: {innerException.Message}", dbEx);
            }
            throw new InvalidOperationException($"Error saving Health Facilities to database: {dbEx.Message}. Inner exception: {innerException?.Message}", dbEx);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error creating Health Facilities: {ex.Message}", ex);
        }

        return (
            statesDict.Count,
            regionsDict.Count,
            districtsDict.Count,
            facilityTypesDict.Count,
            facilitiesCount
        );
    }
}

// CSV Record mapping class
public class CSVRecord
{
    public string NewFacilityId { get; set; } = string.Empty;
    public string? Latitude { get; set; }
    public string? Longitude { get; set; }
    public string State { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string? HealthFacilityName { get; set; }
    public string HealthFacilityType { get; set; } = string.Empty;
    public string Ownership { get; set; } = string.Empty;
    public string? HCPartners { get; set; }
    public string? HCProjectEndDate { get; set; }
    public string? NutritionClusterPartners { get; set; }
    public string? DamalCaafimaadPartner { get; set; }
    public string? DamalCaafimaadProjectEndDate { get; set; }
    public string? BetterLifeProjectPartner { get; set; }
    public string? BetterLifeProjectEndDate { get; set; }
    public string? CaafimaadPlusPartner { get; set; }
    public string? CaafimaadPlusProjectEndDate { get; set; }
    public string? FacilityInChargeName { get; set; }
    public string? FacilityInChargeNumber { get; set; }
    public string OperationalStatus { get; set; } = string.Empty;
}

// CSV Mapping class for CsvHelper
public sealed class CSVRecordMap : ClassMap<CSVRecord>
{
    public CSVRecordMap()
    {
        Map(m => m.NewFacilityId).Name("New Facility ID");
        Map(m => m.Latitude).Name("Latitude");
        Map(m => m.Longitude).Name("Longitude");
        Map(m => m.State).Name("State");
        Map(m => m.Region).Name("Region");
        Map(m => m.District).Name("District");
        Map(m => m.HealthFacilityName).Name("Health Facility Name");
        Map(m => m.HealthFacilityType).Name("Health Facility Type");
        Map(m => m.Ownership).Name("Ownership");
        Map(m => m.HCPartners).Name("HC partners");
        Map(m => m.HCProjectEndDate).Name("HC Project End date");
        Map(m => m.NutritionClusterPartners).Name("Nutrition Cluster Partners");
        Map(m => m.DamalCaafimaadPartner).Name("Damal Caafimaad Partner");
        Map(m => m.DamalCaafimaadProjectEndDate).Name("Damal Caafimaad Project end date");
        Map(m => m.BetterLifeProjectPartner).Name("Better Life Project Partner");
        Map(m => m.BetterLifeProjectEndDate).Name("Better Life Project End Date");
        Map(m => m.CaafimaadPlusPartner).Name("Caafimaad Plus Partner");
        Map(m => m.CaafimaadPlusProjectEndDate).Name("Caafimaad Plus Project end");
        Map(m => m.FacilityInChargeName).Name("Facility In-charge Name");
        Map(m => m.FacilityInChargeNumber).Name("Facility in-charge Number");
        Map(m => m.OperationalStatus).Name("Operational Status");
    }
}
