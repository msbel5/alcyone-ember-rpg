# E7-020 Stage 0 Input Baseline (2026-05-31)

Mode: `source-only`

This file freezes the legacy `EmberInput` facade contract before any Input System
package work. It is derived directly from
`Assets/Scripts/Presentation/Ember/Inputs/EmberInput.cs`.

## Baseline scenarios

- `S0` idle frame: no relevant keyboard or mouse input.
- `Sx` trigger frame: the specific key/button/axis change for that member.

## 25-member facade baseline

| # | Member | Legacy source expression | Expected in `S0` | Expected in `Sx` |
|---|---|---|---|---|
| 1 | `Move` | `new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"))` | `(0,0)` (neutral) | raw axis value from legacy Input Manager (e.g. `A` => `x < 0`, `D` => `x > 0`, `W` => `y > 0`, `S` => `y < 0`) |
| 2 | `Look` | `new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"))` | `(0,0)` | non-zero raw mouse delta while mouse moves |
| 3 | `LookSmoothed` | `new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"))` | `(0,0)` | non-zero smoothed mouse delta while mouse moves |
| 4 | `Sprint` | `Input.GetKey(KeyCode.LeftShift)` | `false` | `true` while LeftShift is held |
| 5 | `JumpDown` | `Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space)` | `false` | `true` on the frame Jump/Space is pressed |
| 6 | `JumpKeyDown` | `Input.GetKeyDown(KeyCode.Space)` | `false` | `true` on the frame Space is pressed |
| 7 | `Interact` | `Input.GetKeyDown(KeyCode.E)` | `false` | `true` on the frame `E` is pressed |
| 8 | `ToggleCursor` | `Input.GetKeyDown(KeyCode.F1)` | `false` | `true` on the frame `F1` is pressed |
| 9 | `RegenWorld` | `Input.GetKeyDown(KeyCode.R)` | `false` | `true` on the frame `R` is pressed |
| 10 | `ToggleMap` | `Input.GetKeyDown(KeyCode.Tab)` | `false` | `true` on the frame `Tab` is pressed |
| 11 | `SaveQuick` | `Input.GetKeyDown(KeyCode.F5)` | `false` | `true` on the frame `F5` is pressed |
| 12 | `LoadQuick` | `Input.GetKeyDown(KeyCode.F9)` | `false` | `true` on the frame `F9` is pressed |
| 13 | `PauseDown` | `Input.GetKeyDown(KeyCode.Escape)` | `false` | `true` on the frame `Escape` is pressed |
| 14 | `PauseHeld` | `Input.GetKey(KeyCode.Escape)` | `false` | `true` while `Escape` is held |
| 15 | `AttackClick` | `Input.GetMouseButtonDown(0)` | `false` | `true` on the frame LMB is pressed |
| 16 | `SecondaryClick` | `Input.GetMouseButtonDown(1)` | `false` | `true` on the frame RMB is pressed |
| 17 | `MeleeSwing` | `Input.GetKeyDown(KeyCode.F)` | `false` | `true` on the frame `F` is pressed |
| 18 | `NumberKeyDown()` | loop `Input.GetKeyDown(KeyCode.Alpha1 + i)` (`i=0..8`) | `0` | `n` when `Alpha<n>` (1..9) is pressed this frame, else `0` |
| 19 | `NumberKeyDown(int oneBased)` | range-check + `Input.GetKeyDown(KeyCode.Alpha1 + (oneBased - 1))` | `false` | `true` only when `oneBased` is 1..9 and that key is pressed this frame |
| 20 | `FunctionKeyDown()` | loop `Input.GetKeyDown(KeyCode.F1 + i)` (`i=0..11`) | `0` | `n` when `F<n>` (1..12) is pressed this frame, else `0` |
| 21 | `KeyDown(KeyCode key)` | `Input.GetKeyDown(key)` | `false` for unpressed key | `true` on the press frame for the provided key |
| 22 | `Key(KeyCode key)` | `Input.GetKey(key)` | `false` for unheld key | `true` while the provided key is held |
| 23 | `MouseDown(int button)` | `Input.GetMouseButtonDown(button)` | `false` for unpressed button | `true` on the press frame for the provided button |
| 24 | `AxisRaw(string axisName)` | `Input.GetAxisRaw(axisName)` | `0` on neutral default axes | raw axis value for the provided axis name |
| 25 | `Axis(string axisName)` | `Input.GetAxis(axisName)` | `0` on neutral default axes | smoothed axis value for the provided axis name |

## Stage 0 note

This is a contract baseline only (`source-only`). It does not claim synthetic
input injection or Input System behavior. Stage 3 converts this table into
device-event assertions once `com.unity.inputsystem` is installed.
