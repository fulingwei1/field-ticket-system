#!/bin/sh
set -e

echo "Waiting for MinIO to be ready..."
sleep 5

# MinIO bucket initialization will be handled by the API service
# The MinIOService will create the bucket on first use if it doesn't exist

echo "MinIO initialization completed!"


