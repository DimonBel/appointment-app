#!/bin/sh
set -e

# Wait for MinIO to be ready
sleep 5

# Create buckets if they don't exist
# Using MinIO client (mc) commands
mc alias set local http://localhost:9000 "${MINIO_ROOT_USER}" "${MINIO_ROOT_PASSWORD}"

# Create avatars bucket
mc mb local/avatars --ignore-existing
# Set public policy for avatars bucket
mc anonymous set download local/avatars

# Create documents bucket
mc mb local/documents --ignore-existing
# Set public policy for documents bucket
mc anonymous set download local/documents

# Start MinIO server
exec /usr/bin/docker-entrypoint.sh "$@"