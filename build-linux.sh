#!/bin/bash
# Build script for Linux deployment

set -e  # Exit on error

echo "======================================"
echo "ECoopSystem Linux Build Script"
echo "======================================"
echo ""

# Configuration
PROJECT_NAME="ECoopSystem"
RUNTIME="linux-x64"
CONFIGURATION="Release"
OUTPUT_DIR="./publish/linux-x64"

# Clean previous builds
echo "Cleaning previous builds..."
if [ -d "$OUTPUT_DIR" ]; then
    rm -rf "$OUTPUT_DIR"
fi

# Restore dependencies
echo "Restoring dependencies..."
dotnet restore

# Build project
echo "Building project..."
dotnet build -c $CONFIGURATION

# Run tests if they exist
if [ -d "Tests" ] || [ -d "tests" ]; then
    echo "Running tests..."
    dotnet test -c $CONFIGURATION --no-build
fi

# Publish application
echo "Publishing application for $RUNTIME..."
dotnet publish -c $CONFIGURATION -r $RUNTIME \
    --self-contained \
    -p:PublishSingleFile=true \
    -p:PublishTrimmed=false \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -o "$OUTPUT_DIR"

# Make executable
echo "Setting executable permissions..."
chmod +x "$OUTPUT_DIR/$PROJECT_NAME"

# Copy configuration files
echo "Copying configuration files..."
cp -f appsettings*.json "$OUTPUT_DIR/" 2>/dev/null || true

# Display build info
echo ""
echo "======================================"
echo "Build completed successfully!"
echo "======================================"
echo "Output directory: $OUTPUT_DIR"
echo "Executable: $OUTPUT_DIR/$PROJECT_NAME"
echo ""
echo "To run the application:"
echo "  cd $OUTPUT_DIR"
echo "  ./$PROJECT_NAME"
echo ""

# Display size
if command -v du &> /dev/null; then
    SIZE=$(du -sh "$OUTPUT_DIR" | cut -f1)
    echo "Total size: $SIZE"
fi

echo ""
echo "======================================"
