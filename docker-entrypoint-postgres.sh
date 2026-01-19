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
        # Use PGPASSWORD environment variable to avoid password prompt issues
        export PGPASSWORD="${POSTGRES_PASSWORD:-postgres}"
        if psql -U postgres -c "ALTER USER postgres WITH PASSWORD '$POSTGRES_PASSWORD';" > /dev/null 2>&1; then
          echo "✅ Password synchronized successfully"
          # Verify password works
          if psql -U postgres -c "SELECT 1;" > /dev/null 2>&1; then
            echo "✅ Password verification successful"
            unset PGPASSWORD
            return 0
          else
            echo "⚠️  Password set but verification failed"
            unset PGPASSWORD
            return 1
          fi
        else
          echo "⚠️  Password sync attempted (may already be correct or database not ready)"
          unset PGPASSWORD
          # Try to verify current password works
          if psql -U postgres -c "SELECT 1;" > /dev/null 2>&1; then
            echo "✅ Current password works correctly"
            return 0
          fi
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
