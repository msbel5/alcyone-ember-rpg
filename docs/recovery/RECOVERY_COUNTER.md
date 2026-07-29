# Ember Recovery Counter

The user superseded the per-PRD branch, pull-request, approval, and stop gates
for this recovery run. PRDs still execute in numeric order with targeted
validation before advancing.

`IMPLEMENTATION_STATUS.md` is the single live counter for DONE state, latest
targeted evidence, and the exact next PRD. This file intentionally carries no
second state table that can drift. `DONE` there records implementation plus
targeted validation; it is not a runtime-proof claim.
