#!/usr/bin/env bash
set -euo pipefail

# packToLocal.sh
# Build and pack the Ducky.Sdk NuGet package into the local ./duckylocal source directory.
# Usage:
#   ./scripts/packToLocal.sh [--version x.y.z] [--no-build] [--skip-tests] [--configuration Debug|Release] [--no-clear-cache] [--clear-all-caches]
#
# The script will:
#   1. Ensure ./duckylocal exists
#   2. Optionally build analyzer and sdk projects
#   3. Determine package version (from nuspec unless overridden)
#   4. Clean any existing nupkg of same ID+version in duckylocal
#   5. Clear NuGet caches for this package/version (and optionally all caches)
#   6. Pack using dotnet pack (honoring Ducky.Sdk.csproj + (possibly temp) nuspec)
#   7. Verify resulting .nupkg
#
# Notes:
#   - When using an external .nuspec, /p:PackageVersion does NOT override the <version> inside the nuspec.
#     To honor --version we generate a temp nuspec file with the replaced version and pass it via -p:NuspecFile.
#   - Requires dotnet SDK installed and accessible in PATH.
#   - Local nuget.config includes duckylocal source.

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PKG_PROJ="${ROOT_DIR}/Sdk/SDKlibs/Ducky.Sdk/Ducky.Sdk.csproj"
NUSPEC_ORIG="${ROOT_DIR}/Sdk/SDKlibs/Ducky.Sdk/Ducky.Sdk.nuspec"
LOCAL_FEED="${ROOT_DIR}/duckylocal"
PACKAGE_ID="Ducky.Sdk"
PACKAGE_ID_LOWER="ducky.sdk"
BUILD=true
RUN_TESTS=true
OVERRIDE_VERSION=""
CONFIGURATION="Debug"
TEMP_NUSPEC=""
CLEAR_CACHE=true
CLEAR_ALL_CACHES=false

function log() { printf "[packToLocal] %s\n" "$*"; }
function warn() { printf "[packToLocal][WARN] %s\n" "$*"; }
function err() { printf "[packToLocal][ERROR] %s\n" "$*" >&2; }

# Parse args
while [[ $# -gt 0 ]]; do
  case "$1" in
    --version)
      OVERRIDE_VERSION="$2"; shift 2;;
    --no-build)
      BUILD=false; shift;;
    --skip-tests)
      RUN_TESTS=false; shift;;
    --configuration)
      CONFIGURATION="$2"; shift 2;;
    --no-clear-cache)
      CLEAR_CACHE=false; shift;;
    --clear-all-caches)
      CLEAR_ALL_CACHES=true; shift;;
    -h|--help)
      sed -n '2,80p' "$0"; exit 0;;
    *) err "Unknown arg: $1"; exit 1;;
  esac
done

log "Root dir: $ROOT_DIR"
log "Local feed: $LOCAL_FEED"
log "Configuration: $CONFIGURATION"
mkdir -p "$LOCAL_FEED"

if [[ ! -f "$PKG_PROJ" ]]; then
  err "Package project not found: $PKG_PROJ"; exit 1
fi
if [[ ! -f "$NUSPEC_ORIG" ]]; then
  err "Nuspec not found: $NUSPEC_ORIG"; exit 1
fi

ORIG_VERSION=$(grep -Po '(?<=<version>)[^<]+' "$NUSPEC_ORIG" | head -1 || true)
if [[ -z "$ORIG_VERSION" ]]; then
  err "Original nuspec version could not be determined."; exit 1
fi

# If no override provided, use original
if [[ -z "$OVERRIDE_VERSION" ]]; then
  OVERRIDE_VERSION="$ORIG_VERSION"
fi

log "Original nuspec version: $ORIG_VERSION"
log "Requested (effective) version: $OVERRIDE_VERSION"

# Prepare nuspec to pack: if override differs, create temp nuspec with new version
NUSPEC_TO_USE="$NUSPEC_ORIG"
if [[ "$OVERRIDE_VERSION" != "$ORIG_VERSION" ]]; then
  TEMP_NUSPEC="${ROOT_DIR}/Sdk/SDKlibs/Ducky.Sdk/Ducky.Sdk.${OVERRIDE_VERSION}.nuspec"
  sed -E "s#<version>[^<]+</version>#<version>${OVERRIDE_VERSION}</version>#" "$NUSPEC_ORIG" > "$TEMP_NUSPEC"
  NUSPEC_TO_USE="$TEMP_NUSPEC"
  log "Created temp nuspec: $TEMP_NUSPEC"
fi

# Optional build
if $BUILD; then
  log "Building analyzer project (required for nuspec reference)"
  dotnet build "$ROOT_DIR/Sdk/Ducky.Sdk.Analyser/Ducky.Sdk.Analyser.csproj" -c "$CONFIGURATION"
  log "Building SDK package project"
  dotnet build "$PKG_PROJ" -c "$CONFIGURATION"
fi

# Optional tests (only if there is a tests project)
if $RUN_TESTS; then
  if [[ -d "$ROOT_DIR/Sdk/Tests" ]] && find "$ROOT_DIR/Sdk/Tests" -type f -name '*.csproj' -print -quit | grep -q .; then
    log "Running tests"
    dotnet test "$ROOT_DIR/Sdk/Tests" -c "$CONFIGURATION" --no-build || warn "Tests failed; continuing"
  else
    log "No test projects detected; skipping"
  fi
fi

# Clean previous same-version package in local feed
TARGET_PKG="${LOCAL_FEED}/${PACKAGE_ID}.${OVERRIDE_VERSION}.nupkg"
if [[ -f "$TARGET_PKG" ]]; then
  log "Removing existing package in local feed: $TARGET_PKG"
  rm -f "$TARGET_PKG"
fi

# Clear caches to avoid stale packages being restored
if $CLEAR_CACHE; then
  # Find global-packages location
  GP_LINE=$(dotnet nuget locals global-packages --list | tr -d '\r' || true)
  GP_DIR=$(printf "%s" "$GP_LINE" | sed -E 's/.*global-packages: *//')
  if [[ -n "$GP_DIR" && -d "$GP_DIR" ]]; then
    PKG_CACHE_DIR="$GP_DIR/$PACKAGE_ID_LOWER/$OVERRIDE_VERSION"
    if [[ -d "$PKG_CACHE_DIR" ]]; then
      log "Clearing global-packages cache for ${PACKAGE_ID} ${OVERRIDE_VERSION}: $PKG_CACHE_DIR"
      rm -rf "$PKG_CACHE_DIR"
    else
      log "No global-packages cache found for ${PACKAGE_ID} ${OVERRIDE_VERSION}"
    fi
  else
    warn "Could not resolve global-packages directory from: $GP_LINE"
  fi

  if $CLEAR_ALL_CACHES; then
    log "Clearing NuGet http-cache and temp caches (may be slow)"
    dotnet nuget locals http-cache --clear || warn "Failed to clear http-cache"
    dotnet nuget locals temp --clear || warn "Failed to clear temp cache"
  fi
fi

# Pack using dotnet with explicit NuspecFile property (this ensures version override is honored)
log "Packing project using nuspec: $NUSPEC_TO_USE"
DOTNET_PACK_CMD=(dotnet pack "$PKG_PROJ" -c "$CONFIGURATION" -o "$LOCAL_FEED" -p:NuspecFile="$NUSPEC_TO_USE" --no-build)
log "Pack command: ${DOTNET_PACK_CMD[*]}"
"${DOTNET_PACK_CMD[@]}" || { err "dotnet pack failed"; exit 1; }

if [[ -f "$TARGET_PKG" ]]; then
  log "Package created: $TARGET_PKG"
else
  # Fallback: list directory content for debugging
  err "Expected package not found: $TARGET_PKG"; ls -l "$LOCAL_FEED"; exit 1
fi

# Clean temp nuspec if created
if [[ -n "$TEMP_NUSPEC" ]]; then
  log "Cleaning temp nuspec"
  rm -f "$TEMP_NUSPEC"
fi

log "Done. To install: dotnet add package ${PACKAGE_ID} --version ${OVERRIDE_VERSION} --source ${LOCAL_FEED}"
