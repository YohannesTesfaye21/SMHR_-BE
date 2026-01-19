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
        psql -U postgres -c "ALTER USER postgres WITH PASSWORD '$POSTGRES_PASSWORD';" > /dev/null 2>&1 && {
          echo "✅ Password synchronized successfully"
          return 0
        } || {
          echo "⚠️  Password sync attempted (may already be correct)"
          return 0
        }
      fi
      sleep 1
    done
    echo "⚠️  Postgres not ready after 60 seconds, password sync skipped"
  fi
}

# Set password after postgres starts
set_password

# Wait for postgres process (this keeps the container running)
wait $POSTGRES_PID
