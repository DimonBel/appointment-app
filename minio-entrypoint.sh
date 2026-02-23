#!/bin/sh
set -e

# Start MinIO server in background
/usr/bin/minio server /data --console-address ":9001" &
MINIO_PID=$!

# Wait for MinIO to be ready
echo "Waiting for MinIO to start..."
until curl -f http://localhost:9000/minio/health/live >/dev/null 2>&1; do
    echo "MinIO is not ready yet, waiting..."
    sleep 2
done
echo "MinIO is ready!"

# Configure MinIO client
mc alias set local http://localhost:9000 "${MINIO_ROOT_USER}" "${MINIO_ROOT_PASSWORD}"

# Create avatars bucket if it doesn't exist
mc mb local/avatars --ignore-existing
# Set public policy for avatars bucket
mc anonymous set download local/avatars

# Create documents bucket if it doesn't exist
mc mb local/documents --ignore-existing
# Set public policy for documents bucket
mc anonymous set download local/documents

echo "MinIO buckets created successfully!"

# Wait for MinIO process
wait $MINIO_PID