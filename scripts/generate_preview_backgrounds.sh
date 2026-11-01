#!/usr/bin/env bash
# Regenerates the ten preview backgrounds (Sdk/Ducky.Sdk.Build/Assets/PreviewBackgrounds/background-00.png .. background-09.png)
# with the local ComfyUI installation.
#
# Maintainer tool only: mod builds never run this script and never call ComfyUI.
# All ten images are generated into a temporary directory first; the committed files are replaced only after every
# job has succeeded, so a failed run changes nothing.
#
# Usage: scripts/generate_preview_backgrounds.sh
# Environment:
#   COMFYUI_URL   ComfyUI server (default http://127.0.0.1:8188)
#   JOB_TIMEOUT   seconds to wait for each generation (default 1200)
# Requires: bash, curl, python3.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
WORKFLOW="$SCRIPT_DIR/comfyui/preview-background.workflow.json"
OUT_DIR="$REPO_ROOT/Sdk/Ducky.Sdk.Build/Assets/PreviewBackgrounds"
COMFYUI_URL="${COMFYUI_URL:-http://127.0.0.1:8188}"
JOB_TIMEOUT="${JOB_TIMEOUT:-1200}"
COUNT=10
SEED_BASE=20261001
MODEL_FILE="anima-turbo-v1.1.safetensors"

die() {
    echo "error: $*" >&2
    exit 1
}

command -v curl >/dev/null 2>&1 || die "curl is required"
command -v python3 >/dev/null 2>&1 || die "python3 is required"
[ -f "$WORKFLOW" ] || die "workflow not found: $WORKFLOW"

# 1. ComfyUI must be reachable before anything else happens.
if ! curl -fsS --max-time 5 "$COMFYUI_URL/system_stats" -o /dev/null 2>/dev/null; then
    die "ComfyUI is not reachable at $COMFYUI_URL. Start ComfyUI (or set COMFYUI_URL) and run this script again. No files were changed."
fi
COMFYUI_VERSION="$(curl -fsS --max-time 5 "$COMFYUI_URL/system_stats" | python3 -I -c 'import json,sys; print(json.load(sys.stdin)["system"].get("comfyui_version","unknown"))')"
echo "ComfyUI $COMFYUI_VERSION at $COMFYUI_URL"

TMP="$(mktemp -d "${TMPDIR:-/tmp}/ducky-preview-backgrounds.XXXXXX")"
trap 'rm -rf "$TMP"' EXIT

# Builds the API job for one background: the workflow with its seed set.
make_job() {
    python3 -I - "$WORKFLOW" "$1" "$2" <<'PY'
import json, sys
workflow_path, seed, out_path = sys.argv[1], int(sys.argv[2]), sys.argv[3]
with open(workflow_path, encoding="utf-8") as f:
    workflow = json.load(f)
workflow["7"]["inputs"]["seed"] = seed
with open(out_path, "w", encoding="utf-8") as f:
    json.dump({"prompt": workflow, "client_id": "ducky-preview-backgrounds"}, f)
PY
}

# Prints the prompt_id from a /prompt response, or fails on node errors.
prompt_id_of() {
    python3 -I -c '
import json, sys
d = json.load(sys.stdin)
if d.get("node_errors"):
    sys.exit("node errors: " + json.dumps(d["node_errors"]))
print(d["prompt_id"])
'
}

# Prints "running" or the output file name ("filename<TAB>subfolder") of the history entry; fails on a ComfyUI error.
job_state_of() {
    python3 -I -c '
import json, sys
prompt_id = sys.argv[1]
h = json.load(sys.stdin).get(prompt_id)
if h is None:
    print("running"); sys.exit(0)
status = h.get("status", {})
if status.get("status_str") == "error":
    msgs = [m for m in status.get("messages", []) if m and m[0] == "execution_error"]
    sys.exit("generation failed: " + (msgs[0][1].get("exception_message", "unknown error") if msgs else "unknown error"))
image = h.get("outputs", {}).get("10", {}).get("images", [])
if not image:
    print("running"); sys.exit(0)
print(image[0]["filename"] + "\t" + image[0].get("subfolder", ""))
' "$1"
}

check_png_256() {
    python3 -I - "$1" <<'PY'
import struct, sys
with open(sys.argv[1], "rb") as f:
    head = f.read(24)
if head[:8] != b"\x89PNG\r\n\x1a\n" or head[12:16] != b"IHDR":
    sys.exit("not a PNG: " + sys.argv[1])
w, h = struct.unpack(">II", head[16:24])
if (w, h) != (256, 256):
    sys.exit(f"expected 256x256, got {w}x{h}: {sys.argv[1]}")
PY
}

# 2. Generate all ten into the temporary directory.
for ((index = 0; index < COUNT; index++)); do
    name="$(printf 'background-%02d.png' "$index")"
    seed=$((SEED_BASE + index))
    echo "[$((index + 1))/$COUNT] $name (seed $seed)"

    make_job "$seed" "$TMP/job.json"
    response="$(curl -fsS --max-time 30 -H 'Content-Type: application/json' -d @"$TMP/job.json" "$COMFYUI_URL/prompt")" \
        || die "POST /prompt failed for $name. No committed files were changed."
    prompt_id="$(printf '%s' "$response" | prompt_id_of)" || die "ComfyUI rejected $name: $response"

    deadline=$((SECONDS + JOB_TIMEOUT))
    while :; do
        if [ "$SECONDS" -ge "$deadline" ]; then
            die "timed out after ${JOB_TIMEOUT}s waiting for $name (prompt $prompt_id). No committed files were changed."
        fi
        sleep 2
        history="$(curl -fsS --max-time 10 "$COMFYUI_URL/history/$prompt_id")" || continue
        state="$(printf '%s' "$history" | job_state_of "$prompt_id")" || die "generation failed for $name (prompt $prompt_id). No committed files were changed."
        [ "$state" = "running" ] || break
    done

    IFS=$'\t' read -r file subfolder <<<"$state"
    curl -fsS --max-time 60 -G \
        --data-urlencode "filename=$file" \
        --data-urlencode "subfolder=$subfolder" \
        --data-urlencode "type=output" \
        "$COMFYUI_URL/view" -o "$TMP/$name" || die "download failed for $name"
    check_png_256 "$TMP/$name"
done

# 3. All ten succeeded: replace the committed files, then record the model and seeds next to them.
mkdir -p "$OUT_DIR"
for ((index = 0; index < COUNT; index++)); do
    name="$(printf 'background-%02d.png' "$index")"
    cp "$TMP/$name" "$OUT_DIR/$name"
done

{
    echo "# Preview backgrounds"
    echo
    echo "Ten 256x256 backgrounds for the \`preview.png\` of each SDK-built mod. \`GeneratePreviewLib\` selects one by a SHA-256 hash of the"
    echo "project name, embeds the file as a resource (see \`Ducky.Sdk.Build.csproj\`), and draws the project abbreviation over it."
    echo
    echo "Regenerate with \`scripts/generate_preview_backgrounds.sh\` (maintainers only; mod builds never call ComfyUI)."
    echo
    echo "- Generated: $(date -u +%Y-%m-%dT%H:%M:%SZ)"
    echo "- ComfyUI: $COMFYUI_VERSION"
    echo "- Model: \`$MODEL_FILE\` (UNET), \`qwen_3_06b_base.safetensors\` (text encoder), \`qwen_image_vae.safetensors\` (VAE)"
    echo "- Sampler: euler, simple scheduler, 8 steps, cfg 1, generated at 1024x1024 and downscaled to 256x256 (Lanczos)"
    echo "- Workflow: \`scripts/comfyui/preview-background.workflow.json\` (prompts are in the workflow)"
    echo
    echo "| File | Seed |"
    echo "| --- | --- |"
    for ((index = 0; index < COUNT; index++)); do
        printf '| background-%02d.png | %d |\n' "$index" "$((SEED_BASE + index))"
    done
} > "$OUT_DIR/README.md"

echo "Wrote $COUNT backgrounds to $OUT_DIR"
