# Fix PostgreSQL Password Authentication Issue

## Problem
The application is experiencing password authentication failures with error:
```
28P01: password authentication failed for user "postgres"
```

This happens when the PostgreSQL container was initialized with a different password than what's configured in `docker-compose.yml`.

## Quick Fix (On Server)

### Option 1: Run the fix script (Recommended)
```bash
cd /opt/smhfr-be
chmod +x fix-db-password.sh
./fix-db-password.sh
docker compose restart api
```

### Option 2: Manual password reset
```bash
# Connect to the PostgreSQL container and reset the password
docker exec -it smhfr-postgres psql -U postgres -d smhfr_db -c "ALTER USER postgres WITH PASSWORD 'postgres';"

# Restart the API container to apply changes
docker compose restart api
```

### Option 3: Quick one-liner
```bash
docker exec smhfr-postgres psql -U postgres -c "ALTER USER postgres WITH PASSWORD 'postgres';" && docker compose restart api
```

## Verify Fix
```bash
# Check API logs for connection errors
docker compose logs api | grep -i "password\|authentication\|28P01"

# Test the API health endpoint
curl http://localhost:8080/api/health

# Test the health facilities endpoint
curl http://localhost:8080/api/healthfacilities
```

## Prevention

The updated configuration includes:
1. **Connection retry logic** - Automatically retries failed database connections
2. **Restart policies** - Containers automatically restart on failure
3. **Better health checks** - More accurate database readiness checks
4. **Connection pooling** - Improved connection management

## If Password Reset Doesn't Work

If the password reset doesn't work, you may need to recreate the database volume (⚠️ **THIS WILL DELETE ALL DATA**):

```bash
# BACKUP FIRST if you need to preserve data
docker exec smhfr-postgres pg_dump -U postgres smhfr_db > backup.sql

# Stop containers and remove volume
docker compose down -v

# Recreate containers
docker compose up -d --build

# Restore backup if needed
docker exec -i smhfr-postgres psql -U postgres smhfr_db < backup.sql
```

## Configuration

The password is configured in:
- `docker-compose.yml` - Environment variable `POSTGRES_PASSWORD`
- `docker-compose.yml` - Connection string `ConnectionStrings__DefaultConnection`

Both should match: **Password = `postgres`**
