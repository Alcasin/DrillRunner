# M6 — Premium Toy Industrial presentation and game feel

Implemented against approved M5 commit **`80d19dd`** on `main`. Before edits, the working tree was clean and a read-only remote check confirmed `origin/main` and local HEAD both at `80d19ddaf04a6b96aa54aec1d05ee72d370d3f97`. No commit, push, or M7 work is included.

## Primary changes and files

Paths below are relative to `Assets/_DrillRunner/`.

| New scripts | Responsibility |
| --- | --- |
| `Runtime/PresentationTheme.cs` | Shared sound/material/color references and feedback cue definitions |
| `Runtime/DrillPresentation.cs` | One authoritative-state observer for rotor/housing animation and feedback transitions |
| `Runtime/PresentationAudio.cs` | One mechanical loop plus four reusable one-shot voices |
| `Runtime/PresentationEffects.cs` | One contact emitter plus four reusable destruction emitters |
| `Runtime/ObstacleAppearance.cs` | Resettable visual damage stages and decoration visibility |
| `Runtime/PresentationHud.cs` | Charge/readability styling, reward toast, results visibility, and existing Retry placement |
| `Editor/PresentationAssets.cs` | Original beveled/fluted meshes, shared materials, sprites, and procedural PCM synthesis |
| `Editor/PresentationWorld.cs` | Material-specific obstacle details, safe yard decoration, material-group mesh combination |
| `Editor/PresentationUi.cs` | Existing Workshop/HUD/Results styling through Editor APIs |
| `Editor/VisualPolishSetup.cs` | Single idempotent M6 setup action and scene/reference/lighting integration |
| `Tests/EditMode/PresentationTests.cs`, `Tests/PlayMode/PresentationPlayModeTests.cs` | Presentation, original-audio, geometry, lifecycle, render, and desktop-counter checks |

Generated assets: **30 shared mesh assets**, **18 shared materials**, **10 original mono WAV clips**, **2 small UI sprites**, and `ScriptableObjects/M6Presentation.asset`, with Unity-generated metadata. The two existing form-preview PNGs are rerendered in place.

Updated existing assets: the original hero prefab's `Visuals` subtree, both visual-only form prefabs, four obstacle prefabs, all eleven authored section prefabs, and Gameplay scene. Prefab/texture GUIDs and existing component references are retained. Existing material assets are preserved; M6 uses new shared materials.

Targeted existing code changes: `DrillObstacle` publishes a restoration notification; `WorkshopController` publishes local presentation notifications after successful actions; `PrototypeHud` gains an optional rich-text summary format; `WorkshopView` displays styled resource numbers/glow and preserves Retry Save placement. README is updated. Gameplay math, damage, energy, input, camera follow, track selection/safety, reward ownership, purchases, evolution transactions, persistence, and save format are unchanged. No balance adjustments were needed.

## Drill art and mechanical animation

Pocket Drill has a rounded orange painted chassis, rubber tracks/cleats, ivory motor cap, metal vents, window, and a silver fluted drill. Workshop Drill has a reinforced teal hull, dark larger housing, metal side armor, copper top cap/rings, and a heavier drill assembly. The models use custom shared beveled-box and helical-bit meshes, with roughness/metallic separation for paint, rubber, steel, and copper. Static geometry is combined by material; each form stays within twelve renderers and twenty thousand triangles.

Animation is on visual children only. The existing motor/footprint/tip/root transform never moves because of feedback. The coordinator observes `DrillMotor.SimulationAdvanced` and existing break/reset/unregister events; late-frame sampling also handles charge transitions and animates the cached rotor/housing transforms. It binds parts only when the active visual changes, restores base poses on switch/reset/disable, and does not rebuild meshes or instantiate materials per frame.

| State | Presentation |
| --- | --- |
| Ready / idle | Slow rotor and restrained housing life |
| Charging | Rotor increases with actual normalized charge; real charge meter, percentage, and MAX label |
| Launch | One release sound, including a valid quick tap, and a brief housing response |
| Moving | Stable rotor spin; approved motion and camera behavior retained |
| Drilling | Faster rotor, tiny resistance motion, material-pitched loop, local chips, cracks/tint stages |
| Blocked | Successful drilling effects stop; distinct orange/text rejection and dull sound; no damage-stage advancement |
| RunEnded / Retry / Workshop | Loops and contact effects stop; reset clears particles/toast/poses |
| Evolution | Existing 0.9-second saved transition gains warm preview glow/pulse, buildup loop, and completion cue |

Both forms preserve the original **1.7 × 1.3 × 3** logical collider and forward tip. Decorative models contain no colliders, rigidbodies, scripts, or cameras. The existing one motor, input adapter, camera, and progression controller remain. Workshop uses static model renders rather than a second runtime camera; the visible live game model supplies mechanical motion.

## Workshop Yard, obstacles, and lighting

The deck/yard use a controlled teal/ivory/amber palette, cross-deck panel joints, narrow free-steering boundaries, shoulders, and small crates outside the playable path. The outside ground is continuous across section ends; decorative rounded-ground joints found during visual review were corrected. Marks do not divide the course into lanes. All decoration is collider-free; source surface dimensions, obstacle placement/registration, entrance/exit, seeded section sequence, fallback safety, and cleanup remain intact.

| Material | Identity and progressive/final feedback |
| --- | --- |
| Wood | Warm plank segments, grain hints and rivets; light elongated splinters |
| Brick | Red-brown segmented masonry and contrasting mortar; chunkier chips |
| Stone | Cool slate and mineral veins; dense shape and heavier loop pitch when legitimately drillable |
| Copper Ore | Mineral teal with raised copper-colored crystals; taller bright fragments, separate break/reward tones, and ledger-backed Copper toast |

`ObstacleAppearance` uses cached renderers and one reusable material property block for damage stages. It shows up to three local crack marks and tint stages according to actual remaining HP. Restoration resets the stages; final break immediately hides decorations while the original component disables its collider/intact renderer. The presentation never applies damage or grants rewards. Blocked state produces no drilling emission or HP animation.

One existing directional light now provides warm soft shadows, balanced by scene ambient fill. Distance fog blends the far streamed horizon without obscuring nearby routes. No extra real-time lights or full-screen post-processing are added. **Camera offset, FOV, rotation, interpolation, and reset composition are preserved**; there is no camera shake. Actual 9:16 renders verify the resulting composition.

## Bounded VFX and original audio

VFX consists of **five authored systems**: contact capacity **24**, plus **four destruction systems of 22 each**, for **112 particles maximum**. Emitters and material references are reused. Contact particles are light local chips; break systems use small mesh fragments with material-dependent proportions. There are no physical debris GameObjects, collision particles, new lights, or unbounded emitters. Focus loss, pause, disable, retry, and Workshop entry clear/stop effects appropriately.

All ten sounds are synthesized locally by project Editor code as **mono 22,050-Hz PCM WAV**. There are no downloaded sound libraries or external asset packs. Loops have soft endpoints; note envelopes avoid abrupt internal pitch-step discontinuities. Tests verify non-silent bounded peaks, near-zero DC, and quiet endpoints. Music is optional in the brief and is not added.

| Cue | Trigger |
| --- | --- |
| Charge | One looping motor buildup, pitch following real charge |
| Launch | Valid release / quick tap, once per launch |
| Drill | One continuing loop, with material pitch variation; no restart per frame |
| Blocked | One rejection on entering Blocked |
| Break | Normal fully destroyed obstacle, once per encounter/attempt |
| CopperBreak | Copper Ore fully destroyed |
| CopperReward | Only after the authoritative ledger increases Copper |
| Upgrade | Successful saved purchase only |
| Evolution | Completion of the existing saved presentation; no repeat purchase or load-time grant |
| Finish | Entry into RunEnded |

There are **five audio sources total**: one loop and four ring-buffer one-shot voices. New one-shots replace the oldest voice rather than adding more sources or unlimited `PlayOneShot` overlap. Pitch variation does not use Unity's global random state. Relative volumes are deliberately conservative. Loops stop/change on contact exit, break, Blocked, run end, Retry, Workshop, focus/pause, and disable. Numerical waveform and event-lifecycle validation was performed; **physical headphone/speaker mix listening remains manual QA**.

## Workshop, HUD, Results, and evolution UI

The existing screen structure remains: ivory rounded cards, dark ink text, clear Coin/Copper pills, larger current-form preview, teal primary actions, copper/gold evolution emphasis, consistent spacing, MAX states, and readable next stats/prices. Disabled actions retain native validation. No new tabs or navigation layers are added.

Gameplay separates the state/charge header, temporary earnings, and lower power/energy/material instruments, leaving the drill/routes visible. Blocked uses text and color together. Charge fill remains the real normalized value; the charge-only visuals disappear after release. The results card formats the existing frozen ledger into distance and currency, shows banking status, and reuses Workshop/Retry buttons. Toasts, sounds, rich text, and results animations do not call reward or banking operations.

Evolution still commits before presentation. A warm glow, enlarged model preview, mechanical buildup, and completion tone use the existing transition. Interrupted presentation still restores/skips safely; no new saved animation state or transaction dependency exists. Temporary QA profile funding/evolution isolation is unchanged.

## Editor automation

Executed **Tools → DrillRunner → Setup M6 Visual Polish** in Unity **6000.3.8f1**. All mesh/prefab/material/audio/UI/scene work used Unity Editor APIs; no scene or prefab YAML was edited by hand. No manual component assignments remain. The setup validates existing wiring and returns without rebuilding configured assets. Dirty loaded scenes are refused, custom scene work and GUIDs are preserved, and earlier milestone setup actions remain functional.

Initial setup needs graphical Editor execution for model-preview renders. Rerunning an already configured scene works with `-nographics`. Batch entry: `-batchmode -quit -projectPath <project> -executeMethod DrillRunner.Editor.VisualPolishSetup.Setup -logFile Logs/M6-Setup.log`. The setup never accesses player save files or changes progression values.

## Representative visual review

All captures are **540 × 960**, rendered with the actual gameplay camera/HUD through Direct3D11. Some obstacle poses and early-depletion states are staged in tests for clear review; production placements and balance are unchanged. Captures are ignored local evidence under `Logs/`.

| Required state | Evidence |
| --- | --- |
| Pocket Workshop | `M6-WorkshopPocket.png` |
| Pocket charging | `M6-PocketCharging.png` |
| Wood / Brick / Stone / Copper | `M6-Material-wood.png`, `M6-Material-brick.png`, `M6-Material-stone.png`, `M6-Material-copper_ore.png` |
| Drilling / break / rejection | `M6-WoodDrilling.png`, `M6-CopperDrilling.png`, `M6-CopperBreak.png`, `M6-StoneBlocked.png` |
| Workshop Drill Workshop / gameplay | `M6-WorkshopEvo2.png`, `M6-WorkshopDrillMoving.png` |
| Evolution readiness / transition | `M6-EvolutionReady.png`, `M6-Evolution.png` |
| Results | `M6-Results.png` |

Accepted M5 screenshots were copied **before regression tests could overwrite them** to `M6-BeforeWorkshopPocket.png`, `M6-BeforeWorkshopEvo2.png`, and `M6-BeforeResults.png`. The Pocket before image and `M6-EvolutionReady.png` show matching MAX levels / 7,520 Coins / 24 Copper for direct visual comparison. Review covered text/controls, distinct silhouettes/surfaces, brightness, restrained particles, model selection, route visibility, and continuous section connections.

## Executed validation and performance

Actual Unity Test Framework execution, Unity **6000.3.8f1**, Windows Editor batch mode (Direct3D11 for graphical PlayMode):

| Validation | Result |
| --- | --- |
| Compilation and automated M6 scene/asset setup | Passed, exit 0 |
| Actual setup rerun / asset SHA-256 comparison | **All 395 files unchanged**, exit 0 |
| Complete EditMode assembly | **122 passed, 0 failed, 0 skipped** |
| Complete PlayMode assembly | **54 passed, 0 failed, 0 skipped** |
| Original M0–M5 tests | **All 117 EditMode + 46 PlayMode unchanged and passing** |
| New M6 coverage | **5 EditMode + 8 PlayMode passing** |
| Original sound waveforms, cue coverage, bounded voices/particles, cleanup/focus/retry | Passed |
| Real charge / quick tap, progressive effects, blocked no-damage feedback, once-only breaks | Passed |
| Copper feedback separate from currency logic, results banking, purchases/evolution | Passed |
| 9:16 captures and visual inspection | All required states obtained and reviewed |
| Static source/JSON, metadata, GUIDs, original-file preservation | Passed |

The original-file SHA-256 audit covers **286 tracked baseline files**, with exactly **26 intended presentation/source/documentation changes** and no deletions. All original tests, gameplay/progression/save rules and data, input, camera, original materials, package/project settings, and original metadata are unchanged. All **189 owned asset/folder entries** have companion metadata and all **209 asset GUIDs** are unique. Static checks are separate from the actual Unity tests above.

Evidence is local/ignored: `Logs/M6-Setup.log`, `M6-Setup-Repeat.log`, `M6-EditMode.log/.xml`, `M6-PlayMode.log/.xml`, `M6-Profiler.txt`, `M6-Baseline.json`, all M6 captures listed above, and the unchanged M5 economy log. New files include 73 presentation assets/scripts with metadata plus this report.

Final Git state: `main` at **`80d19dd`**, **26 modified, 147 untracked, 0 staged**. No commit/push or remote configuration changes were made.

The explicit-render-target desktop smoke test traverses **over 15 km** with the open-section catalog, retaining **eight sections** at the final sample and never exceeding the existing ten-section bound. The unchanged M3 >30-km test and M5 unfunded evolution journey also run. The latter still reaches evolution in **six runs**, earning **2,704 Coins and 12 Copper**: polish does not change the economy.

The final open-track **540×960 desktop Editor batch snapshot** recorded **16 draw calls**, **1.78 ms main-thread counter**, and **8,620 bytes GC allocated in that frame**, with five audio sources and 112-particle capacity. Earlier smoke passes varied to 14 draw calls / 2.49 ms. These are single Editor/engine/UI observations in a low-density course, **not** an average, worst-case budget, isolated presentation allocation measurement, GPU timing, or verified mobile FPS. An initial zero-draw-call batch sample was rejected as unrepresentative; the final probe used an explicit camera render target.

Shared geometry is generated in Editor and combined by material. Runtime animation caches its transforms, damage uses a reusable property block, and UI charge text updates only when state/integer percentage changes. Existing section instantiate/destroy cadence remains; no generic pool or pipeline rewrite is introduced. Target-device profiling is still needed.

## Known remaining limits / checks not performed

- No Android/player build, physical touchscreen/device test, hardware audio mix audition, standalone close/reopen, real-time endurance run, full Unity Profiler timeline session, GPU capture, or representative mobile performance validation.
- The art remains a cohesive procedural prototype: no bespoke texture art, physically cut meshes, final cinematic, music, or production sound recording. Workshop previews are static renders with a short evolution pulse; live game models animate mechanically.
- Contact feedback is intentionally local and small. Camera shake is absent. Human assessment of sound mix, perceived impact, and sustained play comfort remains one of the four manual checks below.
- The profiler smoke scene uses open sections and does not represent peak obstacle/VFX density. Editor GC includes engine/UI/test activity. Existing URP quality assets, project settings, and camera smoothing are retained.
- Existing world-float precision, static axis-aligned collision scope, conservative route certification, synchronous persistence, and in-memory pending-bank limits remain from accepted milestones. No changes to save schema or recovery are introduced.

## Four focused manual QA checks

1. **Workshop / evolution:** Open Gameplay, select Portrait 9:16, Play, and WORKSHOP. Check resource pills, cards, prices/MAX, form preview, and evolution requirements. Use the existing confirmed temporary funded profile (24 Copper), max Pocket upgrades, and evolve once. Check the concise presentation, one Copper deduction, new model/tier, and usable controls; leave Play to verify real progress is unaffected.
2. **Launch / steering / camera:** Charge to partial and MAX, release, and steer to both bounds. Check the real meter, increasing spin/sound, launch response, readable routes, continuous yard joins, and stable camera.
3. **Drilling / audio / resources:** Drill Wood/Brick/Copper, attempt Stone at power 5, then try upgraded/evolved power. Listen for distinct buildup, release, working/rejection/break/reward sounds. Check chips/cracks/progress, no successful-damage feedback while Blocked, one Copper reward, and loops stopping on escape/focus loss/Retry.
4. **Results / repeated flow:** Deplete Energy, check frozen distance/Coins/Copper and banking status, use Retry/Workshop repeatedly, and confirm no duplicate rewards, leftover particles/loops, clipped controls, or changed permanent upgrade/evolution behavior.

M6 stops here for user review. No commit or push was performed; M7 is not started.

## M6-B — targeted visual and game feel revision

Implemented on top of the complete, uncommitted M6 work at **`80d19dd`**. The original M6 sections above record that milestone's validation. This section records the revised assets and current results. No gameplay, economy, progression, save, migration, or input rules were changed. README remains unchanged relative to M6.

### Findings and drill visibility

The reviewed M6 portraits confirmed the owner's observation: the rear housing hid the low, short bit, and solid cylinder caps on the decorative flutes looked like a disk from behind. The highly metallic steel also read dark in the gameplay lighting. Inspection during revision exposed an uncapped rear face on the cone; simply removing the disks revealed a hole.

Both forms now use a lower, shorter rear housing and a longer, slimmer exposed cutting cone. The rotor center is **Y 0.94 / Z 0.02**, the cone is **1.81 units long**, and its forward end remains **Z 1.83**, inside the unchanged logical tip at Z 1.85. Pocket's bit is narrower than Workshop's reinforced bit. Open drive rings replace the occluding solid collars; the cone has a closed rear face, bright steel with restrained metallic response, dark helical edges, and an offset rotating tooth. Workshop keeps its teal body, copper ring/cap, and stronger assembly. Original prefab/mesh GUIDs and the existing visual root references are retained.

The whole model was not enlarged. The **1.7 × 1.3 × 3** collider, center, motor root, contact calculations, movement axes, power, damage, speed, and energy remain unchanged. Decorations contain no colliders. Rotation remains on the visual rotor about +Z; the existing housing response stays local.

Original and two elevated camera alternatives were rendered for comparison. Elevated framings put more distant targets behind the existing top HUD, so they were rejected. **Production camera offset (0, 6, -9), FOV 60, rotation, look-ahead, smoothing, and horizontal follow remain unchanged.** The final portrait projection test verifies that the tip projects beyond the upper rear collar by more than 18 pixels at 540 × 960 for both forms and that both escape edges remain in frame. Actual rendered frames, rather than that geometric check alone, were inspected.

### Drilling, destruction, and Copper

Contact chips are larger, material shaped, and directed upward/backward from just outside the actual tip contact, making them less likely to disappear inside the obstacle. Existing HP-driven cracks and the authoritative durability bar remain. Blocked never activates successful drilling chips or damage stages.

The existing four reusable break emitters now distribute fragments across the destroyed obstacle's bounds. Opaque lit mesh particles replace the soft radial texture on debris, so chunks read as solid pieces rather than round particles. Fragments retain most of their size briefly before shrinking, rotate, and fall without physics objects. The authoritative obstacle collider still disables immediately on destruction.

| Material | Revised final response |
| --- | --- |
| Wood | 18 warm, long thin splinters; 0.75-second life |
| Brick | 22 chunky reddish pieces; 0.75-second life |
| Stone | 22 gray rock chips; longer 0.9-second response |
| Copper Ore | Copper chunks with six brighter upward reward glints inside the same 22-particle burst when Copper is confirmed |

Copper feedback still reads the existing ledger after the authoritative break/reward handlers. Only an actual Copper increase enables the reward glints, the existing reward sound, and **+3 COPPER** indicator. The indicator is now 28-point bold, lasts 1.6 seconds, rises slightly, and has a short scale response. It uses the ledger delta; presentation never calculates or grants resources. Existing per-attempt destruction deduplication also guards the combined response. A restored/destroyed duplicate produces neither another reward nor another popup/sound/glint response.

Run End now clears outstanding fragments and the toast immediately. Retry, Workshop, focus/pause, and disable cleanup remain. Capacity stays **five particle systems / 112 particles**: one contact system of 24 and four break systems of 22. No extra emitters, rigidbodies, decorative colliders, runtime meshes, or impact-specific material instances were added.

### Yard and HUD

Eleven existing section prefabs gain one of **three shared decoration variants**: cross-deck panels/fasteners, side compressors, and pipe arrangements outside the playable corridor. A stable section-name hash chooses the authored variant in Editor. It consumes no gameplay or Unity RNG, adds no obstacle/resource definitions, and changes no catalog, section metadata, obstacle positions, surfaces, route guarantees, streaming, or section joins. The common continuous yard remains. Each variant is combined into four shared material groups; meshes are reused across sections.

Gameplay HUD changes are limited to the undersized elements: energy caption 12→16 points, effective power/energy 18→22, charge caption 13→18, distance/Coins/Copper 20→22, hint 17→19, and material text 17→18. Charge, energy, and drilling bars are thicker, with adjusted spacing. State/BLOCKED already had strong text and contrasting plates and retain that treatment. The Workshop and Results layouts retain M6 styling and behavior; only model preview images are refreshed to match the new geometry.

### Audio and EVO 2 verification

No audio playback/routing defect was demonstrated, so **all ten original WAV files and the audio controller remain byte-for-byte unchanged**. Checks found one enabled listener, five enabled/unmuted 2D sources, direct output without a mixer attenuation route, valid clip references, master volume above zero, and no listener pause.

Actual graphical Unity PlayMode measured all ten cues as playing with advancing sample positions. The DSP clock advanced, and the mixed listener buffer contained nonzero output. The actual gameplay drilling loop measured approximately **0.0303 source peak / 0.0662 listener peak** at a 48-kHz output rate. The charge loop played with advancing samples and nonzero mixed output; its first source-buffer read was zero, so isolated source-output confirmation for charge is limited. Early cold per-source buffer reads also returned zero before later readings became populated. These measurements establish runtime audio output; they do not identify why the owner's recording was silent or verify a physical speaker/headphone mix.

Existing and new tests exercise charge/launch, drilling loop transitions without per-frame restarts, BLOCKED, break/Copper reward, Finish, upgrades, evolution, and Retry cleanup. Physical listening and recording software capture remain manual checks; no physical audition is claimed.

EVO 2 was exercised through the existing saved-before-animation evolution path with an isolated memory profile. Its updated preview, transition, live model, charge/movement/drilling animation, and stronger copper/teal silhouette were rendered and inspected. Tests verify a single active form, unchanged footprint, correct model selection, one evolution transaction/cue, and unchanged effective stats. No evolution rules, prices, Copper requirements, persistence fields, or further forms were added.

### Executed validation and evidence

Actual Unity **6000.3.8f1** execution against the final revision:

| Check | Result |
| --- | --- |
| Complete EditMode suite | **125 passed, 0 failed, 0 skipped** |
| Complete graphical PlayMode suite | **60 passed, 0 failed, 0 skipped** |
| Earlier M0–M6 tests | **All 176 unchanged and passing** |
| Focused M6-B coverage | **3 EditMode + 6 PlayMode passing** |
| Existing M6 setup action, applying versioned revision | Passed, exit 0; no manual wiring |
| Actual setup rerun | Exit 0; **all 435 Assets files unchanged** |
| Baseline preservation | All 367 pre-revision scoped files retained; no earlier tests/audio changed |

New regressions cover real form selection/spin/footprint, exposed bit projection and escape edges, all four real material breaks, immediate collision release, bounded early/late fragment animation and expiry, confirmed once-only Copper, Run End/Retry cleanup, BLOCKED without damage particles, authoritative HUD values, three collider-free yard variants, listener/source wiring, runtime output measurements, and the existing setup idempotence checks. All 163 M0–M5 tests and 13 original M6 tests remain unedited. Memory profiles avoid real player saves.

New assets comprise **16 shared mesh assets, two shared materials, and two test scripts**, with Unity-generated metadata. Existing M6 meshes/prefabs/previews are updated in place through Editor APIs. No scene/prefab YAML was manually rewritten. No previous M6 asset was deleted. Source changes are confined to the four existing M6 Editor helpers and three presentation runtime classes, plus the new test classes and this report.

Evidence is ignored/local under `Logs/`: `M6B-Setup.log`, `M6B-Setup-Repeat.log`, `M6B-EditMode.log/.xml`, `M6B-PlayMode.log/.xml`, `M6B-Audio.txt`, `M6B-Profiler.txt`, `M6B-Baseline.json`, and `M6B-SetupHashes.json`.

| Visually inspected final portrait | Capture |
| --- | --- |
| Pocket / Workshop live silhouette | `M6B-PocketSilhouette.png`, `M6B-WorkshopSilhouette.png` |
| Matched M6 charging before / revised charging | `M6B-BeforePocket.png`, `M6B-Final-PocketCharging.png` |
| EVO 2 moving / preview / evolution | `M6B-Final-WorkshopMoving.png`, `M6B-Final-WorkshopPreview.png`, `M6B-Final-Evolution.png` |
| Wood / Brick contact | `M6B-Final-woodContact.png`, `M6B-Final-brickContact.png` |
| Four material breaks at early and later times | `M6B-Final-{wood,brick,stone,copper_ore}Break{Early,Late}.png` |
| Copper reward / BLOCKED / yard / HUD | `M6B-Final-CopperReward.png`, `M6B-Final-Blocked.png`, `M6B-Final-Yard.png` |

All frames are actual **540 × 960** gameplay-camera/HUD renders. Some obstacle positions are staged for inspection without changing production placement. Early/late captures and runtime particle movement/expiry checks validate animated destruction; they are not a physical-device play session. Final copies preserve the reviewed images separately from original M6 test output paths, which the unchanged tests overwrite.

The unchanged >15-km open-track smoke test again retained eight streamed sections, five sources, and 112-particle capacity. Its final desktop Editor snapshot was **16 draw calls, approximately 1.71 ms main-thread counter, and 8,620 bytes GC in that frame**. Earlier passes varied, including 13 draw calls and 4.22 ms. These are single low-density Editor snapshots, not averages, peak-effect budgets, GPU measurements, isolated presentation allocation figures, or mobile FPS. Larger fragments increase visible screen coverage; they use opaque shading and the same bounded particle count. Target-device profiling remains necessary.

Final Git state: **`main` at `80d19dd`**, **26 modified, 187 untracked, 0 staged** including preserved M6 work. No commit, push, remote changes, or M7 work.

### Three focused manual review checks

1. **Silhouette and steering:** In Portrait 9:16, charge, launch, and drill with Pocket and temporarily funded EVO 2. Judge the visible silver cutting cone/spin at normal viewing size, including both steering edges and incoming targets.
2. **Impact and reward:** Break Wood, Brick, Copper, and legitimately drillable Stone. Judge fragment weight/readability, one Copper indicator/counter increase, and immediate cleanup on Run End/Retry. Check the modest side-equipment variation during an ordinary run.
3. **Physical audio:** Listen through speakers/headphones to charge, launch, working/rejection, break/reward, Finish, and evolution. Check mix comfort and loop cleanup; verify desktop-audio capture settings separately if recording another review.

No player/device build, physical audio audition, mobile performance test, or full GPU/Profiler timeline capture was performed. M6-B stops here for Project Owner review.
