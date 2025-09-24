#!/bin/bash

MGCB_FILE="ColorWars/Content/Content.mgcb"

if [ ! -f "$MGCB_FILE" ]; then
  echo "❌ Could not find $MGCB_FILE"
  exit 1
fi

# Find all PNG files in Content/ folder recursively
find Content -type f -name "*.png" | while read filepath; do
  # Get the relative path of the file from the Content directory
  relpath=$(realpath --relative-to=Content "$filepath" 2>/dev/null)

  # fallback if macOS `realpath` doesn't support --relative-to
  if [ -z "$relpath" ]; then
    relpath=$(echo "$filepath" | sed 's|^Content/||')
  fi

  echo "" >> "$MGCB_FILE"
  echo "#begin $relpath" >> "$MGCB_FILE"
  echo "/importer:TextureImporter" >> "$MGCB_FILE"
  echo "/processor:TextureProcessor" >> "$MGCB_FILE"
  echo "/build:$relpath" >> "$MGCB_FILE"

  echo "✅ Added: $relpath"
done