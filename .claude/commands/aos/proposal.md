---
name: AOS: Proposal
description: Create OpenSpec proposals with integrated notifications and sound alerts.
category: AOS
tags: [aos, openspec, proposal, notifications, sound]
---
<!-- OPENSPEC:START -->

**Sound Notifications**
Use the helper script `.claude/commands/aos/notify.sh` for all notifications:
```bash
# Usage: notify.sh <type> <title> <message>
# Types: start, progress, milestone, complete, warning, error

# Example:
.claude/commands/aos/notify.sh start "AOS: Proposal" "Starting proposal creation"
```
The script automatically handles both desktop notifications and sound alerts.
**Guardrails**
- Favor straightforward, minimal implementations first and add complexity only when it is requested or clearly required.
- Keep changes tightly scoped to the requested outcome.
- Refer to `openspec/AGENTS.md` (located inside the `openspec/` directory—run `ls openspec` or `openspec update` if you don't see it) if you need additional OpenSpec conventions or clarifications.
- Identify any vague or ambiguous details and ask the necessary follow-up questions before editing files.
- Do not write any code during the proposal stage. Only create design documents (proposal.md, tasks.md, design.md, and spec deltas). Implementation happens in the apply stage after approval.

**Steps**
1. Send start notification with sound for proposal creation:
   ```bash
   .claude/commands/aos/notify.sh start "AOS: Proposal Started" "Creating OpenSpec change proposal"
   ```
2. Review `openspec/project.md`, run `openspec list` and `openspec list --specs`, and inspect related code or docs (e.g., via `rg`/`ls`) to ground the proposal in current behaviour; note any gaps that require clarification.
3. Choose a unique verb-led `change-id` and scaffold `proposal.md`, `tasks.md`, and `design.md` (when needed) under `openspec/changes/<id>/`.
4. Map the change into concrete capabilities or requirements, breaking multi-scope efforts into distinct spec deltas with clear relationships and sequencing.
5. Capture architectural reasoning in `design.md` when the solution spans multiple systems, introduces new patterns, or demands trade-off discussion before committing to specs.
6. Draft spec deltas in `changes/<id>/specs/<capability>/spec.md` (one folder per capability) using `## ADDED|MODIFIED|REMOVED Requirements` with at least one `#### Scenario:` per requirement and cross-reference related capabilities when relevant.
7. Draft `tasks.md` as an ordered list of small, verifiable work items that deliver user-visible progress, include validation (tests, tooling), and highlight dependencies or parallelizable work.
8. Send completion notification with sound and validation status:
   ```bash
   .claude/commands/aos/notify.sh complete "AOS: Proposal Completed" "Change proposal created successfully"
   ```
9. Validate with `openspec validate <id> --strict` and resolve every issue before sharing the proposal.

**Reference**
- Use `openspec show <id> --json --deltas-only` or `openspec show <spec> --type spec` to inspect details when validation fails.
- Search existing requirements with `rg -n "Requirement:|Scenario:" openspec/specs` before writing new ones.
- Explore the codebase with `rg <keyword>`, `ls`, or direct file reads so proposals align with current implementation realities.
<!-- OPENSPEC:END -->