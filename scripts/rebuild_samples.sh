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
#   ./scripts/rebuild_samples.sh

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PACK_SCRIPT="${ROOT_DIR}/scripts/packToLocal.sh"
SAMPLES_DIR="${ROOT_DIR}/Samples"
SAMPLE_SOLUTION="${SAMPLES_DIR}/Docky.Sdk.Sample.slnx"
VERSION="0.0.1"
PACKAGE_ID="Ducky.Sdk"
PACKAGE_ID_LOWER="ducky.sdk"

function log() { printf "[rebuild_samples] %s\n" "$*"; }
function warn() { printf "[rebuild_samples][WARN] %s\n" "$*"; }
function err() { printf "[rebuild_samples][ERROR] %s\n" "$*" >&2; exit 1; }

# Check prerequisites
[[ -f "$PACK_SCRIPT" ]] || err "packToLocal.sh not found: $PACK_SCRIPT"
[[ -d "$SAMPLES_DIR" ]] || err "Samples directory not found: $SAMPLES_DIR"
[[ -f "$SAMPLE_SOLUTION" ]] || err "Sample solution not found: $SAMPLE_SOLUTION"

# Step 1: Pack Ducky.Sdk version 0.0.1 to local feed
log "Step 1: Packing Ducky.Sdk version ${VERSION} to local feed"
bash "$PACK_SCRIPT" --version "$VERSION" --configuration Debug || err "Failed to pack Ducky.Sdk"

# Step 2: Clear NuGet caches for the package to ensure fresh installation
log "Step 2: Clearing NuGet caches for ${PACKAGE_ID} ${VERSION}"

# Clear global-packages cache for this specific package/version
GP_LINE=$(dotnet nuget locals global-packages --list | tr -d '\r' || true)
GP_DIR=$(printf "%s" "$GP_LINE" | sed -E 's/.*global-packages: *//')

if [[ -n "$GP_DIR" && -d "$GP_DIR" ]]; then
  PKG_CACHE_DIR="$GP_DIR/$PACKAGE_ID_LOWER/$VERSION"
  if [[ -d "$PKG_CACHE_DIR" ]]; then
    log "Removing cached package: $PKG_CACHE_DIR"
    rm -rf "$PKG_CACHE_DIR"
  fi
  
  # Also clear the entire package cache (all versions) to be thorough
  PKG_ALL_VERSIONS="$GP_DIR/$PACKAGE_ID_LOWER"
  if [[ -d "$PKG_ALL_VERSIONS" ]]; then
    log "Removing all cached versions of ${PACKAGE_ID}: $PKG_ALL_VERSIONS"
    rm -rf "$PKG_ALL_VERSIONS"
  fi
else
  warn "Could not determine global-packages directory"
fi

# Clear http-cache and temp to ensure no stale metadata
log "Clearing NuGet http-cache and temp caches"
dotnet nuget locals http-cache --clear || warn "Failed to clear http-cache"
dotnet nuget locals temp --clear || warn "Failed to clear temp cache"

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
dotnet restore "$SAMPLE_SOLUTION" --force || err "Failed to restore sample solution"

# Step 5: Rebuild the solution
log "Step 5: Rebuilding sample solution"
dotnet build "$SAMPLE_SOLUTION" --no-restore --configuration Debug || err "Failed to build sample solution"

log "✓ Sample projects rebuilt successfully with Ducky.Sdk ${VERSION}"
log "Done."
