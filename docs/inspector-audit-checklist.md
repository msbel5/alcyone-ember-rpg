# Inspector audit checklist

Inspector applies this checklist to every Captain PR before merge. A single FAIL row blocks the merge.

This checklist is referenced by `docs/agent-rules-v2.md` Rules 6-9 and by the top-of-file Debt ledger gate in every active sprint atom map (under `docs/archive/sprint/`).

## A. Mandatory PR body fields (per `docs/agent-rules-v2.md` Rule 9)

- [ ] `Primary box:` present; value is exactly one of `TIME | WORLD | LIVING | MATTER | PROCESS | SOCIETY | CRPG | AI/DM`.
- [ ] `Visible proof artifact:` present. Either a path to a test/log/snapshot/event row exists in the diff, OR the value is `none-this-is-foundational` AND `Carry-over debt row advanced` is `CO-XX-closed` or `CO-XX-advanced`.
- [ ] `New enum / helper / class added:` present. If `yes`, the same-PR consumer path is named OR the deferred-PR# is named.
- [ ] `Carry-over debt row advanced:` present; value is exactly one of `CO-XX-closed | CO-XX-advanced | CO-XX-deferred-to-faz-N | none-ledger-empty`.
- [ ] `Why this is the next bundle:` present; ties to the active Debt ledger row OR to the rail of the atom map being progressed.
- [ ] `Phase fences honored:` present; value is `yes` OR a fence-breach proposal is included in the sprint summary.

## B. Hard fail paths (per `docs/agent-rules-v2.md` Rule 6)

- [ ] No diff touches `Assets/Scenes/`, `Assets/Art/`, `Assets/Prefabs/`, `Assets/Resources/`, `Assets/Materials/`, `Assets/Textures/`.
- [ ] No diff adds binary files anywhere (`.png`, `.jpg`, `.fbx`, `.wav`, `.mp3`, `.psd`, `.blend`, `.tga`, `.exr`).
- [ ] No diff touches `docs/screenshots/` or `docs/images/`.
- [ ] No diff touches `Assets/Scripts/Presentation/` except pure-C# files with zero `using UnityEngine` AND a Mami-side consumer cited in the PR body.

## C. Speculative utility (Rule 2)

- [ ] Every new helper / overload / extension method either has a same-PR consumer in the diff, or is named in a `next-PR backlink` with a concrete consumer file path.
- [ ] Optional parameters added to existing methods are consumed in the same PR or by a same-PR test calling the new overload distinctly from the old one.

## D. Phase fences (per `docs/EMBER_VISION_NOTES_MAMI.md` section 1)

- [ ] No atom row implements Memory state before Faz 9.
- [ ] No atom row implements shared NPC/party/DM tool surface before Faz 10.
- [ ] No atom row calls or imports a real or mock LLM client outside Faz 12 as a default execution path. Tests may use deterministic mocks (this exception is preserved from `docs/EMBER_VISION_NOTES_MAMI.md` section 1).
- [ ] No atom row generates procedural world content outside a dedicated post-Faz 12 faz.
- [ ] No atom row touches multiverse / 100K-year / interplanetary scope at the implementation level.
- [ ] No atom row builds free-text dialog parsing outside Faz 9.

## E. Anti-drift detector (Rule 8)

- [ ] If the previous Captain PR had `Visible proof artifact: none-this-is-foundational` AND `Carry-over debt row advanced: none-ledger-empty`, the current PR MUST either close a CO row, advance a CO row (with diff evidence per Checklist G), OR present a visible proof artifact path. (Aligned with `agent-rules-v2.md` Rule 8: a concrete `CO-XX-advanced` report breaks the drift, since the audit fields are no longer `none-ledger-empty`.)

## F. Box tag schema (per `docs/mechanics/MASTER_MECHANICS_BIBLE.md`)

- [ ] Each atom row in any new atom map carries exactly one `primary_box` from the 8-box list.
- [ ] Optional cross-cutting commentary (`infra`, `meta`, `playable`) appears in row commentary, not in the `primary_box` column.
- [ ] No new atom row uses `[box=PROCESS][box=LIVING]`-style multi-box syntax.

## G. Debt ledger gate (per active atom map "Debt ledger" section)

- [ ] The kickoff doc for the current bundle records which CO row was `closed`, `advanced`, or `deferred-to-faz-N`. If `none-ledger-empty`, the ledger MUST be empty (all rows `closed` or `deferred`) — Inspector verifies.
- [ ] If `deferred-to-faz-N`, the reason is tied to the current bundle in plain language, not generic ("not in scope" is not sufficient). The same row may not have been deferred in a prior kickoff doc.
- [ ] If `closed`, the PR diff satisfies the row's Exit proof column. Inspector verifies the diff matches the Exit proof literally.
- [ ] If `advanced`, the row has been `advanced` at most twice consecutively across prior PRs; a third `advanced` report against the same row fails this check and must be promoted to `closed`.

## H. Kickoff doc anchors (per `docs/EMBER_VISION_NOTES_MAMI.md` sections 1 and 2)

- [ ] The kickoff doc for any new atom map or new bundle within an existing atom map contains a `## Vision anchors` heading that cites by number which of the 9 anchors (from `docs/EMBER_VISION_NOTES_MAMI.md` section 2) the sprint serves. A sprint citing zero anchors is "internal-scaffolding only" and capped at one PR with explicit justification.
- [ ] The kickoff doc contains a `## Phase fences` heading that explicitly lists which of the 6 fences (from `docs/EMBER_VISION_NOTES_MAMI.md` section 1) are honored, OR states "no fence crossed by this bundle."
- [ ] If a fence is crossed, a one-paragraph **fence breach proposal** is present in the sprint summary, AND atom rows that cross the fence are absent from the bundle until Mami lifts the fence.

## Failure escalation

| Failure category | Inspector action |
|---|---|
| Checklist B (hard fail paths) | Revert immediately, comment on the PR with the violated rule, do not request human review. |
| Checklist E (anti-drift) | Halt cron, open `loop-halt` issue tagged with both PR numbers, ping Mami, do not merge until Mami rewrites the active atom map's "Next increment". |
| Checklist D (phase fence) | Request changes on the PR, comment with the specific fence violated, let Captain re-open after a fence-breach proposal in the sprint summary OR a rewrite that respects the fence. |
| All other failures (A, C, F, G, H) | Request changes on the PR, list every failing item, let Captain re-open. |

## Notes

- This checklist is a living document. When a new rule lands in `agent-rules-v2.md`, a matching checklist row is added here in the same PR.
- Inspector is currently a human (Mami) + bot reviews (Copilot, CodeRabbit, ChatGPT-Codex). When automated Inspector lands in a future faz, this checklist is the canonical spec.
