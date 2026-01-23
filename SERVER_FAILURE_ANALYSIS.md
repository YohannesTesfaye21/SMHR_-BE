# Server Failure Analysis: Why the Server Goes Down Multiple Times

## 🎯 TL;DR - The Problem

**Your fix script works once but fails again because:**
1. **PostgreSQL entrypoint script is NOT configured** - A script exists (`docker-entrypoint-postgres.sh`) that would sync passwords on every start, but it's not being used
2. **PostgreSQL volumes persist passwords** - Once created, volumes ignore `POSTGRES_PASSWORD` changes
3. **API persists connection strings** - Old passwords are cached in `/app/data/.connectionstring`
4. **Connection pools cache old passwords** - Stale connections remain even after password fix

**The fix:** Enable the entrypoint script in `docker-compose.yml` so password syncs automatically on every container start.

---

## Executive Summary

The server fails repeatedly due to a **cascading password synchronization problem** between PostgreSQL, the API container's connection string, and persisted connection string files. Even after fixing the password once, the system fails again because of multiple persistence mechanisms that cache the old password.

---

## 🔴 CRITICAL FINDING

**The `docker-entrypoint-postgres.sh` script exists but is NOT configured in `docker-compose.yml`!**

This script would sync the PostgreSQL password on every container start, but it's not being used. This is the **root cause** of why the password fix works once but fails again.

---

## Root Causes

### 1. **PostgreSQL Entrypoint Script Not Used** (CRITICAL - Primary Issue)

**Problem:**
- A custom entrypoint script `docker-entrypoint-postgres.sh` exists that syncs the password on every container start
- **BUT:** It's not configured in `docker-compose.yml`
- The script would ensure password always matches `POSTGRES_PASSWORD` environment variable

**Evidence:**
```bash
# File exists: docker-entrypoint-postgres.sh
# But docker-compose.yml has NO entrypoint configuration for postgres service
```

**Why it fails again:**
- Without the entrypoint script, PostgreSQL uses the default entrypoint
- Default entrypoint only uses `POSTGRES_PASSWORD` on first volume creation
- Password changes in `.env` are ignored after volume exists

**Fix:**
Add to `docker-compose.yml`:
```yaml
postgres:
  entrypoint: ["/docker-entrypoint-postgres.sh"]
  # ... rest of config
```

### 2. **PostgreSQL Volume Password Persistence** (Secondary Issue)

**Problem:**
- PostgreSQL only uses `POSTGRES_PASSWORD` environment variable during **initial database creation**
- Once the `postgres_data` volume exists, changing `POSTGRES_PASSWORD` in `.env` or `docker-compose.yml` has **NO EFFECT**
- The password stored in the PostgreSQL data directory persists across container restarts

**Evidence:**
```yaml
# docker-compose.yml line 11
POSTGRES_PASSWORD: ${DB_PASSWORD}  # Only used on first volume creation
```

**Why it fails again:**
- Your fix script sets the password correctly: `ALTER USER postgres WITH PASSWORD '$DB_PASS';`
- But if PostgreSQL restarts or the volume is reattached, the password might revert
- Or if the password in `.env` changes between deployments, the volume still has the old password

---

### 2. **API Connection String Persistence** (Secondary Issue)

**Problem:**
The `ConnectionStringService` persists connection strings to `/app/data/.connectionstring` (stored in `api_data` volume):

```csharp
// ConnectionStringService.cs lines 54-70
// Priority 2: Persisted file (for persistence across restarts)
if (File.Exists(_persistenceFilePath))
{
    var persisted = File.ReadAllText(_persistenceFilePath, Encoding.UTF8).Trim();
    if (!string.IsNullOrWhiteSpace(persisted))
    {
        _cachedConnectionString = persisted;  // Uses OLD password!
        return persisted;
    }
}
```

**Why it fails again:**
- Even after you remove `api_data` volume and restart, if the persisted file exists with an old password, it takes priority over environment variables
- The service caches the connection string in memory (`_cachedConnectionString`), so even after fixing the password, the cached value persists until container restart

**Priority Order (from code):**
1. Environment variable `ConnectionStrings__DefaultConnection` ✅ (correct)
2. **Persisted file** `/app/data/.connectionstring` ❌ (may have old password)
3. `appsettings.json` (fallback)

---

### 3. **Connection Pool Stale Connections** (Tertiary Issue)

**Problem:**
Npgsql maintains a connection pool. Even after fixing the password:
- Existing connections in the pool still have the old password
- New connections use the new password
- This causes intermittent failures (some requests work, others fail)

**Evidence:**
```csharp
// Program.cs line 392
Npgsql.NpgsqlConnection.ClearAllPools();  // Only cleared on startup
```

**Why it fails again:**
- Pool is only cleared on application startup
- If password changes while the app is running, old connections remain in the pool
- The middleware clears pools on 28P01 errors, but by then requests have already failed

---

### 4. **Docker Restart Policy Loop** (Amplification Issue)

**Problem:**
```yaml
# docker-compose.yml line 33
restart: always
```

**Why it fails again:**
- API crashes due to password mismatch → Docker restarts it
- API tries to connect with wrong password → Crashes again
- This creates a **restart loop** until the password is fixed
- Even after fixing, if the persisted connection string file has the old password, it will crash on next restart

---

### 5. **Deployment Script Timing Issues** (CI/CD Issue)

**Problem in `deploy.yml`:**
```yaml
# Line 76: Sets password
docker compose exec -T postgres psql -U postgres -d postgres -c "ALTER USER postgres WITH PASSWORD '${{ secrets.DB_PASSWORD }}';"

# Line 92: Removes volume
docker volume rm smhfr-be_api_data 2>/dev/null || true

# Line 96: Starts API
docker compose --env-file .env up -d --build --no-deps api
```

**Why it fails again:**
1. Password is set correctly
2. Volume is removed (good)
3. But if the API container was already running, it might have cached the old connection string
4. The `--no-deps` flag means it doesn't wait for postgres to be fully ready
5. If postgres restarts between password setting and API start, password might not be set

---

### 6. **Environment Variable Not Refreshed** (Configuration Issue)

**Problem:**
The connection string is built in `docker-compose.yml`:
```yaml
# docker-compose.yml line 42
ConnectionStrings__DefaultConnection: "Host=${DB_HOST};Port=${DB_PORT};Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD};..."
```

**Why it fails again:**
- If `.env` file changes but containers aren't recreated, environment variables don't update
- `docker compose restart` doesn't reload environment variables
- Need `docker compose up -d` to recreate containers with new env vars

---

## Failure Scenarios

### Scenario 1: First Deployment
1. Volume created with password X
2. Code deployed with password Y in `.env`
3. API tries password Y → PostgreSQL expects X → **28P01 Error**
4. Fix script sets password to Y
5. API works temporarily
6. **Next deployment:** Password changes to Z, but volume still has Y → Fails again

### Scenario 2: Volume Persistence
1. Password fixed correctly
2. `api_data` volume has persisted connection string with old password
3. API restarts → Reads persisted file → Uses old password → **28P01 Error**
4. Fix script removes volume and restarts
5. **But:** If password in `.env` doesn't match PostgreSQL volume → Fails again

### Scenario 3: Connection Pool
1. Password fixed while API is running
2. Old connections in pool still have wrong password
3. Some requests use old connections → **28P01 Error**
4. Pool eventually clears, but intermittent failures continue

### Scenario 4: PostgreSQL Restart
1. Password fixed correctly
2. PostgreSQL container restarts (Docker restart, server reboot, etc.)
3. PostgreSQL volume persists, but if password was changed via `ALTER USER`, it might not persist correctly
4. API tries to connect → **28P01 Error**

---

## Why Your Fix Script Works Once But Fails Again

Your script:
```bash
# 1. Gets password from .env ✅
DB_PASS=$(grep DB_PASSWORD .env | cut -d '=' -f2)

# 2. Sets password in PostgreSQL ✅
docker compose exec postgres psql -U postgres -d postgres -c "ALTER USER postgres WITH PASSWORD '$DB_PASS';"

# 3. Verifies password ✅
PGPASSWORD="$DB_PASS" docker compose exec -T postgres psql -h localhost -U postgres -d postgres -c "SELECT 1;"

# 4. Removes persisted connection string volume ✅
docker volume rm smhfr-be_api_data 2>/dev/null || true

# 5. Restarts API ✅
docker compose restart api
```

**Why it works once:**
- Sets password correctly
- Removes old connection string
- API restarts with fresh connection string from environment

**Why it fails again:**
1. **If `.env` password changes:** PostgreSQL volume still has old password, but your script sets it to new one. However, if PostgreSQL restarts or volume is reattached, it might revert.
2. **If API container caches connection string:** Even after removing volume, if the container was already running, it might have cached the connection string in memory.
3. **If deployment happens while API is running:** The persisted file might be recreated with the wrong password before the volume is removed.
4. **If PostgreSQL restarts after your script:** The password change via `ALTER USER` might not persist if the volume has a different password stored.

---

## 🔧 IMMEDIATE FIX REQUIRED

### Fix 1: Enable PostgreSQL Entrypoint Script (CRITICAL)

The `docker-entrypoint-postgres.sh` script exists but is not being used. Add it to `docker-compose.yml`:

```yaml
postgres:
  image: postgres:16-alpine
  container_name: smhfr-postgres
  restart: always
  entrypoint: ["/docker-entrypoint-postgres.sh"]  # ADD THIS LINE
  volumes:
    - postgres_data:/var/lib/postgresql/data
    - ./docker-entrypoint-postgres.sh:/docker-entrypoint-postgres.sh:ro  # ADD THIS LINE
  # ... rest of config
```

**Why this fixes it:**
- Script syncs password on every container start
- Ensures password always matches `POSTGRES_PASSWORD` from `.env`
- Prevents password drift between deployments

### Fix 2: Remove Connection String Persistence (Recommended)

Modify `ConnectionStringService.cs` to not persist passwords:

```csharp
// In GetConnectionString(), remove persistence for production:
if (!string.IsNullOrWhiteSpace(envConnectionString))
{
    _cachedConnectionString = envConnectionString;
    // DON'T persist passwords in production
    if (!_environment.IsProduction())
    {
        PersistConnectionString(envConnectionString);
    }
    return envConnectionString;
}
```

---

## Recommendations

### Immediate Fixes

1. **✅ Enable PostgreSQL entrypoint script** (see Fix 1 above)
2. **Remove password persistence** (see Fix 2 above)

2. **Clear connection pool on password errors:**
   - Already done in middleware, but ensure it happens before API crashes

3. **Remove persisted connection string file on startup if password mismatch:**
   - Add startup check: if persisted file password doesn't match environment variable, delete it

4. **Use health checks to prevent restart loops:**
   - Add health check that fails if database connection fails
   - This prevents Docker from restarting in a loop

### Long-term Solutions

1. **Use PostgreSQL init scripts:**
   - Create `/docker-entrypoint-initdb.d/` script that sets password from environment
   - This ensures password is always synced on container start

2. **Remove connection string persistence:**
   - Don't persist connection strings to files
   - Always use environment variables in production

3. **Add startup validation:**
   - On API startup, verify connection string password matches PostgreSQL
   - If mismatch, log error and exit (don't start with wrong password)

4. **Use secrets management:**
   - Use Docker secrets or external secret management
   - Ensures password is always consistent

---

## Specific Code Issues

### Issue 1: ConnectionStringService Persistence
**File:** `Services/ConnectionStringService.cs`
**Problem:** Persisted file takes priority over environment variables in some cases
**Fix:** Always prefer environment variables, never persist passwords

### Issue 2: No Password Validation on Startup
**File:** `Program.cs`
**Problem:** App starts even if password is wrong, then crashes on first DB call
**Fix:** Validate password on startup, exit if wrong

### Issue 3: PostgreSQL Password Not Synced on Container Start
**File:** `docker-compose.yml`
**Problem:** `POSTGRES_PASSWORD` only used on first volume creation
**Fix:** Use init script or entrypoint to always sync password

### Issue 4: Restart Policy Too Aggressive
**File:** `docker-compose.yml`
**Problem:** `restart: always` causes restart loops on password errors
**Fix:** Add health check, or use `restart: unless-stopped` with proper health checks

---

## Monitoring & Debugging

To identify when and why failures occur:

1. **Check PostgreSQL password:**
   ```bash
   docker compose exec postgres psql -U postgres -c "SELECT current_user;"
   ```

2. **Check API connection string:**
   ```bash
   docker compose exec api printenv | grep ConnectionStrings
   ```

3. **Check persisted connection string:**
   ```bash
   docker compose exec api cat /app/data/.connectionstring
   ```

4. **Monitor restart loops:**
   ```bash
   docker compose ps
   # Check restart count - if high, indicates restart loop
   ```

5. **Check logs for password errors:**
   ```bash
   docker compose logs api | grep -E "28P01|password|Password|authentication"
   ```

---

## Conclusion

The server fails multiple times because:
1. **PostgreSQL password persists in volume** and doesn't sync with `.env` automatically
2. **API persists connection strings** to a file that may have old passwords
3. **Connection pools cache** old passwords
4. **Restart policy** causes loops when password is wrong
5. **Deployment timing** issues can cause password mismatches

The fix script works once but fails again because it doesn't address the root cause: **PostgreSQL volume password persistence**. The password needs to be synced on every container start, not just when manually fixed.
