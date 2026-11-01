#!/usr/bin/env bash
# Hot reload check of one Samples/ mod in the real game (remote Windows machine), generic over samples.
#   ./run-remote-sample.sh <Ducky.SampleName> [reloads=3]
# Builds the sample in Debug (DuckyHotReload is on by default in the sample csproj), deploys its folder, enables it,
# launches the game, waits for the first load, then reloads it with unique-name Cores.
# Checks per load/unload (the shim's REPORT lines, see docs/hot-reload.md):
#   after-load:   childObjects=1 hostComponents=1 buffs=<1 if the sample registers a buff, else 0>
#   after-unload: childObjects=0 hostComponents=0 buffs=0
# Plus: no exception or "Error enabling" line that mentions the sample, ModBehaviourBase or HotReload.
# Exit code 2: a Duckov process is already running on the remote machine (not touched).
set -euo pipefail
cd "$(dirname "$0")"

SAMPLE=${1:?usage: run-remote-sample.sh <Ducky.SampleName> [reloads]}
RELOADS=${2:-3}
export WINHOST_PY=${WINHOST_PY:-/home/newbe36524/repos/hagi/hagicode-mono/repos/personal-agent-skills/windows-host/scripts/windows_host.py}
export MOD_NAME=$SAMPLE
export TEST_BRIDGE=0
R=${REMOTE_TEST:-../../../Newbe.Dod/scripts/remote-test/remote-test.sh}
CACHE=${REMOTE_TEST_CACHE:-$HOME/.cache/newbe-dod-remote-test}
STAGE=$CACHE/stage/$MOD_NAME
ART=${ARTIFACT_DIR:-$CACHE/artifacts/sample-$SAMPLE-$(date +%Y%m%d-%H%M%S)}
export ARTIFACT_DIR=$ART
GAME=${REMOTE_GAME_DIR:-C:/Program Files (x86)/Steam/steamapps/common/Escape from Duckov}
MODDIR="$GAME/Duckov_Data/Mods/$MOD_NAME"
CONFIG='$env:USERPROFILE\AppData\LocalLow\TeamSoda\Duckov\Ducky\ModsLocalConfig.json'
mkdir -p "$ART"

win() { python3 "$WINHOST_PY" "$@"; }
log() { echo "[$SAMPLE] $*"; }
fail=0
check() { # check <description> <grep -E pattern>
  if grep -qE "$2" "$ART/Player.log"; then log "ok   $1"; else log "FAIL $1   (wanted /$2/)"; fail=1; fi
}

state=$(win run 'if (Get-Process Duckov -ErrorAction SilentlyContinue) { "RUNNING" } else { "FREE" }' | tr -d '\r\n')
[ "$state" = FREE ] || { echo "remote game is running; not touching it (someone may be using it)"; exit 2; }

REMOTE_HOME=$(win run '$env:USERPROFILE' | tr -d '\r' | sed 's#\\#/#g' | tail -n1)
W=$REMOTE_HOME/duckov-test

fetch_log() {
  win run "powershell -NoProfile -ExecutionPolicy Bypass -File '$W/ps/snapshot-log.ps1' -Out '$W/sample.snapshot.log'" >/dev/null
  win copy-from "$W/sample.snapshot.log" "$ART/Player.log" >/dev/null
}
wait_for() { # wait_for <pattern> [seconds]
  local end=$((SECONDS + ${2:-60}))
  while [ $SECONDS -lt $end ]; do fetch_log 2>/dev/null && grep -qE "$1" "$ART/Player.log" && return 0; sleep 3; done
  fetch_log || true; return 1
}

cleanup() {
  log "cleanup: stop game, restore save, remove test mod, restore the shared mod config"
  "$R" stop || true
  "$R" restore-save || true
  win run "if (Test-Path '$MODDIR') { Remove-Item -Recurse -Force '$MODDIR' }" >/dev/null || true
  win run "if (Test-Path \"$CONFIG.sample-backup\") { Move-Item -Force \"$CONFIG.sample-backup\" \"$CONFIG\" }" >/dev/null || true
}
win run "if (Test-Path \"$CONFIG\") { Copy-Item -Force \"$CONFIG\" \"$CONFIG.sample-backup\" }" >/dev/null || true
trap cleanup EXIT

SAMPLE_DIR=../../Samples/$SAMPLE
build_sample() { (cd "$SAMPLE_DIR" && CI=true dotnet build "$SAMPLE.csproj" -c Debug -v q -nologo >/dev/null); }
# the deploy folder is the sample's Publish/ (ModsDirectory set) or the repo's artifacts/Mods (CI default)
deploy_dir() {
  for d in "$SAMPLE_DIR/Publish/$SAMPLE" "../../artifacts/Mods/$SAMPLE"; do
    [ -f "$d/$SAMPLE.dll" ] && { echo "$d"; return; }
  done
  echo "no deploy folder with $SAMPLE.dll" >&2; return 1
}
newest_core() { ls -t "$PUB"/$SAMPLE.Core.*.dll | head -1; }
core_name() { basename "$1" .dll; }

BUFFS=0
[ "$SAMPLE" = Ducky.TryHarmony ] && BUFFS=1   # registers a buff in ModEnabled

log "build (Debug, hot reload on by default) and stage"
build_sample
PUB=$(deploy_dir)
rm -rf "$STAGE"; mkdir -p "$STAGE"; cp -r "$PUB/." "$STAGE/"
log "staged $PUB ($(ls "$PUB" | wc -l) files)"
"$R" deploy
"$R" enable
"$R" preset-save
"$R" launch
log "waiting for the first load"
if ! wait_for "REPORT load=1 stage=after-load" 180; then
  log "FAIL the mod never reported a first load"; fail=1
else
  first=$(core_name "$(newest_core)")
  check "load 1: one child, one host, buffs=$BUFFS, Core $first" "REPORT load=1 stage=after-load core=$first .*childObjects=1 hostComponents=1 buffs=$BUFFS\\s*\$"
fi

n=1
for i in $(seq 1 "$RELOADS"); do
  previous=$(core_name "$(newest_core)")
  build_sample; sleep 1
  f=$(newest_core); name=$(core_name "$f")
  win copy-to "$f" "$MODDIR/" >/dev/null
  n=$((n + 1))
  if wait_for "REPORT load=$n stage=after-load" 90; then
    check "reload $n: after-unload of $previous is clean" "REPORT load=$((n - 1)) stage=after-unload core=$previous .*childObjects=0 hostComponents=0 buffs=0\\s*\$"
    check "reload $n: after-load of $name is one of each" "REPORT load=$n stage=after-load core=$name .*childObjects=1 hostComponents=1 buffs=$BUFFS\\s*\$"
  else
    log "FAIL reload $n never completed"; fail=1
  fi
done

# an exception line has no mod name, so look at the lines after it: the stack trace names the mod
problems=$(python3 - "$ART/Player.log" "$SAMPLE" <<'PY'
import sys
lines=open(sys.argv[1],encoding='utf-8',errors='replace').read().replace('\r','').split('\n')
out=set()
for i,l in enumerate(lines):
    if 'Exception' in l or 'Error enabling' in l:
        block=lines[i:i+6]
        if any(sys.argv[2] in b or 'ModBehaviourBase' in b or 'HotReload' in b for b in block):
            out.add(l.strip())
print('\n'.join(sorted(out)))
PY
)
if [ -n "$problems" ]; then log "exceptions mentioning the sample:"; echo "$problems" | sed 's/^/    /'; fail=1; else log "ok   no exceptions mentioning the sample"; fi
grep -E '\[HotReload|REPORT|'"$SAMPLE"'|ModBehaviourBase' "$ART/Player.log" > "$ART/sample-lines.log" || true
log "lines: $ART/sample-lines.log"
if [ $fail -eq 0 ]; then log "PASS"; exit 0; else log "FAILED"; exit 1; fi
