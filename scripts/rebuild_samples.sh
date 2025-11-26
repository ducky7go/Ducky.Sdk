#!/usr/bin/env bash
set -euo pipefail

# rebuild_samples.sh
# Rebuild all sample projects using a fresh local Ducky.Sdk package
#
# This script:
#   1. Calls packToLocal.sh to publish auto-incremented version of Ducky.Sdk
#   2. Restores and rebuilds the Docky.Sdk.Sample.slnx solution
#   Note: With auto-incrementing versions, cache clearing is no longer needed
#
# Usage:
#   ./scripts/rebuild_samples.sh [--purge-all-versions] [--no-clear-all-caches] [--skip-tests] [--no-build]

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PACK_SCRIPT="${ROOT_DIR}/scripts/packToLocal.sh"
SAMPLES_DIR="${ROOT_DIR}/Samples"
SAMPLE_SOLUTION="${SAMPLES_DIR}/Docky.Sdk.Sample.slnx"
PURGE_ALL_VERSIONS=false
CLEAR_ALL_CACHES=false  # Disabled by default since auto-increment versions avoid cache issues
FORWARD_PACK_FLAGS=()
PACKAGE_ID="Ducky.Sdk"
PACKAGE_ID_LOWER="ducky.sdk"

function log() { printf "[rebuild_samples] %s\n" "$*"; }
function warn() { printf "[rebuild_samples][WARN] %s\n" "$*"; }
function err() { printf "[rebuild_samples][ERROR] %s\n" "$*" >&2; exit 1; }

while [[ $# -gt 0 ]]; do
  case "$1" in
    --purge-all-versions)
      PURGE_ALL_VERSIONS=true; shift;;
    --clear-all-caches)
      CLEAR_ALL_CACHES=true; shift;;
    --no-clear-all-caches)
      CLEAR_ALL_CACHES=false; shift;;
    --skip-tests|--no-build)
      FORWARD_PACK_FLAGS+=("$1"); shift;;
    --configuration)
      FORWARD_PACK_FLAGS+=("--configuration" "$2"); shift 2;;
    --max-packages)
      FORWARD_PACK_FLAGS+=("--max-packages" "$2"); shift 2;;
    --no-cleanup)
      FORWARD_PACK_FLAGS+=("--no-cleanup"); shift;;
    -h|--help)
      sed -n '2,80p' "$0"; exit 0;;
    *) err "Unknown arg: $1";;
  esac
done

# Check prerequisites
[[ -f "$PACK_SCRIPT" ]] || err "packToLocal.sh not found: $PACK_SCRIPT"
[[ -d "$SAMPLES_DIR" ]] || err "Samples directory not found: $SAMPLES_DIR"
[[ -f "$SAMPLE_SOLUTION" ]] || err "Sample solution not found: $SAMPLE_SOLUTION"

# Step 1: Pack Ducky.Sdk with auto-incremented version to local feed
log "Step 1: Packing Ducky.Sdk with auto-incremented version to local feed"
PACK_ARGS=(--configuration Debug)
${PURGE_ALL_VERSIONS} && PACK_ARGS+=(--purge-all-versions)
${CLEAR_ALL_CACHES} && PACK_ARGS+=(--clear-all-caches)
PACK_ARGS+=("${FORWARD_PACK_FLAGS[@]}")
bash "$PACK_SCRIPT" "${PACK_ARGS[@]}" || err "Failed to pack Ducky.Sdk"

# Step 2: Restore NuGet packages for the solution
log "Step 2: Restoring NuGet packages for sample solution"
cd "$SAMPLES_DIR"
dotnet clean "$SAMPLE_SOLUTION" || err "Failed to clean sample solution"
dotnet restore "$SAMPLE_SOLUTION" || err "Failed to restore sample solution"

# Step 3: Update Sample projects with new version
log "Step 3: Updating Sample projects with new Ducky.Sdk version"
if [[ -f "${ROOT_DIR}/nuget.props" ]]; then
  ACTUAL_VERSION=$(grep -Po '(?<=<LocalNuGetVersion>)[^<]+' "${ROOT_DIR}/nuget.props" | head -1 || true)
  if [[ -n "$ACTUAL_VERSION" ]]; then
    log "Updating all Sample projects to Ducky.Sdk ${ACTUAL_VERSION}"

    # Find all csproj files in samples directory and update Ducky.Sdk version
    find . -name "*.csproj" -exec grep -l "Ducky.Sdk" {} \; | while read proj; do
      log "Updating Ducky.Sdk version in $proj"
      # Use dotnet add package to update the version reliably
      cd "$(dirname "$proj")"
      dotnet add package "Ducky.Sdk" --version "$ACTUAL_VERSION" --source "${ROOT_DIR}/duckylocal" || err "Failed to update Ducky.Sdk in $(basename "$proj")"
      cd "$SAMPLES_DIR"
    done

    log "✓ All Sample projects updated to Ducky.Sdk ${ACTUAL_VERSION}"
  else
    err "Failed to extract version from nuget.props"
  fi
else
  err "nuget.props not found"
fi

# Step 4: Rebuild the solution
log "Step 4: Rebuilding sample solution"
dotnet build "$SAMPLE_SOLUTION" --configuration Debug || err "Failed to build sample solution"

# Read the actual version that was used from nuget.props
if [[ -f "${ROOT_DIR}/nuget.props" ]]; then
  ACTUAL_VERSION=$(grep -Po '(?<=<LocalNuGetVersion>)[^<]+' "${ROOT_DIR}/nuget.props" | head -1 || true)
else
  ACTUAL_VERSION="unknown"
fi

log "✓ Sample projects rebuilt successfully with Ducky.Sdk ${ACTUAL_VERSION}"
log "Done."
