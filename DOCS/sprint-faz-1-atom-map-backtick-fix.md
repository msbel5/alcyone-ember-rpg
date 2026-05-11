# Sprint Faz 1 — atom-map backtick fix

## Goal

Address Copilot PR #89 inline review comment on
`DOCS/sprint-faz-1-atom-map.md:106`. The backtick-wrapped
`Assets/Scripts/Domain/World/WorldEventLog.cs` path was split across
two source lines, which broke the markdown inline code span (backtick
opened on one line, closed on the next). Keep the path inside a single
pair of backticks on one rendered line.

## Files changed

- `DOCS/sprint-faz-1-atom-map.md` — move the line break out of the
  inline code span; the path now renders as a single code token.

## Validation

`./tools/validation/run-validation.sh --mode fallback`

Result: `Passed! - Failed: 0, Passed: 729, Skipped: 0, Total: 729`.
Docs-only change, no Unity asset touched.

## Thalamus packet

- `packet_id` = `pkt_20260511062137_b906ad9c5085`
- `resolver_key` =
  `sha256:ee9dd59fbf6acd7fb56233518ad1a22ad9c74aef291b20f433fa06d07599b11d`

## Next increment

`ReasonTrace` (`Assets/Scripts/Domain/World/ReasonTrace.cs`) — a pure
causal-chain record that can be attached to a `WorldEvent`. Pinned by
NUnit tests under `Assets/Tests/EditMode/World/ReasonTraceTests.cs`.
After that, `WorldEventLog` lands the append-only log over
`WorldEvent`.
