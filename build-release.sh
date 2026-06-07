#!/bin/bash

set -e

APP_NAME="TruthDoctor"
VERSION="1.0.0"
FRAMEWORK="net10.0"
OUTPUT_DIR="releases"

echo "Starting multi-platform build for $APP_NAME v$VERSION"

# Clean old output
rm -rf $OUTPUT_DIR
mkdir -p $OUTPUT_DIR

# Runtime targets
RUNTIMES=(
  "osx-arm64"
  "osx-x64"
  "win-x64"
  "linux-x64"
)

for RUNTIME in "${RUNTIMES[@]}"
do
  echo "----------------------------------------"
  echo "Building for $RUNTIME"
  echo "----------------------------------------"

  dotnet publish -c Release -r $RUNTIME --self-contained true

  PUBLISH_PATH="bin/Release/$FRAMEWORK/$RUNTIME/publish"
  ZIP_NAME="$APP_NAME-v$VERSION-$RUNTIME.zip"

  if [ -d "$PUBLISH_PATH" ]; then
    echo "Packaging $ZIP_NAME"
    cd "bin/Release/$FRAMEWORK"
    zip -r "../../../$OUTPUT_DIR/$ZIP_NAME" "$RUNTIME/publish" > /dev/null
    cd - > /dev/null
  else
    echo "Publish folder not found for $RUNTIME"
  fi
done

echo "----------------------------------------"
echo "Build complete"
echo "Release files located in: $OUTPUT_DIR"
echo "----------------------------------------"
