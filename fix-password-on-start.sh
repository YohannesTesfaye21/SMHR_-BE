#!/bin/bash
# Script to fix postgres password on every container start
# This ensures password always matches .env file

cd /opt/smhfr-be || exit 1

# Wait for postgres to be ready
for i in {1..30}; do
  if docker compose exec -T postgres pg_isready -U postgres > /dev/null 2>&1; then
    break
  fi
  sleep 2
done

# Get password from .env
DB_PASSWORD=$(grep DB_PASSWORD .env | cut -d= -f2)

# Set password in postgres
docker compose exec -T postgres psql -U postgres -c "ALTER USER postgres WITH PASSWORD '$DB_PASSWORD';" > /dev/null 2>&1

echo "Password synchronized"
