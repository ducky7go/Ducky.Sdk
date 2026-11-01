#!/usr/bin/env bash
# Run hot reload in the real game on the remote Windows machine.
# Reuses the single steps of Newbe.Dod's remote-test.sh (deploy/enable/preset-save/launch/stop/restore-save).
# Refuses to run while ANY Duckov process exists on the remote machine: that game may be somebody's debugging session.
#
#   ./run-remote.sh            experiment mode: shim + Core spike (A; reloads B C unique names; D E same name, E must NOT take effect; F G H unique again)
#   ./run-remote.sh sample     sample mode (tasks 6.2, 6.3): Samples/Ducky.HotReloadSample built by the SDK in Debug (dev hot reload),
#                              deployed as the real mod; 8 consecutive unique-name reloads, then a same-name Core that must be refused.
#                              PASS only if every after-unload line is zero and every after-load line is exactly one of each.

# Scenario checks (one grep per line of Player.log, see Probe/Report in Ducky.HotReload/ModBehaviour.cs):
#   after every unload: marker=none pingHits=0 tickHandlers=0 coreRunners=0   (Core cleaned up completely)
#   after every load:   marker=<new> pingHits=1 tickHandlers=1 coreRunners=1   (new code live, nothing doubled)
set -euo pipefail
cd "$(dirname "$0")"

MODE=${1:-experiment}
case "$MODE" in experiment|sample) ;; *) echo "usage: run-remote.sh [experiment|sample]" >&2; exit 2 ;; esac

export WINHOST_PY=${WINHOST_PY:-/home/newbe36524/repos/hagi/hagicode-mono/repos/personal-agent-skills/windows-host/scripts/windows_host.py}
export MOD_NAME=$([ "$MODE" = sample ] && echo Ducky.HotReloadSample || echo Ducky.HotReload)
export TEST_BRIDGE=0
R=${REMOTE_TEST:-../../../Newbe.Dod/scripts/remote-test/remote-test.sh}
CACHE=${REMOTE_TEST_CACHE:-$HOME/.cache/newbe-dod-remote-test}
STAGE=$CACHE/stage/$MOD_NAME
ART=${ARTIFACT_DIR:-$CACHE/artifacts/hotreload-$MODE-$(date +%Y%m%d-%H%M%S)}
export ARTIFACT_DIR=$ART
GAME=${REMOTE_GAME_DIR:-C:/Program Files (x86)/Steam/steamapps/common/Escape from Duckov}
MODDIR="$GAME/Duckov_Data/Mods/$MOD_NAME"
mkdir -p "$ART"

win() { python3 "$WINHOST_PY" "$@"; }
log() { echo "[hotreload] $*"; }
fail=0
check() { # check <description> <grep -E pattern>
  if grep -qE "$2" "$ART/Player.log"; then log "ok   $1"; else log "FAIL $1   (wanted /$2/)"; fail=1; fi
}

state=$(win run 'if (Get-Process Duckov -ErrorAction SilentlyContinue) { "RUNNING" } else { "FREE" }' | tr -d '\r\n')
[ "$state" = FREE ] || { echo "remote game is running; not touching it (someone may be using it)"; exit 2; }

REMOTE_HOME=$(win run '$env:USERPROFILE' | tr -d '\r' | sed 's#\\#/#g' | tail -n1)
W=$REMOTE_HOME/duckov-test

fetch_log() {
  win run "powershell -NoProfile -ExecutionPolicy Bypass -File '$W/ps/snapshot-log.ps1' -Out '$W/hotreload.snapshot.log'" >/dev/null
  win copy-from "$W/hotreload.snapshot.log" "$ART/Player.log" >/dev/null
}
wait_for() { # wait_for <pattern> [seconds]
  local end=$((SECONDS + ${2:-60}))
  while [ $SECONDS -lt $end ]; do fetch_log 2>/dev/null && grep -qE "$1" "$ART/Player.log" && return 0; sleep 3; done
  fetch_log || true; return 1
}

cleanup() {
  log "cleanup: stop game, restore save, remove test mod"
  "$R" stop || true
  "$R" restore-save || true
  win run "if (Test-Path '$MODDIR') { Remove-Item -Recurse -Force '$MODDIR' }" >/dev/null || true
}
trap cleanup EXIT

if [ "$MODE" = sample ]; then
  # Sample mode: the SDK builds the sample (shim, Harmony, marker and one stamped Core) into Publish/; that folder is the mod.
  SAMPLE_DIR=../../Samples/Ducky.HotReloadSample
  PUB=$SAMPLE_DIR/Publish/$MOD_NAME
  CONFIG='$env:USERPROFILE\AppData\LocalLow\TeamSoda\Duckov\Ducky\ModsLocalConfig.json'
  # the sample writes BuffIdRegion entries to the shared config: keep a copy and put it back afterwards
  win run "if (Test-Path \"$CONFIG\") { Copy-Item -Force \"$CONFIG\" \"$CONFIG.hotreload-backup\" }" >/dev/null || true
  restore_config() {
    win run "if (Test-Path \"$CONFIG.hotreload-backup\") { Move-Item -Force \"$CONFIG.hotreload-backup\" \"$CONFIG\" }" >/dev/null || true
  }
  trap 'cleanup; restore_config' EXIT

  build_sample() { (cd "$SAMPLE_DIR" && CI=true dotnet build Ducky.HotReloadSample.csproj -c Debug -v q -nologo >/dev/null); }
  newest_core() { ls -t "$PUB"/$MOD_NAME.Core.*.dll | head -1; }
  core_name() { basename "$1" .dll; }
  # line of a REPORT stage, as a regex prefix; $1 = load number, $2 = stage
  report_wanted_after_load() { echo "REPORT load=$1 stage=after-load core=$2 coreAssembliesInDomain=[0-9]+ childObjects=1 hostComponents=1 buffs=1 samplePatches=1 sampleSubscriptions=1 sampleUi=1 sampleHolders=1 sampleLocalization=sample $2\s*\$"; }
  report_wanted_after_unload() { echo "REPORT load=$1 stage=after-unload core=$2 coreAssembliesInDomain=[0-9]+ childObjects=0 hostComponents=0 buffs=0 samplePatches=0 sampleSubscriptions=0 sampleUi=0 sampleHolders=0 sampleLocalization=none"; }

  log "build the sample (SDK dev build) and stage the whole Publish folder"
  build_sample
  rm -rf "$STAGE"; mkdir -p "$STAGE"; cp -r "$PUB/." "$STAGE/"
  "$R" deploy
  "$R" enable
  "$R" preset-save
  "$R" launch
  log "waiting for the first load"
  wait_for "REPORT load=1 stage=after-load" 150 || { log "FAIL sample never reported a first load"; fail=1; }
  first=$(core_name "$(newest_core)")
  check "load 1: one child, one host, one buff, one patch, one subscription, localized text of this Core" "$(report_wanted_after_load 1 "$first")"

  n=1
  for i in 1 2 3 4 5 6 7 8; do
    previous=$(core_name "$(newest_core)")
    build_sample; sleep 1
    f=$(newest_core); name=$(core_name "$f")
    win copy-to "$f" "$MODDIR/" >/dev/null
    n=$((n + 1))
    log "reload $n: $name"
    if wait_for "REPORT load=$n stage=after-load" 90; then
      check "reload $n: after-unload of $previous is clean" "$(report_wanted_after_unload $((n - 1)) "$previous")"
      check "reload $n: after-load of $name is exactly one of each" "$(report_wanted_after_load "$n" "$name")"
    else
      log "FAIL reload $n never completed"; fail=1
    fi
  done

  # 6.3: a Core whose assembly name is already loaded must be refused, and the old code must keep running
  log "same-name Core (must be refused)"
  same=$(newest_core); same_name=$(core_name "$same")
  win copy-to "$same" "$MODDIR/" >/dev/null
  if wait_for "already loaded in this session" 60; then log "ok   same-name rejection logged for $same_name"; else log "FAIL same-name rejection not logged"; fail=1; fi
  if grep -qE "REPORT load=$((n + 1)) stage=" "$ART/Player.log"; then log "FAIL same-name file was reloaded"; fail=1; else log "ok   same-name file was not reloaded"; fi

  grep -E '\[HotReload|REPORT|HotReloadSample|ModBehaviourBase|Exception' "$ART/Player.log" > "$ART/sample-lines.log" || true
  log "sample lines: $ART/sample-lines.log"
  if [ $fail -eq 0 ]; then log "PASS"; exit 0; else log "FAILED"; exit 1; fi
fi

log "build A (unique name) and stage"
./build.sh A --stamp
rm -rf "$STAGE"; mkdir -p "$STAGE"; cp -r out/Ducky.HotReload/. "$STAGE/"
"$R" deploy
"$R" enable
"$R" preset-save
"$R" launch
log "waiting for the first load"
wait_for 'REPORT load=1 stage=after-load' 150 || { log "FAIL shim never reported a first load"; fail=1; }
check "load 1: marker A, one patch, one handler, one runner" 'REPORT load=1 stage=after-load marker=A pingHits=1 tickHandlers=1 coreRunners=1'

n=1
reload() { # reload <marker> <--stamp|--same> [expect-stale-marker]
  local marker=$1 mode=$2 stale=${3:-}; n=$((n + 1))
  ./build.sh "$marker" "$([ "$mode" = --stamp ] && echo --stamp)"
  win copy-to out/core-update/*.dll "$MODDIR/" >/dev/null
  log "copied Core marker=$marker ($mode), waiting for reload $n"
  if wait_for "REPORT load=$n stage=after-load" 60; then
    check "reload $n unloaded cleanly" "REPORT load=$((n - 1)) stage=after-unload marker=none pingHits=0 tickHandlers=0 coreRunners=0"
    if [ -n "$stale" ]; then
      # Known Mono behaviour (found by this experiment): a second load of a same-named assembly yields a new Assembly object
      # but type lookup still returns the FIRST same-named assembly, so the OLD code runs.
      check "reload $n same name: old code (marker $stale) still runs, not $marker" "REPORT load=$n stage=after-load marker=$stale pingHits=1 tickHandlers=1 coreRunners=1"
    else
      check "reload $n: marker $marker live, one patch, one handler, one runner" "REPORT load=$n stage=after-load marker=$marker pingHits=1 tickHandlers=1 coreRunners=1"
    fi
  else
    log "FAIL reload $n never completed"; fail=1
  fi
}
reload B --stamp
reload C --stamp
reload D --same            # plain name Ducky.HotReload.Core.dll, first time: works
reload E --same D          # same plain name again: expected to keep running D
reload F --stamp           # unique names keep working after a same-name load
reload G --stamp
reload H --stamp

grep -E '\[HotReload' "$ART/Player.log" > "$ART/hotreload-lines.log" || true
log "same-name finding:"; grep -E "sameAssemblyObjectAsEarlierLoad" "$ART/Player.log" | sed 's/^/    /' || true
log "exceptions mentioning HotReload:"; grep -nE "Exception" "$ART/Player.log" | grep -i hotreload | head -5 | sed 's/^/    /' || true
log "artifacts: $ART"
[ $fail -eq 0 ] && log "PASS" || { log "FAILED"; exit 1; }
