#!/bin/bash
# Script to fix postgres password - ensures password always matches .env file
# This runs every 5 minutes via cron to prevent password drift

set -e

LOG_FILE="/var/log/smhfr-password-sync.log"
WORK_DIR="/opt/smhfr-be"

cd "$WORK_DIR" || {
  echo "$(date): ERROR: Cannot cd to $WORK_DIR" >> "$LOG_FILE"
  exit 1
}

# Load .env file
if [ ! -f .env ]; then
  echo "$(date): ERROR: .env file not found" >> "$LOG_FILE"
  exit 1
fi

# Source .env to get variables
export $(grep -v '^#' .env | xargs)

# Wait for postgres to be ready
POSTGRES_READY=0
for i in {1..30}; do
  if docker compose --env-file .env exec -T postgres pg_isready -U postgres > /dev/null 2>&1; then
    POSTGRES_READY=1
    break
  fi
  sleep 1
done

if [ $POSTGRES_READY -eq 0 ]; then
  echo "$(date): ERROR: Postgres not ready after 30 seconds" >> "$LOG_FILE"
  exit 1
fi

# Get password from .env (use the exported variable)
if [ -z "$DB_PASSWORD" ]; then
  DB_PASSWORD=$(grep "^DB_PASSWORD=" .env | cut -d= -f2- | tr -d '"' | tr -d "'")
fi

if [ -z "$DB_PASSWORD" ]; then
  echo "$(date): ERROR: DB_PASSWORD not found in .env" >> "$LOG_FILE"
  exit 1
fi

# Set password in postgres
if docker compose --env-file .env exec -T postgres psql -U postgres -c "ALTER USER postgres WITH PASSWORD '$DB_PASSWORD';" > /dev/null 2>&1; then
  echo "$(date): SUCCESS: Password synchronized" >> "$LOG_FILE"
else
  echo "$(date): ERROR: Failed to set password in postgres" >> "$LOG_FILE"
  exit 1
fi
