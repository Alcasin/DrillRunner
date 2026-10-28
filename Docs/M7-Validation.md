# M7 — gameplay variety and risk/reward

The original M7 report below is retained as historical evidence. **Current behavior and results are recorded in the M7-B revision section at the end.**

Implemented against approved M6/M6-B commit **`cab310f7526285eb3fed4ec349aea72a779ae295`** on `main`. Before editing, the working tree was clean and a read-only `git ls-remote` check confirmed the same commit on remote `main`. Validation used Unity **6000.3.8f1**, Windows PC Editor, on **2026-10-28**. No commit, push, M8, Android, mobile release, or store work is included.

## Implementation and authored content

M7 extends the existing `TrackStreamer`. There is one spawning/registration/retirement system and the same `RunProgress`, motor, input adapter, camera, reward ledger, Workshop, and persistent progression. The accepted four-obstacle opening and Z=160–200 transition buffer remain. Encounter selection starts at Z=200.

| New file, relative to `Assets/_DrillRunner/` | Responsibility |
| --- | --- |
| `Runtime/EncounterDefinition.cs` | Category, design intent, prefab, pressure, weights, minimum ordinal, and open-bypass witness |
| `Runtime/EncounterLibrary.cs` | Existing catalog reference and configurable pacing/reaction/mining-budget parameters |
| `Runtime/EncounterSafety.cs` | Full-footprint, steering-time, transition, starting-Power mining-line, and Energy-budget certificates |
| `Runtime/EncounterSequence.cs` | Deterministic selection and pacing; no object spawning or reward operations |
| `Editor/GameplayVarietySetup.cs` | One idempotent M7 setup action; authors and validates assets through Editor APIs |
| `Tests/EditMode/EncounterTests.cs` | Authoring, determinism, pacing, motion/sweep, mining, invalid-data, threshold, and setup checks |
| `Tests/PlayMode/EncounterPlayModeTests.cs` | Actual motor, resources, banking, progression, streaming, renders, real-time drilling, and profiling |

New data: **14 section prefabs** under `Prefabs/Encounters/`, **14 definition assets** and one library under `ScriptableObjects/Encounters/`, plus Unity-generated folder/script/asset metadata. These reference the existing M6-B obstacle prefabs, shared meshes, materials, and effects. There are no new obstacle materials, currencies, presentation managers, or per-encounter particle/audio systems.

Modified existing code: `TrackStreamer` gains the library reference, selection hook, and bounded active-instance diagnostics; `DrillMotion` exposes two read-only values from its existing movement snapshot; `RunProgressInspector` shows current encounter/seed/validation information beside the existing replay controls. Gameplay scene gains one reference. README is updated. No movement integration, contact, HP, Energy, hardness, reward, purchase, evolution, migration, or persistence behavior was changed.

The original catalog and eleven legacy section assets are unchanged, including their GUIDs. New encounters form a separate authoring collection alongside them. An explicit runtime assignment of a different legacy catalog retains that catalog's original selection unless a matching encounter library is also assigned. This preserves the existing controlled-content QA fixtures and authoring overrides. Normal Gameplay uses the M7 library; original tests using the normal catalog also exercise M7.

## Encounter vocabulary

All surfaces remain **6 × 40**. Decorative arrangements are shared from the accepted yard and have no blocking colliders. Obstacles use their actual original material definitions and authored dimensions, including a 0.8-unit depth. Left/right names describe mirrored arrangements; they introduce no lanes or input restrictions.

| Category | Patterns | Decision and purpose |
| --- | --- | --- |
| Easy Flow | `EasyLeft`, `EasyRight` | Two aligned Wood targets or the wide open side; momentum and low-pressure destruction |
| Dense Drilling | `DenseLeft`, `DenseRight` | Three Wood targets on one approach, a Brick alternative, and an open outside bypass; repeated destruction versus Energy preservation |
| Risk/Reward | `OreOpenLeft/Right`, `OreGuardLeft/Right` | Unguarded Ore or Brick followed by aligned Ore; optional Copper with different mining costs |
| Precision Escape | `EscapeLeft`, `EscapeRight` | Two staggered Stone targets with advance space and a wide clear side; early steering and recoverable BLOCKED |
| Power Test | `PowerLeft`, `PowerRight` | Parallel Stone/Wood choices plus an open edge; Stone becomes drillable at Power 9 and faster thereafter |
| Recovery | `RecoveryOpen`, `RecoveryOre` | Clear deck, sometimes with one optional Ore; pacing relief with normal Energy drain |

Dense Drilling at Power 5 costs **33.6 drilling Energy** for the three-Wood approach, awarding three normal obstacle Coins. Its single-Brick alternative also costs 33.6 drilling Energy and awards two Coins. Both retain a completely open bypass. These are optional destruction choices; traveled distance remains the dominant Coin source under the approved economy.

At starting Power 5, unguarded Copper costs **18 drilling Energy**. Brick plus Copper costs **51.6**, awards the existing two Brick Coins and three Copper, and has an open bypass. Stronger Power lowers these costs without changing the layout. Continuous steering also permits skilled approaches around a guard where sufficient space remains; M7 does not enforce a paid route with invisible restrictions.

Power Test's early open bypass remains free of all obstacles. A weak drill that deliberately enters Stone can escape laterally into the adjacent drillable Wood option; the runtime test confirms Stone takes no damage and Wood can be cleared. The dedicated Precision Escape arrangement separately verifies a direct return from BLOCKED to free movement without drilling another target.

## Determinism and pacing

Selection uses private xorshift state initialized from the existing run seed. It consumes no Unity/global decorative RNG. Validity, weights, categories, pressure, and transition compatibility are snapshotted at attempt start. Same seed, definitions, weights, and movement configuration reproduce the selected sections. Diagnostic skips advance the same sequence rather than reseeding or drawing decorative randomness.

Configurable defaults:

- First three generated selections use early weights; high-pressure patterns have minimum ordinal 3. The opening resource selection uses an accessible low-pressure opportunity.
- Regular weights apply thereafter. From ordinal 20, approximately Z=1000, later weights modestly favor Dense Drilling and Power Test while retaining relief.
- The same category cannot occur consecutively; mirrored copies do not evade this restriction.
- At most one pressure-2 encounter occurs consecutively.
- Copper appears within three selections and Recovery within six. `RecoveryOre` allows both constraints to be met together without forcing a costly guard.
- Empty Recovery cannot repeat consecutively. Pressure is not automatically increased because the player upgraded; accepted progression profiles retain the same valid pattern set.

An invalid pattern is rejected before selection. Missing eligible choices use the existing open fallback and record a reason without generating rewards. Fundamental invalid catalog/pacing configuration is rejected explicitly. The shipped defaults generated **128,000 selections across 128 seeds with zero fallbacks**, all fourteen patterns present, all six categories represented, and all pressure/resource/recovery bounds maintained.

Long-sample category counts, in enum order, were **15,074 Easy Flow; 17,599 Dense Drilling; 38,274 Risk/Reward; 8,791 Precision Escape; 21,064 Power Test; 27,198 Recovery**. This represents 5,120 km of selection, not 5,120 km of real-time play.

## Safety certificate and its limits

This is stronger than checking for a visible gap. Each accepted pattern must satisfy:

1. Existing section/floor/obstacle validation: connected dimensions, axis alignment, bounds, no overlapping obstacles, unchanged hardness, and a valid legacy route.
2. An authored **collision-free** constant-X bypass for the entire player width, regardless of whether materials could be drilled. The witness does not constrain player movement.
3. Enough empty approach distance to reach that bypass from any allowed entry X after a reaction allowance, using the actual exponential steering response.
4. Sufficient clearance between every accepted pair of sections, allowing for the front and rear footprint and release from drilling's lateral lock.
5. A constant-X, centrally aligned mining approach/exit for each Ore, including any guards, at starting Power 5. Every intercepted material must be drillable.
6. A full undamaged starting-Power Ore line costing at most the configurable **60 drilling Energy** budget. A geometrically valid but excessively expensive guard is rejected. This validation never alters HP, damage, or Energy consumption.

The accepted envelope uses full width **1.7**, front reach **1.85**, rear reach **1.15**, maximum forward speed **26**, and steering response **18**. Clearance is 0.08; steering tolerance is 0.04. With reaction time 0.35 seconds:

`maneuver time = reaction + ln((track width - player width) / tolerance) / response ≈ 0.61 seconds`.

The earliest target face is at local Z=19.6. Required entry room, including front reach and margin, is approximately 17.8 units at speed 26. The tightest authored pair leaves approximately **24.2 units** after full-footprint allowances. Bypasses require no mid-cluster lane change, mining, or lateral action while drilling. Mining witnesses also remain on a single aligned line through their targets, respecting lateral lock.

Automated swept tests run the **actual `DrillMotion` integration and `DrillContacts` sweep functions** through every ordered pair from both steering extremes at constant speeds 24 and 26, including reaction delays at entries. This is **784 pair/speed/start combinations**. Separate actual `DrillMotor` tests traverse every pattern from both extremes at Power **5, 7, 10, 12, and 18**: **140 cases**, with no drilling/BLOCKED or resource awards on the free bypasses. Actual mining/escape tests cover all five Ore routes, both sides of the Stone threshold, reward deduplication, and banking.

The certificate establishes available routes under these assumptions. It is not a guarantee for arbitrary or late input, human reaction times, partial pointer gestures, focus loss, or every conceivable custom configuration. The response model assumes the player can request the target after the reaction allowance; it does not model mouse gesture duration separately. More extreme custom speed/response configurations are revalidated and can reject patterns instead of reusing the accepted certificate. Existing large-world floating-point limitations remain. The 60-Energy authoring budget does not promise an exhausted player can afford every Ore they encounter; remaining Energy is deliberately a decision.

## Rewards, progression, and comparative journeys

Generation only arranges original obstacles. `RunRewards` still grants distance Coins, normal obstacle Coins on full destruction, and Copper only on complete Ore destruction. No payout is added for passing an Ore, choosing a route, or completing an encounter. Save version, durable receipts, upgrade prices/values, evolution cost, and existing temporary QA profile are unchanged.

The controlled **M6 comparison** uses the byte-unchanged legacy catalog and `SectionSequence` through an explicit catalog override; M7 uses the normal library. Both run the same motor, Energy rules, seeded inputs, and guided policy. Each cohort uses seeds **1337, 904, 2026** and the accepted attributes from the progression configuration.

The policy seeks at most one affordable Ore, then preserves Energy. It prefers an open line through the visible section when possible, otherwise handles successive legacy collision groups. Mining costs and available Energy affect its choice. It is an informed comparison driver, not a measured human play style.

| Profile | M6 mean distance | M7 mean distance | Coins/run, both | Copper/run, both | Drilling Energy/run, both |
| --- | ---: | ---: | ---: | ---: | ---: |
| Starting Pocket, Power 5 | 1,173.9 | 1,173.9 | 234 | 3 | 18.00 |
| Intermediate Pocket, Power 7 | 1,810.1 | 1,810.2 | 362 | 3 | 12.86 |
| MAX Pocket, Power 10 | 2,935.3 | 2,935.3 | 587 | 3 | 9.00 |
| Starting Workshop, Power 12 | 3,357.6 | 3,357.5 | 671 | 3 | 7.50 |
| MAX Workshop, Power 18 | 5,484.8 | 5,484.8 | 1,096 | 3 | 5.00 |

All thirty samples completed one Ore destruction and had zero BLOCKED episodes under that policy. Mean visible Ore opportunities were **14.3/22.7/36.3/42.3/70.7** in M6 versus **11.0/17.3/29.3/32.7/55.0** in M7 across those profiles. M7 offers fewer incidental opportunities but maintains the three-section maximum gap. Every authored Ore has a certified starting-Power line and was actually mined in runtime tests; that is geometric/initial-budget accessibility, not an assertion that every opportunity remains affordable late in a run.

Two independent unfunded evolution seed families, **900 and 1900**, were run on each generator using normal purchases and banking. **All four journeys reached evolution in six runs, earning 2,704 Coins and 12 Copper**, within the accepted 5–8-run target. The original unchanged M5 guided evolution regression also passes on ordinary M7 content. No weights, prices, or rewards were inflated to achieve these results.

Density creates more optional destruction, so mining every target can spend Energy faster and reduce distance income. The comparative table is limited to its stated preservation-oriented policy; it does not establish every player's earnings or all possible play styles. The live Dense Drilling test and guarded-Ore test independently verify their higher actual destruction cost/rewards. Human balance/play-style assessment remains part of review.

## Editor integration and exact automated results

Executed **Tools > DrillRunner > Setup M7 Gameplay Variety** through Unity Editor APIs. The scene is ready without manual wiring. Setup validates all definitions and all transitions, preserves custom work and existing GUIDs, refuses dirty loaded scenes, and returns without writes on a configured scene. No scene or prefab YAML was manually rewritten.

An actual final setup rerun exited **0**, with **all 509 Assets files unchanged** by SHA-256 comparison. The tracked-checkpoint audit covered **473 files**, with exactly five intended changes (the three existing scripts, Gameplay reference, and README), no deletions, and zero original test changes. All existing material, audio, model, UI, progression/save/input data, package/project settings, and original metadata files are unchanged. All owned entries have metadata and all **267 asset GUIDs are unique**.

Actual final Unity Test Framework runs:

| Suite | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| Complete EditMode | **135** | **0** | **0** |
| Complete graphical PlayMode | **68** | **0** | **0** |
| Original M0–M6-B coverage | **185 unchanged** | **0** | **0** |
| New M7 coverage | **10 EditMode + 8 PlayMode** | **0** | **0** |

The first runtime pass exposed two flaws in new QA expectations: the comparison driver could deadlock on a legacy Mixed section, and a Power Test escape can legitimately enter drillable Wood. The driver/expectations were corrected; the dedicated free Precision Escape and the unchanged earlier tests verify actual BLOCKED behavior. No original test was edited, weakened, or skipped. Final counts above are actual full-suite execution, not static analysis.

Validation includes deterministic replay/skips/global-RNG isolation, all categories/pacing bounds, every transition's swept motion, invalid/overlapping/early/unaffordable patterns, actual five-profile bypasses, all Ore routes, once-only rewards/banking, retries/Workshop/evolution, and existing presentation cleanup. Scripted generation-window tests cover **eight seeds × 50 km**, retaining at most ten active sections and forty registered obstacles with connected joins and no duplicate registrations. Diagnostic jumps are not represented as real-time motor travel. Normal Energy-limited journeys add actual gameplay traversal, and the original >30-km motor/streaming regression remains intact.

The real-time Dense Drilling test enables the actual motor's normal Update, observes progressive contact, first break, second contact, and completion of all three Wood targets. It awards exactly three normal obstacle Coins, leaves the alternative Brick untouched, and retains five audio sources and five particle systems. These are rendered runtime states, not manually emitted substitute destruction.

Evidence is ignored/local in `Logs/`: `M7-Setup.log`, `M7-Setup-Repeat.log`, `M7-EditMode.log/.xml`, `M7-PlayMode.log/.xml`, `M7-Encounters.log/.xml`, `M7-Balance.csv`, `M7-EvolutionJourneys.csv`, `M7-Distribution.txt`, `M7-DenseProfiler.txt`, `M7-Baseline.json`, and `M7-SetupHashes.json`.

## Visual review and performance

All captures use the actual gameplay camera/HUD at **540 × 960** through Direct3D11. Six encounter categories were rendered at approach, choice, and response times, with three additional consecutive-section transition frames. Representative category/transition frames and the live Dense Drilling sequence were inspected. Copper remains clearly visible behind the optional Brick guard; both the high-cost route and the open side can be read without arrows or a new HUD. The M6-B drill silhouette, materials, sounds, particles, camera, and UI style remain.

| Evidence | Files under `Logs/` |
| --- | --- |
| Easy Flow | `M7-EasyRight-{Approach,Choice,Response}.png` |
| Dense Drilling | `M7-DenseRight-{Approach,Choice,Response}.png` |
| Risk/Reward | `M7-OreGuardRight-{Approach,Choice,Response}.png` |
| Precision Escape | `M7-EscapeRight-{Approach,Choice,Response}.png` |
| Power Test | `M7-PowerRight-{Approach,Choice,Response}.png`, `M7-Power-{5,7,10,12,18}.png` |
| Recovery | `M7-RecoveryOpen-{Approach,Choice,Response}.png` |
| Consecutive joins | `M7-Transition-{0,1,2}.png` |
| Real-time destruction sequence | `M7-Dense-LiveContact.png`, `M7-Dense-LiveFirstBreak.png`, `M7-Dense-LiveSecondContact.png` |
| Confirmed Copper | `M7-OreMined-{OreOpenLeft,OreOpenRight,OreGuardLeft,OreGuardRight,RecoveryOre}.png` |

Some capture fixtures relocate the player to a deterministically selected generated section, so their displayed distance Coins include that test relocation. Those frames are presentation evidence and are not economy measurements. Balance journeys begin at the ordinary start and use unchanged natural Energy consumption. No video or physical listening session is claimed.

The live dense probe samples thirty actual rendered Editor frames with eight streamed sections, eighteen registered obstacles, and working drilling effects. Its latest recorded mean/peak main-thread counters are approximately **1.55 / 1.95 ms**, peak **373 draw calls**, peak **8,228 bytes GC/frame** including Editor/UI/test activity, and **16 active particles** during the sample. Earlier passes reached approximately 2.08/3.07 ms and 378 draws. Capacity remains **112 particles**, five emitters, and five sources.

Dense rendering has appreciable draw-call cost and needs GPU/representative performance profiling before broader platform conclusions. This short Editor sample is not FPS, a mobile/device result, an isolated allocation measurement, or a worst-case guarantee. Earlier low-density/accelerated-streaming smoke counters are not a matched rendering baseline. Selection is O(14) at spawn time; validation/transition compatibility is prepared once per attempt. No per-frame pathfinder, runtime track mesh rebuild, new physics debris, extra lights, or extra audio sources were added.

## Four focused manual review checks

1. **Ordinary variety:** Run from the normal start past Z=200. Compare Easy Flow, Dense Drilling, and Recovery; choose different continuous horizontal approaches and judge reaction time, target readability, and drill visibility.
2. **Copper decisions:** At starting Power, compare unguarded Ore, the Brick/Ore approach, and the open bypass. Watch actual Energy and one +3 Copper confirmation; complete the run and check one bank through Retry/Workshop.
3. **Power and escape:** Try Precision Escape and Power Test below Stone hardness, then with upgraded Pocket/EVO 2. Judge accessible bypasses, lateral escape, Wood's alternative, and the faster destruction of familiar Stone.
4. **Replay and flow:** Use the existing Inspector replay controls for fixed seeds 1337 and 904, then normal varied Retry. Check repeated patterns reproduce for a fixed configuration, section joins remain clean, and particles/audio/navigation stay tidy across Workshop and evolution.

No Android build, touch-input adaptation, store preparation, additional forms/regions/materials/resources, Energy regeneration, missions, combos, or M8 work was performed. Human play-test balance, physical audio comfort, full GPU profiling, and arbitrary-distance floating-point guarantees remain outside this automated validation.

Final Git state: **`main` at `cab310f`**, **5 modified, 75 untracked, 0 staged**. No commit, push, branch/remote change, or M8 work was performed. M7 stops here for Project Owner review.

## M7-B — encounter pacing and strategic route revision

Implemented and validated on **2026-10-28**, Unity **6000.3.8f1**, Windows PC Editor, against the existing uncommitted M7 work. The approved Git checkpoint remains `cab310f7526285eb3fed4ec349aea72a779ae295`. All fourteen encounter patterns and six categories remain. M8 was not started.

### Root cause and early-run pacing

The delay was structural. Original `TrackStreamer.Begin` registered four 40-meter legacy starters at Z=0–160, then filled the remaining 200-meter ahead window with Open at Z=160–200. Only later coverage requests selected encounter content starting at Z=200, whose first targets were around Z=220–224. Increasing pattern weights could not affect those first 200 meters. Prefetched encounters were visible ahead before the player reached their sections; generation time and traveled encounter distance were different quantities.

Normal Gameplay now uses **one existing starter, Z=0–40**, retaining its simple Wood target at Z=35. The other three scene starters are unregistered and inactive, with their references/templates preserved. There is no extra opening buffer. `Begin` fills the unchanged 200-meter ahead window with actual seeded encounters at Z=40, 80, 120, and 160. The open rear camera section remains. Retry restores this configuration without duplicate registrations. An explicit legacy catalog override still restores all four starters and the original buffer for controlled-content QA.

| Phase | Current behavior |
| --- | --- |
| Introduction, 0–40 m | One familiar target, launch/orientation space, continuous steering |
| First decisions, sections starting 40/80/120 m | First selection has pressure zero and multiple targets. Easy Flow, unguarded Wood/Ore alternatives, and basic Dense Drilling are eligible. Copper occurs by the third selection, at target Z≤144. No Stone in these three sections. |
| Main variety, sections starting 160 m onward | All six categories become eligible, with their own weights and pacing constraints; first targets are around 180 m. |

These phases use authored section length and selection ordinal, with no per-frame world-coordinate difficulty switch. The later-weight threshold remains ordinal 20 (now approximately Z=840 instead of Z=1000). Upgraded drills see the same region and familiar materials; Power does not replace easy content with harder content.

### Pattern and scheduling changes

Existing prefabs and definitions were revised in place through Unity Editor APIs, preserving their GUIDs and M6-B visuals:

- **Easy Flow:** two Wood targets now form a splayed arc rather than one straight line.
- **Dense Drilling:** three aligned Wood targets retain their destruction rhythm; the alternative Brick is beside the first Wood at local Z=20, exposing the choice before entering the cluster. Dense can appear from ordinal 1.
- **Unguarded risk/reward:** Wood and Ore share an approach row, making their different resource/cost choices visible together.
- **Guarded risk/reward:** Brick followed by Ore gains a separate Wood approach on the other side. Each guarded prefab adds one reused Wood instance; there are no new meshes, materials, or gameplay patterns.
- **Precision Escape:** the Stone pair has a stronger lateral stagger. Power Test and Recovery retain their distinct parallel-choice/open-deck roles.

Selection still uses the private seeded xorshift state, once per pick, with configuration snapshotted at attempt start. It prevents consecutive identical categories and prefers avoiding the last **two arrangement families**, including mirrored variants and the similar unguarded/Recovery Ore family. Family memory is a soft preference: it relaxes only if needed to satisfy the hard eligibility, transition, resource, or pacing rules. `ArrangementRelaxations` exposes this case. It does not relax safety or force a fixed opening sequence.

The first encounter is calm. Every pressure-2 selection must be followed by pressure zero. Copper remains within three selections and Recovery within six. Empty Recovery requires **four intervening selections**, preventing repeated long empty stretches; optional-Ore Recovery can satisfy relief/resource cadence together. The existing pressure, starting-Power mining budget, reaction, and transition rules remain.

The original long-selection regression now exercises M7-B: **128 seeds × 1,000 selections**, zero fallbacks, all fourteen patterns/six categories present. Category counts are **25,140 Easy Flow; 17,916 Dense Drilling; 34,832 Risk/Reward; 10,022 Precision Escape; 13,230 Power Test; 26,860 Recovery**. The focused revision test adds 64,000 selections checking pressure relief/empty spacing, with family relaxation below its 20% bound. A separate 256-seed opening test finds over twenty distinct opening sequences, multi-target choices before 100 m, and accessible Copper before 150 m. These are generation samples, not real-time kilometers traveled.

### Risk/reward and reachability

At starting Power 5, the independent Wood route costs **11.2 drilling Energy**, yields one normal Coin, and yields no Copper. The aligned Brick/Ore route costs **51.6 drilling Energy**, yields two Brick Coins and **three Copper after complete Ore destruction**. Unguarded Ore still costs 18 drilling Energy. A collision-free outside bypass is available; continuous steering can also permit an informed approach around a guard. There are no invisible route restrictions or mandatory fees. At high Power, familiar targets take less time/Energy through the unchanged damage rule.

The actual motor traversed both guarded/cheap approaches at Power **5, 7, 10, 12, and 18**, confirming the expected damage-derived cost, distinct rewards, independent footprint clearance, and zero Copper while Ore remained partially intact. The Wood line cannot clip the Ore line and vice versa under the full 1.7-unit footprint. All Ore lines remain drillable at starting Power and under the unchanged 60-Energy initial authoring budget; none require Stone.

All original safety checks run against the revised assets: **784 ordered transition/speed/start combinations**, **140 actual-motor free-bypass cases** across five progression profiles, starting-Power mining certificates, hardness-dependent BLOCKED/escape, and actual reward/banking tests. A new check validates the retained introduction's join to every encounter at maximum accepted speed **26**. Reaction allowance **0.35 seconds**, steering response **18**, full front/rear reach **1.85/1.15**, and tolerance **0.04** remain unchanged. Eight-seed × 50-km generation-window tests and the earlier >30-km motor/streaming regression still pass, with bounded sections and registrations.

This establishes available routes under the stated movement/reaction approximation. It does not guarantee arbitrary late human input, gesture duration, focus loss, affordability with depleted Energy, every custom speed configuration, or unlimited floating-point distance. No safety threshold, hardness, damage, Energy rule, or steering behavior was weakened.

### Fair M7 economy comparison

The baseline here is **pre-revision M7, not the M6 legacy catalog** used in the historical report. Before changing layouts/scheduling, `Tests/Fixtures/M7-Reference.json` captured all fourteen M7 layouts/weights and three 256-selection golden sequences (seeds **1337, 904, 2026**). A test-only fixture reconstructs those layouts with unchanged obstacle assets and original four-starter/one-buffer pacing. All **768 golden IDs** match, and its runtime start remains Z=200 with four registered targets. The fixture is excluded from normal gameplay and never accesses the real save.

Both cohorts use identical starting attributes and the same policy: seek one affordable Ore, then preserve Energy using open routes. Thirty samples cover five profiles × three seeds × two generators. The table reports observed means; identical rewards were not required by the comparison test.

| Profile | M7 distance | M7-B distance | Coins, both | Copper, both | Drilling Energy, both |
| --- | ---: | ---: | ---: | ---: | ---: |
| Starting Pocket, Power 5 | 1,173.89 | 1,173.89 | 234 | 3 | 18.00 |
| Intermediate Pocket, Power 7 | 1,810.16 | 1,810.17 | 362 | 3 | 12.86 |
| MAX Pocket, Power 10 | 2,935.30 | 2,935.30 | 587 | 3 | 9.00 |
| Starting Workshop, Power 12 | 3,357.54 | 3,357.55 | 671 | 3 | 7.50 |
| MAX Workshop, Power 18 | 5,484.82 | 5,484.80 | 1,096 | 3 | 5.00 |

Each sample destroyed one Ore and had zero BLOCKED episodes. Earlier Copper timing changed the experience without meaningful income drift under this preservation-oriented policy. These long avoidance runs do not predict the approximately 337-meter drilling-heavy run from manual review, nor every human play style.

Two unfunded seed families (**900, 1900**) on each generator use real purchases, run completion, banking, and evolution. All **four journeys reached evolution in six runs, earning 2,704 Coins and 12 Copper**, within the unchanged 5–8 target. The earlier guided evolution regression also passes on normal M7-B. An initial revision placing Dense Brick farther back caused that reactive driver to mine it unintentionally and miss its budget; moving the Brick alongside the first Wood restored the six-run result. The test's eight-run limit was retained. Coin formulas, Copper amounts, 2,480-Coin Pocket upgrade budget, 12-Copper evolution requirement, form stats, save format, and receipt logic were not changed.

### Exact tests, setup, and preservation audit

| Complete suite | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| EditMode | **139** | **0** | **0** |
| Graphical PlayMode | **74** | **0** | **0** |
| Total | **213** | **0** | **0** |

All **203 earlier M0–M7 test cases remain**; M7-B adds **four EditMode and six PlayMode tests**. Earlier tests were not removed or skipped. The shorter opening required narrow fixture adaptations: archived M2 material-isolation tests explicitly select a cloned legacy catalog when needed; isolation deactivates all registered non-target obstacles; retry checks exact active-obstacle membership instead of a fixed four-target count; M7 staging uses `EncounterStartZ` instead of literal 200. Six previously tracked test files and the existing untracked M7 PlayMode fixture changed for these assumptions. Material, reward, progression, runtime-safety, and presentation assertions remain, including the original unfunded evolution test. This is not a claim that old test files are byte-unchanged.

The existing **Tools > DrillRunner > Setup M7 Gameplay Variety** action migrates original M7 data once to revision 2, then validates without rewrites or manual reference wiring. An actual final setup rerun exited **0**: **520 Assets files before/after, zero changed/deleted files** by SHA-256. All **273 metadata GUIDs are unique**. The pre-revision M7 hash snapshot has **zero missing files**; changes are confined to encounter scheduling/layout/data/tooling and necessary fixtures. No original material, model, audio, UI, camera, input, legacy section/catalog, progression/save code/data, package, or project-setting assets changed during M7-B. The Gameplay scene's existing M7 reference was preserved without another scene edit. No scene/prefab YAML was manually edited.

### Rendered evidence and performance

Actual **540 × 960** gameplay camera/HUD captures were rendered and inspected. A natural motor-Update episode traveled from the ordinary start without relocation, using synthetic steering. Its sampled frames cover early Ore, a multi-target Dense approach, Flow, guarded choice, Recovery, mirrored unguarded choice, and Power Test:

| Inspected evidence | Files under `Logs/` |
| --- | --- |
| Early Wood/Ore choice, before mining | `M7B-Natural-42.png` (HUD 43 m) |
| Early complete Copper payout | `M7B-Natural-62.png` |
| Dense Drilling before 150 m | `M7B-Natural-95.png` (HUD 96 m) |
| Flow → guarded choice → Recovery transitions | `M7B-Natural-{130,170,210}.png` |
| Mirrored unguarded choice and Power Test | `M7B-Natural-{280,330}.png` |
| Precision Escape approach | `M7B-EscapeRight-Choice.png` |
| Actual live contact, break, next contact | `M7B-Dense-Live{Contact,FirstBreak,SecondContact}.png` |
| Distinct cheap/guarded outcomes | `M7B-{Cheap,Guarded}-Outcome.png` |

The same 9:16 composition keeps drill/HUD readable. Wood, Brick, Stone, and crystalline Ore retain distinct silhouettes/colors; parallel choices and open outside gaps are visible. No decorative obstacle/collider, new arrow, warning UI, camera change, or audio/particle manager was introduced. The sampled category changes and separate live destruction sequence were inspected; there is no recorded video or physical listening-session claim. Staged route/Precision/live-contact fixtures relocate the player and their distance totals are presentation evidence only; comparative economy uses ordinary-start journeys.

The current thirty-frame live Dense probe measured **351 peak draw calls** versus the reported M7 sample around **373**, with **1.67 / 2.17 ms mean/peak main-thread counters**, peak **8,228 bytes GC/frame** including Editor/test activity, **16 particles**, eight active sections, and sixteen registered obstacles. This is a short Editor observation, not a matched GPU benchmark, an optimization claim, a spawn-spike guarantee, mobile FPS, or isolated allocation measurement. No new art/material/audio/VFX systems were added; at most four obstacles per encounter and the existing bounded streaming window remain. Selection remains O(14) on spawn, with no per-frame generation/pathfinding or unbounded pool.

Current evidence: `Logs/M7B-EditMode.log/.xml`, `M7B-PlayMode.log/.xml`, `M7B-Setup-Repeat.log`, `M7B-SetupHashes.json`, `M7B-Baseline.json`, `M7B-Balance.csv`, `M7B-Evolution.csv`, `M7B-Distribution.txt`, `M7B-NaturalTrace.txt`, `M7B-DenseProfiler.txt`, and the captures above. Logs are ignored local artifacts. Rerunning the historical M7 test class also refreshes its `M7-*` output files with current behavior; the frozen reference fixture and historical tables distinguish the true baseline.

### Three focused manual checks for M7-B review

1. **Early flow/readability:** From the normal start, play to 150–200 m on seeds 1337 and 904. Judge time to see/choose Wood, Dense, and Copper alternatives and the relief after pressure; then try a fresh seed.
2. **Copper tradeoff:** At default Power, compare unguarded Ore, the cheap Wood approach, Brick/Ore, and the open bypass. Check Energy, complete-only +3 Copper, and one bank through Retry/Workshop.
3. **Upgrade/escape replay:** Compare Stone bypass/BLOCKED escape below Power 9 with upgraded Pocket and Workshop destruction, then Retry/Workshop. Check continuous steering, joins, and effect/audio cleanup.

Remaining limitations are human reaction/route comprehension, subjective balance across more play styles, physical audio comfort, and representative GPU/spawn profiling. No new mechanic, economy change, region, form, Android build, release work, or M8 was included.

Final Git state: **`main` at `cab310f`, 11 modified, 86 untracked, 0 staged** (expanded file counts). The original uncommitted M7 work is retained. No commit, push, branch/remote change, or M8 work was performed. Stop for Project Owner review.
