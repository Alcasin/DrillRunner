# M3 — modular track and per-run rewards

Implemented on `main` against accepted M2 commit `6e97400`. The starting working tree was clean. M3 preserves the existing movement, input, drilling, hardness, energy, and camera behavior. No M4 purchasing, storage, progression, or evolution is included.

## Files and assets

Paths below are relative to `Assets/_DrillRunner/`.

| Created | Purpose |
| --- | --- |
| `Runtime/TrackSection.cs` | Authored section dimensions, entrance/exit, surface, obstacle references, and content restoration |
| `Runtime/TrackSafety.cs` | Deterministic complete-footprint route and placement validation |
| `Runtime/TrackCatalog.cs` | Catalog configuration and an isolated seeded section sequence |
| `Runtime/TrackStreamer.cs` | Connected section creation, retention, cleanup, and obstacle registration |
| `Runtime/RunRewards.cs` | One in-memory ledger for distance, distance Coins, obstacle Coins, Copper, seed, and completion |
| `Runtime/RunProgress.cs` | Run lifecycle, seed selection, encounter identities, and reward-event ownership |
| `Editor/ModularTrackSetup.cs` | Single M3 setup menu/batch action |
| `Editor/RunProgressInspector.cs` | Editor-only fixed-seed replay/new-seed controls and active seed display |
| `Prefabs/TrackSections/` | Eleven authored prefabs: four starter modules and seven streamed variants |
| `ScriptableObjects/WorkshopYardCatalog.asset` | Workshop Yard catalog and Copper opportunity cadence |
| `ScriptableObjects/CopperOre.asset`, `Prefabs/ObstacleCopperOre.prefab`, `Materials/ObstacleCopperOre.mat` | Copper Ore using M2 drilling and a distinct teal placeholder |
| `Tests/EditMode/RunRewardsTests.cs`, `ModularTrackTests.cs` | Reward, determinism, safety, configuration, connection, and setup checks |
| `Tests/PlayMode/ModularRunPlayModeTests.cs` | Streaming/traversal, spawned drilling, resources, cleanup, completion, and restart integration |

Modified: `DrillMotor.cs` adds a dynamic obstacle registry and local lifecycle notifications; `MaterialDefinition.cs` adds an explicit Coin/Copper reward kind and amount; `PrototypeHud.cs` displays earnings and a summary; the Wood/Brick/Stone assets gain reward values; `Gameplay.unity` gains modular-course/run wiring. The root README is updated. Unity generated all `.meta` files and serialized all scene/prefab changes through Editor APIs.

The accepted input adapter/actions, movement math, contact solver, energy/durability rules, camera script, hero prefab, original obstacle prefabs, assembly definitions, package files, and project settings remain unchanged. All earlier test source files remain unchanged.

## Section architecture and scene integration

Every section is **6 units wide and 40 units long**, with local entrance `(0,0,0)`, exit `(0,0,40)`, and surface top at Y=0. Floors and edge visuals meet exactly at section boundaries. Each prefab owns authored obstacle placements and references M2 obstacle/material assets. There is no runtime mesh/terrain/voxel generation or lane-based steering.

The accepted opening is preserved as four authored starter prefab instances under the existing M2 Encounter root:

- Starter0: Wood at world Z=35.
- Starter1: Brick at Z=65.
- Starter2: Stone at Z=95.
- Starter3: Wood at Z=130.

The old long reference slab/guides remain **inactive** for earlier milestone inspection and tests. They provide no runtime surface, collision, rails, or distance marks. Gameplay depends on the connected modular surfaces instead. The original camera, input handler, motor, light, HUD Canvas, Restart button, and M2 object references are reused.

An open rear module covers Z=-40–0 for the camera. An open transition module covers Z=160–200. Seeded variants start at Z=200. The seven catalog variants are Open, WoodOffset, BrickOffset, Mixed, StoneBypass, Copper, and RiskReward. RiskReward offers reachable Copper on the right, Stone on the left, and a physically wide free central bypass.

## Streaming and cleanup

Defaults retain **200 units ahead** and **80 behind**. The initial course has six active sections. The default active count remains at or below **10**, including a protected interaction. Normal cruising creates one section about every four seconds, not large batches each frame.

`TrackStreamer` fills the tail, then removes sections behind the retention boundary. It never retires the section containing the player or the supplied current interaction target. Generated sections unregister their obstacles, deactivate immediately, and are destroyed at Unity's normal end-of-frame boundary. The four authored starter roots are retained inactive for replay; this is a constant amount of scene data. A generic pool was unnecessary for the small spawn cadence.

`DrillMotor.RegisteredObstacles` is the authoritative collision set. Registration is idempotent and pairs break subscriptions with unregistration. The existing serialized `Obstacles` array remains the authored opening references. The M2 swept-box contact solver is unchanged and handles generated obstacles through the same registry. Broken colliders/renderers stop blocking immediately. Retired and old-attempt obstacles cannot affect a new run.

Automatic coverage updates follow an enabled motor after simulation and before camera rendering. Disabling the motor pauses automatic streaming for manual simulation/QA; explicit coverage updates remain available. RunEnded stops further generation. Restart unregisters the old course, deactivates pending destruction objects, restores starters, resets generation/seed state, and builds the initial window without duplicating components or callbacks.

Configuration rejects incompatible dimensions, unsafe fallback data, insufficient camera coverage, and stream windows exceeding 32 sections. Very large diagnostic teleports are bounded to avoid an unbounded generation workload; normal forward streaming has no former 20 km limit.

## Determinism and route safety

The sequence uses a private xorshift state initialized from the active seed. It never consumes Unity's global random state. Equal seeds and unchanged catalog/material settings reproduce the same section selections at the same section positions, independent of cosmetic/UI activity. Large diagnostic jumps advance the same sequence without instantiating every skipped section.

Fixed seed **1337** is the default for reproducibility. On **M3 Workshop Run > RunProgress**, edit Configured Seed and use **QA: Replay configured seed**, or use **QA: New random seed**. Non-fixed attempts choose a fresh seed independently and avoid consecutive repeats. The active seed is displayed in the development Inspector and logged once per attempt. Catalog/settings references are captured for the attempt; edit authoring data deliberately and restart before expecting a new configuration.

The first procedural selection and every third selection thereafter offer Copper. Other draws cover all seven variants. Unsafe selected variants return the authored Open fallback instead of spawning a broken encounter. An invalid fallback or missing required safe Copper configuration stops the attempt with a clear error.

The validator checks:

- Finite, compatible width/length, active visible surface, aligned entrance/exit, and axis-aligned unscaled roots.
- Valid active obstacle/material references and compatible non-trigger colliders.
- Physical dimensions and nonoverlapping authored obstacles inside the section.
- Clearance for the player's complete **1.7-unit width**, with additional **0.05-unit route margin**.
- A constructive constant-X route through the section. Collisions along that route must either be bypassed with full clearance or permit central drilling at normal power 5; side-grazing positions are not accepted as drillable paths.
- Copper drillability and a certified approach/exit at its center.
- At least 4 units of boundary clearance for obstacles and 8 for non-drillable obstacles. Consecutive catalog pairs are tested against M1 lateral response and maximum speed, including the player's length, to support route changes between modules.

This certificate is intentionally conservative: it can reject a layout that a more complex planner might traverse. It does not approve a narrow visual gap as a safe route. It validates geometry, reachability, and hardness; it does not promise that a player can spend energy indefinitely or make every possible steering choice safely.

## Distance, Coins, and Copper

`RunRewards` owns all counters. A run starts after a valid launch. Distance is the monotonic maximum of actual world Z minus the initial Z, so lateral steering, stationary drilling/blocking, and backtracking do not produce extra distance. Coins are calculated from completed absolute distance intervals rather than summed fractional payouts.

| Source | Configuration / payout |
| --- | --- |
| Distance | **1 Coin per completed 5 units**, primary income |
| Wood destruction | **1 Coin** |
| Brick destruction | **2 Coins** |
| Stone destruction when power permits | **3 Coins** |
| Copper Ore destruction | **3 Copper**, no normal obstacle Coins |

Copper Ore has stable ID `copper_ore`, hardness **4**, durability **90**, and drilling cost **10 energy/s**. Initial power 5 drills it in **1.8 seconds**, costing **18 drilling energy**. It uses the exact M2 progressive damage, hardness, collision, steering-lock, and energy rules.

Reward kind and amount are explicit material data. Each registered encounter receives a run-scoped identity; full destruction can claim it once. Partial damage and repeated break notifications cannot pay. Restoring an already claimed obstacle within the same registered encounter cannot pay again. Old unregistered objects and events after completion cannot add current-run rewards.

On energy depletion, final forward progress is recorded before completion freezes distance, both Coin sources, Copper, and seed. The minimal HUD summary shows total distance, total Coins, and Copper with the existing Restart control. Restart closes the old ledger and creates a new empty one. No balance is written to PlayerPrefs, files, a generic inventory, or persistent progression.

## Editor automation

Executed **Tools > DrillRunner > Setup M3 Modular Track** with Unity 6000.3.8f1. No routine manual prefab/scene wiring remains.

The command preserves original GUIDs and unrelated objects, authors the new assets, converts the existing four opening obstacles into starter modules without changing their world poses, adds one streamer/run tracker, and extends the existing HUD. Reruns validate existing references/catalog without overwriting tuning or duplicating assets. A loaded dirty scene is refused before editing. Existing files with orphaned `.meta` files require restoration instead of GUID replacement.

Repeatability was checked both on a scene copy containing user-authored work and on the actual Gameplay scene. SHA-256 comparisons around the actual rerun covered the scene, catalog, Copper/normal material definitions, and every section prefab and `.meta` file; they remained unchanged.

## Executed validation

| Check | Result |
| --- | --- |
| Unity compilation and M3 scene/prefab setup | **Passed**, exit 0 |
| Unity EditMode | **71 passed, 0 failed, 0 skipped**; includes all **56 unchanged earlier tests** |
| Unity PlayMode | **29 passed, 0 failed, 0 skipped**; includes all **20 unchanged earlier tests** |
| Equal-seed replay, different-seed variation, independent global random state | **Passed** |
| Eleven authored module certificates and all consecutive catalog pairs | **Passed** |
| Narrow gaps/full-width Stone/unsafe Copper/boundary layouts rejected | **Passed** |
| Invalid/invisible surfaces rejected and unsafe selections fall back | **Passed** |
| Initial coverage, connection, runtime registration/unregistration, active target retention | **Passed** |
| Accelerated actual vehicle simulation beyond **30 km** | **Passed**, with bounded active sections and coverage ahead |
| Spawned Copper drilling, Copper-only reward, normal once-only rewards, frozen summary | **Passed** |
| Generated Stone blocking and whole-footprint escape | **Passed** |
| Restart from Moving/Drilling/Blocked/RunEnded, partial/destroyed targets, repeated cleanup | **Passed** |
| Invalid streaming input/configuration stops safely | **Passed** |
| 540 x 960 GPU renders of Copper drilling, Stone escape, and summary | **Generated and visually inspected** |
| Source/JSON checks, `.meta` coverage, unique GUIDs, original-file preservation audit | **Passed** |

Evidence is local and ignored by Git: `Logs/M3-Setup.log`, `M3-Setup-Repeat.log`, `M3-EditMode.log`, `M3-EditMode.xml`, `M3-PlayMode.log`, `M3-PlayMode.xml`, `M3-CopperDrilling.png`, `M3-StoneEscape.png`, `M3-Summary.png`, and `M3-Baseline.json`.

## Limitations and checks not executed

- No Android/player build, physical touchscreen QA, hardware focus/background QA, real-time endurance run, or performance profiling was performed. The long-distance test uses an accelerated simulation clock, not a claim of an uninterrupted real-time play session.
- The existing M2 axis-aligned box collision scope remains. Arbitrary rotated/dynamic obstacles, terrain, branching/curved tracks, and floating-origin support are not added. Very large world distances remain subject to Unity float precision.
- Authored templates/catalog data are expected to remain stable during an attempt. Restart after editing them for reproducibility. The route certificate is conservative rather than a general pathfinding system.
- Fresh generated section instances are created/destroyed at section cadence; no pool is implemented. Four inactive starter roots and the old inactive reference geometry remain intentionally bounded scene data.
- The Editor seed controls' underlying replay/new-seed behavior was tested; their physical Inspector-button clicks remain a brief manual QA action.
- Prototype reward rates, energy costs, and visuals are configurable test values, not final balance or art.
- No package, assembly definition, project setting, build target, input binding, camera configuration, Git setting, or remote configuration was changed. Unity's generated YAML may contain standard empty-value trailing spaces; serialized scene/prefab YAML was not hand-edited.

## Minimum manual QA

1. Open Gameplay, select **Portrait 9:16**, and press **Play**. No setup or GameObject wiring is required. Launch and play the preserved Wood/Brick/Stone opening; bypass Stone at power 5.
2. Continue through the open buffer into seeded sections beyond Z=200. Confirm smooth floor/edge connections and controlled layout variety. Drill teal Copper Ore and verify **+3 Copper**, with no obstacle Coin payout for that Ore. Watch distance Coins remain the larger normal income source.
3. Select **M3 Workshop Run > RunProgress**. Replay fixed seed **1337**, then try another configured seed or **QA: New random seed**. Compare the course and note the active seed in the Inspector/Console.
4. Let energy reach zero. Confirm distance, Coins, and Copper freeze in the summary and the course stops extending. Use Restart during Drilling, Blocked, Moving, and RunEnded; verify zero rewards, full energy, restored opening, and a clean new course.

M3 stops here for review. No commit or push was performed. M4 is not started.
