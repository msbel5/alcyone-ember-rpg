<!--
Mandatory PR audit block — see docs/agent-rules-v2.md Rule 9 and
docs/inspector-audit-checklist.md.

Captain: fill in every field below. Inspector rejects PRs missing the block
or any field. State vocabulary for the Carry-over field is defined in
docs/agent-rules-v2.md Rule 8 and the active sprint atom map's top-of-file
Debt ledger (under docs/archive/sprint/).
-->

## PR audit fields

```
Primary box: <one of TIME|WORLD|LIVING|MATTER|PROCESS|SOCIETY|CRPG|AI/DM>
Visible proof artifact: <path to test / log / snapshot / event row in the diff, OR "none-this-is-foundational" + CO row ID>
New enum / helper / class added: <yes-with-same-PR-consumer-at-PATH | yes-deferred-to-PR#... | no>
Carry-over debt row advanced: <CO-XX-closed | CO-XX-advanced | CO-XX-deferred-to-faz-N | none-ledger-empty>
Why this is the next bundle: <one sentence tying to ledger + atom map>
Phase fences honored: <yes | called-out-violation-because-...>
```

## Goal

<!-- One sentence: what this PR adds, removes, or fixes. Tie to the active atom map's rail or the Debt ledger row being advanced. -->

## Files changed

<!-- Bullet list of paths added/modified, grouped by Domain / Simulation / Data / Tests / DOCS. -->

## Behaviour

<!-- What the simulation does now that it did not do before. What it deliberately does not do (so Inspector can tell scope). -->

## Validation

- [ ] `./tools/validation/run-validation.sh --mode fallback` passed locally.
- [ ] Existing tests still green; new tests pin the new behaviour deterministically.
- [ ] Self-checked against `docs/inspector-audit-checklist.md` checklists A-H.

## Thalamus

<!-- packet_id, resolver_key, AoT session if relevant. -->

## Next increment

<!-- One sentence: what Captain plans to ship next, tied to the atom map's Next increment line or to the Debt ledger. -->
