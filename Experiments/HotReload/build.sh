#!/usr/bin/env bash
# Build the shim + Core and stage a deployable mod folder.
#   ./build.sh <marker> [--stamp]
# Output: out/Ducky.HotReload/        full mod folder (shim, 0Harmony, info.ini, hotreload.enabled, one Core dll)
#         out/core-update/            only the newest Core dll, to copy over a running game's mod folder
# --stamp makes the Core assembly name unique per build (Ducky.HotReload.Core.<stamp>.dll).
set -euo pipefail
cd "$(dirname "$0")"
MARKER="${1:?usage: build.sh <marker> [--stamp]}"
STAMP=""
[[ "${2:-}" == "--stamp" ]] && STAMP=".$(date +%s%N | cut -c1-13)"

dotnet build Ducky.HotReload.Core/Ducky.HotReload.Core.csproj -c Release -v q -nologo \
  -p:Marker="$MARKER" -p:CoreStamp="$STAMP" >/dev/null

MOD=out/Ducky.HotReload
mkdir -p "$MOD" out/core-update
rm -f out/core-update/*.dll "$MOD"/Ducky.HotReload.Core*.dll
cp Ducky.HotReload/bin/Release/netstandard2.1/Ducky.HotReload.dll "$MOD/"
cp ../../Sdk/SDKlibs/Ducky.Sdk/0Harmony.dll "$MOD/"
CORE="Ducky.HotReload.Core/bin/Release/netstandard2.1/Ducky.HotReload.Core${STAMP}.dll"
cp "$CORE" "$MOD/"
cp "$CORE" out/core-update/
printf 'name = Ducky.HotReload\ndisplayName = Ducky.HotReload\ndescription = Hot reload experiment\nversion = 0.0.1\ntags = \n' > "$MOD/info.ini"
: > "$MOD/hotreload.enabled"
echo "staged $MOD with $(basename "$CORE") marker=$MARKER"
