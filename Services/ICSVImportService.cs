namespace SMHFR_BE.Services;

public interface ICSVImportService
{
    Task<(int states, int regions, int districts, int facilityTypes, int facilities, int updated, List<string> skippedRecords)> ImportFromCSVAsync(string csvFilePath, bool updateExisting = false);
}
