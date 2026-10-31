# M10 — full-game progression, balance and replayability

Unity **6000.3.8f1**, URP, Windows PC Editor, 540×960 portrait. Work began on clean `main` at approved checkpoint **b70a4cfe861253d081204633439e3831a96e4278**. `Logs/M10-BaselineHashes.json` records all **1,110 existing Assets files** before edits. M8-R, M9-A and M9-B/M9-B-R reports were reviewed. Simulations use isolated memory stores; the owner's real profile is not loaded or altered. No commit, push, Android adaptation, new content system or M11 work.

## Findings and scope decision

**Keep the approved economy, drilling pressure and encounter configuration.** The reproduced Balanced journey is **126 / 132 / 126 attempts** to EVO 8, exactly matching M9-B-R. The 105 paid purchases are frequent enough that its longest pre-MAX stretch without a purchase is **2–3 runs**. EVO 6 is the principal Coin bottleneck, not a missing-resource softlock. Different decisions produce useful performance differences. These measurements do not prove subjective satisfaction, but do not justify undoing the recently approved Energy pressure or lowering every price.

Two small Workshop issues were reproduced and fixed in the existing `WorkshopView`:

| Existing behavior | Measured problem | Change | Measured effect / regression boundary |
|---|---|---|---|
| Two callbacks on the same upgrade card in one frame both purchase | Funded Pocket Power 0→2, spending **60+100=160 Coins** | Guard one UI purchase per card per frame, matching the existing Start Run guard | Same two callbacks now produce **0→1, spending 60**. A later-frame click still buys level 2; different cards remain independent. Direct service/controller purchases and saved prices are unchanged. |
| MAX profiles still say “MAX all upgrades and collect required resources” | Heavy Borer with Iron 50/50 and Copper 0/12 does not identify its required return to Yard; Fusion at Nexus similarly needs Refinery Core | Replace the existing final requirements line with one missing ingredient, missing quantity and its available authored source | **COLLECT 12 Copper → Workshop Yard**, or **COLLECT 12 Alloy Core → Crystal Refinery**. Counts, MAX eligibility, selected region and resource costs remain authoritative. Text uses the catalog, preserves the line count and performs no transaction. |
| Workshop refresh evaluates evolution separately for text and button | Three equivalent validation/string-building evaluations per refresh | Reuse one eligibility result | One evaluation per refresh; no additional per-frame polling or dynamic scene components. No isolated FPS claim is made. |

No values changed in any progression, price, reward, material, hardness, HP, Capacity, consumption, encounter, movement, camera or presentation asset. No save schema changed. No scene/prefab YAML was edited and no manual wiring is required. No automatic purchase, evolution or region selection was introduced.

## Baseline method and provenance

Before production edits, the actual graphical Unity baseline ran **4/4 cases**, zero failures/skips (`M10-Baseline.xml`, **476.13 s**). This included the existing three fully unfunded M9-B journeys and new full-game, return-region and UI diagnostics. The newly instrumented Balanced rows reproduce the approved **126/132/126** attempts, wallets and active time. The source snapshot and protected exports distinguish historical evidence from regenerated production exports.

Full-game matrix: seed families **8100, 9200, 10300**, five initial strategies, each starting at EVO 1 with **zero Coins, all five resources and upgrades**. It records **3,177 attempts to EVO 8**, plus **58 legitimate optional Quantum upgrade attempts** for the three Balanced profiles. Every run charges, steers, contacts real authored obstacles, damages actual HP, consumes state-specific Energy, ends naturally and banks the production ledger once. All 15 journeys make **105 paid purchases** and satisfy each evolution requirement. The Balanced continuations make all **15 additional paid Quantum purchases**: **120** purchases for all eight forms at MAX. Thus “105 mandatory” means the seven pre-Quantum evolution gates; final-form upgrades are optional, not an eighth evolution gate.

The before copies are `M10-FrozenBefore-M10-Journeys.csv`, `M10-FrozenBefore-M10-CoinRoutes.csv`, `M10-FrozenBefore-M10-UiAudit.csv` and their analysis. Previously approved exports are separately protected as `M10-Protected-*`. Current runs use `M10-Journeys.csv`, `M10-CoinRoutes.csv`, `M10-UiAudit.csv`; source rows retain family/style/attempt/seed/levels, state time/Energy, normal/ore completions, every resource, wallet, affordable categories, purchases, MAX/resource readiness, reached section IDs and streaming peaks. Counted encounters are reached by the player, not merely prefetched.

The informed policy driver retains a reaction delay and actual contact/alignment checks. Only the explicitly imperfect policy limits its knowledge to visible geometry **22 m ahead**: it has **0.55 s observation delays**, **0.65 s responses to BLOCKED**, missed resource choices, occasional poor alignment/too-hard choices, underestimated costs, random affordable upgrade ordering and a one-in-eight low-mining attempt. It does not inspect the generator RNG, future pattern or future reward. Its choices are deterministic for the attempt seed. These are disclosed scripted models, not measured human skill distributions.

An additional **cautious-mining** control separates reasonable safe play from pure avoidance. It accepts affordable ingredients, takes low-cost normals, uses a longer reaction allowance, and reserves more Energy; it limits normal lines to 25% of Capacity and resource lines to 55%. Those thresholds belong only to the test policy. The original raw `Safe` cohort deliberately avoids normal Coin mining, seeking needed ores before bypassing: it is labeled **avoidance** here and is not represented as an ordinary player.

## Full-game audit by evolution

The following are pooled **Balanced journey** medians over three families at their genuinely purchased levels and required region returns, rather than static base/MAX predictions. Independent medians need not sum to a single run's Capacity. Zero median resource income means the quota was often already met; complete ingredient earning is reported below.

| EVO | Samples | Active s | Meters | Coins | Normal / ore completions | Copper / Iron / Quartz / Core / Shard per-run median | Moving / drilling / BLOCKED Energy |
|---|---:|---:|---:|---:|---|---|---|
| 1 Pocket | 42 | 12.15 | 109.46 | 173 | 2 / 0 | 0 / 0 / 0 / 0 / 0 | 31.90 / 8.14 / 0 |
| 2 Workshop | 42 | 19.04 | 196.58 | 372 | 5 / 1 | 3 / 0 / 0 / 0 / 0 | 111.30 / 8.88 / 0 |
| 3 Industrial | 28 | 22.19 | 220.64 | 1,746 | 5 / 0 | 0 / 0 / 0 / 0 / 0 | 211.92 / 20.38 / 0 |
| 4 Heavy Borer | 35 | 22.65 | 268.68 | 1,752 | 6 / 1 | 0 / 0 / 0 / 0 / 0 | 359.28 / 17.65 / 0 |
| 5 Hydraulic | 69 | 17.61 | 165.33 | 1,034 | 2 / 0 | 0 / 0 / 0 / 0 / 0 | 278.62 / 257.00 / 0 |
| 6 Crystal Core | 111 | 20.72 | 181.33 | 1,846 | 4 / 0 | 0 / 0 / 0 / 0 / 0 | 391.69 / 286.72 / 0 |
| 7 Fusion | 57 | 35.92 | 307.77 | 6,396 | 9 / 1 | 0 / 0 / 0 / 0 / 2 | 852.24 / 83.79 / 0.93 |
| 8 Quantum, optional MAX | 58 | 35.36 | 349.91 | 7,572 | 10 / 0 | 0 / 0 / 0 / 0 / 0 | 1,205.20 / 55.88 / 0 |

| EVO | Attempts / attempts to MAX | Attempts with purchases (all families) | No affordable upgrade, before MAX | MAX waiting for resources | Accumulated active seconds per family |
|---|---|---|---:|---:|---:|
| 1 | 14 | 39/42 (92.9%) | 3 | 0 | 170.89–171.07 |
| 2 | 14 | 38/42 (90.5%) | 4 | 0 | 261.14–263.60 |
| 3 | 9–10 | 28/28 (100%) | 0 | 0 | 199.09–221.32 |
| 4 | 11–12 | 26/35 (74.3%) | 9 | 0 | 273.96–287.13 |
| 5 | 21–25 | 41/69 (59.4%) | 28 | 0 | 383.29–445.56 |
| 6 | 35–38 | 45/111 (40.5%) | 66 | 0 | 716.13–784.03 |
| 7 | 19 | 44/57 (77.2%) | 13 | 0 | 631.61–637.32 |
| 8 optional | 19–20 | 42/58 (72.4%) | 16 | Not an evolution gate | 662.91–693.40 |

“No affordable” is evaluated after banking and before purchases, excluding MAX profiles; it is not a claim that a player ignored an affordable card. MAX waiting counts profiles with all upgrades complete but missing ingredients after that attempt. Current Balanced families have **zero** such waits. EVO 6's 66 no-affordable runs are spread between 45 purchasing runs, not one uninterrupted grind; the longest whole-journey pre-MAX purchase gap is **2–3**. Costs remain **2,480 / 5,230 / 15,050 / 20,650 / 31,050 / 70,300 / 108,000 Coins** to MAX the seven gate forms, **252,760 total**. Quantum's optional three-category MAX costs **144,100** more.

## Player policy outcomes and bottlenecks

| Strategy | Attempts to EVO 8 across three families | Active seconds | Purchasing attempts | Longest pre-MAX no-purchase streak |
|---|---|---|---|---|
| Balanced | 126 / 132 / 126 | 2,682.53–2,801.99 | 84–90 | 2–3 |
| Power-first | 126 / 129 / 122 | 2,564.68–2,707.97 | 87–88 | 2–3 |
| Resource goal-aware | 135 / 136 / 135 | 2,968.93–3,055.79 | 87–91 | 5–6 |
| Imperfect casual | 158 / 170 / 171 | 3,353.42–3,643.64 | 89–95 | 4 |
| Cautious mining, supplemental | 139 / 138 / 139 | 2,996.99–3,020.57 | 85–86 | 2–4 |
| Avoid normal mining, collect needed ores | 505 / 505 / 501 | 11,840.30–11,885.27 | 101–102 | 18–19 |

All strategies satisfy the same gates; none receives free upgrades/resources. Resource goal-aware policies switch to normal mining when ingredient quotas are met. Power-first takes advantage of faster mining/hardness access. The imperfect model is slower, but does not require unmodeled knowledge or artificial rescue. Avoidance is **very slow** because distance-only Coins are deliberately insufficient for efficient later purchasing; refusal to exploit drillable targets is a strategic delay, not a circular dependency. This limitation is not concealed by raising passive rewards or lowering approved prices.

Missed ingredients do produce legitimate delays: casual families 8100 and 10300 reach MAX before Copper quotas on some early forms, waiting **1–3 further attempts**. Their missing Copper remains collectible and all journeys complete. These are decision/observation failures rather than source scarcity; the source-specific MAX guidance directly addresses this state without granting the missing ore.

The three Balanced profiles earn **48 Copper, 84 Iron, 108 Quartz, 36 Core and 30 Shards each** before Quantum. Exact costs remain **12 Copper; 24 Copper; 30 Iron; 50 Iron+12 Copper; 40 Quartz; 24 Core+20 Quartz; 30 Shard+12 Core**. All seven stages require the current three categories at MAX. Four Yard returns for Copper at EVO 4 and four Refinery returns for Core at EVO 7 remain legitimate; prior owned upgrades and surplus resources carry forward. Source unlocks are never later than the form requiring them. Existing resource/complete-only/receipt tests and actual journeys independently verify acquisition and spending.

Per-family ingredient earning by stage is **12 Copper / 24 Copper / 36 Iron / (12 Copper+48 Iron) / 40 Quartz / (52 Quartz+24 Core) / (16 Quartz+12 Core+30 Shards)**. Optional Quantum Coin mining earns no additional ore in this cohort. Thus medians of zero above do not indicate inaccessible resources. After all evolution costs, each family still owns its legitimate **4 surplus Iron and 48 Quartz**; evolution does not retroactively clear the inventory.

### Region specialization and strategic returns

The Coin-route audit runs **180 actual attempts**, five seeds per base/MAX regional configuration, EVO 3–8, in every currently unlocked region. The policy ranks visible normal choices by reward/effort, observes reaction and alignment, and avoids ore-only choices. It never teleports the actor, grants Energy or invents drops. All source costs are also exported in `M10-MaterialEffort.csv`; geometric travel and actual state charges remain separate from analytical HP/DPS mining predictions.

| Region | Preserved normal rewards | Resource / payout | Finding |
|---|---|---|---|
| Workshop Yard | Wood 25, Brick 50, Stone 75 | Copper 3 | Approved first purchases and 6 Coins/5 m remain. Late forms master these materials, but low face values prevent Yard becoming the leading Coin farm. |
| Quarry | Granite 300, Plating 600, Reinforced 1,000 | Iron 6 | A useful mid-game Coin-return region. Stronger drills earn more efficiently through real HP/hardness mastery. It cannot supply Quartz, Core or Shards. |
| Crystal Refinery | Composite 400, Energy Alloy 800; sparse shared Reinforced 1,000 | Quartz 4, Core 3 | Intended expensive drilling and ingredient specialization. Older Reinforced is unusually efficient per mining Energy, but sparse authored access and travel prevent extrapolating that ratio into unlimited farming income. |
| Energy Nexus | Energy Alloy 800, Composite 400 | Shard 2 | Leads late-game Coin income while supplying required Shards. Its original rates remain; no Refinery multiplier is applied to shared materials here. |

Examples of Coin-only medians, **Coins/run (Coins/active second)**:

| Profile | Yard | Quarry | Refinery | Nexus |
|---|---:|---:|---:|---:|
| EVO 5 MAX | 715 (27.25) | 3,672 (130.61) | 1,780 (92.21) | Locked |
| EVO 6 base | 574 (26.79) | 3,006 (130.71) | 1,404 (77.34) | Locked |
| EVO 6 MAX | 770 (27.84) | 3,702 (128.34) | 2,646 (116.32) | Locked |
| EVO 7 base | 623 (27.36) | 3,636 (151.08) | 2,482 (113.61) | 5,518 (172.78) |
| EVO 7 MAX | 825 (28.06) | 5,038 (164.70) | 3,524 (130.23) | 6,820 (182.61) |

Quarry can beat Refinery for **Coins alone**. That is a legitimate specialization decision: it gives up the ingredients that justify Refinery's Energy pressure. The data does not support equalizing income in every region or inflating new-material payouts to compensate for every cost. No route dominates all Coin/resource objectives; no earlier region produces the later ingredients, and there is no free-repeat payout, partial-ore claim or bank duplication. Per-mining-Energy ratios omit travel, reachable frequency and ingredient value and are not sufficient evidence of an exploit.

A separate disclosed **region-trader** policy mines required ingredients, then returns to the measured Coin region. Its three unfunded journeys take **112 / 117 / 110 attempts**, **2,541.83–2,691.12 active seconds**, with **105 paid purchases each**. Stages 5/6 take **14–15 / 26–28** attempts, versus Balanced **21–25 / 35–38**. Stage 7 takes **21–23** because ingredient-first collection sacrifices normal income before switching. It remains a route/upgrade-order tradeoff rather than a reward exploit. These are different-policy results, **not** an alleged before/after tuning gain. `M10-RegionTrader.csv` retains each actual region, ingredient, purchase and gate.

## Upgrade and evolution effectiveness

The new **144-profile controlled sweep** covers eight forms × three categories × six independently purchased levels. Power/Capacity use actual isolated contacts against the form's regional material, with unchanged HP and ordinary state consumption. These contact fixtures disable unrelated contacts and are excluded from economic income. Drive uses ordinary reaction-aware safe routes with real streaming. Every adjacent Power level reduces actual full break time and mining Energy; every Capacity level extends actual endurance/distance without raising rates or break cost; every Drive level increases safe-route distance by more than one meter at unchanged Capacity/rates/duration.

| Form / probe | Base → MAX Power | Actual break seconds | Actual mining Energy |
|---|---|---|---|
| Pocket / Wood | 5 → 10 | 1.400 → 0.700 | 4.340 → 2.170 |
| Workshop / Brick | 12 → 18 | 1.167 → 0.778 | 3.617 → 2.411 |
| Industrial / Granite | 20 → 30 | 1.300 → 0.867 | 4.680 → 3.120 |
| Heavy / Plating | 33 → 48 | 1.273 → 0.875 | 5.218 → 3.587 |
| Hydraulic / Composite | 52 → 75 | 2.885 → 2.000 | 115.385 → 80.000 |
| Crystal Core / Core | 82 → 115 | 2.195 → 1.565 | 121.500 → 86.635 |
| Fusion / Shard | 125 → 180 | 0.960 → 0.667 | 5.760 → 4.000 |
| Quantum / Energy Alloy | 195 → 280 | 1.128 → 0.786 | 7.503 → 5.225 |

Capacity ranges remain **25–55 / 90–140 / 190–280 / 300–420 / 440–580 / 600–780 / 800–1,040 / 1,080–1,400**. Familiar-material consumption rates do not depend on purchased Power/Capacity. Drive still improves launch carry and recovery without raising the 24/26 peak-speed envelopes; the existing full-footprint reaction, join and lateral-escape regressions remain. Data: `M10-AdjacentUpgrades.csv`, plus the retained all-form coast/route tests.

All **seven previous-MAX → next-base transitions** were additionally tested by actually breaking the **same familiar material in the same region**. Mining Energy improves **2.170→1.692, 2.411→2.310, 3.120→2.994, 3.587→3.473, 80.000→77.561, 86.635→82.296, 4.000→3.877**; actual break times also decrease. Base Capacity rises at every transition. New operating profiles deliberately rebase travel consumption, so a new base form can have a shorter duration/distance than the old MAX; this accepted rapid-run tradeoff is not hidden or “fixed” by lengthening every evolution. EVO 3/5/7 open new regions/resources, and later Power levels unlock Plating/Reinforced/Composite/Alloy/Shard thresholds. `M10-EvolutionEfficiency.csv` separates this mechanical proof from human perception of strength.

Refinery retains the approved five-seed Balanced static medians **2 / 3 / 4** completions at EVO 5 base/intermediate/MAX and **4 / 7** at EVO 6 base/MAX. Multipliers remain **8 Composite, 8 Quartz, 9 Core, 9 Energy Alloy**. Quartz remains accessible at EVO 5 and Core at EVO 6; costs remain **46 Energy** for base Hydraulic Quartz and **121.5** for base Crystal Core Core. Existing focused tests still cover natural depletion during partial Core drilling, zero unfinished-resource reward and once-only banking.

## Encounter variety and repeat play

The deterministic audit examines **32 seeds × 16 selections × four regions = 2,048 selections**. Every region reaches all **14 authored patterns** and all six categories, with **zero immediate identical patterns, rejected patterns or fallback selections**. Resource cadence stays within the existing bound. The first four selections across seeds expose **10 Yard patterns** and **all 14 Quarry/Refinery/Nexus patterns**; these are across-seed unions, not a claim that one short run sees 14 sections. Actual reached section IDs are separately retained in each progression row.

| Region | First-selection alternatives | Resource-bearing selections / 512 | Category distribution: flow / dense / risk / escape / power / recovery |
|---|---:|---:|---|
| Yard | 4 | 201 | 129 / 71 / 161 / 37 / 30 / 84 |
| Quarry | 3 | 209 | 104 / 46 / 147 / 49 / 54 / 112 |
| Refinery | 3 | 208 | 93 / 56 / 152 / 55 / 45 / 111 |
| Nexus | 3 | 208 | 93 / 56 / 152 / 55 / 45 / 111 |

The mirrored introductions and guaranteed early ingredients intentionally repeat their role. Refinery/Nexus share category pacing but differ in material thresholds, ingredient economics, lateral placements, resource composition and presentation. Matching category counts are not proof that gameplay is identical. The measured prefixes show no scarcity or repetition defect requiring a weight/geometry change, so those approved assets remain untouched. No new generator or unavoidable wall was added.

Progression is finite: after unlocking Quantum, the three Balanced profiles legitimately finish its 15 optional upgrades in **19–20** more attempts. Existing independent regional distance records and different routes remain replay goals. M10 adds no EVO 9, prestige, daily reward, quest or monetization loop. Scripted diversity and intrinsic record pursuit do not establish long-term human retention.

## Loop friction, safety and timing

Results, banking, Workshop and Retry already respond without a mandatory transition delay. The existing **0.9-second evolution presentation** and **1.1-second full charge** remain. Duplicate Start/Retry and bank receipts retain their existing guards. The new upgrade-card guard covers the previously unguarded UI event boundary; direct controller transactions remain available for multiple deliberate purchases. Different cards can purchase in one frame, and the same card works on the next frame. Unaffordable/MAX/read-only/pending-bank rules remain in the service, and no retry can discard an unbanked run.

Only MAX profiles missing resources receive the source-specific instruction. Partial-upgrade profiles retain the existing MAX/ingredient progress counts; ready evolution, unavailable evolution and final Quantum text retain the authoritative descriptions. The new line guides one ingredient rather than enlarging the panel or auto-switching regions. The existing selector remains the player's choice. Funded visual fixtures and missing-resource states are clearly temporary; their wallet/ingredients do not count as acquisition evidence.

Active time excludes Ready, charge, Workshop, Results and evolution presentation. Headless test execution and screenshots do **not** measure human menu time. As a disclosed sensitivity estimate, **3–6 seconds per Results/Workshop cycle** adds **378–792 seconds** across 126–132 Balanced attempts. Full charge adds another **138.6–145.2 seconds** if used every attempt; seven evolution presentations total **6.3 seconds**. Human decisions/animations may overlap and vary, so these are assumptions, not stopwatch measurements. The two UI changes reduce accidental actions/ambiguity; no numerical claim is made that they save a specific number of human seconds.

## Validation and performance evidence

Complete current EditMode: **264/264 passed, zero failures/skips, 47.18 s** (`M10-EditMode.xml/.log`), retaining all **247** previous cases and adding 17. Initial expanded EditMode was **261 pass / 3 new audit failures** because the new resource-source test assumed Yard used its optional `Materials` array; the real Copper definition is in its encounter prefabs. Inspecting actual authored obstacles corrected that audit, and the check now tests each missing ingredient independently with the others supplied. Production assets were never changed to satisfy that test. Initial XML/logs are retained as `M10-InitialEditMode.*`.

The focused graphics run passed **5/5**, zero failures/skips, **106.00 s** (`M10-Focused.xml/.log`): regional adjacent upgrades, all seven evolution transitions, cautious journeys, strategic region returns and Workshop behavior/readability. Complete graphical PlayMode and final file/capture audits are recorded below.

New tests augment the existing gates, route safety, complete-only rewards, snapshots, save migration/backup/recovery, durable receipt failure/retry, four records, temporary QA isolation, Energy accounting, VFX/audio shutdown and all-form presentation tests. No earlier test is removed, weakened or intentionally skipped. The only production source changed is `WorkshopView.cs`; there is no simulation, reward, save, motor or track implementation change.

The existing Editor profiler exports cover main-thread spikes, draws, GC, bounded streaming and repeated loops. New simulation CSV output appends complete rows instead of rewriting the growing file every attempt, reducing diagnostic I/O; this is test tooling, not a gameplay change. New runtime work is confined to UI callbacks/refresh; there are no additional per-frame allocations, lookups, materials, physics bodies, audio sources or particles. Profiling includes Editor, diagnostic LINQ and capture/readback overhead and cannot establish release-device FPS or mobile allocation. No Android build/profile is performed.

## Completed suite, preservation and Editor audit

The final complete graphics-enabled Unity run passed **124/124 PlayMode**, **0 failures / 0 skips**, exit **0**, **2,058.33 s** (`Logs/M10-PlayMode.xml/.log`). It used **Direct3D11**, without `-nographics`. Complete EditMode passed **264/264**, **0 failures / 0 skips**, exit **0**, **47.18 s** (`M10-EditMode.xml/.log`). Test-name comparison in `M10-Audit.json` retains **all 247 previous EditMode and 117 previous PlayMode identities**, adding **17 / 7** with **zero removals**. No production or test source changed after these complete successful suites; only documentation/audit outputs were finalized.

The seven added PlayMode cases cover all five original full-game policies and paid optional Quantum upgrades, cautious mining, 180 return-region Coin runs, actual regional adjacent upgrades, all seven evolution efficiency transitions, three unfunded region-trader journeys and the Workshop UI boundary. The separate cautious cohort adds **416 attempts** across three additional complete fresh journeys. All workload bounds are diagnostic assertions, never production termination mechanics. The earlier failed EditMode audit and successful focused run remain separate from the final unique-case counts.

Final before/after equality is **byte-for-byte**, not a comparison between different strategies:

| Regenerated export | Rows | Comparison |
|---|---:|---|
| M10 full-game policies including optional Quantum | 3,235 | Exact against the pre-production-edit M10 baseline |
| M10 regional Coin routes | 180 | Exact against the pre-production-edit M10 baseline |
| Approved M9-B-R full fresh journeys | 384 | Exact against protected checkpoint output |
| Approved Refinery/Nexus profile comparison | 360 | Exact against protected checkpoint output |
| Approved Yard production sweep | 300 | Exact against protected checkpoint output |
| Approved Quarry sweep | 160 | Exact against protected checkpoint output |
| Approved early fresh journeys | 147 | Exact against protected checkpoint output |
| Approved advanced policy controls | 1,905 | Exact against protected checkpoint output |

Thus Balanced remains **126/132/126 attempts**, **2,709.29/2,801.99/2,682.53 active seconds**, with the same earnings, purchases, wallets and ingredients. Other paired original policies likewise retain their exact rows. Region-trader/cautious results are supplemental strategy comparisons, not claimed tuning improvements. Reward quantities, saved balances and progression requirements were not retroactively rebalanced.

All **1,110 original Assets files** are present. The sole modified original Asset is **`Assets/_DrillRunner/Runtime/WorkshopView.cs`**. Six added Assets files are the three new focused test sources and their metadata. The scene, all prefabs/meshes/materials/audio, eight forms, all prices/rewards/requirements, four regional configurations, encounter weights/geometry, motor, input, camera, resource/banking/persistence implementation remain byte-identical to the task-start checkpoint. No package, competing controller or save system is introduced. Current Assets total **1,116**, metadata has **573 unique GUIDs**, with **zero duplicates, missing metadata or orphan targets**. Normal Unity test-scene cleanup completed; no test scene remains in Assets.

Integration uses the already-wired `WorkshopView`; no M10 asset generation or manual references are necessary. The complete EditMode suite retains the existing whole-Assets idempotence tests for Region/Advanced/Refinery setups. An additional final Unity Editor rerun of **`AdvancedRegionSetup.ApplyRefineryDrillingCosts`** exited **0**, reported already configured, and preserved **1,116/1,116 Assets files byte-for-byte**, with zero additions/removals (`M10-FinalSetupHashes.json`, `M10-FinalSetupAudit.json`, `M10-FinalSetupRepeat.log`). This confirms approved tuning remains protected. C#/Markdown whitespace checks pass.

Profiler observations remain bounded: the five regular normal-Update episodes sampled mean main-thread values **2.15–3.30 ms**, peaks **16.27–26.61 ms**; Quarry's complete sample peaks at **30.48 ms**. Across inspected regular/Quarry/advanced exports, peak draws are **362**, peak GC **243,109 bytes/frame**, and normal-Update streaming peaks are **8 sections / 21 targets**. These include Editor/policy/readback effects and are not a matched before/after FPS benchmark. The full M10 five-policy explicit-clock cohort peaks at **8 sections / 23 targets**. Existing assertions retain **5 AudioSources, 5 ParticleSystems, a 112-particle cap**, and stopped gameplay loops in Results/Workshop. No material instantiation or renderer/physics work was added by the UI change.

Save compatibility is maintained by leaving the existing Version-1 schema/store/migrations/receipts untouched. All prior failure, recovery, four-region records, mixed-resource banking and temporary-profile isolation tests passed. New text uses detached snapshots and the existing eligibility result; duplicate UI suppression prevents extra requests without bypassing service validation. Full progression tests bank only earned ledgers and assert once-only receipts. Structural/funded visual fixtures are not mixed into economic totals.

Final Git state is **main**, unchanged HEAD **b70a4cfe861253d081204633439e3831a96e4278**, **2 modified / 7 untracked / 0 staged** files. Modified tracked files are `WorkshopView.cs` and README; untracked files are this report and the three focused tests plus metadata. No original work was reset, reverted, cleaned or deleted. No commit/push, Android or M11 work. `M10-Audit.json` and `M10-Git-After.txt` record the final inventory.

## Final portrait inspection

Eleven final **540×960** frames generated by the complete graphical suite were copied to distinct M10 names and visually inspected. The gameplay views use the actual existing camera/HUD; parameter-selection/evolution/source-hint stills use explicitly temporary QA profiles and are excluded from income claims. Captures do not constitute recorded motion or physical listening.

| State | Local artifact in `Logs/` |
|---|---|
| Pocket with zero upgrades, Power 5 / Capacity 25 | `M10-Final-Pocket-Fresh.png` |
| Upgraded Workshop, Power 18 / Capacity 140 | `M10-Final-Workshop-Upgraded.png` |
| Industrial drilling Granite in Quarry | `M10-Final-Industrial-Quarry.png` |
| Hydraulic drilling Composite in Refinery | `M10-Final-Hydraulic-Refinery.png` |
| Crystal Core acquiring Core and Quartz | `M10-Final-Crystal-Core-Refinery.png` |
| Fusion drilling Energy Alloy in Nexus | `M10-Final-Fusion-Nexus.png` |
| Quantum in Nexus | `M10-Final-Quantum-Nexus.png` |
| Energy-empty mixed Results and Retry/Workshop controls | `M10-Final-Mixed-Results.png` |
| Heavy Borer MAX: Copper source in Yard | `M10-Final-Copper-Return-Goal.png` |
| Fusion MAX: Core source in Refinery | `M10-Final-Core-Return-Goal.png` |
| Quantum evolution presentation and base upgrade cards | `M10-Final-Quantum-Evolution.png` |

No new clipping was observed in the inspected requirements lines, resource counts or controls. Existing material silhouettes, damage progress, Energy values, different regional scenery, free-steering space and evolution/Results states remain visible. Source mapping is retained in `M10-FinalFrames.json`; inherited raw capture prefixes do not imply older production code.

## Remaining limits

The three-family strategy cohorts, five-seed static comparisons and 32-seed encounter audit are reproducible samples, not human skill/retention distributions. EVO 6 remains Coin-limited; ingredient-first play can have 5–6-run savings gaps, and deliberately avoiding normal mining remains a poor progression strategy. Refinery is less efficient for Coins alone than a Quarry return, while remaining necessary for its resources. That specialization and the extra region/menu decisions need owner judgment. Final-form progression is finite, with regional records as the existing replay objective. Static screenshots and Editor counters do not prove motion comfort, physical sound mixing or release/mobile performance. No subjective satisfaction claim replaces the four checks below.

## Current owner checks

1. Play fresh Pocket through upgraded Workshop. Try rapid repeated clicks on one upgrade card and deliberate purchases on different cards; verify prices, wallet and immediate Start/Retry behavior.
2. In Quarry, compare Industrial/Heavy drilling and a higher-form Coin return. Check actual rewards, Iron/Copper requirements and the new MAX-profile Yard instruction.
3. Compare Hydraulic base/intermediate/MAX and Crystal Core in Refinery. Judge the retained Energy pressure, visible upgrade improvements and whether 2–3-run savings gaps feel rewarding.
4. At Fusion/Quantum, mine Shards, return for Core, inspect evolution/final upgrades and records, then leave temporary QA to verify real-profile restoration. Judge portrait readability, movement, effects and physical audio comfort.

Stop after M10 for Project Owner review. No commit, push or M11 work.
