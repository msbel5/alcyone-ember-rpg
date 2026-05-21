# Kickoff: agent/sprint-4-job-refusal

Branch: agent/sprint-4-job-refusal
Primary box: PROCESS

## Vision anchors
- Living-world over showroom
- Deterministic-first, LLM-last
- Systemic interaction

## Debt ledger action
Carry-over debt row advanced: CO-01-advanced
Visible proof artifact: none-this-is-foundational
Why: advancing CO-01 (IPathfinder interface scaffold) unblocks future pathfinder implementation needed by JobAssignmentSystem and Process pathing. This PR adds the interface scaffold only (no behavior change). Phase fences honored: yes

## Next bundle
Implement `JobAssignmentSystem.CanActorWorkJob` to enforce refusal when hunger/mood exceed thresholds and to emit `WorldEventKind.JobRefused` when appropriate. This kickoff notes CO-01 advanced and does not close any CO row.

