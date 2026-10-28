# DrillRunner

A portrait mobile 3D evolution runner built with **Unity 6000.3.8f1 (6.3 LTS)** and **Universal Render Pipeline 17.3.0**. The approved game has eight drill forms and four regions; the first vertical slice will cover one region and Form 1 to Form 2.

**M7-B brings encounter choices into the early Workshop Yard run:** a 40-meter introduction, 14 patterns across six categories, deterministic pacing, and readable safe/Copper alternatives. The M6-B presentation, continuous steering, hardness, Energy, rewards, upgrades, evolution, and saves retain their approved rules. Stop here for M7-B review; M8 and Android/release work are not started.

## Open and inspect

1. Open this directory with Unity Hub using 6000.3.8f1.
2. Open `Assets/_DrillRunner/Scenes/Gameplay.unity`.
3. Select the already verified **Portrait 9:16** Game View aspect and press **Play**.
4. Hold on the track to charge; release to launch. Drag horizontally to steer. Drill Wood/Brick and teal Copper Ore; bypass Stone at default power 5. The HUD tracks distance, Coins, and Copper. **RESTART** restores gameplay and clears the current run's course, rewards, and summary.

The Editor keeps this direct gameplay entry for M0–M3 development QA. Click **WORKSHOP** while Ready or after a completed run. Player builds open Workshop first. **START RUN** applies purchased stats immediately; hold/release still launches the drill. Completed-run Retry preserves banked balances. **QA RESET during an active attempt discards its unbanked earnings**. No Editor restart is needed to apply purchases.

Forward remains **+Z**, steering **X**, and track height **Y = 0**. Normal M7-B uses the first 40-meter starter with Wood at Z=35, then seeded encounters from Z=40. The other three original starter modules remain available for legacy QA and are inactive during normal play. Energy limits normal run duration. The old 20 km slab/guides remain inactive reference objects and provide no gameplay surface or collision.

Portrait orientation and a 540 x 960 default player window remain configured. The Windows build target is retained; Android has not been selected. If the local aspect preference is missing on another machine: **Game View > aspect dropdown > + > Aspect Ratio > 9 by 16**, label it **Portrait 9:16**, save and select.

`Gameplay` is the first enabled global build scene; the template `SampleScene` remains enabled after it. Setup also includes Gameplay in an active Unity 6 build profile if that profile overrides global scenes; inactive profiles are untouched.

## Structure

All project-owned production assets live under `Assets/_DrillRunner/`:

| Folder | Purpose |
| --- | --- |
| `Runtime/` | Existing gameplay/progression plus bounded presentation audio/effects, drill animation, obstacle appearance, and HUD presentation |
| `Editor/` | Earlier tooling, one M6 visual-polish setup command, procedural art/audio generation, and existing isolated QA Inspector controls |
| `Tests/EditMode/` | Editor-only `DrillRunner.EditModeTests`, excluded from player builds |
| `Tests/PlayMode/` | Runtime movement/input/UI/camera, drilling/contact/energy/restart integration tests |
| `Scenes/`, `Settings/`, `Materials/` | Gameplay scene, unchanged gameplay input asset, URP prototype materials |
| `Prefabs/`, `Models/` | Drill/four obstacle prefabs, 11 preserved `TrackSections/` prefabs, 14 new `Encounters/` prefabs, shared presentation meshes |
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

`TrackStreamer` keeps **200 units ahead** and **80 behind**, including an open rear section for the camera. Normal M7-B prefills actual encounters from Z=40; spawn-ahead coverage does not delay their start. An explicit legacy catalog override retains the original four starters and Z=160–200 buffer. It creates one section roughly every four seconds at cruise, unregisters/deactivates retired sections, and destroys generated ones. Authored starter roots are retained for clean replay. Default active section counts remain at or below 10, including a protected interaction. No large pooling framework or giant replacement track is used. Streaming stops after RunEnded and pauses automatic extension while the motor component is disabled for manual simulation.

`TrackSafety` validates complete-footprint routes, central alignment for drillable materials, real Stone bypasses, nonoverlapping placements, transition clearances, and reachable Copper at initial power 5. The validator constructs a conservative constant-X route certificate rather than accepting visual gaps. Catalog variants have clearance for changing routes between sections. An unsafe selected variant becomes the known-safe Open fallback; an invalid fallback/catalog stops the attempt with an error.

The run uses a private xorshift sequence, independent of Unity's global random state. On **M3 Workshop Run > RunProgress**, fixed seed **1337** is enabled by default. Edit **Configured Seed**, then use **QA: Replay configured seed** to reproduce it, or **QA: New random seed** for a fresh attempt. The active seed appears in the Inspector and Console. Matching seeds require unchanged catalog/material/pacing configuration. Normal M7-B offers Copper within three encounter selections; explicit legacy selection retains its original Copper schedule.

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

Workshop is a portrait panel on the existing HUD Canvas. It reuses the same motor, input adapter, camera, and M3 tracker. It shows permanent Coins/Copper, the current form preview, three upgrade rows, current/maximum levels, current/next stats, prices, MAX states, evolution requirements, and Start Run. Buying/evolving is available only in Workshop between attempts. Coins buy normal upgrades; Copper buys the single supported evolution.

Tune `Assets/_DrillRunner/ScriptableObjects/PocketDrillProgression.asset` outside Play Mode:

| Category | Values at levels 0–5 | Costs for levels 1–5 (Coins) |
| --- | --- | --- |
| Drill Power | 5, 6, 7, 8, **9**, 10 | 60, 100, 160, 240, 340 |
| Energy Capacity | 100, 115, 130, 145, 160, 180 | 50, 90, 140, 210, 300 |
| Drive / cruise speed | 10, 10.4, 10.8, 11.2, 11.6, 12 | 50, 90, 140, 210, 300 |

Drive also increases minimum launch boost from 2 to 3 and post-contact acceleration from 12 to 22. Maximum boost stays 12; the existing speed ceiling stays 24. Drill Power alone controls hardness and damage. Level 4 Power 9 can destroy unchanged-hardness-9 Stone. Total cost to maximize all three categories is 2,480 Coins. These are prototype prices.

`ProgressionService` owns permanent transactions. It saves a candidate state before publishing it; failure leaves currency and levels unchanged. Completed-run banking uses a persisted monotonic attempt receipt, making repeated completion, Retry, and Workshop visits idempotent. A failed bank leaves the completed ledger frozen, offers **RETRY SAVE**, and blocks reset/new-run/purchases until successful.

The local file is **`Application.persistentDataPath/drillrunner-progress.json`**, schema version **1**, with one previous revision in `.bak`. Select **M4 Workshop** in Play Mode to inspect the exact platform-specific path. The JSON envelope contains a payload and SHA-256 corruption check. Writes flush a unique staging file, then atomically replace the primary and retain a backup under an exclusive writer lock. No PlayerPrefs or cloud service is used. Invalid/corrupt/unsupported saves are preserved read-only; a valid backup can be recovered with explicit confirmation in the Editor Inspector, archiving the rejected primary.

For upgrade/evolution QA without editing JSON or affecting real progress, select **M4 Workshop**, set **Temporary Copper** (default 24; use 0 for a missing-resource check), then confirm **QA: Temporary funded profile (no disk writes)**. This gives 10,000 temporary Coins and the selected Copper. Buy upgrades and evolve, then leave Play Mode to return to real progression. Neither temporary purchases, evolution, nor banked rewards reach the local save. M2 temporary power/energy QA runs also cannot bank permanent earnings.

**Tools → DrillRunner → Setup M4 Workshop** has already run. It is idempotent, preserves earlier scene references and tuning, and requires no manual wiring. Batch entry: `DrillRunner.Editor.WorkshopSetup.Setup` with the same Unity command pattern below. Automated M4 tests use memory stores or unique OS-temp directories, never the real game profile.

See `Docs/M4-Validation.md` for the accepted M4 checkpoint; `Docs/M5-Validation.md` records current evolution behavior and validation.

## M5 evolution and Workshop Drill

**EVOLVE requires Pocket Drill Power 5/5, Energy 5/5, Drive 5/5, and at least 12 Copper.** The requirement panel explains missing conditions. The progression service calculates eligibility and commits the form change, Copper deduction, and new tier atomically. There is no extra Coin fee. Repeated clicks, active-run requests, save failures, and interrupted presentation cannot repeat or partially apply the transaction.

Pocket upgrade levels remain saved at MAX as history. Workshop levels start at zero, using stronger absolute tier values; old bonuses are neither removed nor added twice. The original 2,480-Coin Pocket upgrade budget is unchanged. A guided effective-player simulation reached evolution in six runs, earning 2,704 Coins and 12 Copper without funding, then purchased the first Workshop Power upgrade. Human pacing still needs manual QA.

| Workshop Drill category | Values at levels 0–5 | Coin costs for levels 1–5 |
| --- | --- | --- |
| Drill Power | **12**, 13, 14, 15, 16, 18 | 150, 230, 340, 480, 650 |
| Energy Capacity | **200**, 220, 240, 260, 280, 300 | 130, 210, 310, 440, 600 |
| Drive cruise | **12.2**, 12.35, 12.5, 12.65, 12.8, 13 | 130, 210, 310, 440, 600 |

Workshop minimum launch boost rises from 3.25 to 4.25; maximum boost from 12.25 to 13; recovery acceleration from 24 to 34. Its conservative speed ceiling is **26**. Pocket stays at 24. Both use the same full 1.7×3 horizontal collision footprint, strict hardness rules, input, camera, and modular tracks. Visual-only child models have no decorative colliders or extra controller. Static rendered previews reuse the same models without a second runtime camera.

Tune the existing Pocket progression asset and `Assets/_DrillRunner/ScriptableObjects/WorkshopDrillProgression.asset`. Only these two forms exist. The evolved tier has no next evolution or functional EVO 3 control. The short presentation locks controls for 0.9 seconds; the save completes before it begins. Reload/interrupt restores the committed form directly.

Save envelope/payload version remains **1**, extended additively with `FormSchema = 1` and three Workshop-level fields. Original M4 Pocket profiles (extension absent/zero) retain Coins, Copper, identity, upgrade levels, revision, and banking receipts. Missing new counters become zero; no migration-only disk rewrite occurs. The next normal transaction stores the extension through the unchanged atomic save/backup mechanism. Unsupported versions, extension versions, identifiers, or inconsistent form histories remain read-only with original data preserved.

**Tools → DrillRunner → Setup M5 Evolution** has already created/wired all assets and UI through Editor APIs. It is idempotent and does not access player saves. No manual references are required. Batch entry: `DrillRunner.Editor.EvolutionSetup.Setup`; graphical Editor execution is needed only when initially generating model preview textures. Existing previews allow subsequent setup/test runs without graphics.

## Repeatable setup

M3 wiring has already run. **Tools > DrillRunner > Setup M3 Modular Track** is the single M3 setup command. It creates section/Copper assets, converts the existing opening into connected starter prefab instances, wires one streamer/run tracker, and extends the existing HUD. Earlier test assets and original GUIDs are preserved. Rerunning validates existing references/catalog without rewriting the scene or tuning. A dirty loaded scene is refused until saved. Earlier setup commands remain available and preserve the extended scene.

Batch M3 setup, from the project root (close this project in the Editor first):

```powershell
$unityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.3.8f1\Editor\Unity.exe'
& $unityEditor -batchmode -nographics -quit -projectPath . -executeMethod DrillRunner.Editor.ModularTrackSetup.Setup -logFile Logs/M3-Setup.log
```

No setup runs automatically on import. Scene and prefab YAML are generated only by Unity. Replace the prefab's `Visuals` children for future art while retaining the movement root and configured footprint.

## M6 setup

M6 is already wired. **Tools → DrillRunner → Setup M6 Visual Polish** is the single polish setup action; reruns preserve configured scene/assets and never access player saves. Initial setup renders model previews and needs a graphical Editor; an already configured scene can be verified without graphics. Batch entry: `DrillRunner.Editor.VisualPolishSetup.Setup`.

## M6 visual identity and game feel

Pocket Drill uses rounded orange painted parts, rubber tracks, ivory trim, and a silver fluted bit. Workshop Drill uses a reinforced teal hull, dark motor housing, metal armor, and copper rings. Shared generated meshes/materials keep silhouettes consistent; only the rotor and housing animate. Both retain the original logical footprint and one motor/input/camera. Charging escalates spin and the real meter; launch gives a short housing response; drilling adds restrained resistance motion. Blocked stops successful-drilling effects and uses a separate orange/text state and rejection sound.

Wood uses warm planks/grain; Brick uses segmented masonry; Stone uses cool slate/veins; Copper Ore uses mineral teal with raised copper crystals. Existing durability/hardness/reward data is unchanged. Damage stages add local cracks/tint, then the intact renderers disappear immediately on the existing break event. Chips and the Copper toast observe authoritative events/ledger values and award nothing themselves.

Workshop Yard has continuous matte deck/ground, cross-deck panel joints, narrow boundaries, and lightweight props outside the playable path. Geometry is combined by shared material. No lane restrictions, decorative colliders, new obstacles, or generation changes are introduced. Warm directional lighting, ambient fill, and distance fog improve depth; camera position, FOV, rotation, and smoothing remain unchanged. No shake or full-screen post-processing is added.

Workshop, HUD, and Results use ivory cards, ink text, teal actions, and copper/gold emphasis. Resource totals, next stats/prices, MAX, evolution requirements, Power/Energy, charge, and results remain authoritative. A brief evolution glow follows the existing save-before-presentation transaction; skipping/reloading remains safe. Active-run RESET still discards unbanked QA attempt rewards; completed RETRY preserves banked balances.

Ten original mono PCM sounds cover charge, launch, drilling, rejection, normal break, Copper break, Copper reward, purchase, evolution completion, and run finish. They are synthesized locally by Editor code; no sound library was downloaded. Material pitch variations use no gameplay randomness. Loops change once per state and stop on contact exit, Workshop, end, retry, focus loss, pause, or disable. There is **one loop source and four reusable one-shot voices**; new one-shots replace the oldest voice. Music is not included.

VFX uses **one capped contact system and four reusable break systems**, with **112 particles maximum** in total. Wood splinters, brick/stone chips, and taller copper fragments vary in shape/color. No debris GameObjects, runtime meshes, material clones, physics fragments, or extra real-time lights are spawned. Sounds and particles never own currency or damage. The new `M6Presentation` asset holds sound/material/color references; particle settings remain editable on the authored systems.

See `Docs/M6-Validation.md` for representative 9:16 renders, before/after evidence, exact test results, desktop profiler caveats, audio validation limits, and four focused manual QA checks. No mobile FPS or physical-device/audio-mix validation is claimed.

## M7 / M7-B encounter variety

Run **Tools > DrillRunner > Setup M7 Gameplay Variety** to validate/integrate the authored library. The Gameplay scene is already wired; setup migrates the existing M7 assets to revision 2, then reruns validate without writes. One 40-meter starter introduces the drill; encounter sections begin at Z=40, with multi-target choices around Z=60 and Copper before Z=150. The first three selections avoid Stone; the full vocabulary is eligible from the fourth section at Z=160. These phases follow section ordinals rather than movement-coordinate gates.

The six categories are **Easy Flow**, **Dense Drilling**, **Risk/Reward**, **Precision Escape**, **Power Test**, and **Recovery**. Left/right variants are arrangements, not lanes. Flow arcs, dense clusters, simultaneous Wood/Ore choices, and staggered Stone vary the silhouette. Guarded Copper now has a separate cheap Wood option: at Power 5, Wood costs 11.2 drilling Energy versus 51.6 for Brick plus Ore. A collision-free bypass remains available. Other patterns retain unguarded Ore. Recovery provides lower pressure without Energy regeneration; Stone still requires Power 9.

`ScriptableObjects/Encounters/WorkshopYardEncounters.asset` holds the existing catalog reference, 14 pattern definitions, weights, pacing limits, and reaction/steering allowances. Each definition references an authored section prefab and an open-bypass witness. Mining routes are certified at starting Power. Entry/transition checks use the full footprint and steering response, with maximum accepted speed 26; they establish available choices under stated reaction assumptions, not safety for arbitrary input. The complete validation and limitations are in `Docs/M7-Validation.md`.

Selection prevents consecutive identical categories, remembers the last two arrangement families, follows high pressure with pressure-zero relief, offers Copper within three selections, and includes Recovery within six. Empty Recovery has at least four intervening selections. Family memory can relax when required to honor safety/resource/pacing rules. The first encounter is calm; later weights favor some richer arrangements without changing material stats or automatically scaling difficulty with Power. The existing `TrackStreamer` owns all spawning/registration/cleanup. Inspector diagnostics on `RunProgress` show the current encounter, seed, validation/fallback counts, and existing replay buttons.

The original catalog and 11 legacy sections remain intact. An explicit runtime assignment of a different legacy catalog uses that catalog's original selection unless a matching encounter library is also assigned. This preserves existing controlled-content QA and authoring overrides; normal Gameplay uses M7.

## Automated verification

Unity Test Framework **1.6.0** is already installed. In **Window > General > Test Runner**, run **EditMode > Run All**, then **PlayMode > Run All**. All 203 M0–M7 regressions remain, with narrow fixture adaptations for the shorter opening documented in `Docs/M7-Validation.md`. M7-B adds ten checks for early choices, cadence, routes, frozen-M7 comparisons, progression, and natural runtime captures. Tests use memory or unique OS-temp stores, preserving the actual profile. Current results: **139 EditMode / 74 PlayMode passed**, with no failures or skips. See the M7-B revision section of `Docs/M7-Validation.md` for evidence and limits.

Batch run (close this project in the Editor first; omit `-quit` because the test runner exits when finished):

```powershell
& $unityEditor -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -assemblyNames DrillRunner.EditModeTests -testResults Logs/M7B-EditMode.xml -logFile Logs/M7B-EditMode.log
& $unityEditor -batchmode -projectPath . -runTests -testPlatform PlayMode -assemblyNames DrillRunner.PlayModeTests -testResults Logs/M7B-PlayMode.xml -logFile Logs/M7B-PlayMode.log
```

Run those processes sequentially. Graphical tests capture 540 x 960 images under `Logs/`; `-nographics` omits captures. Logs/results are ignored local files. See `Docs/M3-Validation.md` for executed results and minimum QA. Earlier validation reports record the accepted M0–M2 milestones.

## Source control

Unity Force Text serialization, Visible Meta Files, `.gitignore`, and `.gitattributes` are retained. Version `Assets` with all `.meta` files, `Packages`, `ProjectSettings`, and documentation. Unity caches, local settings, builds, logs, and generated IDE projects are ignored. No commit, push, remote changes, or Git LFS setup are part of M7/M7-B.
