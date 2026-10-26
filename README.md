# DrillRunner

A portrait mobile 3D evolution runner built with **Unity 6000.3.8f1 (6.3 LTS)** and **Universal Render Pipeline 17.3.0**. The approved game has eight drill forms and four regions; the first vertical slice will cover one region and Form 1 to Form 2.

**M1 contains a playable launch, movement, steering, and camera prototype.** Drilling, obstacles, resources, economy, upgrades, energy, and evolution are not implemented. Stop here for review before M2.

## Open and inspect

1. Open this directory with Unity Hub using 6000.3.8f1.
2. Open `Assets/_DrillRunner/Scenes/Gameplay.unity`.
3. Select the already verified **Portrait 9:16** Game View aspect and press **Play**.
4. Hold on the track to charge; release to launch. After launching, press and drag horizontally to steer. Release to stop steering. Click **RESTART** to return the drill, charge, speed, input, and camera to Ready.

The scene contains the M0 camera/light/track, a primitive drill prefab, two visible track edges, static distance marks, and a temporary charge/restart HUD. Forward is **+Z**, steering is **X**, and the drill root remains at **Y = 0** on the track surface. Its stepped drill nose points forward. The 20 km slab supports about **33 minutes** of cruising; restart before reaching the end. This is a finite QA track with no runtime track generation.

Portrait orientation and a 540 x 960 default player window remain configured. The Windows build target is retained; Android has not been selected. If the local aspect preference is missing on another machine: **Game View > aspect dropdown > + > Aspect Ratio > 9 by 16**, label it **Portrait 9:16**, save and select.

`Gameplay` is the first enabled global build scene; the template `SampleScene` remains enabled after it. Setup also includes Gameplay in an active Unity 6 build profile if that profile overrides global scenes; inactive profiles are untouched.

## Structure

All project-owned production assets live under `Assets/_DrillRunner/`:

| Folder | Purpose |
| --- | --- |
| `Runtime/` | `DrillMotion` / serializable movement settings, `DrillMotor`, `DrillPointerInput`, `RunnerCamera`, `PrototypeHud` |
| `Editor/` | Editor-only M0 foundation tooling and one M1 scene/prefab setup command |
| `Tests/EditMode/` | Editor-only `DrillRunner.EditModeTests`, excluded from player builds |
| `Tests/PlayMode/` | Runtime scene/input/UI/camera integration tests; test-only assembly |
| `Scenes/`, `Settings/`, `Materials/` | Gameplay scene, unchanged gameplay input asset, URP prototype materials |
| `Prefabs/`, `Models/` | Replaceable primitive drill prefab and one static distance-mark mesh |
| `ScriptableObjects/`, `Textures/`, `Audio/`, `VFX/`, `UI/` | Asset destinations reserved for later milestones |

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

`DrillMotion` integrates boost distance analytically and uses exponential horizontal response. `DrillMotor` applies its result to a transform; no Rigidbody or gravity is used. `SetSteeringEnabled` provides a simple temporary steering gate for a future milestone without implementing drilling.

`RunnerCamera` exposes offset **(0, 6, -9)**, forward response **8/second**, horizontal response **5/second**, horizontal follow fraction **0.25**, look-ahead **6**, and look height **0.65**. FOV is **60 degrees**. LateUpdate follows translation with a stable rotation, and reset snaps to the initial composition. No Cinemachine, shake, zoom, or camera packages were added.

## Repeatable setup

M1 scene wiring has already run. **Tools > DrillRunner > Setup M1 Movement Prototype** is the single M1 setup command. It creates missing M1 objects/assets, extends the known M0 slab, uses the existing camera, and saves through Unity Editor APIs. Existing prefab, material, component settings, references, and unrelated scene objects are preserved on reruns. A dirty loaded Gameplay scene is refused until saved. Custom conflicting configurations produce an error rather than being replaced. The separate M0 command remains available and preserves the M1 scene.

Batch M1 setup, from the project root (close this project in the Editor first):

```powershell
$unityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.3.8f1\Editor\Unity.exe'
& $unityEditor -batchmode -nographics -quit -projectPath . -executeMethod DrillRunner.Editor.MovementPrototypeSetup.Setup -logFile Logs/M1-Setup.log
```

No setup runs automatically on import. Scene and prefab YAML are generated only by Unity. Replace the prefab's `Visuals` children for future art while retaining the movement root and configured footprint.

## Testing

Unity Test Framework **1.6.0** is already installed. In **Window > General > Test Runner**, run **EditMode > Run All**, then **PlayMode > Run All**. The original nine M0 tests remain unchanged. M1 adds movement/charge/steering/camera rules, scene wiring and repeatability checks, plus real scene lifecycle, synthetic mouse/touch, actual Restart button, and portrait render integration tests.

Batch run (close this project in the Editor first; omit `-quit` because the test runner exits when finished):

```powershell
& $unityEditor -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -assemblyNames DrillRunner.EditModeTests -testResults Logs/M1-EditMode.xml -logFile Logs/M1-EditMode.log
& $unityEditor -batchmode -projectPath . -runTests -testPlatform PlayMode -assemblyNames DrillRunner.PlayModeTests -testResults Logs/M1-PlayMode.xml -logFile Logs/M1-PlayMode.log
```

Run those processes sequentially. Graphical PlayMode tests capture 540 x 960 Ready/Charging/Moving images under `Logs/`; with `-nographics`, the numerical runtime checks still run but captures are omitted. Logs and test results are local ignored files. See `Docs/M1-Validation.md` for executed checks and limitations; `Docs/M0-Validation.md` records the earlier accepted foundation milestone.

## Source control

Unity Force Text serialization, Visible Meta Files, `.gitignore`, and `.gitattributes` are retained. Version `Assets` with all `.meta` files, `Packages`, `ProjectSettings`, and documentation. Unity caches, local settings, builds, logs, and generated IDE projects are ignored. No commit, push, remote changes, or Git LFS setup are part of M1.
