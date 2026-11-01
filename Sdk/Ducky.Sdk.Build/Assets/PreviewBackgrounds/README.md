# Preview backgrounds

Ten 256x256 backgrounds for the `preview.png` of each SDK-built mod. `GeneratePreviewLib` selects one by a SHA-256 hash of the
project name, embeds the file as a resource (see `Ducky.Sdk.Build.csproj`), and draws the project abbreviation over it.

Regenerate with `scripts/generate_preview_backgrounds.sh` (maintainers only; mod builds never call ComfyUI).

- Generated: 2026-11-01T04:39:43Z
- ComfyUI: 0.43.2
- Model: `anima-turbo-v1.1.safetensors` (UNET), `qwen_3_06b_base.safetensors` (text encoder), `qwen_image_vae.safetensors` (VAE)
- Sampler: euler, simple scheduler, 8 steps, cfg 1, generated at 1024x1024 and downscaled to 256x256 (Lanczos)
- Workflow: `scripts/comfyui/preview-background.workflow.json` (prompts are in the workflow)

| File | Seed |
| --- | --- |
| background-00.png | 20261001 |
| background-01.png | 20261002 |
| background-02.png | 20261003 |
| background-03.png | 20261004 |
| background-04.png | 20261005 |
| background-05.png | 20261006 |
| background-06.png | 20261007 |
| background-07.png | 20261008 |
| background-08.png | 20261009 |
| background-09.png | 20261010 |
