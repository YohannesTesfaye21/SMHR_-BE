#!/bin/bash
# Don't use set -e here - we want to continue even if password sync fails
# The original entrypoint will handle errors appropriately

# Find the original postgres entrypoint script
# In postgres:16-alpine, it's typically at /usr/local/bin/docker-entrypoint.sh
POSTGRES_ENTRYPOINT="/usr/local/bin/docker-entrypoint.sh"
if [ ! -f "$POSTGRES_ENTRYPOINT" ]; then
  # Fallback to common locations
  POSTGRES_ENTRYPOINT="/docker-entrypoint.sh"
  if [ ! -f "$POSTGRES_ENTRYPOINT" ]; then
    # Try to find it in PATH
    POSTGRES_ENTRYPOINT=$(which docker-entrypoint.sh 2>/dev/null || echo "")
    if [ -z "$POSTGRES_ENTRYPOINT" ]; then
      echo "❌ ERROR: Cannot find docker-entrypoint.sh"
      echo "   Tried: /usr/local/bin/docker-entrypoint.sh, /docker-entrypoint.sh"
      exit 1
    fi
  fi
fi

# First, run the original postgres entrypoint which initializes the database
# This will start postgres in the background
"$POSTGRES_ENTRYPOINT" postgres "$@" &
POSTGRES_PID=$!

# Function to set password (will be called after postgres is ready)
# This function is non-blocking - it won't cause the container to exit on failure
set_password() {
  if [ -n "$POSTGRES_PASSWORD" ]; then
    echo "🔐 Synchronizing PostgreSQL password from POSTGRES_PASSWORD..."
    for i in {1..60}; do
      if pg_isready -U postgres > /dev/null 2>&1; then
        # Postgres is ready, set the password
        # Try multiple connection methods to ensure we can connect
        # Method 1: Unix socket (peer/trust auth - no password needed)
        # Method 2: Localhost with current password (if we know it)
        # Method 3: Localhost with trust auth (if configured)
        PASSWORD_SET=0
        if psql -U postgres -d postgres -c "ALTER USER postgres WITH PASSWORD '$POSTGRES_PASSWORD';" > /dev/null 2>&1; then
          PASSWORD_SET=1
        elif [ -n "$PGPASSWORD" ] && psql -h localhost -U postgres -d postgres -c "ALTER USER postgres WITH PASSWORD '$POSTGRES_PASSWORD';" > /dev/null 2>&1; then
          PASSWORD_SET=1
        fi
        
        if [ $PASSWORD_SET -eq 1 ]; then
          echo "✅ Password synchronized successfully"
          # Wait a moment for password change to take effect
          sleep 1
          # Verify password works with network connection (what API will use)
          export PGPASSWORD="$POSTGRES_PASSWORD"
          if psql -h localhost -U postgres -d postgres -c "SELECT 1;" > /dev/null 2>&1; then
            echo "✅ Password verification successful (network connection works)"
            unset PGPASSWORD
            return 0
          else
            echo "⚠️  Password set but network verification failed - may need to wait for postgres to reload"
            unset PGPASSWORD
            # Wait a bit and retry
            sleep 2
            export PGPASSWORD="$POSTGRES_PASSWORD"
            if psql -h localhost -U postgres -c "SELECT 1;" > /dev/null 2>&1; then
              echo "✅ Password verification successful after retry"
              unset PGPASSWORD
              return 0
            fi
            unset PGPASSWORD
            # Don't fail - password might still work
            echo "⚠️  Network verification failed but continuing (password may still work)"
            return 0
          fi
        else
          echo "⚠️  Password sync command failed - checking if password already matches..."
          # Try to verify current password works (test with new password via network)
          export PGPASSWORD="$POSTGRES_PASSWORD"
          if psql -h localhost -U postgres -c "SELECT 1;" > /dev/null 2>&1; then
            echo "✅ Current password already matches POSTGRES_PASSWORD"
            unset PGPASSWORD
            return 0
          fi
          unset PGPASSWORD
          echo "⚠️  Password mismatch detected but ALTER USER failed - continuing anyway"
          # Don't fail - let the application handle the error
          return 0
        fi
      fi
      sleep 1
    done
    echo "⚠️  Postgres not ready after 60 seconds, password sync skipped (continuing anyway)"
    return 0  # Don't fail - postgres might still work
  fi
}

# Set password after postgres starts (non-blocking)
# Run password sync in background but log results
(set_password && echo "✅ Password sync completed") || echo "⚠️  Password sync had issues but continuing..."

# Wait for postgres process (this keeps the container running)
# This is the critical part - we must wait for the main postgres process
wait $POSTGRES_PID
