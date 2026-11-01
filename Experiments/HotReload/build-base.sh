#!/usr/bin/env bash
# Task 1.1 probe build: shim + Core that derives from ModBehaviourBase (SDK Lib sources compiled in).
#   ./build-base.sh <marker> [--stamp]
# Output: out-base/Ducky.HotReload/ (full mod folder), out-base/core-update/ (newest Core dll only)
set -euo pipefail
cd "$(dirname "$0")"
MARKER="${1:?usage: build-base.sh <marker> [--stamp]}"
STAMP=""
[[ "${2:-}" == "--stamp" ]] && STAMP=".$(date +%Y%m%d%H%M%S%3N)" || true

dotnet build Ducky.HotReload.BaseCore/Ducky.HotReload.BaseCore.csproj -c Release -v q -nologo \
  -p:Marker="$MARKER" -p:CoreStamp="$STAMP" >/dev/null

MOD=out-base/Ducky.HotReload
mkdir -p "$MOD" out-base/core-update
rm -f out-base/core-update/*.dll "$MOD"/Ducky.HotReload.Core*.dll
cp Ducky.HotReload/bin/Release/netstandard2.1/Ducky.HotReload.dll "$MOD/"
cp ../../Sdk/SDKlibs/Ducky.Sdk/0Harmony.dll "$MOD/"
CORE="Ducky.HotReload.BaseCore/bin/Release/netstandard2.1/Ducky.HotReload.Core${STAMP}.dll"
cp "$CORE" "$MOD/"
cp "$CORE" out-base/core-update/
printf 'name = Ducky.HotReload\ndisplayName = Ducky.HotReload\ndescription = Hot reload probe\nversion = 0.0.1\ntags = \n' > "$MOD/info.ini"
: > "$MOD/hotreload.enabled"
echo "staged $MOD with $(basename "$CORE") marker=$MARKER"
