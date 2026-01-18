# Password Authentication Error Analysis

## Error Locations

### Location 1: Health Facilities Endpoint (Most Common)
**File:** `Controllers/HealthFacilitiesController.cs`  
**Method:** `GetHealthFacilities()` (line 38-119)  
**Endpoint:** `GET /api/HealthFacilities` or `/facilities`

The error occurs when executing these database queries:
```csharp
var totalCount = await query.CountAsync();  // Line 95
var facilities = await query.ToListAsync();  // Line 98-102
```

### Location 2: Dashboard Cards Endpoint
**File:** `Controllers/DashboardController.cs`  
**Method:** `GetCardStatistics()` (line 27-50)  
**Endpoint:** `GET /api/Dashboard/cards`

The error occurs when executing these database queries:
```csharp
var totalStates = await _context.States.CountAsync();
var totalRegions = await _context.Regions.CountAsync();
var totalDistricts = await _context.Districts.CountAsync();
var totalFacilities = await _context.HealthFacilities.CountAsync();
```

## Root Causes

### 1. **PostgreSQL Volume Has Different Password** (Most Likely)
- The PostgreSQL volume (`postgres_data`) was created with a different password
- When the volume already exists, `POSTGRES_PASSWORD` environment variable is **IGNORED**
- PostgreSQL only uses `POSTGRES_PASSWORD` during **initial database creation**

### 2. **Connection Pool Has Stale Connections**
- Entity Framework/Npgsql maintains a connection pool
- If connections were established with wrong password, they stay in the pool
- Even after fixing the password, old connections may still be used

### 3. **Connection String Mismatch**
- The API container connection string doesn't match PostgreSQL's actual password
- Check: `docker compose exec api printenv | grep ConnectionStrings`
- Should be: `Password=postgres`

## How to Fix

### Option 1: Reset PostgreSQL Volume (Deletes All Data)
```bash
docker compose down -v
docker compose up -d
```

### Option 2: Reset Password Manually on Server
```bash
# SSH into server
ssh user@server

# Access PostgreSQL container
docker exec -it smhfr-postgres psql -U postgres

# Reset password
ALTER USER postgres WITH PASSWORD 'postgres';
\q
```

### Option 3: Check Current Connection String
```bash
# On server
docker compose exec api printenv | grep ConnectionStrings
```

Should show:
```
ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=smhfr_db;Username=postgres;Password=postgres;...
```

## Logs to Check

1. **API Container Logs:**
   ```bash
   docker compose logs api | grep -i "password\|28P01\|authentication"
   ```

2. **PostgreSQL Container Logs:**
   ```bash
   docker compose logs postgres | grep -i "password\|authentication"
   ```

3. **Application Startup Logs:**
   ```bash
   docker compose logs api | grep -i "connection\|database\|password"
   ```

## Why This Keeps Happening

1. **First Deployment:** Volume created with password X
2. **Code Updated:** docker-compose.yml now uses password Y
3. **Volume Persists:** Old password X remains in PostgreSQL data
4. **API Connects:** Tries password Y, but PostgreSQL expects X → **28P01 Error**

## Solution Applied

- Added better error logging in `DashboardController.cs` to show:
  - Connection string being used (password hidden)
  - Clear password authentication errors
  - Automatic connection pool clearing

- Deploy script now attempts to reset password automatically
