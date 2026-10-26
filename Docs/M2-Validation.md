# M2 — drilling, hardness, and energy

Implemented on `main` against approved M1 commit `b4069ab`. The starting working tree was clean. M2 extends the existing movement owner, input, camera, HUD, prefab, and scene. No M3 systems were introduced.

## Files and assets

Paths below are relative to `Assets/_DrillRunner/`.

| Created | Purpose |
| --- | --- |
| `Runtime/MaterialDefinition.cs` | Small ScriptableObject configuration with stable material ID, name, hardness, durability, drilling energy rate, feedback color, and validation |
| `Runtime/DrillRules.cs` | Serializable drilling settings, deterministic hardness/damage rules, per-obstacle durability and run energy models |
| `Runtime/DrillObstacle.cs` | Collider/renderer integration, safe invalid-material fallback, one break callback, full restoration |
| `Runtime/DrillContacts.cs` | Allocation-free analytical swept boxes for the static M2 layout |
| `Editor/DrillingPrototypeSetup.cs` | One M2 menu/batch setup action with repeatability and conflict checks |
| `Editor/DrillMotorInspector.cs` | Editor-only power and energy QA controls; no production cheat/upgrade UI |
| `ScriptableObjects/Wood.asset`, `Brick.asset`, `Stone.asset` | Three configurable material definitions |
| `Prefabs/ObstacleWood.prefab`, `ObstacleBrick.prefab`, `ObstacleStone.prefab` | Distinct simple URP boxes with stable non-trigger BoxColliders |
| `Materials/ObstacleWood.mat`, `ObstacleBrick.mat`, `ObstacleStone.mat` | Minimal material colors |
| `Tests/EditMode/DrillingRulesTests.cs`, `DrillingSceneTests.cs` | Hardness/damage/energy/geometry, scene, and setup preservation tests |
| `Tests/PlayMode/DrillingPlayModeTests.cs` | Runtime collision, states, energy, invalid-data handling, restart, and rendered feedback tests |

Modified: `DrillMotion.cs` (appended states, obstruction/recovery integration), `DrillMotor.cs` (sole authoritative contact/energy/movement pipeline), `PrototypeHud.cs` (energy/progress/state feedback), `DrillPrototype.prefab` (contact tip, existing BoxCollider reference, new settings), and `Gameplay.unity` (encounter and HUD wiring). Root `README.md` was updated. Unity generated all asset and folder `.meta` files.

The original input adapter, input assets, camera script, M0/M1 setup scripts, assembly definitions, original tests, URP settings/assets, and existing render materials remain unchanged. Existing asset GUIDs are retained. Scene/prefab serialization was performed only through Unity Editor APIs.

## Rules and prototype values

| Material | Stable ID | Hardness | Durability | Drilling energy/s | Time at power 5 | Time at power 10 |
| --- | --- | --- | --- | --- | --- | --- |
| Wood | `wood` | 2 | 70 | 8 | 1.4 s | 0.7 s |
| Brick | `brick` | 5 | 140 | 12 | 2.8 s | 1.4 s |
| Stone | `stone` | 9 | 240 | 18 | Blocked | 2.4 s |

Default Drill Power is **5**; damage/second is **Drill Power × DamagePerPower**, with DamagePerPower **10**. Hardness is a strict prerequisite, including equality. Speed/launch boost do not participate in damage. Higher power increases damage rate while the material's per-second cost stays fixed, generally reducing total drilling energy. These values are prototype balance only.

Material IDs/names must be present. Hardness and energy rates must be finite and nonnegative; durability, Drill Power, damage factor, and energy capacity must be finite and positive. Invalid time/rate/damage values are rejected before changing mathematical state. Invalid material definitions stay solid and cannot take damage, with a diagnostic warning. Invalid initial run settings stop the motor safely with an error. Invalid edits are rejected before resetting an existing run.

The material asset is immutable during damage: each obstacle has separate run durability. Damage is elapsed-time-based, clamped, and break notification occurs once per run. On break, the renderer and collider are disabled; the object remains available for restart. No reward event, debris physics, final VFX, or sound system is added.

## Movement, contact, and state integration

`DrillMotor` remains the only movement owner. No Rigidbody, CharacterController, collision-callback dependency, competing controller, or generic collision framework was added.

The encounter uses registered, static axis-aligned BoxColliders. The solver computes their world extents directly from collider data/transforms, sweeps the player's complete box laterally, then sweeps forward against intact targets. It checks the complete requested distance, so thin obstacles cannot be skipped. The 0.005-unit skin creates separation; even a small positive footprint overlap blocks forward travel. Lateral sweeps prevent entering an obstacle's side while passing it.

Simulation intervals are bounded to **1/120 s**. The final fractional interval is always processed, preserving M1 steering responsiveness at high rendering frame rates. Forward arrival is resolved at an interval boundary (at most 8.33 ms); damage/energy are calculated from elapsed time, not render frame count. The public simulation method accepts finite intervals from **0 to 10 seconds**, rejecting larger values to avoid unbounded work from invalid inputs.

The original Ready/Charging/Moving states remain. Launching uses the accepted M1 Moving state with boost. Appended states are Drilling, Blocked, and RunEnded:

- Central contact requires the forward-facing tip to align horizontally and vertically with the material's front face. Track borders never become drilling targets.
- A valid drillable contact enters Drilling, holds a stable position, ignores steering, and keeps one target until completion/contact loss. Queued lateral motion is discarded.
- Side grazing cannot initiate drilling. Physical obstruction uses Blocked while permitting steering to align the tip or escape.
- Too-hard contact enters Blocked, applies zero damage, and permits lateral escape around the full footprint. It never ends a run by itself.
- Clearing an obstruction resumes Moving with **12 units/s²** acceleration toward the unchanged **10 units/s** cruise speed. The player is not teleported past the obstacle.
- A broken, disabled, removed, or moved-away target releases its drilling lock. Multiple nearby targets are processed in stable registered order without competing drilling states.
- RunEnded stops movement, steering, damage, and energy consumption. Existing launch input cannot start another run without Restart.

The M1 camera behavior/settings and full-footprint track bounds remain intact.

## Energy integration

Capacity is **100**. Ready/Charging consume **0**; Moving/boosting consumes **0.7/s**; Drilling consumes **only the material rate**; Blocked consumes **0.5/s**; RunEnded consumes **0**.

One state rate is applied per simulation interval. A drilling interval is truncated at target completion or energy depletion. After a break, any remaining time is simulated as movement with its own rate; it is not charged twice. If destruction and energy depletion occur at the same instant, destruction completes and the run also ends. If energy ends first, the intact obstacle retains partial durability and emits no break event. Depletion/run completion occurs once until reset.

## Scene setup and QA controls

Executed **Tools > DrillRunner > Setup M2 Drilling Prototype** in Unity 6000.3.8f1 batch mode. No routine manual wiring remains.

The existing 20 km reference track is retained. Four centrally placed obstacles form a compact encounter: Wood at **Z=35**, Brick at **Z=65**, Stone at **Z=95**, and Wood at **Z=130**. Their widths leave enough clearance on both sides for the 1.7-unit player footprint within the 6-unit track. There is no procedural layout or scattering across the long track.

The existing Canvas and Restart button are reused. New bottom energy/progress bars and material/power text accompany Drilling, Blocked, and Energy Empty feedback. Intact objects disappear when broken. Portrait 540 x 960 Drilling/Blocked/RunEnded captures were generated with Direct3D 11 and visually inspected.

Select the drill root's **DrillMotor** Inspector in Play Mode:

1. **QA: Power 5 / Energy 100 - Restart**: default Wood/Brick drilling and Stone blocking.
2. **QA: Power 10 / Energy 100 - Restart**: Stone becomes drillable.
3. **QA: Power 5 / Energy 8 - Restart**: early depletion while drilling Wood.

These controls are Editor-only, use the existing restart path, do not persist progression, and revert on leaving Play Mode. Movement settings are captured at Play startup; drilling/energy settings are validated and captured at Restart. Editing shared material assets in Play Mode can persist asset edits, so change those deliberately outside Play Mode.

Restart restores position, charge, speed, full energy, input neutrality, contact state, all obstacle durability/renderers/colliders, steering availability after launch, and camera composition. It does not recreate objects or subscriptions. Object placement and intentionally disabled unrelated objects are not rewritten by a gameplay restart.

Setup reruns preserve existing asset contents, GUIDs, component tuning, serialized references, and unrelated scene objects. Loaded dirty scenes are refused before asset changes. Missing asset files with existing `.meta` files are reported for restoration rather than regenerated with new GUIDs. Tests use a scene copy with custom work, repeat setup, compare scene/asset bytes, and verify dirty-scene refusal. The real scene/prefab/material assets are also hash-checked around a setup rerun.

## Executed validation

Final suite results:

| Check | Result |
| --- | --- |
| Unity compilation and M2 setup | **Passed**, exit 0 |
| Unity EditMode | **56 passed, 0 failed, 0 skipped**; includes all **33 unchanged M0/M1 tests** |
| Unity PlayMode | **20 passed, 0 failed, 0 skipped**; includes all **7 unchanged M1 tests** |
| Hardness below/equal/above threshold; faster drilling at higher power | **Passed** |
| Frame-independent damage/energy, clamping, once-only break/depletion, restoration | **Passed** |
| Central Wood contact, progressive damage, exact material energy rate without stacking | **Passed** |
| Maximum-launch thin Stone contact; blocked escape; side penetration prevention | **Passed** |
| Side grazing, stable target selection, target movement/disable cleanup | **Passed** |
| Higher-power Stone destruction | **Passed** |
| Ready/Charging/Moving/Blocked energy; depletion during Moving/Blocked/Drilling | **Passed** |
| Restart during Moving/Drilling/Blocked/RunEnded, including original pointer/input/UI tests | **Passed** |
| Missing material and invalid run configuration fail safely | **Passed** |
| Scene wiring, three materials, four obstacles, original GUIDs and repeatable setup | **Passed** |
| GPU portrait renders | **Generated and inspected** |
| Static JSON/source checks, `.meta` coverage, GUID uniqueness, preservation audit | **Passed** |

Local ignored evidence: `Logs/M2-Setup.log`, `M2-Setup-Repeat.log`, `M2-EditMode.log`, `M2-EditMode.xml`, `M2-PlayMode.log`, `M2-PlayMode.xml`, `M2-Drilling.png`, `M2-Blocked.png`, `M2-RunEnded.png`, and `M2-Baseline.json`. Initial failures were fixed without removing or weakening any prior test.

## Limitations and checks not executed

- Collision scope is the explicitly registered M2 static axis-aligned boxes. Arbitrary rotated/non-box colliders, dynamic rigidbody obstacles, and large procedural obstacle sets are outside M2.
- Contact timing is bounded by the simulation interval; damage/energy math is elapsed-time-based. No claim of arbitrary-scene continuous physics is made.
- No Android/player build, physical mobile-touch QA, hardware focus/background QA, performance profiling, or long endurance test was performed. Existing synthetic mouse/touch/lifecycle tests did run.
- Editor QA parameter changes are validated through runtime tests; the custom Inspector buttons still need the user's brief manual click check.
- Prototype materials, values, and visuals are not final balance/art. No M3 rewards/tracks, upgrades, economy, saving, or evolution were added.
- No packages, assembly definitions, `ProjectSettings`, build target, graphics quality, input bindings, Git settings, or remote configuration changed. Unity-generated empty YAML values may retain standard trailing whitespace; serialized YAML was left as Unity emitted it.

## Minimum manual QA

1. Open `Assets/_DrillRunner/Scenes/Gameplay.unity`, select **Portrait 9:16**, and press **Play**. No setup or GameObject wiring is required.
2. Launch centrally. Watch Wood and Brick lose durability while the drill stops and steering locks, then movement resumes. Confirm energy falls faster while drilling.
3. At Stone, confirm Blocked/Requires Power, zero damage, stopped forward movement, and safe escape by dragging around either side. Return toward the center for the final Wood.
4. Select the drill root, click **QA: Power 10 / Energy 100 - Restart**, launch again, and verify Stone can be drilled.
5. Click **QA: Power 5 / Energy 8 - Restart**, launch, and verify Energy Empty with an unfinished Wood obstacle and no further movement/damage.
6. Use **RESTART** in Moving, Drilling, Blocked, and RunEnded; confirm full energy/durability, visible obstacles, launch readiness, and camera reset. Return to the default QA preset when finished.

M2 stops here for user review. No commit or push was performed.
