---
name: AOS: Apply
description: Implement an approved OpenSpec change with progress notifications and sound alerts.
category: AOS
tags: [aos, openspec, apply, notifications, sound]
---
<!-- OPENSPEC:START -->

**Sound Notifications**
Use the helper script `.claude/commands/aos/notify.sh` for all notifications:
```bash
# Usage: notify.sh <type> <title> <message>
# Types: start, progress, milestone, complete, warning, error

# Example:
commands/aos/notify.sh start "AOS: Apply" "Starting implementation"
```
The script automatically handles both desktop notifications and sound alerts.
**Guardrails**
- Favor straightforward, minimal implementations first and add complexity only when it is requested or clearly required.
- Keep changes tightly scoped to the requested outcome.
- Refer to `openspec/AGENTS.md` (located inside the `openspec/` directory—run `ls openspec` or `openspec update` if you don't see it) if you need additional OpenSpec conventions or clarifications.

**Steps**
Track these steps as TODOs and complete them one by one.
1. Send start notification with sound for apply operation:
   ```bash
   commands/aos/notify.sh start "AOS: Apply Started" "Implementing approved OpenSpec change"
   ```
2. Read `changes/<id>/proposal.md`, `design.md` (if present), and `tasks.md` to confirm scope and acceptance criteria.
3. Send progress notification with sound when starting task implementation:
   ```bash
   commands/aos/notify.sh progress "AOS: Implementation Progress" "Started implementing tasks"
   ```
4. Work through tasks sequentially, keeping edits minimal and focused on the requested change.
5. Send milestone notifications with sound for major task completions:
   ```bash
   commands/aos/notify.sh milestone "AOS: Milestone Completed" "Major task implementation completed"
   ```
6. Confirm completion before updating statuses—make sure every item in `tasks.md` is finished.
7. Update the checklist after all work is done so each task is marked `- [x]` and reflects reality.
8. Send completion notification with sound and summary of applied changes:
   ```bash
   commands/aos/notify.sh complete "AOS: Apply Completed" "Change implementation completed successfully"
   ```
9. Reference `openspec list` or `openspec show <item>` when additional context is required.

**Reference**
- Use `openspec show <id> --json --deltas-only` if you need additional context from the proposal while implementing.
<!-- OPENSPEC:END -->