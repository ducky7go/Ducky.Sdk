---
name: AOS: Archive
description: Archive a deployed OpenSpec change with completion notifications and sound alerts.
category: AOS
tags: [aos, openspec, archive, notifications, sound]
---
<!-- OPENSPEC:START -->

**Sound Notifications**
Use the helper script `.claude/commands/aos/notify.sh` for all notifications:
```bash
# Usage: notify.sh <type> <title> <message>
# Types: start, progress, milestone, complete, warning, error

# Example:
.claude/commands/aos/notify.sh start "AOS: Archive" "Starting archive operation"
```
The script automatically handles both desktop notifications and sound alerts.
**Guardrails**
- Favor straightforward, minimal implementations first and add complexity only when it is requested or clearly required.
- Keep changes tightly scoped to the requested outcome.
- Refer to `openspec/AGENTS.md` (located inside the `openspec/` directory—run `ls openspec` or `openspec update` if you don't see it) if you need additional OpenSpec conventions or clarifications.

**Steps**
1. Determine the change ID to archive:
   - If this prompt already includes a specific change ID (for example inside a `<ChangeId>` block populated by slash-command arguments), use that value after trimming whitespace.
   - If the conversation references a change loosely (for example by title or summary), run `openspec list` to surface likely IDs, share the relevant candidates, and confirm which one the user intends.
   - Otherwise, review the conversation, run `openspec list`, and ask the user which change to archive; wait for a confirmed change ID before proceeding.
   - If you still cannot identify a single change ID, stop and tell the user you cannot archive anything yet.
2. Send start notification with sound for archiving operation:
   ```bash
   .claude/commands/aos/notify.sh start "AOS: Archive Started" "Archiving deployed OpenSpec change"
   ```
3. Validate the change ID by running `openspec list` (or `openspec show <id>`) and stop if the change is missing, already archived, or otherwise not ready to archive.
4. Run `openspec archive <id> --yes` so the CLI moves the change and applies spec updates without prompts (use `--skip-specs` only for tooling-only work).
5. Send progress notification with sound when archive operation begins:
   ```bash
   .claude/commands/aos/notify.sh progress "AOS: Archive Progress" "Moving change to archive"
   ```
6. Review the command output to confirm the target specs were updated and the change landed in `changes/archive/`.
7. Send completion notification with sound and archive summary:
   ```bash
   .claude/commands/aos/notify.sh complete "AOS: Archive Completed" "Change successfully archived"
   ```
8. Validate with `openspec validate --strict` and inspect with `openspec show <id>` if anything looks off.

**Reference**
- Use `openspec list` to confirm change IDs before archiving.
- Inspect refreshed specs with `openspec list --specs` and address any validation issues before handing off.
<!-- OPENSPEC:END -->