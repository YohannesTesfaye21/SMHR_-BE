# CSV Import Instructions

## How to Import CSV Data

### Option 1: Using API Endpoint (Upload File)

1. Start the application:
   ```bash
   docker-compose up
   ```

2. Access Swagger UI: http://localhost:8080

3. Use the `/api/CSVImport/upload` endpoint:
   - Click "Try it out"
   - Choose your CSV file
   - Click "Execute"

### Option 2: Using API Endpoint (File Path)

1. Copy your CSV file to a location accessible by the application

2. Make a POST request to `/api/CSVImport/import`:
   ```json
   {
     "csvFilePath": "/path/to/your/file.csv"
   }
   ```

### Option 3: Using curl

```bash
curl -X POST "http://localhost:8080/api/CSVImport/upload" \
  -H "Content-Type: multipart/form-data" \
  -F "file=@/path/to/Final Somalia Master Health Facility List(AutoRecovered) (version 1) - MHFLs.csv"
```

## What the Import Does

The import service will:

1. **Extract and create lookup tables:**
   - States (from State column)
   - Regions (from Region column, linked to States)
   - Districts (from District column, linked to Regions)
   - Facility Types (from Health Facility Type column)
   - Ownership types (from Ownership column)
   - Operational Statuses (from Operational Status column)

2. **Transform and import health facilities:**
   - Maps CSV columns to database schema
   - Handles "Missing", "No", "N/A" values (converts to NULL)
   - Parses dates from various formats
   - Converts latitude/longitude strings to decimals
   - Links facilities to lookup tables via foreign keys

3. **Skip duplicates:**
   - Existing records with the same FacilityId will be skipped
   - Lookup table values are deduplicated automatically

## Expected CSV Format

The CSV file should have these columns:
- New Facility ID
- Latitude
- Longitude
- State
- Region
- District
- Health Facility Name
- Health Facility Type
- Ownership
- HC partners
- HC Project End date
- Nutrition Cluster Partners
- Damal Caafimaad Partner
- Damal Caafimaad Project end date
- Better Life Project Partner
- Better Life Project End Date
- Caafimaad Plus Partner
- Caafimaad Plus Project end
- Facility In-charge Name
- Facility in-charge Number
- Operational Status

## Database Schema

The import creates a normalized database structure:

- **States** → **Regions** → **Districts** → **HealthFacilities**
- **FacilityTypes** → **HealthFacilities**
- **Ownerships** → **HealthFacilities**
- **OperationalStatuses** → **HealthFacilities**

This design allows for efficient querying and reporting by administrative level, facility type, ownership, and operational status.
