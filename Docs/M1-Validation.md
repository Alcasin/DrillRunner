# M1 — launch, movement, and camera

Implemented against the accepted M0 commit `6efc3b6` on `main`, with a clean starting working tree. M1 is ready for manual QA. No M2 systems were added.

## Implementation and assets

| File / asset | Purpose |
| --- | --- |
| `Runtime/DrillMotion.cs` | Validated settings and scene-independent Ready/Charging/Moving rules, analytical boost integration, normalized drag, footprint bounds, cancellation, reset |
| `Runtime/DrillMotor.cs` | Applies motion to the gameplay root in Update; fixed Y, +Z forward, no Rigidbody |
| `Runtime/DrillPointerInput.cs` | Private clone of the existing actions, physical pointer ownership, UI filtering, cancellation/focus/device handling, paired subscriptions |
| `Runtime/RunnerCamera.cs` | Stable elevated third-person composition, exponential position damping in LateUpdate, reset snap |
| `Runtime/PrototypeHud.cs` | Ready/Charging/Moving feedback, charge fill, Restart button |
| `Editor/MovementPrototypeSetup.cs` | One M1 menu and batch entry point; Unity API scene/prefab/material creation and wiring |
| `Prefabs/DrillPrototype.prefab` | One movement root and BoxCollider, replaceable Visuals child, body/cabin/treads and stepped forward drill bit |
| `Scenes/Gameplay.unity` | Existing scene extended in place; existing camera/light/track objects retained, player/HUD/edge guides added |
| `Materials/Drill*.mat`, `TrackEdges.mat`, `TrackDistanceMarks.mat` | Six simple URP/Lit prototype materials; existing TrackReference material preserved |
| `Models/PrototypeDistanceMarks.asset` | One lightweight static mesh of track distance cues; no runtime track generation |
| `Tests/EditMode/MovementTests.cs`, `MovementSceneTests.cs` | Motion rules, numerical edge cases, camera math, scene wiring, footprint, preservation and repeated setup |
| `Tests/PlayMode/` | Seven integration tests covering actual scene components, input, UI, lifecycle, and rendered composition |

Paths above are relative to `Assets/_DrillRunner/`. Unity generated all new `.meta` files. The Runtime, Editor, and EditMode assembly definitions gained references to the already installed `Unity.ugui` assembly. Runtime assembly metadata and the root README were updated.

## Input and tuning

**No Input Actions changes.** The M0 gameplay asset, action/binding IDs, control schemes, and `.meta` GUID are unchanged. Charge is the existing default Button; Steer is the existing Vector2 delta. The existing template UI map drives the standard Input System UI module. No generated wrapper, legacy input, PlayerInput component, or alternative gameplay input system was introduced.

The captured physical pointer owns a gesture. Release is confirmed after the input update; action cancellation alone cannot launch. Touch slots are resolved after the primary-touch callback, and the original finger remains the owner while other fingers are held. Device reset/removal, focus loss, pause, disable, and restart cancel the gesture. A mouse release is recognized even when a simultaneous touch keeps the shared Button action actuated. Interactive Restart UI presses do not enter gameplay input.

Defaults:

- Full charge: **1.5 s**, monotonic and capped. A zero-duration valid press/release launches with minimum boost; release without a press does nothing.
- Cruise: **10 units/s**; initial boost **+2 to +12**, exponential decay **1.8/s**, configured speed limit **24** (normal full-charge peak **22**).
- Steering: **8 world units per screen width**, response **18/s**. No steering while charging, hovering, or after release; vertical pointer movement is ignored.
- Track width **6**, collider/footprint width **1.7**; center clamped to **[-2.15, +2.15]**. Complete visible horizontal footprint stays within the collider.
- Camera: offset **(0, 6, -9)**, forward response **8/s**, horizontal response **5/s**, horizontal follow fraction **0.25**, look-ahead **6**, look height **0.65**, FOV **60°**. Rotation remains stable.
- Static reference surface: **20 km**, approximately **33 minutes** at cruising speed. It is finite; use Restart before the end.

Movement settings are validated and copied at run initialization. Inspector edits require re-entering Play to affect movement rules. No upgrades, drilling restrictions, or progression systems are implemented; a single steering-enable gate leaves room for a future restriction.

## Automation performed

Executed **Tools > DrillRunner > Setup M1 Movement Prototype** through Unity 6000.3.8f1 batch mode. Scene/prefab changes were made entirely through Editor APIs. No serialized scene or prefab YAML was edited by hand.

Setup reused the M0 camera and slab, extended the slab, created and instantiated the drill prefab, added a camera follower, and wired the HUD, Restart button, and Input System UI EventSystem. No manual GameObject wiring remains.

Reruns preserve existing prefab/material contents, component tuning, valid serialized references, GUIDs, and unrelated scene objects. Conflicts produce errors. A loaded dirty Gameplay scene is refused without saving or discarding its edits. Tests repeat setup against a disposable scene copy containing a user-authored sentinel, verify byte-for-byte stability and one player/camera, and verify that dirty-scene refusal preserves edits. The real scene setup was also rerun and its scene/prefab hashes checked for stability.

## Executed validation

Final results, after fixes:

| Validation | Result |
| --- | --- |
| Unity 6000.3.8f1 compilation and M1 scene setup | **Passed**, process exit 0 |
| Unity EditMode | **33 passed, 0 failed, 0 skipped**, including all **9 unchanged M0 tests** |
| Unity PlayMode | **7 passed, 0 failed, 0 skipped**, process exit 0 |
| Actual Restart button, charge cap, hover/drag/release behavior | **Passed** in PlayMode |
| Synthetic touch cancellation, primary ownership with a second finger, fresh touch steering | **Passed** in PlayMode |
| Device reset, simulated focus loss, enable/disable cycles, reset while held | **Passed** in PlayMode |
| Overlapping mouse/touch press and owned-pointer release | **Passed** in PlayMode |
| Camera composition at both lateral bounds and reset | **Passed** in PlayMode |
| GPU render captures at 540 x 960 | **Ready, Charging, Moving rendered and visually inspected** using Direct3D 11 |
| JSON / assembly definitions, source formatting, `.meta` coverage and GUID uniqueness | **Passed** |
| Preservation audit against original tracked file hashes | **Passed**; no original files deleted; only intended source/assembly, Gameplay scene, and README changes |

Local ignored evidence: `Logs/M1-Setup.log`, `M1-Setup-Repeat.log`, `M1-EditMode.log`, `M1-EditMode.xml`, `M1-PlayMode.log`, `M1-PlayMode.xml`, `M1-Ready.png`, `M1-Charging.png`, `M1-Moving.png`, and `M1-Baseline.json`.

Source/documentation whitespace checks pass. A whole-repository `git diff --check` reports Unity's generated trailing spaces on empty YAML values in the serialized scene; those serialized lines were left as emitted by Unity.

## Preserved configuration and remaining validation

- No packages installed or changed: URP **17.3.0**, Input System **1.18.0**, Test Framework **1.6.0**, existing uGUI **2.0.0**.
- No `ProjectSettings` changes, build target switches, graphics-quality changes, or build scene changes. Existing portrait configuration remains intact.
- Existing SampleScene, foundation setup code/tests, input assets, track material, URP assets, and all original asset GUIDs are preserved.
- No Android/player build, physical mobile-touch test, hardware focus/background QA, performance profiling, or 33-minute endurance run was performed. Automated focus loss uses the Unity lifecycle message plus device-reset coverage. Physical feel and device-specific behavior still need manual QA.
- No Cinemachine or other dependency was added. No commit, push, remote modification, or Git configuration change was performed.

## Minimum manual Unity QA

1. Open `Assets/_DrillRunner/Scenes/Gameplay.unity`, keep **Portrait 9:16**, press **Play**. No setup command, restart, or wiring is required.
2. Hold on the track for over 1.5 seconds: bar fills and stays full; the drill remains still. Release: it launches once and settles into continuous cruise. Compare a short tap with a full charge.
3. Move the mouse without holding: no steering. Hold and drag left/right to both edges; release: X stays fixed. Vertical movement should not steer.
4. Use **RESTART** during charging and movement, including while holding a pointer. Verify Ready, zero charge/speed, initial position, and camera reset; the old release must not launch.
5. Switch away while charging, then return and release: no launch. Confirm camera readability and control feel during an extended run. Repeat input QA on a real touchscreen when mobile testing becomes available.

M1 stops here for review. M2 is not started.
