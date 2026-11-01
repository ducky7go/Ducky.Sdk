#!/usr/bin/env bash
# Task 1.1 probe in the real game: a ModBehaviourBase Core (buff, localized string, MessageHub host) reloaded twice.
#   ./run-probe.sh
# Load A (plain) -> reload B (plain): what survives an unload  -> reload C with probe-remove.enabled in the mod folder:
# the shim removes the probe registrations through game APIs after the unload, then loads C.
# Exit code 2: a Duckov process is already running on the remote machine (not touched).
set -euo pipefail
cd "$(dirname "$0")"

export WINHOST_PY=${WINHOST_PY:-/home/newbe36524/repos/hagi/hagicode-mono/repos/personal-agent-skills/windows-host/scripts/windows_host.py}
export MOD_NAME=Ducky.HotReload
export TEST_BRIDGE=0
R=${REMOTE_TEST:-../../../Newbe.Dod/scripts/remote-test/remote-test.sh}
CACHE=${REMOTE_TEST_CACHE:-$HOME/.cache/newbe-dod-remote-test}
STAGE=$CACHE/stage/$MOD_NAME
ART=${ARTIFACT_DIR:-$CACHE/artifacts/probe-$(date +%Y%m%d-%H%M%S)}
export ARTIFACT_DIR=$ART
GAME=${REMOTE_GAME_DIR:-C:/Program Files (x86)/Steam/steamapps/common/Escape from Duckov}
MODDIR="$GAME/Duckov_Data/Mods/$MOD_NAME"
mkdir -p "$ART"

win() { python3 "$WINHOST_PY" "$@"; }
log() { echo "[probe] $*"; }
fail=0
check() { # check <description> <grep -E pattern>
  if grep -qE "$2" "$ART/Player.log"; then log "ok   $1"; else log "FAIL $1   (wanted /$2/)"; fail=1; fi
}

state=$(win run 'if (Get-Process Duckov -ErrorAction SilentlyContinue) { "RUNNING" } else { "FREE" }' | tr -d '\r\n')
[ "$state" = FREE ] || { echo "remote game is running; not touching it (someone may be using it)"; exit 2; }

REMOTE_HOME=$(win run '$env:USERPROFILE' | tr -d '\r' | sed 's#\\#/#g' | tail -n1)
W=$REMOTE_HOME/duckov-test

fetch_log() {
  win run "powershell -NoProfile -ExecutionPolicy Bypass -File '$W/ps/snapshot-log.ps1' -Out '$W/probe.snapshot.log'" >/dev/null
  win copy-from "$W/probe.snapshot.log" "$ART/Player.log" >/dev/null
}
wait_for() { # wait_for <pattern> [seconds]
  local end=$((SECONDS + ${2:-60}))
  while [ $SECONDS -lt $end ]; do fetch_log 2>/dev/null && grep -qE "$1" "$ART/Player.log" && return 0; sleep 3; done
  fetch_log || true; return 1
}

CONFIG='$env:USERPROFILE\AppData\LocalLow\TeamSoda\Duckov\Ducky\ModsLocalConfig.json'
cleanup() {
  log "cleanup: stop game, restore save, remove test mod, restore the shared mod config"
  "$R" stop || true
  "$R" restore-save || true
  win run "if (Test-Path '$MODDIR') { Remove-Item -Recurse -Force '$MODDIR' }" >/dev/null || true
  win run "if (Test-Path \"$CONFIG.probe-backup\") { Move-Item -Force \"$CONFIG.probe-backup\" \"$CONFIG\" }" >/dev/null || true
}
trap cleanup EXIT

# the probe writes BuffIdRegion entries to the shared config: keep a copy and put it back afterwards
win run "if (Test-Path \"$CONFIG\") { Copy-Item -Force \"$CONFIG\" \"$CONFIG.probe-backup\" }" >/dev/null || true
log "build A (plain name) and stage"
./build-base.sh A
rm -rf "$STAGE"; mkdir -p "$STAGE"; cp -r out-base/Ducky.HotReload/. "$STAGE/"
"$R" deploy
"$R" enable
"$R" preset-save
"$R" launch
log "waiting for the first load"
wait_for 'REPORT load=1 stage=after-load' 150 || { log "FAIL probe never reported a first load"; fail=1; }

log "reload B (unique name, plain)"
./build-base.sh B --stamp
win copy-to out-base/core-update/*.dll "$MODDIR/" >/dev/null
wait_for 'REPORT load=2 stage=after-load' 90 || { log "FAIL reload B never completed"; fail=1; }

log "reload C with removal through game APIs (marker file probe-remove.enabled)"
win run "New-Item -ItemType File -Force -Path '$MODDIR/probe-remove.enabled' | Out-Null" >/dev/null
./build-base.sh C --stamp
win copy-to out-base/core-update/*.dll "$MODDIR/" >/dev/null
wait_for 'REPORT load=3 stage=after-load' 90 || { log "FAIL reload C never completed"; fail=1; }

grep -E '\[HotReload|REPORT|probe removal' "$ART/Player.log" > "$ART/probe-lines.log" || true
log "probe lines saved to $ART/probe-lines.log"
[ $fail -eq 0 ] && log "PROBE RAN (read the lines, the pass criteria are in the README)" || { log "FAILED"; exit 1; }
