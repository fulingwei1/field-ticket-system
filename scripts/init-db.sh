#!/bin/bash
set -e

echo "Initializing database..."

# Wait for PostgreSQL to be ready
until pg_isready -U "$POSTGRES_USER" -d "$POSTGRES_DB"; do
  echo "Waiting for PostgreSQL to be ready..."
  sleep 2
done

echo "PostgreSQL is ready!"

# Run database migrations (if any)
# This will be handled by the API service on startup
# or can be run manually: docker-compose exec api dotnet ef database update

echo "Database initialization completed!"


