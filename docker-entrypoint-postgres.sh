#!/bin/bash
set -e

# First, run the original postgres entrypoint which initializes the database
# This will start postgres in the background
/docker-entrypoint.sh postgres "$@" &
POSTGRES_PID=$!

# Function to set password (will be called after postgres is ready)
set_password() {
  if [ -n "$POSTGRES_PASSWORD" ]; then
    echo "🔐 Synchronizing PostgreSQL password from POSTGRES_PASSWORD..."
    for i in {1..60}; do
      if pg_isready -U postgres > /dev/null 2>&1; then
        # Postgres is ready, set the password
        # Use Unix socket connection (no -h flag) - this uses peer/trust auth, no password needed
        # This works even if the volume has a different password stored
        if psql -U postgres -c "ALTER USER postgres WITH PASSWORD '$POSTGRES_PASSWORD';" > /dev/null 2>&1; then
          echo "✅ Password synchronized successfully"
          # Verify password works with network connection (what API will use)
          export PGPASSWORD="$POSTGRES_PASSWORD"
          if psql -h localhost -U postgres -c "SELECT 1;" > /dev/null 2>&1; then
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
            return 1
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
          echo "⚠️  Password mismatch detected but ALTER USER failed - may need manual intervention"
        fi
      fi
      sleep 1
    done
    echo "⚠️  Postgres not ready after 60 seconds, password sync skipped"
    return 1
  fi
}

# Set password after postgres starts
set_password

# Wait for postgres process (this keeps the container running)
wait $POSTGRES_PID
