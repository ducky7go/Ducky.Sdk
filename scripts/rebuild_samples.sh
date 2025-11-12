#!/usr/bin/env bash
set -euo pipefail

# rebuild_samples.sh
# Rebuild all sample projects using a fresh local Ducky.Sdk package
#
# This script:
#   1. Calls packToLocal.sh to publish version 0.0.1 of Ducky.Sdk
#   2. Clears NuGet cache to ensure fresh package is used
#   3. Recursively deletes bin and obj directories in Samples folder
#   4. Restores and rebuilds the Docky.Sdk.Sample.slnx solution
#
# Usage:
#   ./scripts/rebuild_samples.sh [--version x.y.z] [--purge-all-versions] [--clear-all-caches]

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PACK_SCRIPT="${ROOT_DIR}/scripts/packToLocal.sh"
SAMPLES_DIR="${ROOT_DIR}/Samples"
SAMPLE_SOLUTION="${SAMPLES_DIR}/Docky.Sdk.Sample.slnx"
VERSION="0.0.1"
PURGE_ALL_VERSIONS=false
CLEAR_ALL_CACHES=false
FORWARD_PACK_FLAGS=()
PACKAGE_ID="Ducky.Sdk"
PACKAGE_ID_LOWER="ducky.sdk"

function log() { printf "[rebuild_samples] %s\n" "$*"; }
function warn() { printf "[rebuild_samples][WARN] %s\n" "$*"; }
function err() { printf "[rebuild_samples][ERROR] %s\n" "$*" >&2; exit 1; }

while [[ $# -gt 0 ]]; do
  case "$1" in
    --version)
      VERSION="$2"; shift 2;;
    --purge-all-versions)
      PURGE_ALL_VERSIONS=true; shift;;
    --clear-all-caches)
      CLEAR_ALL_CACHES=true; shift;;
    --skip-tests|--no-build)
      FORWARD_PACK_FLAGS+=("$1"); shift;;
    --configuration)
      FORWARD_PACK_FLAGS+=("--configuration" "$2"); shift 2;;
    -h|--help)
      sed -n '2,80p' "$0"; exit 0;;
    *) err "Unknown arg: $1";;
  esac
done

# Check prerequisites
[[ -f "$PACK_SCRIPT" ]] || err "packToLocal.sh not found: $PACK_SCRIPT"
[[ -d "$SAMPLES_DIR" ]] || err "Samples directory not found: $SAMPLES_DIR"
[[ -f "$SAMPLE_SOLUTION" ]] || err "Sample solution not found: $SAMPLE_SOLUTION"

# Step 1: Pack Ducky.Sdk version 0.0.1 to local feed
log "Step 1: Packing Ducky.Sdk version ${VERSION} to local feed"
PACK_ARGS=(--version "$VERSION" --configuration Debug)
${PURGE_ALL_VERSIONS} && PACK_ARGS+=(--purge-all-versions)
${CLEAR_ALL_CACHES} && PACK_ARGS+=(--clear-all-caches)
PACK_ARGS+=("${FORWARD_PACK_FLAGS[@]}")
bash "$PACK_SCRIPT" "${PACK_ARGS[@]}" || err "Failed to pack Ducky.Sdk"

# Step 2: Additional cache cleanup (packToLocal already cleared some caches)
log "Step 2: Verifying cache cleanup and clearing additional MSBuild caches"
# Clear http-cache to ensure no stale package metadata
log "Clearing NuGet http-cache to ensure fresh package metadata"
dotnet nuget locals http-cache --clear || warn "Failed to clear http-cache"
if $CLEAR_ALL_CACHES; then
  log "Clearing NuGet temp caches"
  dotnet nuget locals temp --clear || warn "Failed to clear temp cache"
fi

# Step 3: Recursively delete bin and obj directories in Samples folder
log "Step 3: Cleaning bin and obj directories in Samples folder"
if [[ -d "$SAMPLES_DIR" ]]; then
  find "$SAMPLES_DIR" -type d \( -name "bin" -o -name "obj" \) -print0 | while IFS= read -r -d '' dir; do
    log "Removing: $dir"
    rm -rf "$dir"
  done
  log "Cleanup complete"
else
  warn "Samples directory not found: $SAMPLES_DIR"
fi

# Step 4: Restore NuGet packages for the solution
log "Step 4: Restoring NuGet packages for sample solution"
cd "$SAMPLES_DIR"
# Use --force-evaluate to bypass MSBuild cache and --no-cache to skip NuGet cache during restore
dotnet restore "$SAMPLE_SOLUTION" --force --no-cache || err "Failed to restore sample solution"

# Step 5: Rebuild the solution
log "Step 5: Rebuilding sample solution"
dotnet build "$SAMPLE_SOLUTION" --no-restore --configuration Debug || err "Failed to build sample solution"

log "✓ Sample projects rebuilt successfully with Ducky.Sdk ${VERSION}"
log "Done."
