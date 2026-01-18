#!/bin/bash

# Script to fix PostgreSQL password authentication issue
# This script resets the PostgreSQL password to match the docker-compose.yml configuration

set -e

echo "🔧 Fixing PostgreSQL password authentication..."

# Configuration from docker-compose.yml
DB_CONTAINER="smhfr-postgres"
DB_USER="postgres"
DB_NAME="smhfr_db"
NEW_PASSWORD="postgres"

# Check if container is running
if ! docker ps | grep -q "$DB_CONTAINER"; then
    echo "❌ Error: PostgreSQL container '$DB_CONTAINER' is not running!"
    echo "   Please start the containers first: docker compose up -d"
    exit 1
fi

echo "✅ PostgreSQL container is running"

# Reset the password using psql
echo "🔄 Resetting PostgreSQL password for user '$DB_USER'..."

# Method 1: Using psql ALTER USER command
docker exec -it "$DB_CONTAINER" psql -U "$DB_USER" -d "$DB_NAME" -c "ALTER USER $DB_USER WITH PASSWORD '$NEW_PASSWORD';" || {
    echo "⚠️  First method failed, trying alternative method..."
    
    # Method 2: Using environment variable and psql
    PGPASSWORD=postgres docker exec -e PGPASSWORD=postgres "$DB_CONTAINER" psql -U "$DB_USER" -d postgres -c "ALTER USER $DB_USER WITH PASSWORD '$NEW_PASSWORD';" || {
        echo "⚠️  Alternative method failed, trying to reset via docker exec..."
        
        # Method 3: Direct connection attempt
        docker exec "$DB_CONTAINER" psql -U "$DB_USER" -c "ALTER USER $DB_USER WITH PASSWORD '$NEW_PASSWORD';" || {
            echo "❌ All methods failed. The container might need to be recreated."
            echo "   If you want to preserve data, you may need to:"
            echo "   1. Backup the database"
            echo "   2. Remove the volume"
            echo "   3. Recreate with correct password"
            exit 1
        }
    }
}

echo "✅ Password reset successfully!"

# Verify the connection
echo "🔍 Verifying connection with new password..."
if docker exec -e PGPASSWORD="$NEW_PASSWORD" "$DB_CONTAINER" psql -U "$DB_USER" -d "$DB_NAME" -c "SELECT 1;" > /dev/null 2>&1; then
    echo "✅ Connection verified successfully!"
else
    echo "⚠️  Connection verification failed, but password was reset."
    echo "   The API container may need to be restarted: docker compose restart api"
fi

echo ""
echo "📋 Next steps:"
echo "   1. Restart the API container: docker compose restart api"
echo "   2. Check logs: docker compose logs api"
echo "   3. Test the API: curl http://localhost:8080/api/health"
echo ""
echo "✅ Password fix completed!"
