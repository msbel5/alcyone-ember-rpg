# Sprint 4 Faz 1 — Mechanics/Camera Foundation

## What landed

- Added an engine-agnostic Sprint 4 kinematic movement core under `EmberCrpg.Simulation.Movement`.
  - WASD-style planar input is normalized/clamped so diagonal motion does not exceed move speed.
  - Movement is yaw-relative for third-person camera control.
  - Jump impulse and gravity are deterministic and covered by EditMode/fallback tests.
- Added a Unity `CharacterController` adapter (`Sprint4PlayerController`).
  - Chosen approach: **CharacterController**, not Rigidbody.
  - Reason: Sprint 4 Faz 1 needs predictable RPG traversal, explicit jump/gravity tuning, and camera-relative input without physics-force side effects. Rigidbody can come later for physics props or knockback, but the player capsule baseline should stay deterministic and designer-tunable first.
- Added `Sprint4CameraRig` without Cinemachine.
  - Third-person follow with smoothing.
  - First-person toggle on `V`.
  - Camera collision intent through spherecast from pivot to desired camera position.
- Added `Sprint4AnimatorDriver` placeholder contract.
  - Parameters: `MoveSpeed`, `Grounded`, `VerticalSpeed`, `FirstPerson`, `LocomotionState`, `Jump`.
  - Placeholder states: `Idle`, `Move`, `Jump`, `Fall`.
  - It is tolerant of missing AnimatorController art assets so the foundation compiles before final animation assets exist.
- Added a minimal buildable scene at `Assets/Scenes/Sprint4Foundation.unity` and registered it in Build Settings.
  - The previous main failure was `Cannot build untitled scene` because `m_Scenes` was empty.
  - Runtime bootstrap creates a greybox ground, player capsule, light, and camera rig when the Sprint 4 scene loads.

## Manual feel/pass gaps

- Local environment did not provide a real Unity editor during this pass, so Presentation scripts and the scene need a manual Unity import/play/build pass.
- Camera feel values are first-pass defaults only: pitch limits, smoothing, collision radius/buffer, and shoulder offset need gamepad/mouse feel tuning.
- Animator work is a parameter/state contract, not final animation blending. A real AnimatorController should be generated/assigned once placeholder clips exist.
- Existing top-down slice code is still present. `SliceRuntimeBootstrap` now skips scenes whose name contains `Sprint4` to avoid double-bootstrapping the new foundation scene.
