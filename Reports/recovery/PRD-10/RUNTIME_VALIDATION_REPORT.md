# PRD-10 Runtime Validation Report — targeted PASS, full lane PARTIAL

## Validation target

- Recovery parent: `b416d0d0ce3c1ecba5e545c5046bfc010b85c93e`.
- The validated tree also contains the Unity-NUnit compatibility bridge, native
  plugin import safety, and bounded built-player proof harness delivered with
  this report.
- Runtime: Unity `6000.3.13f1`, Windows 64-bit player.

## Automated gates

| lane | result | evidence |
|---|---:|---|
| Selected pure-C# recovery gate | PASS 120/120 | `validation-output/post-push/pure-csharp-selected-after-unity-compat.log` |
| Targeted Unity EditMode determinism soak | PASS 5/5 | `validation-output/post-push/unity-editmode-soak-results.xml` |
| Targeted Unity PlayMode projection/story pack | PASS 8/8 | `validation-output/post-push/unity-playmode-recovery-results.xml` |
| Windows64 build | PASS | `validation-output/post-push/windows64-build-visual-proof.log` |
| Built-player shipcheck | PASS 9/9, 0 exceptions | `validation-output/post-push/shipcheck-pass-player.log` |
| Built-player visual tour | COMPLETE, 0 scanned runtime exceptions | `validation-output/post-push/lookaround-final-player.log` |

The Unity `BuildReport` reported a `14,159,578,133`-byte total build payload and
the build step copied the runtime-generated visual set plus provenance
sidecars. The output directory is approximately 14.27 GB.

## Built-player behavior

- World entry, quest seeding, encounter/loot, economy, performance,
  travel/reload soak, economy-chain, audio/Forge fallback handling, and modal
  capture all passed.
- Performance proof measured 12.4 ms average during shipcheck and 7.4 ms average
  during the visual tour. The visual tour worst frame was 41.2 ms.
- Travel/reload proof completed 10 production-path hops, simulated 10 honest
  days, and observed 318 planned journey days without exceptions.
- Economy proof changed stock `117 -> 108` and price `13 -> 3`.
- Corrected screenshots are under
  `validation-output/post-push/lookaround-final`; shipcheck screenshots are
  under `validation-output/post-push/shipcheck-pass`.

## Proof limits

- Computer Use was attempted but its Windows Sky runtime was unavailable.
  Self-play was therefore driven by the visible built-player proof harness, not
  by manual keyboard/mouse input.
- The targeted recovery lanes pass, but the full EditMode run is not a PASS:
  the Forge CUDA smoke terminates Unity inside `cudnn64_9.dll`. Direct Unity
  probing of cuDNN components is now disabled while the DLLs remain included in
  Windows builds; Forge's on-demand CUDA path still needs a separate runtime fix.
- The built-player log contains no authoritative `[Action]` transition rows.
  Runtime action projection is proven by the Unity PlayMode story pack, while a
  player JSONL transition artefact remains deferred.
- Live-population day catch-up took roughly four seconds per simulated day on
  this machine. The proof now yields between bounded production steps instead
  of presenting a synchronous AppHang as a soak result.

## Review score

| area | score |
|---|---:|
| Recovery architecture and deterministic behavior | 9.0/10 |
| Core loop and automated playability | 8.0/10 |
| Runtime stability in the selected lanes | 7.0/10 |
| UX/readability | 5.5/10 |
| Visual presentation | 3.5/10 |
| Current playable build, overall | **6.3/10** |

The map and inventory modal are the strongest presentation surfaces. The world
and dungeons remain visibly prototype-grade: blocky buildings, flat terrain,
small HUD text, weak spell effects, camera/ceiling clipping, repetitive dungeon
materials, and unconvincing boss/chest staging. These are honest presentation
debts, not failures of the recovered action authority contract.
