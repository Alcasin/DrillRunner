# DrillRunner

A portrait mobile 3D evolution runner built with **Unity 6000.3.8f1 (6.3 LTS)** and **Universal Render Pipeline 17.3.0**. The approved game has eight drill forms and four regions; the first vertical slice will cover one region and Form 1 to Form 2.

**M4 adds a Workshop, permanent Coin/Copper balances, and Pocket Drill power, energy, and drive upgrades to the accepted M3 gameplay.** Completed runs bank once to a versioned local save. Evolution and new forms/regions are deferred to M5. Stop here for M4 review.

## Open and inspect

1. Open this directory with Unity Hub using 6000.3.8f1.
2. Open `Assets/_DrillRunner/Scenes/Gameplay.unity`.
3. Select the already verified **Portrait 9:16** Game View aspect and press **Play**.
4. Hold on the track to charge; release to launch. Drag horizontally to steer. Drill Wood/Brick and teal Copper Ore; bypass Stone at default power 5. The HUD tracks distance, Coins, and Copper. **RESTART** restores gameplay and clears the current run's course, rewards, and summary.

The Editor keeps this direct gameplay entry for M0–M3 development QA. Click **WORKSHOP** while Ready or after a completed run. Player builds open Workshop first. **START RUN** applies purchased stats immediately; hold/release still launches the drill. Completed-run Retry preserves banked balances. **QA RESET during an active attempt discards its unbanked earnings**. No Editor restart is needed to apply purchases.

Forward remains **+Z**, steering **X**, and track height **Y = 0**. The accepted M2 opening is now four connected authored modules: Wood at Z=35, Brick at Z=65, Stone at Z=95, and Wood at Z=130. Beyond an open transition buffer, seeded Workshop Yard sections continue indefinitely. Energy limits normal run duration. The old 20 km slab/guides remain inactive reference objects and provide no gameplay surface or collision.

Portrait orientation and a 540 x 960 default player window remain configured. The Windows build target is retained; Android has not been selected. If the local aspect preference is missing on another machine: **Game View > aspect dropdown > + > Aspect Ratio > 9 by 16**, label it **Portrait 9:16**, save and select.

`Gameplay` is the first enabled global build scene; the template `SampleScene` remains enabled after it. Setup also includes Gameplay in an active Unity 6 build profile if that profile overrides global scenes; inactive profiles are untouched.

## Structure

All project-owned production assets live under `Assets/_DrillRunner/`:

| Folder | Purpose |
| --- | --- |
| `Runtime/` | Existing M0–M3 gameplay plus progression model/configuration, file store, transaction service, and Workshop controller/view |
| `Editor/` | Earlier milestone tooling, one M4 setup command, power/energy, seed, and isolated development-profile Inspector controls |
| `Tests/EditMode/` | Editor-only `DrillRunner.EditModeTests`, excluded from player builds |
| `Tests/PlayMode/` | Runtime movement/input/UI/camera, drilling/contact/energy/restart integration tests |
| `Scenes/`, `Settings/`, `Materials/` | Gameplay scene, unchanged gameplay input asset, URP prototype materials |
| `Prefabs/`, `Models/` | Drill/four obstacle prefabs, 11 authored `TrackSections/` prefabs, archived distance-mark mesh |
| `ScriptableObjects/` | Wood/Brick/Stone/Copper Ore definitions and Workshop Yard track catalog |
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

For power/energy QA, select the drill root's **DrillMotor** Inspector while playing:

- **QA: Power 5 / Energy 100 - Restart** restores the default test configuration.
- **QA: Power 10 / Energy 100 - Restart** permits drilling Stone.
- **QA: Power 5 / Energy 8 - Restart** demonstrates depletion during the first Wood obstacle.

These buttons exist only in the Editor, reset the complete run, and revert on leaving Play Mode. M4 marks these temporary override attempts as unable to bank permanent earnings. They never change saved upgrade levels. Material assets can be tuned in the Inspector. Drilling/energy and movement settings are captured on Restart; normal M4 Start Run derives effective attributes from loaded progression.

## M3 modular course and rewards

All modules are **6 units wide × 40 units long**, with entrance at local Z=0 and exit at Z=40. The four starter prefabs preserve the accepted opening. Seven streamed variants provide Open, offset Wood, offset Brick, Mixed, Stone Bypass, Copper, and Risk/Reward encounters. They use the same M2 obstacle components and collision rules; steering remains continuous.

`TrackStreamer` keeps **200 units ahead** and **80 behind**, initially including an open rear section for the camera and an open section at Z=160–200. Seeded variants begin at Z=200. It creates one section roughly every four seconds at cruise, unregisters/deactivates retired sections, and destroys generated ones. The four authored starter roots are retained inactive for clean replay. Default active section counts remain at or below 10, including a protected interaction. No large pooling framework or giant replacement track is used. Streaming stops after RunEnded and pauses automatic extension while the motor component is disabled for manual simulation.

`TrackSafety` validates complete-footprint routes, central alignment for drillable materials, real Stone bypasses, nonoverlapping placements, transition clearances, and reachable Copper at initial power 5. The validator constructs a conservative constant-X route certificate rather than accepting visual gaps. Catalog variants have clearance for changing routes between sections. An unsafe selected variant becomes the known-safe Open fallback; an invalid fallback/catalog stops the attempt with an error.

The run uses a private xorshift sequence, independent of Unity's global random state. On **M3 Workshop Run > RunProgress**, fixed seed **1337** is enabled by default. Edit **Configured Seed**, then use **QA: Replay configured seed** to reproduce it, or **QA: New random seed** for a fresh attempt. The active seed appears in the Inspector and Console. Matching seeds require unchanged catalog/material configuration. The first procedural section and every third selection thereafter offer reachable Copper.

| Reward source | Per-run payout |
| --- | --- |
| Forward distance | 1 Coin per completed 5 units; primary income source |
| Wood destruction | 1 Coin |
| Brick destruction | 2 Coins |
| Stone destruction, when power permits | 3 Coins |
| Copper Ore destruction | 3 Copper and **0 obstacle Coins** |

Copper Ore has hardness **4**, durability **90**, and drilling cost **10 energy/s**. At power 5 it takes **1.8 seconds** and **18 drilling energy**. Its teal material distinguishes it from normal obstacles. Rewards are configured by `MaterialDefinition.RewardKind` and `RewardAmount`; distance parameters live on RunProgress's Reward Settings.

`RunRewards` remains the only per-run reward ledger. Distance uses maximum forward Z progress from the run start, ignoring lateral movement, pauses, and backtracking. Integer interval payouts avoid accumulated fractional Coin rounding. Obstacle encounter IDs prevent repeat payouts; partial damage, restoration, stale unregistered objects, and post-completion callbacks cannot add rewards. Completing the run freezes distance, both Coin sources, Copper, and seed. M4 banks that completed ledger once into separate permanent balances. Restart creates an empty ledger and clean course.

The HUD shows temporary earnings, effective Power, current/max Energy, a frozen completion summary, and banking status. M4 adds Workshop and Retry navigation to it.

## M4 Workshop and persistent progress

Workshop is a portrait panel on the existing HUD Canvas. It reuses the same motor, input adapter, camera, and M3 tracker. It shows permanent Coins/Copper, a Pocket Drill placeholder, three upgrade rows, current/maximum levels, current/next stats, prices, MAX states, and Start Run. Buying is available only in Workshop between attempts. Copper is retained for M5 and is never spent in M4.

Tune `Assets/_DrillRunner/ScriptableObjects/PocketDrillProgression.asset` outside Play Mode:

| Category | Values at levels 0–5 | Costs for levels 1–5 (Coins) |
| --- | --- | --- |
| Drill Power | 5, 6, 7, 8, **9**, 10 | 60, 100, 160, 240, 340 |
| Energy Capacity | 100, 115, 130, 145, 160, 180 | 50, 90, 140, 210, 300 |
| Drive / cruise speed | 10, 10.4, 10.8, 11.2, 11.6, 12 | 50, 90, 140, 210, 300 |

Drive also increases minimum launch boost from 2 to 3 and post-contact acceleration from 12 to 22. Maximum boost stays 12; the existing speed ceiling stays 24. Drill Power alone controls hardness and damage. Level 4 Power 9 can destroy unchanged-hardness-9 Stone. Total cost to maximize all three categories is 2,480 Coins. These are prototype prices.

`ProgressionService` owns permanent transactions. It saves a candidate state before publishing it; failure leaves currency and levels unchanged. Completed-run banking uses a persisted monotonic attempt receipt, making repeated completion, Retry, and Workshop visits idempotent. A failed bank leaves the completed ledger frozen, offers **RETRY SAVE**, and blocks reset/new-run/purchases until successful.

The local file is **`Application.persistentDataPath/drillrunner-progress.json`**, schema version **1**, with one previous revision in `.bak`. Select **M4 Workshop** in Play Mode to inspect the exact platform-specific path. The JSON envelope contains a payload and SHA-256 corruption check. Writes flush a unique staging file, then atomically replace the primary and retain a backup under an exclusive writer lock. No PlayerPrefs or cloud service is used. Invalid/corrupt/unsupported saves are preserved read-only; a valid backup can be recovered with explicit confirmation in the Editor Inspector, archiving the rejected primary.

For upgrade QA without editing JSON or affecting real progress, select **M4 Workshop → QA: Temporary funded profile (no disk writes)**, then confirm. This gives a temporary 10,000-Coin profile. Buy upgrades, test them, then leave Play Mode to return to real progression. Neither temporary purchases nor its banked run rewards reach the local save. M2 temporary power/energy QA runs also cannot bank permanent earnings.

**Tools → DrillRunner → Setup M4 Workshop** has already run. It is idempotent, preserves earlier scene references and tuning, and requires no manual wiring. Batch entry: `DrillRunner.Editor.WorkshopSetup.Setup` with the same Unity command pattern below. Automated M4 tests use memory stores or unique OS-temp directories, never the real game profile.

See `Docs/M4-Validation.md` for implementation details, actual tests, limitations, and minimum manual QA. No M5 Evolution system is included.

## Repeatable setup

M3 wiring has already run. **Tools > DrillRunner > Setup M3 Modular Track** is the single M3 setup command. It creates section/Copper assets, converts the existing opening into connected starter prefab instances, wires one streamer/run tracker, and extends the existing HUD. Earlier test assets and original GUIDs are preserved. Rerunning validates existing references/catalog without rewriting the scene or tuning. A dirty loaded scene is refused until saved. Earlier setup commands remain available and preserve the extended scene.

Batch M3 setup, from the project root (close this project in the Editor first):

```powershell
$unityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.3.8f1\Editor\Unity.exe'
& $unityEditor -batchmode -nographics -quit -projectPath . -executeMethod DrillRunner.Editor.ModularTrackSetup.Setup -logFile Logs/M3-Setup.log
```

No setup runs automatically on import. Scene and prefab YAML are generated only by Unity. Replace the prefab's `Visuals` children for future art while retaining the movement root and configured footprint.

## Testing

Unity Test Framework **1.6.0** is already installed. In **Window > General > Test Runner**, run **EditMode > Run All**, then **PlayMode > Run All**. All original M0–M3 tests remain unchanged. M4 adds persistence, economy, banking, Workshop navigation, and upgraded gameplay tests. Tests use isolated stores, preserving the actual profile.

Batch run (close this project in the Editor first; omit `-quit` because the test runner exits when finished):

```powershell
& $unityEditor -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -assemblyNames DrillRunner.EditModeTests -testResults Logs/M3-EditMode.xml -logFile Logs/M3-EditMode.log
& $unityEditor -batchmode -projectPath . -runTests -testPlatform PlayMode -assemblyNames DrillRunner.PlayModeTests -testResults Logs/M3-PlayMode.xml -logFile Logs/M3-PlayMode.log
```

Run those processes sequentially. Graphical tests capture 540 x 960 images under `Logs/`; `-nographics` omits captures. Logs/results are ignored local files. See `Docs/M3-Validation.md` for executed results and minimum QA. Earlier validation reports record the accepted M0–M2 milestones.

## Source control

Unity Force Text serialization, Visible Meta Files, `.gitignore`, and `.gitattributes` are retained. Version `Assets` with all `.meta` files, `Packages`, `ProjectSettings`, and documentation. Unity caches, local settings, builds, logs, and generated IDE projects are ignored. No commit, push, remote changes, or Git LFS setup are part of M4.
