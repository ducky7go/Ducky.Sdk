#!/usr/bin/env bash
set -euo pipefail

# packToLocal.sh
# Build and pack the Ducky.Sdk NuGet package into the local ./duckylocal source directory.
# Usage:
#   ./scripts/packToLocal.sh [--version x.y.z] [--no-build] [--skip-tests] [--configuration Debug|Release] [--no-clear-cache] [--no-clear-all-caches] [--purge-all-versions] [--max-packages N] [--no-cleanup]
#
# The script will:
#   1. Ensure ./duckylocal exists
#   2. Optionally build analyzer and sdk projects
#   3. Determine package version:
#      - If --version specified: use provided version
#      - If no --version: auto-increment version from nuget.props (format: 0.0.XXXXXX-dev)
#   4. Clean any existing nupkg of same ID+version in duckylocal
#   5. Clear NuGet caches for this package/version (and optionally all caches)
#   6. Pack using dotnet pack (honoring Ducky.Sdk.csproj + (possibly temp) nuspec)
#   7. Verify resulting .nupkg
#   8. Clean up old packages (keep latest N by default)
#
# Notes:
#   - Auto-increment: When no version is specified, reads from nuget.props, increments 6-digit suffix,
#     and updates nuget.props with new version to avoid NuGet cache issues.
#   - Version format: 0.0.XXXXXX-dev where XXXXXX is auto-incremented (100001-999999)
#   - Package cleanup: Automatically keeps latest 20 packages by default, use --max-packages N to change, --no-cleanup to disable
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
CLEAR_CACHE=false
CLEAR_ALL_CACHES=true
PURGE_ALL_VERSIONS=false
MAX_PACKAGES_TO_KEEP=20
PROPS_FILE="${ROOT_DIR}/nuget.props"

function log() { printf "[packToLocal] %s\n" "$*"; }
function warn() { printf "[packToLocal][WARN] %s\n" "$*"; }
function err() { printf "[packToLocal][ERROR] %s\n" "$*" >&2; }

# Version management functions
function read_current_version() {
  if [[ -f "$PROPS_FILE" ]]; then
    grep -Po '(?<=<LocalNuGetVersion>)[^<]+' "$PROPS_FILE" | head -1 || true
  else
    echo ""
  fi
}

function increment_version() {
  local current_version="$1"
  if [[ ! "$current_version" =~ ^0\.0\.([0-9]{6})-dev$ ]]; then
    err "Invalid version format: $current_version. Expected format: 0.0.XXXXXX-dev"
    exit 1
  fi

  local current_num="${BASH_REMATCH[1]}"
  local next_num=$((10#$current_num + 1))

  if [[ $next_num -gt 999999 ]]; then
    err "Version number exceeded maximum 999999. Consider resetting to 000001."
    exit 1
  fi

  printf "0.0.%06d-dev" $next_num
}

function update_props_file() {
  local new_version="$1"
  local temp_file="${PROPS_FILE}.tmp"

  # Create props file if it doesn't exist
  if [[ ! -f "$PROPS_FILE" ]]; then
    cat > "$PROPS_FILE" << 'EOF'
<Project>
  <PropertyGroup>
    <LocalNuGetVersion>0.0.100001-dev</LocalNuGetVersion>
  </PropertyGroup>
</Project>
EOF
  fi

  # Update the version in the props file
  sed -E "s/<LocalNuGetVersion>[^<]+<\/LocalNuGetVersion>/<LocalNuGetVersion>${new_version}<\/LocalNuGetVersion>/" "$PROPS_FILE" > "$temp_file"
  mv "$temp_file" "$PROPS_FILE"
  log "Updated $PROPS_FILE with version: $new_version"
}

function cleanup_old_packages() {
  if [[ ! -d "$LOCAL_FEED" ]]; then
    return 0
  fi

  # Count current .nupkg files (excluding placeholder.txt)
  local package_count=$(find "$LOCAL_FEED" -name "*.nupkg" -type f | grep -v placeholder | wc -l)

  if [[ $package_count -le $MAX_PACKAGES_TO_KEEP ]]; then
    return 0
  fi

  log "Cleaning up old packages (keeping latest $MAX_PACKAGES_TO_KEEP)"

  # List packages by modification time (newest first), keep only the newest N
  local packages_to_remove=$(find "$LOCAL_FEED" -name "*.nupkg" -type f -printf "%T@ %p\n" | grep -v placeholder | sort -n | head -n -$MAX_PACKAGES_TO_KEEP | cut -d' ' -f2-)

  if [[ -n "$packages_to_remove" ]]; then
    while IFS= read -r pkg; do
      if [[ -f "$pkg" ]]; then
        log "Removing old package: $(basename "$pkg")"
        rm -f "$pkg"
      fi
    done <<< "$packages_to_remove"

    local remaining_count=$(find "$LOCAL_FEED" -name "*.nupkg" -type f | grep -v placeholder | wc -l)
    log "Cleanup complete. $remaining_count packages remaining."
  fi
}

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
    --no-clear-all-caches)
      CLEAR_ALL_CACHES=false; shift;;
    --purge-all-versions)
      PURGE_ALL_VERSIONS=true; shift;;
    --max-packages)
      MAX_PACKAGES_TO_KEEP="$2"; shift 2;;
    --no-cleanup)
      MAX_PACKAGES_TO_KEEP=0; shift;;
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

# If no override provided, use auto-increment from nuget.props
if [[ -z "$OVERRIDE_VERSION" ]]; then
  CURRENT_VERSION=$(read_current_version)
  if [[ -z "$CURRENT_VERSION" ]]; then
    # No props file or version, create initial version
    OVERRIDE_VERSION="0.0.100001-dev"
    update_props_file "$OVERRIDE_VERSION"
    log "Created initial version: $OVERRIDE_VERSION"
  else
    # Increment existing version
    OVERRIDE_VERSION=$(increment_version "$CURRENT_VERSION")
    update_props_file "$OVERRIDE_VERSION"
    log "Auto-incremented version: $CURRENT_VERSION → $OVERRIDE_VERSION"
  fi
  AUTO_INCREMENTED=true
else
  AUTO_INCREMENTED=false
fi

log "Original nuspec version: $ORIG_VERSION"
log "Requested (effective) version: $OVERRIDE_VERSION"
if [[ "$AUTO_INCREMENTED" == "true" ]]; then
  log "Version auto-incremented from nuget.props"
fi

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
  TESTS_DIR="$ROOT_DIR/Sdk/Tests"
  if [[ -d "$TESTS_DIR" ]]; then
    # Find test project files
    TEST_PROJECTS=$(find "$TESTS_DIR" -type f -name '*.csproj' 2>/dev/null || true)
    if [[ -n "$TEST_PROJECTS" ]]; then
      log "Running tests"
      # Test each project individually to avoid MSB1003 error
      while IFS= read -r proj; do
        log "Testing: $proj"
        dotnet test "$proj" -c "$CONFIGURATION" --no-build || warn "Tests failed for $proj; continuing"
      done <<< "$TEST_PROJECTS"
    else
      log "No test projects detected; skipping"
    fi
  else
    log "Tests directory not found; skipping"
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
  GP_LINE=$(dotnet nuget locals global-packages --list | tr -d '\r' || true)
  GP_DIR=$(printf "%s" "$GP_LINE" | sed -E 's/.*global-packages: *//')
  if [[ -n "$GP_DIR" && -d "$GP_DIR" ]]; then
    VERSION_DIR="$GP_DIR/$PACKAGE_ID_LOWER/$OVERRIDE_VERSION"
    if [[ -d "$VERSION_DIR" ]]; then
      log "Removing version-specific cache: $VERSION_DIR"
      rm -rf "$VERSION_DIR"
    else
      log "No version-specific global-packages cache for ${PACKAGE_ID} ${OVERRIDE_VERSION}"
    fi
    if $PURGE_ALL_VERSIONS; then
      ALL_DIR="$GP_DIR/$PACKAGE_ID_LOWER"
      if [[ -d "$ALL_DIR" ]]; then
        log "Purging ALL cached versions of ${PACKAGE_ID}: $ALL_DIR"
        rm -rf "$ALL_DIR"
      fi
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
  # Check if package was created with normalized version (NuGet removes leading zeros)
  NORMALIZED_VERSION=$(echo "$OVERRIDE_VERSION" | sed -E 's/\.0*([0-9]{1,6}-dev)$/.\1/' | sed -E 's/^0\.0\./0.0./' | sed -E 's/\.0*([0-9]+)$/.\1/')
  NORMALIZED_TARGET_PKG="${LOCAL_FEED}/${PACKAGE_ID}.${NORMALIZED_VERSION}.nupkg"
  if [[ -f "$NORMALIZED_TARGET_PKG" ]]; then
    log "Package created with normalized version: $NORMALIZED_TARGET_PKG"
    log "Note: NuGet normalized version '$OVERRIDE_VERSION' to '$NORMALIZED_VERSION'"
  else
    # Fallback: try to find any package with our version pattern
    FOUND_PKG=$(find "$LOCAL_FEED" -name "${PACKAGE_ID}.${OVERRIDE_VERSION%.dev}-dev.nupkg" -o -name "${PACKAGE_ID}.*-dev.nupkg" | head -1 || true)
    if [[ -n "$FOUND_PKG" && -f "$FOUND_PKG" ]]; then
      log "Package found: $FOUND_PKG"
    else
      err "Expected package not found: $TARGET_PKG"; ls -l "$LOCAL_FEED"; exit 1
    fi
  fi
fi

# Clean temp nuspec if created
if [[ -n "$TEMP_NUSPEC" ]]; then
  log "Cleaning temp nuspec"
  rm -f "$TEMP_NUSPEC"
fi

# Clean up old packages if enabled
if [[ $MAX_PACKAGES_TO_KEEP -gt 0 ]]; then
  cleanup_old_packages
fi

log "Done. To install: dotnet add package ${PACKAGE_ID} --version ${OVERRIDE_VERSION} --source ${LOCAL_FEED}"
