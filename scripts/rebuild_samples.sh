#!/usr/bin/env bash
set -euo pipefail

# rebuild_samples.sh
# Rebuild all sample projects using a fresh local Ducky.Sdk package
#
# This script:
#   1. Calls packToLocal.sh to publish auto-incremented version of Ducky.Sdk
#   2. Rewrites the Ducky.Sdk version in every sample csproj, then restores and rebuilds Docky.Sdk.Sample.slnx
#   Note: packToLocal.sh replaces a version already in the NuGet cache (the packed version is never reused silently)
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

# Step 2: Point Sample projects at the freshly packed Ducky.Sdk version.
# This must happen BEFORE any restore: the committed samples pin a version that
# the local feed does not contain on a clean checkout (e.g. CI), so restoring first fails with NU1102.
log "Step 2: Updating Sample projects with new Ducky.Sdk version"
[[ -f "${ROOT_DIR}/nuget.props" ]] || err "nuget.props not found"
ACTUAL_VERSION=$(grep -Po '(?<=<LocalNuGetVersion>)[^<]+' "${ROOT_DIR}/nuget.props" | head -1 || true)
[[ -n "$ACTUAL_VERSION" ]] || err "Failed to extract version from nuget.props"

cd "$SAMPLES_DIR"
log "Updating all Sample projects to Ducky.Sdk ${ACTUAL_VERSION}"
while IFS= read -r proj; do
  log "Updating Ducky.Sdk version in $proj"
  sed -E -i "s#(<PackageReference[[:space:]]+Include=\"Ducky\.Sdk\"[[:space:]]+Version=\")[^\"]+(\")#\1${ACTUAL_VERSION}\2#" "$proj"
done < <(find . -name "*.csproj" -exec grep -l 'Include="Ducky.Sdk"' {} \;)

# Step 3: Clean and restore the solution against the updated versions
log "Step 3: Restoring NuGet packages for sample solution"
dotnet clean "$SAMPLE_SOLUTION" || err "Failed to clean sample solution"
dotnet restore "$SAMPLE_SOLUTION" || err "Failed to restore sample solution"
log "✓ All Sample projects updated to Ducky.Sdk ${ACTUAL_VERSION}"

# Step 4: Rebuild the solution
log "Step 4: Rebuilding sample solution"
# Parallel builds are safe: the build runner has no shared script cache (the former dotnet-script race is gone)
dotnet build "$SAMPLE_SOLUTION" --configuration Debug || err "Failed to build sample solution"

log "✓ Sample projects rebuilt successfully with Ducky.Sdk ${ACTUAL_VERSION}"
log "Done."
