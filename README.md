# DrillRunner

A portrait mobile 3D evolution runner built with **Unity 6000.3.8f1 (6.3 LTS)** and **Universal Render Pipeline 17.3.0**. The approved game has eight drill forms and four regions; the first vertical slice will cover one region and Form 1 to Form 2.

**M2 adds progressive drilling, strict material hardness, and run energy to the accepted M1 controls and camera.** Resources, rewards, economy, upgrades, evolution, and procedural tracks are not implemented. Stop here for review before M3.

## Open and inspect

1. Open this directory with Unity Hub using 6000.3.8f1.
2. Open `Assets/_DrillRunner/Scenes/Gameplay.unity`.
3. Select the already verified **Portrait 9:16** Game View aspect and press **Play**.
4. Hold on the track to charge; release to launch. Press and drag horizontally to steer. Wood and Brick drill automatically on central contact. Stone blocks the default drill: drag around either side. **RESTART** restores the player, charge, speed, energy, obstacles, input, and camera.

The scene retains the M0/M1 camera, light, primitive drill, track edges, and static distance marks. Forward is **+Z**, steering is **X**, and the drill root stays at **Y = 0**. The drill nose faces forward. The 20 km slab remains a finite static reference; energy now limits a run's duration. The M2 encounter is confined to Wood at Z=35, Brick at Z=65, Stone at Z=95, and another Wood at Z=130. Both sides of each obstacle have room for the complete player footprint.

Portrait orientation and a 540 x 960 default player window remain configured. The Windows build target is retained; Android has not been selected. If the local aspect preference is missing on another machine: **Game View > aspect dropdown > + > Aspect Ratio > 9 by 16**, label it **Portrait 9:16**, save and select.

`Gameplay` is the first enabled global build scene; the template `SampleScene` remains enabled after it. Setup also includes Gameplay in an active Unity 6 build profile if that profile overrides global scenes; inactive profiles are untouched.

## Structure

All project-owned production assets live under `Assets/_DrillRunner/`:

| Folder | Purpose |
| --- | --- |
| `Runtime/` | Existing motion/input/camera/HUD, extended motor, `DrillRules`, `DrillContacts`, `DrillObstacle`, `MaterialDefinition` |
| `Editor/` | M0/M1 tooling, one M2 setup command, Editor-only power/energy QA Inspector controls |
| `Tests/EditMode/` | Editor-only `DrillRunner.EditModeTests`, excluded from player builds |
| `Tests/PlayMode/` | Runtime movement/input/UI/camera, drilling/contact/energy/restart integration tests |
| `Scenes/`, `Settings/`, `Materials/` | Gameplay scene, unchanged gameplay input asset, URP prototype materials |
| `Prefabs/`, `Models/` | Drill and three obstacle prefabs; static distance-mark mesh |
| `ScriptableObjects/` | Wood, Brick, and Stone material definitions |
| `Textures/`, `Audio/`, `VFX/`, `UI/` | Asset destinations reserved for later milestones |

The original template assets under `Assets/Settings`, `Assets/Scenes`, `Assets/TutorialInfo`, and the root input asset remain in place with their original GUIDs. Existing URP pipeline assets and quality tiers are retained. Empty project folders are kept through their versioned Unity folder `.meta` files; Unity recreates the folders when importing a fresh checkout.

## Input and cancellation

Input System **1.18.0** is already installed; **Active Input Handling = Input System Package (New)**. No restart or legacy Input Manager dependency is needed.

Use `Assets/_DrillRunner/Settings/DrillRunner.inputactions`, map **Gameplay**:

| Action | Type | Mouse | Touch |
| --- | --- | --- | --- |
| `Charge` | Button, default interaction | Left button | Primary touch press |
| `Steer` | Vector2 value, pixel delta | Mouse delta | Primary touch delta |

The input asset and all its IDs/bindings are unchanged from M0. `DrillPointerInput` owns one private runtime clone and pairs subscriptions with enable/disable. It accepts a fresh mouse or primary-touch press, ignores the first drag delta, normalizes horizontal displacement by screen width, and gates steering to a held pointer after launch. Delta is displacement, so it is not multiplied by elapsed time. Vertical delta is ignored.

`Charge.canceled` alone does not launch: release is confirmed in Update against the owned physical button/touch phase. Touch cancellation, device resets/removal, focus loss, pause, disabling, and restart clear the gesture. No pointer takeover occurs when another finger is held. A fresh press is required after cancellation. The Restart button is excluded through a synchronous UI raycast before beginning a gameplay gesture. Noninteractive HUD text and the bar allow pointer input through.

There is no generated wrapper or PlayerInput component. The original template project-wide input reference is preserved. Its existing **UI** map supplies the standard Input System UI module; its Player map is not consumed by the drill.

## Movement and camera tuning

Select the drill's `DrillMotor` component (or its prefab) to tune these validated settings. Movement settings are captured at Play startup; re-enter Play after editing them.

| Setting | Default |
| --- | --- |
| Full charge duration | 1.5 seconds; clamps and never wraps |
| Cruising speed | 10 units/second |
| Launch boost | +2 to +12 units/second based on charge, including a zero-duration tap |
| Speed ceiling | 24 units/second; the configured launch peaks at 22 |
| Boost decay | Exponential rate 1.8/second, converges smoothly to cruise |
| Track width / drill half footprint | 6 / 0.85 units; center bounds -2.15 to +2.15 |
| Steering displacement | 8 world units per full screen width; continuous, no lanes |
| Steering response | 18/second; releasing cancels remaining lateral smoothing |
| Resume acceleration after obstruction | 12 units/second squared, up to normal cruise |

`DrillMotion` integrates boost distance analytically and uses exponential horizontal response. `DrillMotor` remains the sole transform movement owner; no Rigidbody or gravity is used. It splits elapsed time into intervals of at most 1/120 second, including the final fraction so high-frame-rate steering remains responsive. Its existing state now also represents Drilling, Blocked, and RunEnded. Launching continues to use M1's Moving state with an initial boost.

`RunnerCamera` exposes offset **(0, 6, -9)**, forward response **8/second**, horizontal response **5/second**, horizontal follow fraction **0.25**, look-ahead **6**, and look height **0.65**. FOV is **60 degrees**. LateUpdate follows translation with a stable rotation, and reset snaps to the initial composition. No Cinemachine, shake, zoom, or camera packages were added.

## M2 drilling and energy

The strict rule is **Drill Power >= Hardness**. Speed and launch boost never enter the damage calculation. Material definitions are ScriptableObjects with stable IDs; obstacle durability is separate run data, so drilling never modifies the shared asset.

| Material ID | Hardness | Durability | Drilling energy/second | Time at power 5 |
| --- | --- | --- | --- | --- |
| `wood` | 2 | 70 | 8 | 1.4 s |
| `brick` | 5 | 140 | 12 | 2.8 s |
| `stone` | 9 | 240 | 18 | Cannot drill |

Initial **power is 5**, damage is **power × 10 per second**, and energy capacity is **100**. At power 10, Stone takes **2.4 seconds** and approximately **43.2 drilling energy**. These are prototype values, not final balance.

Central forward-tip contact stops progression and enters Drilling. Steering is inactive until the target breaks or leaves contact. Side grazing stops physical penetration but keeps escape steering available; it does not initiate drilling. Too-hard materials take zero damage and enter Blocked. Moving clear restores forward movement with a short acceleration ramp. One target controls drilling at a time; breaking disables its intact renderer and collider exactly once per run.

The controlled layout uses static, axis-aligned BoxColliders. `DrillContacts` performs axis-separated swept footprint checks (lateral first, then forward), with 0.005-unit clearance. It checks the full travel distance, including thin obstacles; it does not rely on Rigidbody collision callbacks, native query buffer capacity, or physics transform synchronization. Track borders are handled by the existing footprint bounds and never become drilling targets.

| State | Energy per second |
| --- | --- |
| Ready / Charging | 0 |
| Moving, including launch boost | 0.7 |
| Drilling | Material's rate only; no movement rate stacked |
| Blocked | 0.5, with no damage or drilling cost |
| RunEnded | 0; movement and damage stop |

Energy and durability use elapsed time, clamp at zero, and emit completion once. A drilling interval stops at target completion or energy exhaustion; any remainder after a break is simulated as movement. If energy ends first, the obstacle stays partially damaged until restart. If destruction and depletion occur at exactly the same instant, destruction completes and the run also ends. Restart restores all obstacle durability/visibility/colliders and full energy, clears contact and pending pointer state, and snaps the existing camera.

Invalid material data stays solid and undamageable with a clear warning. Invalid run settings are rejected before modifying an existing run. Invalid initial drilling configuration stops the motor with an error instead of permitting movement. Public motor simulation accepts finite intervals from 0 to 10 seconds; ordinary Unity frame deltas are well within this range.

For QA, select the drill root's **DrillMotor** Inspector while playing:

- **QA: Power 5 / Energy 100 - Restart** restores the default test configuration.
- **QA: Power 10 / Energy 100 - Restart** permits drilling Stone.
- **QA: Power 5 / Energy 8 - Restart** demonstrates depletion during the first Wood obstacle.

These buttons exist only in the Editor, reset the complete run, and revert on leaving Play Mode. No production upgrade UI or progression is added. Material assets can be tuned in the Inspector. Drilling/energy settings are captured on Restart; movement settings are captured on entering Play.

## Repeatable setup

M2 wiring has already run. **Tools > DrillRunner > Setup M2 Drilling Prototype** is the single M2 setup command. It adds missing material/obstacle assets, extends the existing drill prefab with a contact-tip reference, and wires the encounter and feedback into the existing scene/HUD. Existing component tuning, assets, references, GUIDs, and unrelated objects are preserved on reruns. A dirty loaded Gameplay scene is refused until saved. Conflicts produce errors instead of replacing user work. M0/M1 setup commands remain available and preserve the extended scene.

Batch M2 setup, from the project root (close this project in the Editor first):

```powershell
$unityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.3.8f1\Editor\Unity.exe'
& $unityEditor -batchmode -nographics -quit -projectPath . -executeMethod DrillRunner.Editor.DrillingPrototypeSetup.Setup -logFile Logs/M2-Setup.log
```

No setup runs automatically on import. Scene and prefab YAML are generated only by Unity. Replace the prefab's `Visuals` children for future art while retaining the movement root and configured footprint.

## Testing

Unity Test Framework **1.6.0** is already installed. In **Window > General > Test Runner**, run **EditMode > Run All**, then **PlayMode > Run All**. All original M0/M1 tests remain unchanged. M2 adds hardness/durability/energy rules, swept contact and state integration, missing/invalid data handling, and setup/restart preservation checks.

Batch run (close this project in the Editor first; omit `-quit` because the test runner exits when finished):

```powershell
& $unityEditor -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -assemblyNames DrillRunner.EditModeTests -testResults Logs/M2-EditMode.xml -logFile Logs/M2-EditMode.log
& $unityEditor -batchmode -projectPath . -runTests -testPlatform PlayMode -assemblyNames DrillRunner.PlayModeTests -testResults Logs/M2-PlayMode.xml -logFile Logs/M2-PlayMode.log
```

Run those processes sequentially. Graphical PlayMode tests capture 540 x 960 images under `Logs/`, including Drilling, Blocked, and RunEnded; with `-nographics`, numerical runtime checks still run but captures are omitted. Logs and test results are ignored local files. See `Docs/M2-Validation.md` for executed results and minimum QA. M0/M1 validation reports record the earlier accepted milestones.

## Source control

Unity Force Text serialization, Visible Meta Files, `.gitignore`, and `.gitattributes` are retained. Version `Assets` with all `.meta` files, `Packages`, `ProjectSettings`, and documentation. Unity caches, local settings, builds, logs, and generated IDE projects are ignored. No commit, push, remote changes, or Git LFS setup are part of M2.
