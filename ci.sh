#!/bin/bash
set -e

echo "========================================="
echo "  Running ci"
echo "========================================="

# Step 1: Restore
echo ""
echo "[1/3] Restoring packages..."
dotnet restore
echo "Restore successful"

# Step 2: Build
echo ""
echo "[2/3] Building..."
dotnet build --no-incremental
echo "Build successful"

# Step 3: Tests
echo ""
echo "[3/3] Running tests..."
dotnet test tests/Tests.csproj --no-build --verbosity normal
echo "Tests passed"

echo ""
echo "========================================="
echo "  All ci checks passed!"
echo "========================================="
