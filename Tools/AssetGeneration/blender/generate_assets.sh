#!/usr/bin/env bash
# Generates all four Milestone 8 secret-room props (FBX) in one pass.
# Requires Blender (free, https://www.blender.org/download/) on PATH.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

if ! command -v blender >/dev/null 2>&1; then
    echo "Error: Blender is not installed or not on PATH." >&2
    echo "Install it (e.g. 'sudo apt install blender' on Ubuntu/Debian, or download from blender.org) and re-run this script." >&2
    exit 1
fi

echo "Running Blender headlessly to generate props..."
blender --background --python "$SCRIPT_DIR/generate_props.py"

echo ""
echo "Done. Generated files in $SCRIPT_DIR/output:"
ls -la "$SCRIPT_DIR/output"
