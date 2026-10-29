# M8 — full drill evolution and progression

This is the historical M8 report. **M8-R supersedes its long-run Energy/reward assumptions and duration estimates; see `M8-R-RapidRunValidation.md` for current operating profiles and measured rapid-loop results.** Form stats, prices, resources, and save compatibility described here remain.

Implemented in Unity **6000.3.8f1**, Windows PC Editor, portrait 9:16. Before editing, `main` was clean at **`b9cdf9b536dcee30efe7dfa14752bee6f2f4b772`** and a live `git ls-remote origin refs/heads/main` returned the same commit. This confirms approved M7/M7-B was committed and pushed before M8. No player save was accessed by setup or reset for QA. No commit, push, Android build, or M9 content is part of this milestone.

## Eight forms and acquisition boundary

The existing configuration chain, progression service, Workshop, visual selector, motor, and reward ledger are extended. There is no second inventory, controller, track generator, region selector, or cinematic system. Each form has five Coin purchases in each of the three existing categories. Effective values are absolute form-local tables calculated only by `ProgressionConfig.Calculate`; previous purchase history is retained separately and is not added again as a bonus.

| EVO | Stable identifier | Display name | Power, base → MAX | Energy, base → MAX | Cruise, base → MAX | Total upgrade Coins | Next evolution resources |
| --- | --- | --- | ---: | ---: | ---: | ---: | --- |
| 1 | `pocket_drill` | Pocket Drill | 5 → 10 | 100 → 180 | 10 → 12 | 2,480 | 12 Copper |
| 2 | `workshop_drill` | Workshop Drill | 12 → 18 | 200 → 300 | 12.2 → 13 | 5,230 | 24 Copper |
| 3 | `industrial_drill` | Industrial Drill | 20 → 30 | 320 → 460 | 13 | 15,050 | 30 Iron |
| 4 | `heavy_borer` | Heavy Borer | 33 → 48 | 480 → 650 | 13 | 20,650 | 50 Iron + 12 Copper |
| 5 | `hydraulic_drill` | Hydraulic Drill | 52 → 75 | 680 → 900 | 13 | 31,050 | 40 Quartz |
| 6 | `crystal_core_drill` | Crystal Core Drill | 82 → 115 | 950 → 1,250 | 13 | 70,300 | 24 Alloy Core + 20 Quartz |
| 7 | `fusion_drill` | Fusion Drill | 125 → 180 | 1,320 → 1,700 | 13 | 108,000 | 30 Energy Shard + 12 Alloy Core |
| 8 | `quantum_drill` | Quantum Drill | 195 → 280 | 1,800 → 2,300 | 13 | 144,100 | Final form; no next evolution |

**Production-reachable in M8: EVO 1 → EVO 2 → EVO 3.** Workshop Yard continues to award only existing Coins and Copper. Iron, Quartz, Alloy Core, and Energy Shard are valid persistent resources, but have **no gameplay source yet**. Normal EVO 3 → EVO 4 stops at its Iron requirement until M9. Advanced stages are tested with explicit temporary or automated isolated funding; no requirement is bypassed in production.

`EligibleRegionCount` exposes the approved future eligibility schedule: one region at EVO 1–2, two at EVO 3–4, three at EVO 5–6, four at EVO 7–8. This is metadata, not region availability or a selection UI. Resource dependency validation requires Copper no later than stage 1, Iron from 3, Quartz/Alloy Core from 5, and Energy Shard from 7. A transition cannot require a resource whose first eligible stage is later than its current form; duplicate/unknown/nonpositive costs and skipped/cyclic stages are rejected.

## Explicit stat and price tables

All rows below are levels **0, 1, 2, 3, 4, 5**. Pocket and Workshop values and all upgrade prices are preserved from M5. Their existing visual prefabs and preview textures are also preserved. Workshop only gains its next-form link and data-driven Copper requirement.

| EVO | Drill Power | Maximum Energy |
| --- | --- | --- |
| 1 | 5, 6, 7, 8, 9, 10 | 100, 115, 130, 145, 160, 180 |
| 2 | 12, 13, 14, 15, 16, 18 | 200, 220, 240, 260, 280, 300 |
| 3 | 20, 22, 24, 26, 28, 30 | 320, 345, 370, 400, 430, 460 |
| 4 | 33, 36, 39, 42, 45, 48 | 480, 510, 540, 575, 610, 650 |
| 5 | 52, 56, 60, 65, 70, 75 | 680, 720, 760, 805, 850, 900 |
| 6 | 82, 88, 94, 101, 108, 115 | 950, 1000, 1060, 1120, 1180, 1250 |
| 7 | 125, 135, 145, 156, 168, 180 | 1320, 1390, 1460, 1540, 1620, 1700 |
| 8 | 195, 210, 225, 242, 260, 280 | 1800, 1890, 1980, 2080, 2190, 2300 |

| EVO | Power Coin prices | Energy Coin prices | Drive Coin prices |
| --- | --- | --- | --- |
| 1 | 60, 100, 160, 240, 340 | 50, 90, 140, 210, 300 | 50, 90, 140, 210, 300 |
| 2 | 150, 230, 340, 480, 650 | 130, 210, 310, 440, 600 | 130, 210, 310, 440, 600 |
| 3 | 500, 750, 1000, 1400, 1850 | 450, 650, 900, 1250, 1650 | 400, 600, 850, 1200, 1600 |
| 4 | 700, 1000, 1400, 1900, 2500 | 600, 900, 1250, 1700, 2250 | 550, 850, 1200, 1650, 2200 |
| 5 | 1100, 1500, 2100, 2800, 3700 | 950, 1350, 1900, 2550, 3400 | 900, 1300, 1800, 2450, 3250 |
| 6 | 2600, 3500, 4700, 6200, 8000 | 2400, 3200, 4300, 5700, 7400 | 2300, 3100, 4200, 5500, 7200 |
| 7 | 4000, 5400, 7200, 9500, 12500 | 3600, 4900, 6600, 8800, 11500 | 3400, 4700, 6400, 8500, 11000 |
| 8 | 5400, 7200, 9600, 12600, 16500 | 4900, 6600, 8900, 11800, 15400 | 4600, 6300, 8500, 11200, 14600 |

| EVO | Minimum launch boost | Recovery acceleration |
| --- | --- | --- |
| 1 | 2, 2.2, 2.4, 2.6, 2.8, 3 | 12, 14, 16, 18, 20, 22 |
| 2 | 3.25, 3.4, 3.6, 3.8, 4, 4.25 | 24, 26, 28, 30, 32, 34 |
| 3 | 4.5, 4.75, 5, 5.25, 5.5, 5.75 | 36, 38, 40, 42, 44, 46 |
| 4 | 6, 6.25, 6.5, 6.75, 7, 7.25 | 48, 50, 52, 54, 56, 58 |
| 5 | 7.5, 7.75, 8, 8.25, 8.5, 8.75 | 60, 62, 64, 66, 68, 70 |
| 6 | 9, 9.2, 9.4, 9.6, 9.8, 10 | 72, 74, 76, 78, 80, 82 |
| 7 | 10.25, 10.45, 10.65, 10.85, 11.05, 11.25 | 84, 86, 88, 90, 92, 94 |
| 8 | 11.5, 11.75, 12, 12.25, 12.5, 12.75 | 96, 98, 100, 102, 104, 106 |

Pocket cruise remains 10, 10.4, 10.8, 11.2, 11.6, 12, with maximum launch boost 12 and speed ceiling 24. Workshop cruise remains 12.2, 12.35, 12.5, 12.65, 12.8, 13; maximum boost remains 12.25, 12.4, 12.5, 12.6, 12.75, 13, with ceiling 26. EVO 3–8 use cruise **13** and maximum boost **13** at every Drive level, ceiling **26**. Drive purchases improve minimum-charge launch and recovery. This preserves MAX Workshop's purchased peak speed without exceeding the M7 reaction/sweep envelope. Only advanced Drive tables permit a cruise plateau; Power/Energy and original Drive value validation remain strict. No movement integration, collision solver, camera, steering response, hardness, or Energy-consumption constant changed.

## Transaction, inventory, and migration

`ProgressionService` remains the sole permanent transaction owner. Eligibility combines all current-form MAX levels, every configured resource, adjacent next form, writable progress, and permitted Workshop state. Evolution clones the entire profile, subtracts exact costs, initializes only the next form, validates the full ownership history, and saves before publishing. Failed writes and reentrant/duplicate callbacks cannot publish partial spending or upgrade mutations. The presentation is shared across all seven transitions and remains 0.9 seconds, with input/purchase/navigation/reset locks. Reloading an interrupted presentation restores the committed form directly. EVO 8 has a final-state message and no visible EVOLVE/EVO 9 action.

The checksum envelope/payload **Version 1**, path, atomic store/backup mechanism, `FormSchema = 1`, original Pocket/Workshop level fields, Coins, **Copper field**, profile identity, revisions, and issued/banked receipts retain their original meanings. A versioned additive **`EvolutionSchema = 1`** adds `Iron`, `Quartz`, `AlloyCore`, `EnergyShard`, and ordered `AdvancedForms` records with stable form identifiers and three local levels. Missing extension fields are accepted only for valid legacy EVO 1/2 histories, initialized in memory, and written on the next normal transaction. Loading a valid old save does not rewrite its bytes.

Previous forms must have MAX purchases, records must be consecutive and unique, and the current form must have valid local levels. Snapshot/store copies deep-copy records, so failed candidate writes cannot mutate published history. Negative balances, unknown forms/resources, invalid/future extension versions, corrupt checksums, skipped histories, invalid levels, and receipt inconsistencies remain read-only with original files preserved. Checked currency arithmetic rejects overflow. Automated persistence tests use unique OS-temp directories; gameplay tests use memory stores, never the user's profile. Advanced resource generation is absent from the run reward ledger, so a normal bank cannot fabricate Iron/Quartz/Alloy/Shards.

## Models, Workshop, and Editor integration

Six new visual-only prefabs use shared beveled meshes/materials, combined static geometry, and the existing `Drill Rotor` and `Motor Housing` animation mounts. Industrial uses a ribbed inline motor/transmission; Heavy Borer a broad red shell and larger cutting assembly; Hydraulic twin pressure barrels/polished rods; Crystal Core a faceted crystal in an ivory cradle; Fusion a ceramic shell/contained halo; Quantum swept conductive rails and intersecting core gimbals. Shared flutes and metal/rubber materials keep the toy-industrial family coherent. Emissive accents use materials, with no extra lights or particle systems.

Horizontal decorative geometry remains within the accepted footprint; no visual prefab has a collider, rigidbody, script, camera, or motor. One inactive Pocket child and at most one reused/replaced advanced visual are retained; only the selected form is active. The authoritative motor root is never scaled. Gameplay uses the same one camera, full 1.7 × 1.3 × 3 collider, input, effects, and audio. Preview textures are Editor renders of those same prefabs, with no runtime preview camera or eight-model display.

Workshop retains its existing three upgrade cards and interactions. Current name/stage and preview use the selected config; requirements list every cost and owned/required amount. The existing Coins/Copper cards remain. A compact contextual row shows relevant advanced resources and any other owned advanced balances, leaving fresh Pocket players' preview/card layout intact. Insufficient resources disable evolution without spending. Final form retains upgrades and Start Run.

Run **Tools > DrillRunner > Setup M8 Full Evolution** (batch entry `DrillRunner.Editor.FullEvolutionSetup.Setup`). It generates/wires the new prefabs, shared batches, materials, previews, six configuration assets, chain, and contextual resource row through Unity Editor APIs. No manual references or handwritten scene/prefab YAML are required. Initial preview generation needs graphical Editor execution; configured reruns can validate without graphics. Earlier setup commands remain idempotent.

For QA, select **M4 Workshop** during Play Mode. The existing temporary funded profile remains. The new **M8 development-only stage shortcut** exposes EVO 1–8, temporary Coins, Copper, Iron, Quartz, Alloy Core, Energy Shard, and MAX-current-form selection. Use **QA: Select temporary stage (no disk writes)** while Ready/Completed. Confirming replaces only this session with a `MemoryProgressStore`, valid purchased history, attributes, and selected visual. Inspector shows effective Power/Energy/cruise/cap and region eligibility. `DEVELOPMENT SANDBOX` remains visible in Workshop. Purchases, evolution, and Results banking stay in memory; backup recovery cannot target a disk store during the sandbox. Leave Play Mode to restore real progression. Set a required resource to zero to check missing-resource behavior; no JSON edits are needed.

## Balance, validation results, and evidence

The complete suites passed **157 EditMode and 81 graphical PlayMode tests**, with **zero failures or skips**. All **213 earlier cases remain**, plus **18 EditMode and seven PlayMode M8 cases**, for **238 unique cases**. A test-name comparison against the M7-B XML found zero missing earlier cases. After the final MAX-message and invariant CSV refinements, the focused M8 suites also passed **18/18 EditMode and 7/7 PlayMode**, zero failures/skips. These focused reruns are not counted twice in the 238-case total.

Coverage includes all seven successful transitions; every missing upgrade/resource; exact multi-resource spending; failed-write rollback/deep-copy isolation/reentrant callbacks; fresh, authentic M4, authentic M5, EVO 3, and EVO 8 profiles; unsupported schemas/invalid histories/negative balances/overflow; completed-run receipts and backup recovery; and all-form visual/attribute/reload/charge/drilling/steering/HP/Energy/Copper tests. The new actual-motor safety test traverses **224 cases: eight forms × base/MAX × fourteen patterns**, entering from the opposite bypass side after the 0.35-second reaction allowance. All avoid drilling/BLOCKED and award no obstacle/Copper rewards. The original 784 ordered-pair motion tests, 140 five-profile bypass cases, long-distance streaming, and ordinary progression regressions remain. Weak-Power Stone/BLOCKED escape is exercised under all eight visuals using the existing isolated override, with unchanged hardness 9 and zero Stone damage below it.

Earlier tests are retained. Two M5 assertions specifically described EVO 2 as terminal: the scene test now checks the Industrial link, and the UI test checks a visible but disabled next EVOLVE action after entering level-zero Workshop. Their original transaction, resource, input-lock, visual, and persistence assertions remain; no case is removed or skipped. M7/M7-B fixture behavior is retained.

Production journeys use ordinary-start motor travel, natural Energy depletion, actual Copper destruction, banking, and affordable purchases, without funding, obstacle relocation, altered rates, or prices. Future-region structural tests use explicit isolated funding to exercise the complete chain. Later price/resource values are provisional: target 10–15 middle-stage and 18–25 late-stage runs requires validation when M9 supplies real regions and acquisition rates. No future region Coin income or ore density is claimed, and structural funding is not proof of normal advanced-form acquisition.

Both production seed families, **8100 and 9200**, reached Workshop after **six runs** and Industrial after **eight more** (fourteen total). Each new tier was bought/evolved through the actual service and ledger. No Iron/Quartz/Alloy/Shards were granted. The original M5 six-run regression and all M7-B comparative journeys still pass. The 24-Copper Workshop requirement represents eight ordinary complete Ore payouts and creates no pre-EVO-3 resource dependency on M9.

The future estimate test uses **pre-funded advanced resources**, zero initial Coins at each stage, and a deliberately generous mathematical Workshop Yard avoidance model: `run seconds = current Energy / 0.7`, `Coins = floor(cruise × seconds / 5)`, then normal Energy/Drive/Power purchases. It uses actual prices/stats and bank receipts, but is **not motor-traveled gameplay** and assumes no mining cost. It makes no assumption about M9 region income or ore density.

| Starting EVO | Modeled runs to next form | Earned Coins | Mean modeled minutes/run |
| --- | ---: | ---: | ---: |
| 3 | 10 | 15,892 | 10.19 |
| 4 | 10 | 22,671 | 14.54 |
| 5 | 10 | 31,546 | 20.23 |
| 6 | 17 | 73,421 | 27.69 |
| 7 | 19 | 112,946 | 38.11 |

These are provisional Coin-limited estimates with resource waiting removed, not promised 10–19-run progression. Mining reduces remaining Energy/distance; unavailable resource supply can lengthen progression, while future income could change it. Advanced Energy tables also permit long avoidance-only runs in Workshop Yard. The modeled durations highlight a tuning risk for M9, not intended future session-length claims or a reason to silently change approved Energy rules. No artificial waiting timer or monetization was introduced.

## Final visual and performance review

Actual **540 × 960** gameplay and Workshop images were rendered through the existing camera/HUD and inspected for **all eight forms**, including unchanged Pocket/Workshop appearance, six new gameplay models, every Workshop preview, two interrupted evolution presentations (2 → 3 and 7 → 8), and EVO 8 base/MAX final states. Separate real-contact images show Industrial and Quantum drilling with partial HP and the existing chips/progress feedback. Distinct body/core silhouettes and the forward bit remain visible, with readable HUD, unchanged camera framing, and no decorative collision or duplicated active form. Advanced Drive cards show improving launch boost/recovery instead of a misleading cruise-to-identical-cruise upgrade. Single-resource requirements retain three readable lines; multi-resource requirements fit four lines without hiding their status.

| Inspected evidence | Files under `Logs/` |
| --- | --- |
| Gameplay, all eight forms | `M8-Evo{1..8}-Gameplay.png` |
| Workshop previews, all eight forms | `M8-Evo{1..8}-Workshop.png` |
| Real partial drilling | `M8-Evo3-Drilling.png`, `M8-Evo8-Drilling.png` |
| Interrupted, already-saved evolution presentations | `M8-Final-Structural-Evolution2to3.png`, `M8-Final-Structural-Evolution7to8.png` |
| Final form, base/MAX | `M8-Final-Evo8-Workshop.png`, `M8-Final-Evo8-MAX-Workshop.png` |

These are actual rendered runtime states, with a synthetic input/manual motor clock used for most controlled captures. They are not recorded real-time footage or proof of human animation/reaction comfort. Later-form images and structural evolutions use clearly identified isolated funding or the visible development sandbox; they do not imply advanced resources can be mined in Workshop Yard. No physical listening session is claimed.

The controlled rendering probe uses seed **1337**, approximately 12 m, a 540 × 960 target, normal shared rotor animation, and thirty sampled frames per form. The motor clock is held for this rendering comparison; it is not a full moving/drilling benchmark. Latest final-run results:

| EVO | Active-model renderers | Referenced mesh vertices | Peak Editor draw calls |
| --- | ---: | ---: | ---: |
| 1 | 9 | 7,167 | 314 |
| 2 | 9 | 7,167 | 314 |
| 3 | 11 | 7,383 | 324 |
| 4 | 10 | 7,383 | 319 |
| 5 | 11 | 6,871 | 324 |
| 6 | 10 | 6,543 | 319 |
| 7 | 9 | 6,647 | 314 |
| 8 | 10 | 7,015 | 319 |

Latest mean main-thread counters span **1.43–1.55 ms**, peak counters **1.93–2.34 ms**, with peak **7,966 bytes GC/frame** including Editor/test/UI activity. An earlier full pass varied up to approximately 2.00 ms mean/3.33 ms peak; these short Editor samples vary and are not FPS or isolated allocations. Relative rendering cost is bounded to at most two additional active-model renderers/ten observed draws in this controlled scene. The retained real-time Pocket Dense regression also measured **346 peak draws**, approximately **1.89/2.77 ms** mean/peak, 8,228 GC bytes/frame, sixteen particles, eight sections and sixteen targets; its density/timing differs from the all-form comparison. No GPU, worst-case spawn spike, mobile/device FPS, or broad optimization claim is made.

Models share authored materials/batches and allocate no meshes/materials per frame. Runtime retains one inactive Pocket visual and at most one advanced visual, reusing it on Retry and replacing it only on a form change. Five audio sources, five particle systems, and the existing 112-particle cap remain. Streaming/pooling/obstacle counts and the M7 generator are unchanged.

## Final setup and Git audit

An actual final `FullEvolutionSetup.Setup` rerun exited **0** with **704 Assets files before and after, zero changed/deleted files** by SHA-256. The 520-file pre-M8 Assets snapshot has **zero missing files** and twelve intended existing-file changes: eight progression/visual/Workshop scripts including the Inspector, Gameplay's new contextual resource label/reference, Workshop's next link/resource cost, and the two M5 test assertions described above. The approved Pocket asset, both original form prefabs/previews, original materials/meshes, input, camera, movement/contact/energy/reward/save-store code, M7/M7-B encounter assets/logic, package/project settings, and existing metadata are unchanged. All owned assets have metadata; all **365 GUIDs are unique**. Source/Markdown diff whitespace checks pass; serialized assets were produced by Unity APIs.

Evidence is local/ignored under `Logs/`: `M8-Setup.log`, `M8-Previews.log`, `M8-Setup-Repeat.log`, `M8-Baseline.json`, `M8-SetupHashes.json`, complete `M8-EditMode.log/.xml`, complete `M8-PlayMode.log/.xml`, focused `M8-FinalEdit.log/.xml`, focused `M8-FinalPlay.log/.xml`, `M8-Forms.csv`, `M8-ProductionJourneys.csv`, `M8-FutureEstimates.csv`, `M8-RenderProfiler.csv`, and the image files above. CSV decimal output is culture-independent. The existing regression classes also refresh their historical-prefix log/image outputs with current behavior; the approved earlier reports remain historical records.

Git remains **`main` at `b9cdf9b`, 13 modified, 185 untracked, 0 staged** (expanded file counts). No commit, push, branch/remote change, user-save reset, or M9 work occurred. M8 is ready for Project Owner review.

## Four focused manual review checks

1. **Production loop:** Play EVO 1 → EVO 2 → EVO 3 through Workshop Yard; check the unchanged Pocket progression, Copper banking, and the intentional Iron gate after Industrial.
2. **All-form presentation:** Use the temporary EVO selector to compare all eight Workshop/gameplay silhouettes, visible bits, charge/drilling animation, and camera framing at both steering extremes.
3. **Evolution/resources:** Temporarily MAX a multi-resource stage, remove one required balance, then fund it and evolve. Interrupt/reload a presentation, inspect history, and check EVO 8 has no further action.
4. **Sandbox and safety:** Buy/evolve/run/Retry in the sandbox, leave Play Mode, and verify real progress returns. At high Drive check encounters, Stone/BLOCKED escape with the existing Power override, and effect/audio cleanup.

Known limitations: only Workshop Yard and Copper acquisition exist; future-region balance and acquisition remain unvalidated; reaction safety uses the existing stated movement approximation; visual comfort and human play style need owner review; desktop Editor counters do not establish GPU/device/mobile performance. No M9 region/material/resource source, new mechanic, currency beyond the approved five resources, monetization, Android/release work, commit, or push is included.
