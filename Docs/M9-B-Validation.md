# M9-B — Crystal Refinery, Energy Nexus and complete region progression

Unity **6000.3.8f1**, URP, Windows PC Editor, portrait **540 × 960**. Work began on `main` at `ed3f9516f84531294264c582bd85fb9ef0d56f06` with **27 modified / 145 untracked / 0 staged** M9-A files. `Logs/M9B-Baseline.json` records all **878 existing Assets files** before revision; `M9B-GitBaseline.txt` retains the initial working tree. All existing M9-A work is retained. No real player profile is used for automated validation. No commit, push, Android or M10 work is included.

## Four playable regions

| Stable persistence ID | Region | Unlock | Legitimate resources |
|---|---|---:|---|
| `workshop_yard` | Workshop Yard | EVO 1 | Copper |
| `quarry` | Quarry / Scrapworks | EVO 3 | Iron |
| `advanced_works` | Crystal Refinery | EVO 5 | Quartz, Alloy Core |
| `high_energy` | Energy Nexus | EVO 7 | Energy Shard |

The existing IDs are unchanged when production display names replace deferred metadata. The same four-row Workshop selector, `WorkshopController`, motor, `TrackStreamer`, `EncounterSequence`, ledger, progression service and profile remain authoritative. Earlier regions stay available. Selection still requires Workshop access and a durable successful transaction; selection during a run, pending bank or evolution remains disallowed. The selector shows unlock stages, contextual resources and independent records. No new scene, gameplay controller, generator, inventory or save file is introduced.

## Authored regions and materials

Crystal Refinery has purple processing chambers, cyan quartz clusters, steel extraction caps, conveyor slats and conveyed crystals. Energy Nexus uses navy reactor columns, stacked steel collars, blue vertical power channels, separate conduit runs and amber cabinet status panels. Distinct structures, palettes and silhouettes separate both from Quarry. All reusable dressing is combined by shared material into region environment prefabs. No decorative colliders, new real-time lights, AudioSources, ParticleSystems, bloom or full-screen effects are attached to scenery. Camera geometry, drill models and approved world lighting remain.

| New definition | Stable ID | Hardness | HP | Raw material Energy/s | Full destruction reward |
|---|---|---:|---:|---:|---|
| Crystal Composite | `crystal_composite` | 50 | 1,500 | 38 | 400 Coins (raw 16 × existing 25 multiplier) |
| Energy Alloy | `energy_alloy` | 100 | 2,200 | 55 | 800 Coins (raw 32 × 25) |
| Quartz Ore | `quartz_ore` | 50 | 650 | 30 | 4 Quartz, zero Coins |
| Alloy Core | `alloy_core` | 52 | 1,800 | 55 | 3 Alloy Core, zero Coins |
| Energy Shard Ore | `energy_shard` | 120 | 1,200 | 48 | 2 Energy Shard, zero Coins |

These extend the six previous normal tiers to **eight**. Existing Wood, Brick, Stone, Granite, Metal Plating, Reinforced Block, Copper and Iron definitions/payouts are unchanged. Hydraulic base Power 52 can mine Composite and Quartz. Alloy Core is already possible at 52, but its larger HP makes it slower and more costly than Quartz; Crystal Core base Power 82 improves that efficiency without needing Fusion. Energy Alloy blocks low-Power Refinery drills until Crystal Core's level-three Power 101, with full-footprint bypasses throughout. Fusion base Power 125 can mine Energy Shards at hardness 120; Quantum is never required for its own evolution ingredients.

The full normal hardness ladder is **Wood 2 → Brick 5 → Stone 9 → Granite 18 → Metal Plating 26 → Reinforced Block 40 → Crystal Composite 50 → Energy Alloy 100**. Special ore thresholds are independent. For scale, definition-derived predictions using the unchanged damage/rate formula give Hydraulic Power 52 **1.25 s / 5.75 Energy for Quartz** and **3.46 s / 20.25 Energy for Alloy Core**; Crystal Core Power 82 reduces that same Alloy Core to **2.20 s / 13.50 Energy**. Fusion Power 125 needs **0.96 s / 5.76 Energy for an Energy Shard**. These examples are formula calculations, while the exports below record actual charged state intervals and complete payouts.

Quartz has faceted cyan spear clusters over dark host rock; Alloy Core has an amber diamond core, dark center and steel containment rails; Energy Shard has blue pointed deposits and a steel collar. Normal Composite uses purple armor with diagonal cyan inlays; Energy Alloy has navy armor, steel rails and blue conductors. Existing damage cracks and material-colored bounded break particles distinguish damage/destruction. All special ores reuse existing ore break/reward audio cues and the shared feedback pools. Runtime cue/count assertions and static captures do not establish physical listening or animated quality.

## Encounter pacing, movement safety and operating costs

Each new region has **14 authored patterns**, all six accepted categories and its own introduction/open fallback. Refinery's two-resource extraction lines, asymmetric Power choices and processing clusters differ from Nexus's separated resource/normal choices, longer flow pairs, compact dense trains and staggered reactor hazards. The libraries use the existing category cooldown, pressure relief, arrangement memory, resource cadence, Recovery and private seeded RNG.

The first generated encounter is calm and resource-bearing, with Quartz/Alloy around **59/67 m** and Nexus Shards around **61/71 m** in the open extraction pattern. Resource-bearing Refinery Recovery/guard patterns also include both resources, preventing a second-resource scheduling drought. These are authored positions, not claims that every prefetched target is actually visited. No hundreds-of-meters empty approach or forced tutorial sequence is added.

All sections remain **6 × 40 m**. Existing full 1.7-meter drill width, tip/rear extents, 24/26 speed ceilings, 0.35-second reaction allowance and actual exponential lateral response remain. Each pattern has an obstacle-free full-footprint bypass, every Ore has a certified initial-Power mining approach, all ordered joins have reaction/steering clearance, and solids do not overlap. The authoring Ore budget is 500 raw-rate units for these larger late materials; this is a conservative validation witness, never a charged rate or Energy grant. Runtime fixtures separately exercise actual movement, costs and opposite-side entry.

**No Energy, charge, Capacity, Power, Drive, price or evolution-cost table was retuned.** New ores use actual progressive HP damage, hardness and the unchanged form's material-rate formula. Normal gameplay ends from empty Energy; simulation workload limits fail a test instead of terminating a run. Yard remains **6 Coins per completed 5 m**, with normal payouts 25/50/75 and Copper 3. Quarry's Granite/Plating/Reinforced/Iron economy remains. Neither new region has a passive distance multiplier or new reward source.

## Evolution dependencies and persistence

| Transition | Required resources | Source available before transition |
|---|---|---|
| EVO 1 → 2 | 12 Copper | Yard from EVO 1 |
| EVO 2 → 3 | 24 Copper | Yard |
| EVO 3 → 4 | 30 Iron | Quarry from EVO 3 |
| EVO 4 → 5 | 50 Iron + 12 Copper | Quarry and return to Yard |
| EVO 5 → 6 | 40 Quartz | Refinery from EVO 5 |
| EVO 6 → 7 | 24 Alloy Core + 20 Quartz | Refinery |
| EVO 7 → 8 | 30 Energy Shard + 12 Alloy Core | Nexus and return to Refinery |

All fifteen current-form upgrades must be purchased before each evolution; no MAX requirement or stage is skipped. EVO 8 remains final. Earlier-region returns preserve ownership, balances and records.

Append-only obstacle reward enum values keep Coins/Copper/Iron serialized values intact. The **same attempt ledger** now records all five existing resources, rejects partial/duplicate/late awards, and freezes on completion. Banking adds all earned balances and that run's regional record to a candidate profile, validates and saves atomically before publishing. Failed writes/overflow cannot partially publish balances or records. Durable receipts still reject duplicate banking and foreign attempts. Advanced resources are checked against their authorized originating region at banking.

**No save schema migration or new saved field is needed.** M8 already stores Quartz/Alloy Core/Energy Shard, and M9-A already stores all four stable region IDs and records. Existing version-one envelopes, backup recovery, selected regions, balances, purchased history and receipts retain their meaning. Tests use memory or unique temp files. Temporary stage/resource shortcuts remain isolated in the existing memory store, including selection, purchases, records, banking and evolution; leaving Play restores the normal profile. No real save is rewritten to match the economy.

## Measurement method

Balance exports execute the real motor, swept contacts, HP, hardness, Energy consumption, streaming, rewards, banking and paid progression. Scripted steering has the existing 0.35-second observation allowance. No resource grant, teleport, refill, price change or temporary stage selection is permitted in full fresh journeys. Controlled state/safety fixtures explicitly reposition targets/player and force cleanup after assertions; their rewards are excluded from economics.

The new-region matrix has **144 attempts**: Refinery EVO 5/6/7/8 and Nexus EVO 7/8, each base/MAX × three seeds × Safe/Balanced/Resource/Power. Pure Safe and pure Ore-only policies remain visible controls. Progression experiments explicitly use a goal-aware Resource strategy (prioritize ingredients, then mine normal blocks for Coins). The safer progression control bypasses normal mining, then collects its required ingredients after all upgrades reach MAX. Actual policy changes are exported; neither control is represented as ordinary balanced play.

The inherited CSV field `copperOpportunities` counts approached special-resource nodes of any kind in these new-region samples, within the driver's 30-meter window. It is not Copper spawning in later regions, and does not count all prefetched targets as visited. Separate Quartz/Core/Shard columns contain the actual earned quantities.

Three separate fresh profiles start with zero Coins, zero resources and zero upgrades, pass through all four regions and use actual paid upgrade/evolution transactions. Additional EVO 5-start experiments provide prior purchased ownership solely to establish their initial stage; their current Coins/resources/upgrades are zero, and they are labeled separately from full acquisition histories. All run seconds exclude charging and Workshop time. Samples are deterministic informed policies, not measured human outcomes.

The first pilot completed all three fresh journeys in **75/77/76 attempts**, proving dependency reachability but revealing excessive late Coin surplus. Family 8100's advanced stages earned **52,210 / 101,788 / 186,788 Coins in 8/10/9 attempts**; its final wallet was 135,560. Initial new normal payouts were 1,200/2,800 Coins, ores 5/4/5 units. That result was not accepted as balanced. `Logs/M9B-Initial-FullJourneys.csv` and `M9B-Initial-Pilot.xml` retain it. A first revision used normal rewards 700/1,400 and ore yields 4/3/2. All three fresh journeys then took **90–92 attempts** (advanced stages 10/13/19), but Balanced/Power still maxed Fusion early and banked large surpluses while gathering ingredients; final wallets were **112,509–155,945**. Goal-aware Resource play was more efficient, and pure avoidance took 378–382 advanced attempts. These unfavorable/control data remain in `M9B-Revision1-*.csv` and its focused XML (**7 passed / 1 failed**).

The one focused failure was a new test expecting byte-identical profile data on scene reload, overlooking the existing legitimate issuance of a Ready attempt. Its correction asserts exactly **one** new revision/issued attempt, then compares every remaining profile field, balance, record and bank receipt exactly. No production reload behavior changed. The final authoring pass reduced only the two new normal payouts to **400/800** while retaining ore yields **4/3/2**. These reflect frequent regional multi-block patterns rather than a universal hardness-to-Coin formula; Quarry's rarer Reinforced Block remains a valuable earlier-region choice. The Balanced/Power simulation quota is explicitly one configured node per attempt; the Resource policy continues taking feasible nodes. No old-region payout, requirement, price, HP, Energy or encounter tuning was changed to compensate.

## Executed validation and measured results

Final sequential Unity execution passed **234/234 EditMode** in **42.71 s**, then **112/112 Direct3D11 PlayMode** in **1,406.07 s**, both exit **0**, **zero failures and zero skips**. This is **346 cases**, adding 17 EditMode and 8 PlayMode to the M9-A baseline. Evidence: `Logs/M9B-EditMode.xml`, `M9B-PlayMode.xml` and their corresponding Editor logs. Final runtime and Editor source bytes match the versions validated by these suites.

After the full run, one diagnostic fixture added only a **0.12-second real-time HUD settling wait before its two BLOCKED screenshots**. Its explicit-clock simulation had previously reached BLOCKED before the existing 10 Hz numeric Energy refresh, yielding a stale numeric value in a diagnostic still. Runtime HUD/gameplay code was unchanged. The focused Granite/alignment/Plating case reran **1/1 passed, zero failures/skips**, with all gameplay assertions unchanged (`M9B-FinalDiagnostic.xml` / `.log`). This is one of the 112 cases, not an extra unique case. Settled captures now show consistent numeric Energy/fill and the correct distinct messages.

The final source preserves the inherited **22-point Yard/Quarry reward HUD** (a 17-point mixed-resource line is confined to Refinery). One full PlayMode launch was deliberately stopped when source review identified that regression before its inherited readability assertion ran; its log is retained as `M9B-Interrupted-PlayMode.log`. It is not counted as a completed suite. The obsolete Inspector caption claiming later content was deferred was also removed before the final complete run. Final material polish uses restrained emission on the two new crystal/conduit materials, with no added lighting/effect systems; the five new material pitches reuse the existing sound library.

### Test coverage and regression scope

Seventeen new EditMode cases cover exact regional unlocks, five definitions/payouts, initial-Power resource witnesses, deterministic generation/joins, correct dependency sources and MAX gates, atomic mixed banking/rollback/reload and complete-byte setup idempotence. Eight new PlayMode cases cover real partial/full ores, no duplicate rewards, opposite-entry bypasses, the focused Granite/alignment/Plating check, all-region record/retry/reload behavior, the multi-policy matrix, unfunded full/advanced journeys and normal-Update presentation/performance.

The existing M9-A access cases at EVO 5/8 now expect **three/four playable regions** instead of two, and the existing journey-to-EVO-5 case now accepts selecting Refinery at its terminal stage. These are the intended availability contract changes; no prior case was removed or skipped. Historical motor/economy fixtures retain their explicit prior tuning, while new-region production tests use current authored content. The new policy branch is entered only for the two new region IDs; Yard and Quarry policy behavior is unchanged.

The focused Quarry regression confirms **centered Granite hardness 18 is destroyed at Power 20 for the unchanged 300 Coins**. Off-center Granite remains undamaged and shows **Align the drill tip or steer around**; correctly centered Metal Plating hardness 26 rejects Power 20 without losing HP and shows **Requires more power**. Both BLOCKED cases escape laterally. Purchased Power 26 destroys the same Plating and earns the unchanged **600 Coins**. The underlying M9-A contact rules/messages were already correct; no motor refactor was needed. Settled diagnostic stills are `M9B-Final-Settled-Quarry-Alignment.png` and `M9B-Final-Settled-Quarry-Hardness.png`; these staged fixtures remain excluded from economics.

### Final new-region production matrix

All **144 attempts ended naturally from empty Energy**. Each row has twelve attempts (four policies × three seeds); seconds/meters/income/drilling use the three Balanced medians, while the parenthesized duration range includes all four policies. Resource columns are **Quartz / Alloy Core / Energy Shard**. Levels 0 and 5 mean all three categories base or MAX. Income is earned ledger income, excluding the temporary wallet used only to configure these profiles.

| Region / EVO / levels | Median active s (all-policy range) | Meters | Coins | Q / A / S | Drilling s |
|---|---:|---:|---:|---:|---:|
| Refinery / 5 / 0 | 26.90 (19.13–32.37) | 225.9 | 1470 | 4 / 0 / 0 | 9.90 |
| Refinery / 5 / 5 | 34.39 (25.22–34.39) | 297.1 | 3354 | 4 / 0 / 0 | 11.69 |
| Refinery / 6 / 0 | 29.06 (20.69–32.67) | 247.2 | 1894 | 4 / 3 / 0 | 10.30 |
| Refinery / 6 / 5 | 38.01 (26.90–39.08) | 314.9 | 4972 | 4 / 3 / 0 | 13.71 |
| Refinery / 7 / 0 | 31.40 (22.22–32.85) | 267.6 | 3518 | 4 / 3 / 0 | 10.92 |
| Refinery / 7 / 5 | 36.97 (28.89–37.98) | 355.2 | 5426 | 4 / 3 / 0 | 9.59 |
| Refinery / 8 / 0 | 31.01 (24.00–31.01) | 298.6 | 4954 | 4 / 3 / 0 | 8.09 |
| Refinery / 8 / 5 | 37.13 (31.11–37.33) | 392.1 | 6268 | 4 / 3 / 0 | 6.95 |
| Nexus / 7 / 0 | 31.29 (22.22–35.20) | 265.7 | 4718 | 0 / 0 / 2 | 10.96 |
| Nexus / 7 / 5 | 38.91 (28.89–38.91) | 348.5 | 7614 | 0 / 0 / 2 | 12.11 |
| Nexus / 8 / 0 | 32.60 (24.00–32.60) | 294.4 | 6748 | 0 / 0 / 2 | 10.05 |
| Nexus / 8 / 5 | 38.44 (31.11–38.90) | 388.0 | 8462 | 0 / 0 / 2 | 8.57 |

The longest sampled attempt is **39.08 active seconds**. Later-form Yard returns, expensive normal mining and resource pursuit remain strategic choices; these are today's actual regions, not hypothetical resource grants. Full distributions by policy, state Energy, duration, distance and earnings are in `Logs/M9B-AdvancedProfiles.csv`. Pure Safe earns no normal-obstacle Coins or resources; pure Ore prioritizes its ingredient at the expense of normal Coin opportunities. These controls are included in the ranges rather than hidden.

All **144 final matrix rows match reward revision one's non-payout fields exactly**, including state seconds/Energy, distance, destruction/contact counts, resources, seed, ending and distance Coins. Only normal-obstacle and total Coins differ. This is a controlled check that the final payout correction did not change movement, mining, access or duration; paid progression journeys are rerun separately because their purchase timing can change.

### Fully unfunded final journeys

All three genuinely fresh families reached **Quantum / EVO 8**, with zero initial funding, 105 actual paid upgrades each and all seven real evolution transactions. Each reaches EVO 5 in the unchanged **48–50 attempts**; later transitions take **11–12, 19–20 and 19 additional attempts**. These broadly meet the new-region targets without timers, refills, injected resources or altered prices. Seeds identify deterministic scripted-policy families, not human trial participants.

| Family | Attempts by EVO 1 / 2 / 3 / 4 / 5 / 6 / 7 | Total to EVO 8 | Active minutes | Total earned Coins | Final Coins |
|---|---|---:|---:|---:|---:|
| 8100 | 14 / 14 / 9 / 11 / 11 / 19 / 19 | 97 | 43.03 | 263,194 | 10,434 |
| 9200 | 14 / 14 / 10 / 12 / 12 / 20 / 19 | 101 | 44.54 | 262,955 | 10,195 |
| 10300 | 14 / 14 / 9 / 12 / 11 / 20 / 19 | 99 | 44.01 | 271,959 | 19,199 |

Each family pays the unchanged **252,760 Coins** for all pre-Quantum upgrades. Earned minus paid exactly equals the final wallet; for example 263,194 − 252,760 = 10,434. Stage income and affordability are not interchangeable: family 8100 earns **29,038** during Hydraulic but enters with **4,124**, legitimately paying its unchanged **31,050** budget and carrying 2,112. Crystal Core still costs **70,300**, Fusion **108,000**. Quantum's own upgrades remain available after unlock; these remaining wallets cannot immediately MAX that entire form.

Every family earns **48 Copper, 84 Iron, 108 Quartz, 36 Alloy Core and 30 Energy Shards**, spends all exact evolution costs, and reaches EVO 8 with **0 Copper / 4 Iron / 48 Quartz / 0 Alloy Core / 0 Energy Shards**. Four Yard return trips in EVO 4 obtain the extra Copper; four Refinery return trips in EVO 7 obtain the final 12 Alloy Cores. All sources are unlocked before they are needed. Each profile has separate records for all four regions. No stage shortcut, pre-purchased history or resource grant is used in these complete journeys.

Hydraulic has purchases on **8–12 of 11–12 attempts**, Crystal Core **14–15 of 19–20**, and Fusion **13–15 of 19**. Some attempts save for expensive upgrades or pursue the remaining ingredient; neither an immediate full MAX nor a guaranteed purchase after every run is claimed. These measured totals include all ingredient return trips, not just time in the newest region. Full rows, earned/wallet separation, actual policy, drilling time/Energy and paid purchase counts: `Logs/M9B-FullJourneys.csv`.

Across these **297 completed attempts**, pooled region measurements are:

| Region ID | Attempts | Median active s | Median meters | Median Coins | Total earned Copper / Iron / Quartz / Core / Shard |
|---|---:|---:|---:|---:|---:|
| workshop_yard | 96 | 16.85 | 163.7 | 301 | 144 / 0 / 0 / 0 / 0 |
| quarry | 51 | 23.12 | 231.5 | 1,776 | 0 / 252 / 0 / 0 / 0 |
| advanced_works | 105 | 33.31 | 273.6 | 3,590 | 0 / 0 / 324 / 108 / 0 |
| high_energy | 45 | 37.16 | 326.7 | 6,796 | 0 / 0 / 0 / 0 / 90 |

These pooled medians mix upgrade levels and return visits; they are not fixed-form income predictions. Quartz/Core totals include the legitimate lower-region visits required by Fusion. Source earning and banking remain independent of the selected region's display name.

### Unfunded advanced policy controls and limits

These experiments start at EVO 5 with valid prior ownership but **zero current Coins, resources and upgrades**; they are not substituted for the full fresh journeys above. Three families per policy complete all three evolutions through the real service:

| Strategy | EVO 5 → 6 attempts | EVO 6 → 7 | EVO 7 → 8 | Total advanced attempts |
|---|---:|---:|---:|---:|
| Balanced | 12 | 20 | 19 | 51 |
| Power | 12 | 18–20 | 19 | 49–51 |
| Resource goal-aware | 14–16 | 21–24 | 20–21 | 55–61 |
| Pure bypass, resources after MAX | 68–69 | 131–133 | 182–184 | 383–385 |

Goal-aware ingredient-first play can exceed the preliminary goals, notably **21–24 Crystal Core attempts**, because it forgoes normal Coin mining until ingredients are collected. Mixed Balanced play meets the stage targets; Power-first can finish Crystal Core slightly faster. These are measured strategy tradeoffs, not forced attempt counts. **Pure avoidance is extremely slow (383–385 attempts across the advanced stages)**. Distance-only Coins remain finite but small against late prices; this control is not presented as ordinary or satisfying progression. A player refusing normal mining will grind heavily. More useful normal mining/resource route decisions remain available; passive distance inflation and approved price cuts were not used to conceal this limitation.

Pooled medians below include the three zero-current-funding families, their upgrade levels and required return visits. Resource quantities are per-run medians (**Quartz / Core / Shard**), so a zero median does not mean the source is absent; the journey totals and exact evolution requirements remain satisfied. `Resource` here is explicitly goal-aware for progression; the matrix retains pure Ore-only behavior.

| Strategy / starting EVO of attempt | Active s | Meters | Coins | Drilling s | Q / A / S |
|---|---:|---:|---:|---:|---:|
| Balanced / 5 | 32.61 | 256.8 | 2809 | 12.86 | 4 / 0 / 0 |
| Balanced / 6 | 34.16 | 276.0 | 3700 | 11.71 | 4 / 0 / 0 |
| Balanced / 7 | 36.51 | 311.0 | 6748 | 12.56 | 0 / 0 / 2 |
| Power / 5 | 31.81 | 253.1 | 2754 | 12.06 | 4 / 0 / 0 |
| Power / 6 | 34.18 | 282.9 | 3948 | 11.67 | 4 / 0 / 0 |
| Power / 7 | 36.86 | 316.7 | 6796 | 11.68 | 0 / 0 / 2 |
| Resource / 5 | 33.09 | 279.4 | 2348 | 12.73 | 0 / 0 / 0 |
| Resource / 6 | 32.97 | 293.0 | 3436 | 10.23 | 0 / 0 / 0 |
| Resource / 7 | 37.12 | 329.5 | 6396 | 11.76 | 0 / 0 / 0 |
| Safe / 5 | 22.61 | 398.6 | 474 | 0.00 | 0 / 0 / 0 |
| Safe / 6 | 24.14 | 440.8 | 528 | 0.00 | 0 / 0 / 0 |
| Safe / 7 | 26.11 | 488.3 | 582 | 0.00 | 0 / 0 / 0 |

The complete **1,628 cold-stage attempts**, paid purchases, wallet balances, ingredient earning and actual policy switches are exported in `Logs/M9B-AdvancedJourneys.csv`; `M9B-PolicySummary.json` summarizes each family. These plus the 297 fresh attempts and 144 fixed profiles form **2,069 new-region/progression economic samples**. Diagnostic reposition/cleanup fixtures and the earlier tuning candidates are not counted in that total.

### Final rendered episodes and inspection

Four **normal-Update** episodes ran with the existing camera/HUD at 540 × 960, synthetic steering and no relocation. Contact/reward captures briefly pause only the motor to hold a reached state. The five new material interactions, all three resource rewards, mixed Results, four-region selector, EVO 7 → 8 presentation and Quantum gameplay were captured. The evolution stills explicitly use a funded temporary MAX profile for presentation; the separate full-journey exports prove acquisition. Static images and cue/count assertions do not prove animated quality, human reactions or physical audio comfort.

| Live profile | Active s | Meters | Earned Coins | Quartz / Core / Shard | Peak sections / targets | Peak draws | Peak main ms | Peak GC bytes |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Refinery EVO 5 base | 24.65 | 235.5 | 1,082 | 4 / 0 / 0 | 8 / 17 | 308 | 17.70 | 199,200 |
| Refinery EVO 6 MAX | 38.01 | 315.3 | 4,378 | 4 / 3 / 0 | 8 / 19 | 284 | 22.05 | 201,880 |
| Nexus EVO 7 base | 33.75 | 259.4 | 5,906 | 0 / 0 / 2 | 8 / 21 | 302 | 20.36 | 246,844 |
| Nexus EVO 8 MAX | 38.44 | 389.2 | 8,462 | 0 / 0 / 2 | 8 / 18 | 320 | 18.38 | 206,476 |

Every episode ended naturally and banked its actual earned ledger once. Frame-driven charge/input timing can change the chosen route compared with the explicit-clock matrix; those are separate experiments, not additional identical-input statistical replicates. Data: `Logs/M9B-LiveEpisodes.csv`. The profiler includes Editor, scripted-policy LINQ and screenshot/readback overhead; **these GC/main-thread numbers are not isolated production allocation, matched GPU or mobile/FPS measurements**. No new audio/effect pool, decorative collider, real-time light, runtime mesh generation or unbounded streaming system was added. Observed bounds remain **8 sections, 21 targets, 5 AudioSources, 5 ParticleSystems**, with the existing **112-particle cap**.

Fourteen final copied frames under `Logs/M9B-Final-*.png` were inspected. Raw captures retain the inherited `M9A-M8R4-M9B-` prefix; these clear aliases identify final-source evidence:

| Evidence | Final local artifact(s) |
|---|---|
| Distinct Refinery/Nexus environments and clear hero silhouette | `M9B-Final-Refinery-Gameplay.png`, `M9B-Final-Nexus-Gameplay.png` |
| Quartz/Alloy/Shard silhouettes and confirmed reward bursts | `M9B-Final-Quartz-Mining.png`, `Core-Mining.png`, `Quartz-Reward.png`, `Core-Reward.png`, `Shard-Reward.png` (all share the `M9B-Final-` prefix) |
| New normal threshold/material encounter | `M9B-Final-Energy-Alloy.png` |
| Mixed resources and final-region Results | `M9B-Final-Mixed-Results.png`, `M9B-Final-Quantum-Results.png` |
| All four selectable rows at EVO 7 | `M9B-Final-Four-Regions.png` |
| Evolution, final Workshop and Quantum gameplay | `M9B-Final-Evolution.png`, `M9B-Final-Quantum-Workshop.png`, `M9B-Final-Quantum-Gameplay.png` |

The sampled portrait cards/counts fit without observed clipping; new resource silhouettes, damage feedback, distinct scenery and clear side routes remain recognizable. Runtime loop/feedback shutdown, source/system counts and resource receipts are asserted separately. Fine text/preview scale, actual motion, accessibility, reaction comfort and physical sound mixing remain owner review. Local Logs/captures are ignored artifacts and are not committed.

## Integration, preservation and reproduction

**Tools > DrillRunner > Setup M9-B Advanced Regions** (`DrillRunner.Editor.AdvancedRegionSetup.Setup`) has been executed. It uses Unity Editor APIs for authored assets/prefabs and extends the existing two deferred region definitions; no routine manual references or scene/prefab YAML editing. Healthy setup validates and preserves existing tuning/content. Full EditMode tests invoke both old/new setup twice and compare every asset byte and the file set.

The final byte-audited setup rerun exited **0** with **all 1,104 production Assets files unchanged**, zero missing/added files (`M9B-FinalSetupHashes.json`, `M9B-FinalSetupAudit.json`, `M9B-FinalSetupRepeat.log`). The final metadata inventory contains **567 unique GUIDs**, zero duplicates, zero missing metadata or targets. A preliminary final rerun also exited 0 with **1,106/1,106 files unchanged**. Two of those were an orphan `InitTestScene…` and its metadata from the deliberately interrupted run. The interrupted-run log confirms its exact creation, it was absent from the task-start snapshot and unreferenced by build settings. Exact copies were verified in `Logs/M9B-Interrupted-TestScene/`, then only that task-created artifact was retired through `AssetDatabase.DeleteAsset`. The temporary archive command was removed; SHA-256 verified the Editor source returned to its exact tested bytes (`M9B-FinalEditorSourceHash.txt`). No original/user asset was discarded.

The preservation audit finds **all 878 original Assets files present**. The Gameplay scene, original forms/models/previews/audio/materials/prefabs, Yard/Quarry content, operating profiles, prices, requirements, input/camera, saved profile schema and store implementation remain byte-identical to the M9-A task-start state. Exactly **16 existing Assets files** intentionally change: ten runtime resource/UI/presentation/integration files, one Inspector caption, three test files and the two formerly deferred region definitions. New content is configuration-driven, with **9 URP materials, 23 shared meshes, 39 prefabs, 37 ScriptableObjects and 4 source files**, plus their metadata and two directory metadata files. There is no new gameplay controller or generator.

Final Git state is **main**, unchanged HEAD **ed3f9516f84531294264c582bd85fb9ef0d56f06**, **27 modified / 372 untracked / 0 staged** files, including all preserved M9-A work. Assets total **1,104**: the original 878 plus 226 new files/metadata. `M9B-Preservation.json` and `M9B-Git-After.txt` record the final inventory. No commit, push, reset, Git clean, user-file discard, Android or M10 work.

`M9B-EarlierRegionComparisons.json` verifies complete byte equality for the **300-row Yard sweep**, **160-row Quarry sweep** and **147-attempt fresh-to-EVO-5 journeys** against the protected M9-A exports. `M9B-TestAudit.json` finds **all 104 previous PlayMode case names retained**. The only two renamed EditMode case identities are the existing EVO 5/8 availability parameter updates `(5,2) → (5,3)` and `(8,2) → (8,4)`; no prior test method or case coverage was removed. C#/Markdown whitespace checks pass.

Reproduction: close this project in the Editor, run setup if needed, then the complete EditMode and graphical PlayMode commands in README sequentially. Fresh/advanced journeys, material state fixtures and 540 × 960 captures are regenerated by those cases. Local evidence is under `Logs/M9B-*`; no real player file is used. Advanced profile/visual setup is explicitly temporary; production/fresh journeys do not inject funding or resources.

Remaining limits: informed scripted policies are not human skill distributions; pure bypass and ingredient-only decisions can grind much longer than mixed mining. The fixed-profile matrix has three seeds per policy/profile and is not a population guarantee. Normal Update/capture timing produces route variation. Static images do not validate animation/listening quality, and the Editor probe is not a matched GPU/mobile benchmark. Future owner-led readability, comfort and device optimization remain outside M9-B; no M10 or Android work was begun.

## Four focused owner QA checks

1. Use the existing temporary EVO 5 controls, select Crystal Refinery, compare Quartz/Composite/Alloy Core silhouettes and routes, mine ingredients, purchase upgrades and evolve normally into Crystal Core.
2. Use temporary EVO 7, compare Nexus reactor scenery, mine Energy Shards, return to Refinery for Alloy Cores, then test the normal MAX/cost gate and Quantum unlock. Inspect final-form gameplay and portrait Results/Workshop.
3. In Quarry at Power 20, compare centered Granite, off-center Granite and centered Metal Plating. Confirm alignment versus hardness messages and lateral escape; purchase Power 26 and drill Plating.
4. Switch among all unlocked regions, Retry, inspect independent records and once-only earned resources, then leave Play and confirm the real profile is restored. Judge motion, route readability and physical audio comfort manually.

Stop after M9-B for Project Owner review. No commit, push, Android or M10 work.

## M9-B-R revision — advanced drilling Energy pressure

This section supersedes the **Refinery drilling costs and economic predictions** above. The preceding M9-B checkpoint remains historical evidence. M9-B-R began with **27 modified / 372 untracked / 0 staged** files at the same `main` HEAD `ed3f9516f84531294264c582bd85fb9ef0d56f06`. All 1,104 existing Assets files were hashed before edits (`Logs/M9BR-BaselineHashes.json`). No original work is discarded. No save schema, prices, rewards, hardness, HP, forms, speeds, travel/BLOCKED rates, encounter geometry, presentation or earlier-region balance is changed.

### Diagnosis and exact configuration

The actual motor charges one state at a time. Damage is **Power × 10 HP/s**, subject to the unchanged hardness/alignment gate. Previously, active drilling used `form.DrillingBase + rawMaterialRate × 0.05`. At Hydraulic base Power 52, Capacity 440, this costs only **14.423 Energy for Composite, 5.750 for Quartz and 20.250 for Alloy Core**. Moving costs **23 Energy/s** and BLOCKED **24/s**. Drilling was considerably cheaper than traveling while also providing the obstacle reward. The owner's 35–36-second/185-meter/1,622-Coin/4-Quartz/6-Core recording is consistent with cheap advanced drilling, but its seed and input history are unavailable; it is not claimed as an exact replay.

The minimal extension is an Inspector-visible `RegionDefinition.DrillingCosts` table. The selected table is copied between attempts and its coefficient is cached on each registered/restored obstacle's durability snapshot. The motor multiplies the existing drilling rate only during actual centered, sufficient-Power drilling. No coefficient depends on purchased Power or Capacity. No per-substep material search or new gameplay controller is added; changing the coefficient never restores HP. Empty tables retain the old rate exactly. Editor-only diagnostics record actual material contact duration/Energy, completion and cumulative Energy at reward; they never control the run.

| Material in Crystal Refinery | Fixed multiplier | EVO 5 rate / full break Energy at P52 | EVO 6 rate / full break Energy at P82 |
|---|---:|---:|---:|
| Crystal Composite | 8 | 40/s / 115.385 | 42.4/s / 77.561 |
| Quartz Ore | 8 | 36.8/s / 46.000 | 39.2/s / 31.073 |
| Alloy Core | 9 | 52.65/s / 182.250 | 55.35/s / 121.500 |
| Energy Alloy | 9 | Hardness 100 blocks P52 | Hardness 100 blocks P82; at P115: 55.35/s / 105.887 |
| Shared Reinforced Block | 1 (no override) | 4.7/s / 5.604 | 5/s / 3.780 |

All raw definition rates remain **38/30/55/55/32**, with HP **1500/650/1800/2200/620** respectively. Rewards remain **400 Coins / 4 Quartz / 3 Core / 800 Coins / 1,000 Coins**. The older Reinforced Block remains an opportunistic cheap Coin target where the accepted Dense pattern includes it. Its sparse availability is preserved. **Yard, Quarry and Nexus tables are empty**; even Composite/Energy Alloy shared with Nexus retain the previous costs there. Energy Shard remains unchanged (Fusion P125: **6/s, 0.96 seconds, 5.76 Energy**).

The first measured candidate used Composite ×10 with the same resource coefficients: EVO 5 medians **2/3/4** completions, and the first fresh journey needed **26/41** attempts at EVO 5/6. Reducing only Composite to ×8 retained those medians while improving normal Coin opportunities and reducing those stages to **21/38** attempts in that family. No income/price/HP compensation was used. Both candidates and their actual journeys are retained under `Logs/M9BR-Candidate10-*` and `M9BR-Pilot*.xml`.

### Reproducible before/after gameplay

The new actual-motor matrix compares **360 attempts**: historical M9-B consumption versus current production, **five seeds (1337, 904, 2026, 8101, 9201)**, four policies, five Refinery configurations and four Nexus configurations. Historical control disables only the new table between attempts; the frozen 180-attempt pre-configuration export matches it exactly. All attempts end through actual Energy depletion and bank their earned ledger once. No reposition, injected Energy, rewards or cleanup fixture is counted as economic evidence. Values below are independent medians, not a fabricated median run.

| Balanced profile | Completed before → after (after range) | Active seconds before → after | Meters before → after | Coins before → after | Quartz / Core before → after |
|---|---:|---:|---:|---:|---|
| EVO 5 base | 5 → 2 (2–2) | 27.85 → 16.25 | 221.36 → 165.33 | 1,858 → 598 | 4/0 → 4/0 |
| EVO 5 all level 2 | 6 → 3 (3–3) | 31.95 → 16.96 | 241.17 → 147.92 | 2,288 → 974 | 4/0 → 4/0 |
| EVO 5 MAX | 7 → 4 (3–5) | 34.39 → 20.92 | 297.08 → 179.75 | 3,354 → 1,410 | 4/0 → 4/0 |
| EVO 6 base | 7 → 4 (4–4) | 30.55 → 16.73 | 241.57 → 136.88 | 2,288 → 962 | 4/3 → 4/3 |
| EVO 6 MAX | 10 → 7 (6–8) | 37.57 → 22.86 | 317.11 → 203.70 | 4,184 → 2,586 | 4/3 → 4/3 |

Current profile medians, five seeds each. Normal/resource counts refer to fully destroyed objects; quantities are **Quartz/Core**. All BLOCKED medians are zero for these informed, explicit-clock policies. Balanced/Power target one configured resource node per attempt; Resource keeps seeking affordable resource nodes; Safe avoids mining. Level 2 upgrades all three categories. No policy or production system uses a completion count to end a run.

| EVO / level | Policy | Active s | Meters | Normal / ore / total (total range) | Q / Core | Coins | Moving / drilling Energy |
|---|---|---:|---:|---|---|---:|---|
| 5 / base | Balanced | 16.25 | 165.33 | 1 / 1 / 2 (2–2) | 4 / 0 | 598 | 278.62 / 161.38 |
| 5 / base | Power | 16.25 | 165.33 | 1 / 1 / 2 (2–2) | 4 / 0 | 598 | 278.62 / 161.38 |
| 5 / base | Resource | 17.63 | 219.77 | 0 / 2 / 2 (1–3) | 8 / 0 | 258 | 348.00 / 92.00 |
| 5 / base | Safe | 19.13 | 340.38 | 0 / 0 / 0 (0–0) | 0 / 0 | 408 | 440.00 / 0 |
| 5 / level 2 | Balanced | 16.96 | 147.92 | 2 / 1 / 3 (3–3) | 4 / 0 | 974 | 250.13 / 239.87 |
| 5 / level 2 | Power | 16.96 | 147.92 | 2 / 1 / 3 (3–3) | 4 / 0 | 974 | 250.13 / 239.87 |
| 5 / level 2 | Resource | 20.00 | 255.27 | 0 / 2 / 2 (2–3) | 8 / 0 | 306 | 410.27 / 79.73 |
| 5 / level 2 | Safe | 21.30 | 376.89 | 0 / 0 / 0 (0–0) | 0 / 0 | 450 | 490.00 / 0 |
| 5 / MAX | Balanced | 20.92 | 179.75 | 3 / 1 / 4 (3–5) | 4 / 0 | 1,410 | 308.11 / 271.89 |
| 5 / MAX | Power | 20.92 | 179.75 | 3 / 1 / 4 (3–5) | 4 / 0 | 1,410 | 308.11 / 271.89 |
| 5 / MAX | Resource | 22.70 | 269.94 | 0 / 2 / 2 (2–4) | 8 / 0 | 378 | 436.21 / 143.79 |
| 5 / MAX | Safe | 25.22 | 442.87 | 0 / 0 / 0 (0–0) | 0 / 0 | 528 | 580.00 / 0 |
| 6 / base | Balanced | 16.73 | 136.88 | 2 / 2 / 4 (4–4) | 4 / 3 | 962 | 292.30 / 307.70 |
| 6 / base | Power | 16.73 | 136.88 | 2 / 2 / 4 (4–4) | 4 / 3 | 962 | 292.30 / 307.70 |
| 6 / base | Resource | 18.42 | 224.56 | 0 / 2 / 2 (2–3) | 4 / 3 | 264 | 447.43 / 152.57 |
| 6 / base | Safe | 20.69 | 382.54 | 0 / 0 / 0 (0–0) | 0 / 0 | 456 | 600.00 / 0 |
| 6 / MAX | Balanced | 22.86 | 203.70 | 5 / 2 / 7 (6–8) | 4 / 3 | 2,586 | 447.30 / 332.70 |
| 6 / MAX | Power | 22.71 | 183.35 | 5 / 2 / 7 (6–8) | 4 / 3 | 2,586 | 399.41 / 380.59 |
| 6 / MAX | Resource | 23.65 | 274.31 | 0 / 4 / 4 (4–5) | 8 / 6 | 324 | 562.42 / 217.58 |
| 6 / MAX | Safe | 26.90 | 487.05 | 0 / 0 / 0 (0–0) | 0 / 0 | 582 | 780.00 / 0 |

The five starting Balanced samples happen to take the same early affordable line; later branching gives EVO 5 MAX **3–5** and EVO 6 MAX **6–8** completions. Across all policies, even base EVO 5 can complete three objects on an ore-focused route; zero on avoidance. These are route outcomes, not caps. EVO 5 MAX Balanced duration varies **20.26–21.74 s**, Coins **1,070–2,410**; EVO 6 MAX Balanced **21.13–23.31 s**, Coins **1,846–3,210**. Full per-seed ranges, partial contacts and each state are exported in `M9BR-Profiles.csv`, `M9BR-Contacts.csv`, summarized by `M9BR-Analysis.json`.

Fixed-profile preparation uses funded memory-only QA ownership to select the requested form/upgrade levels. Reported income is exclusively the attempt's earned ledger; that starting wallet is not progression evidence. The separate fresh journeys below inject neither ownership nor funding.

Nexus retains **80/80 exact matched rows**, including duration, travel/drilling/BLOCKED Energy, completions and payouts. Balanced medians at EVO 7 base/MAX are **31.29/38.91 s**, **7/11 completions**, **4,718/7,614 Coins**; EVO 8 base/MAX are **31.93/38.44 s**, **9/12 completions**, **6,354/8,462 Coins**. All four yield a median **2 Shards**, end by Energy, and retain useful resource routes. These preserve the stronger late-game performance of the approved M9-B configuration; the Refinery complaint is not used to impose the new costs on shared Nexus materials. The original three-seed M9-B Nexus export also matches all **48** rows exactly.

### Actual contact costs, partials and upgrades

Measured full contact Energy (time in seconds), independent of travel:

| Profile | Composite | Quartz | Alloy Core | Reinforced | Energy Alloy |
|---|---|---|---|---|---|
| EVO 5 base P52 | 115.385 (2.885) | 46.000 (1.250) | No full contact in these policies | — | Hardness blocked |
| EVO 5 level 2 P60 | 100.000 (2.500) | 39.867 (1.083) | No full contact | — | Hardness blocked |
| EVO 5 MAX P75 | 80.000 (2.000) | 31.893 (0.867) | No full contact | 3.885 (0.827) | Hardness blocked |
| EVO 6 base P82 | 77.561 (1.829) | 31.073 (0.793) | 121.500 (2.195) | — | Hardness blocked |
| EVO 6 MAX P115 | 55.304 (1.304) | 22.157 (0.565) | 86.635 (1.565) | 2.696 (0.539) | 105.887 (1.913) |

Every completed contact agrees with unchanged HP divided by actual DPS, multiplied by its actual state rate. Across the fixed matrix, two interrupted Core contacts occur: EVO 5 base **1.924 s / 101.302 Energy**, and MAX **1.533 s / 80.724**. Neither is counted as a completion or pays Core. Balanced samples have no partial median; the CSV still retains every unfinished contact rather than dropping its Energy.

The resource acquisition CSV separately records **cumulative Energy consumed from launch through full destruction**, including travel and earlier mining. For example, pooled EVO 5 base Quartz completion has median **243.99 total Energy consumed**, versus **46.00** solely on its Quartz HP; EVO 6 base Core completion has median **354.47 cumulative**, versus **121.50** on that Core. These cumulative values include previous targets and are not a claim that the whole budget belongs to one ore. Moving/drilling/BLOCKED accounting and contact details permit individual route reconstruction. No travel Energy is charged a second time during drilling.

An independent same-Composite purchase experiment isolates Power and Capacity rather than changing all upgrades together. Base P52/C440 uses **2.885 s / 115.385 Energy**, reaching **186.34 m / 17.00 active s**. Purchasing Power only to P75 preserves **40 Energy/s**, reduces the break to **2.000 s / 80.000 Energy**, and reaches **206.34 m / 17.65 s**. Purchasing Capacity only to 580 retains the original break time/cost and reaches **265.48 m / 23.09 s**. This fixture uses ordinary motor movement/depletion with other contacts disabled and is excluded from progression income. In the full upgraded matrix, policies may spend the extra budget on mining rather than distance; distance need not rise monotonically with every mixed upgrade. Data: `M9BR-IndependentUpgrades.csv`.

Quartz is fully acquired at EVO 5 base in every Balanced sample, and at least one node in all five Resource samples; base EVO 6 fully acquires Core in all Balanced/Resource samples. Existing unguarded resource patterns are unchanged. The static library's original raw-rate/500-Energy safety witness remains a geometry/old-budget check; the **new actual-cost gameplay and complete unfunded journeys**, rather than that historical witness, establish revised accessibility.

### Full unfunded progression and grind tradeoff

All three full fresh profiles start at EVO 1 with zero currency, resources or ownership. Every purchase/evolution uses the production service, all three categories reach MAX, and all ingredients are earned and consumed. No temporary-stage setup is used in these full journeys.

| Seed family | Total attempts before → after | EVO 5 → 6 before → after | EVO 6 → 7 before → after | EVO 7 → 8 before → after | Total active s before → after | Earned Coins / final wallet |
|---|---:|---:|---:|---:|---:|---|
| 8100 | 97 → 126 | 11 → 21 | 19 → 38 | 19 → 19 | 2,581.86 → 2,709.29 | 259,852 / 7,092 |
| 9200 | 101 → 132 | 12 → 25 | 20 → 38 | 19 → 19 | 2,672.39 → 2,801.99 | 257,269 / 4,509 |
| 10300 | 99 → 126 | 11 → 23 | 20 → 35 | 19 → 19 | 2,640.57 → 2,682.53 | 258,873 / 6,113 |

All journeys make **105 paid purchases**, spend **252,760 Coins**, earn **48 Copper, 84 Iron, 108 Quartz, 36 Core and 30 Shards**, and pay the original requirements. EVO 1–4 attempts and metrics are exact matches. Four Yard return trips at EVO 4 and four Refinery return trips at EVO 7 remain legitimate. The selected region and independent records persist.

There are **27–31 additional launches/Results/Workshop cycles**. Total measured active gameplay increases **41.96–129.60 seconds (1.6–4.9%)** because drilling runs become shorter; this excludes human menu time, so it does not erase the extra retry overhead. EVO 6 specifically rises from **637–663** to **716–784 active seconds** and **35–38 attempts**. This is a material pacing tradeoff for the owner to assess. The stronger pressure is achieved within drilling costs; no prices, requirements or payouts are inflated to hide it. Pure avoidance remains a slow strategy. Resource-only fixed profiles intentionally earn few Coins; they are not presented as a viable optimized whole-journey policy.

The three policy families beginning at EVO 5 with valid prior ownership but zero current funds/resources remain separate controls, not substitutes for fresh proof. All **12** reach EVO 8 through actual paid upgrades and resource gates. The full suite regenerates **1,905** attempts in `M9B-AdvancedJourneys.csv`:

| Current-funding-zero strategy | EVO 5 → 6 attempts | EVO 6 → 7 | EVO 7 → 8 | Total advanced attempts (M9-B → M9-B-R) | M9-B-R active seconds |
|---|---:|---:|---:|---:|---:|
| Balanced | 26–29 | 34–36 | 19 | 51 → 79–84 | 1,784–1,916 |
| Power-first | 25–27 | 32–35 | 19 | 49–51 → 76–81 | 1,726–1,852 |
| Resource goal-aware | 26–30 | 37–40 | 20–22 | 55–61 → 85–89 | 1,999–2,092 |
| Avoidance; resources after MAX | 70 | 134 | 181–185 | 383–385 → 385–389 | 9,663–9,757 |

Power-first is faster in all three paired families. Ingredient-first play sacrifices Coin income; avoidance remains an intentionally poor progression control. No successful policy is fabricated by funding or resource injection, and no test bound or eligibility requirement is relaxed. These attempt counts reinforce the retry/menu-overhead limitation above.

### Tests, preservation and visual review

The focused pilot passed **6/6 PlayMode** cases. Complete **247/247 EditMode** passed, zero failures/skips, retaining all **234** prior case identities and adding 13 cases for explicit regional configuration, invalid coefficients/IDs, unchanged raw definitions and all-Assets setup idempotence. Full graphical PlayMode execution, final byte preservation and final capture inspection are recorded in the completion audit below.

The five new PlayMode cases cover the 360-attempt comparison, exact shared-material Nexus regression, unchanged HP and material snapshots on streaming, independent paid Power/Capacity improvement, natural Energy depletion inside an unfinished Core with zero payout, once-only bank, frozen Results, Retry/Workshop cleanup and memory-only QA isolation. Four normal-Update portrait episodes add EVO 5 base/level-2/MAX and EVO 6 base. Existing M9-B episodes retain EVO 6 MAX, Nexus, all five materials, resource payouts, save migration/recovery, route safety and bounded effects. The real player save is never opened by these test stores.

No artwork or camera/input/effects/audio behavior is authored differently. Screenshots use the real 540×960 camera/HUD, briefly pausing only the motor for a reached state. The partial-depletion fixture spends actual travel Energy before approaching an unchanged 1,800-HP Core; it is labeled diagnostic and excluded from economic comparisons. Frame-driven inputs can cause BLOCKED time and route variation, so live captures are distinct from deterministic explicit-clock medians. Static captures and runtime cue assertions do not establish physical listening quality or human game feel.

Use **Tools > DrillRunner > Apply M9-B-R Refinery Drilling Costs** (`AdvancedRegionSetup.ApplyRefineryDrillingCosts`). It has already been applied using Unity Editor APIs, needs no manual wiring, preserves custom tuning, and is safe to rerun when the measured table is present. The historical M9-B setup remains idempotent and does not overwrite this table. Save data remains Version 1; the table belongs to region configuration, not player progress. No new scene, prefab, material, currency or serialized player field is introduced.

### Completion audit — 2026-10-30

Actual Unity **6000.3.8f1** command-line runs completed successfully:

| Run | Result | Failures / skips | Measured duration | Evidence |
|---|---:|---:|---:|---|
| Complete EditMode | **247/247** | 0 / 0 | 40.72 s | `Logs/M9BR-EditMode.xml` and `.log` |
| Complete graphics-enabled PlayMode (`-force-d3d11`, no `-nographics`) | **117/117** | 0 / 0 | 1,527.07 s | `Logs/M9BR-PlayMode.xml` and `.log` |
| Final affected capture/depletion cases | **2/2** | 0 / 0 | 79.28 s | `Logs/M9BR-FinalCaptures.xml` and `.log` |

`M9BR-Audit.json` compares exact test identities: all **234 previous EditMode** and **112 previous PlayMode** cases remain, with **13/5 additions** and **zero removals**. No existing assertion or test bound was weakened. After the complete suites, two calls in the **new capture-only test file** were corrected to retain the selected steering aim during the existing reaction pause, matching the other policy drivers. Its two affected cases were rerun successfully; no production source/configuration changed after the complete suites. The earlier recordings' excess BLOCKED time came from that fixture error and is not used as balance evidence. C#/Markdown whitespace checks pass; Unity-generated scene formatting is preserved.

Final ordinary-Update, seed-1337 episodes use the existing camera, physics, input API and natural depletion. Each is one reached route, not a median or player population:

| Profile | Active s | Meters | Coins | Normal / ore / total | Quartz / Core | Moving / drilling / BLOCKED Energy |
|---|---:|---:|---:|---|---|---|
| EVO 5 base | 16.25 | 164.92 | 592 | 1 / 1 / 2 | 4 / 0 | 278.62 / 161.38 / 0 |
| EVO 5 level 2 | 16.96 | 148.07 | 974 | 2 / 1 / 3 | 4 / 0 | 250.13 / 239.87 / 0 |
| EVO 5 MAX | 20.92 | 176.58 | 2,410 | 4 / 1 / 5 | 4 / 0 | 304.22 / 275.78 / 0 |
| EVO 6 base | 16.73 | 137.03 | 962 | 2 / 2 / 4 | 4 / 3 | 292.30 / 307.70 / 0 |

These final captures agree with the progression curve without a count cap. The separate existing EVO 6 MAX normal-Update episode also completes at **21.13 s / 157.73 m / 2,586 Coins / 4 Quartz / 3 Core**. Peak observed advanced-episode streaming remains **8 sections / 20 targets**, with **5 AudioSources, 5 ParticleSystems and the 112-particle cap**. No extra effect/audio pool is added. Existing profiler exports include Editor, diagnostics, scripted policy LINQ and screenshot overhead; they do not establish release/mobile allocation or frame-rate performance. Runtime coefficient resolution occurs only at obstacle registration/reset, and the contact ledger is Editor-only.

The isolated natural-depletion case ends at **16.44 active seconds**, spending **330.073 moving + 109.927 drilling + 0 BLOCKED = 440 Energy**. Core retains **714.30/1,800 HP**, awards **zero Core**, and remains frozen during Results. Its **324 distance Coins** bank once in memory; Retry restores the ordinary Ready attempt, resource ledger, Energy and HP. Its route/placed contact is diagnostic and excluded from income/progression statistics (`M9BR-PartialDepletion.csv`).

Nine final copied **540×960** frames were visually inspected after the final fixture run:

| Required state | Local final artifact under `Logs/` |
|---|---|
| EVO 5 base drilling | `M9BR-Final-Evo5-Base-Drilling.png` |
| Fully acquired Quartz | `M9BR-Final-Quartz-Reward.png` |
| Base EVO 5 Core attempt | `M9BR-Final-Evo5-Core-Attempt.png` |
| Upgraded EVO 5 | `M9BR-Final-Evo5-MAX-Drilling.png` |
| EVO 6 base resource acquisition | `M9BR-Final-Evo6-Core-Reward.png` |
| EVO 6 MAX Core contact | `M9BR-Final-Evo6-MAX-Core.png` |
| Near-empty Energy during partial drilling | `M9BR-Final-Near-Depletion.png` |
| Zero Energy / unfinished Core / no resource bank | `M9BR-Final-Partial-Results.png` |
| Completed mixed-resource Results | `M9BR-Final-Mixed-Results.png` |

HUD Power/Capacity/resource totals, drilling/BLOCKED wording, contact progress, zero-Energy termination and Results controls remain readable without observed clipping in the inspected frames. Static images do not establish animation comfort or physical audio quality; those remain the three owner checks below.

The final explicit setup rerun exited **0** and preserved **1,110/1,110 Assets files byte-for-byte**, with no added/missing files (`M9BR-FinalSetupHashes.json`, `M9BR-FinalSetupAudit.json`, `M9BR-FinalSetupRepeat.log`). This supplements the passing EditMode tests that call the setup twice and compare every Asset. Metadata inventory: **570 unique GUIDs**, zero duplicates, missing metadata or orphan targets. Normal Unity test-scene cleanup completed; no test scene remains in Assets.

All **1,104 task-start Assets files** remain present. Exactly **eight** change: `AdvancedRegionSetup.cs`, `RegionDefinition.cs`, `DrillRules.cs`, `DrillMotor.cs`, `WorkshopController.cs`, `RunDiagnostics.cs`, `advanced_works.asset`, and the policy `Rate` helper in `RapidRunPlayModeTests.cs`. Six added files are the three focused test sources and their Unity metadata. The scene, all material definitions/HP/rewards, models, art, audio, camera/input, prices, requirements, operating profiles and persistence implementation remain byte-identical to the task-start M9-B snapshot.

The final regenerated **300-row Yard**, **160-row Quarry**, and **147-attempt early fresh-journey** exports all match protected M9-A evidence byte-for-byte. The historical 180-row control matches the frozen pre-configuration matrix exactly; **80** new Nexus comparisons and the original **48** Nexus rows remain exact. Existing tests continue exporting current production values under their inherited `M9A-*`/`M9B-*` filenames; protected historical copies for this comparison are `M9BR-Protected-*`, and actual measured pre-configuration rows are `M9BR-FrozenBefore-*`. Detailed analysis is retained in `M9BR-Analysis.json`; preservation/test identities/Git state in `M9BR-Audit.json`.

Final Git state: **main**, HEAD **ed3f9516f84531294264c582bd85fb9ef0d56f06**, **30 modified / 378 untracked / 0 staged** files, retaining all initial M9-A/M9-B work. No commit, push, reset, revert, clean, original-file deletion, Android or M10 work. Known limits remain the extra retry/menu overhead, five-seed informed-policy sample size, slow avoidance/resource-only Coin progression, and owner judgment of motion/audio/game feel.

### Three current owner QA checks

These replace the historical M9-B four-check list for this revision:

1. In a temporary EVO 5 base Refinery profile, mine Composite and Quartz, attempt Core, and judge the faster visible Energy drain and typical 2–3 completions. Compare all-level-2 and MAX, including lateral escape from BLOCKED.
2. At EVO 6 base/MAX, mine Core and Quartz, then use normal purchases/MAX evolution eligibility. Judge the additional retries, menu time and resource route decisions; compare Nexus at EVO 7/8.
3. End during partial drilling, inspect zero unfinished-ore payout, Retry/Workshop cleanup and once-only banking. Leave temporary QA and verify the real profile returns. Judge portrait readability, movement and physical audio comfort.

Stop for Project Owner review after this focused revision. No commit, push, Android or M10 work.
