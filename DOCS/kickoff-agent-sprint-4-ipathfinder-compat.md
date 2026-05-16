# Kickoff: agent/sprint-4-ipathfinder-compat

Branch: agent/sprint-4-ipathfinder-compat
Primary box: PROCESS

## Vision anchors
- Living-world over showroom
- Deterministic-first, LLM-last
- Systemic interaction

## Phase fences
No fence crossed by this bundle. This closes a deterministic PROCESS contract and does not add Memory state, DM tool surface, LLM calls, procedural generation, multiverse scope, or free-text dialog parsing.

## Debt ledger action
Carry-over debt row advanced: CO-01-closed
Visible proof artifact: Assets/Tests/EditMode/World/IPathfinderTests.cs
Why: closing CO-01 gives PROCESS pathing a C# 9-compatible deterministic interface contract, with a fixture test that pins TryFindPath output before CO-02 adds the concrete GridPathfinder implementation. Phase fences honored: yes

## Next bundle
Implement CO-02 with a concrete deterministic GridPathfinder over GridPosition, then use that result to unblock CO-03 pathing ticks.
