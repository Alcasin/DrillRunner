# M8-R — rapid run loop validation

Unity **6000.3.8f1**, URP, Windows PC Editor, portrait **9:16**. Work began on `main` at `b9cdf9b536dcee30efe7dfa14752bee6f2f4b772`, with **13 modified, 185 untracked, 0 staged** M8 files. That work was retained; `Logs/M8R-BaselineHashes.json` records the 704-file pre-revision Assets snapshot. No player save was read/reset for validation. No commit, push, Android build, new region/resource/material/form, or M9 implementation is included.

## Diagnosis and instrumented control

The reported approximately 827-meter Workshop attempt is consistent with the existing system, but its exact inputs/upgrade levels were not supplied. Previously every form used **0.7 Energy/second moving**, **0.5 BLOCKED**, and the material's unchanged drilling rate (Wood 8, Brick 12, Stone 18, Copper 10). Capacities rise from Pocket 100–180 to Workshop 200–300 and eventually Quantum 1,800–2,300. Stronger Power shortens mining, leaving more Energy for extremely cheap travel. BLOCKED barely consumed Energy. The same track can therefore produce very different long distances depending on steering/mining choices.

Before applying new tuning, an Editor-only motor accountant and actual simulation driver measured **48 baseline attempts**: base/MAX Pocket and Workshop × four policies × seeds 1337/904/2026. The first measurement was preserved as `M8R-FrozenBefore.csv`. The final equivalent-policy control is regenerated in `M8R-OriginalBaseline.csv` using the original rates, charge 1.5 s, exponential boost decay 1.8, distance rewards 1/5 m, original destruction payouts, and original M8 encounter scheduling. These are actual motor/HP/contact/Energy/ledger runs, not capacity-only calculations. The isolated historical configuration disables only the new balance asset and clones the original scheduling fields; it never changes the real save.

The accountant records exact intervals and costs charged by the motor in Moving/Drilling/BLOCKED. Seconds exclude Ready and Charging. First contact, contacts/destructions, open-section travel, distance, rewards, and approached Copper are recorded. Copper opportunities count only when within a 30-meter approach window, not merely prefetched 200 meters ahead. `openSeconds` means moving inside an authored empty section, not all unblocked movement. Total recorded consumption is checked against capacity on every naturally completed sample.

Avoiding obstacles previously allowed **142.86/257.14 s** at base/MAX Pocket and **285.71/428.57 s** at base/MAX Workshop. Balanced/resource policies use substantial drilling Energy and end sooner, but still commonly take minute-scale attempts. The old rates, larger reserves, and improved mining efficiency explain the problem together; encounter prefetch and a hard run limit were not the cause.

The following actual state-time medians make the baseline diagnosis reproducible. Original cohorts contain three paired seeds; rapid cohorts contain five. Open time is a subset of moving time inside an authored empty section; medians are calculated separately, so columns need not sum exactly.

| Balanced profile | Moving old → rapid s | Drilling old → rapid s | BLOCKED old → rapid s | Open-section old → rapid s |
| --- | ---: | ---: | ---: | ---: |
| Pocket base | 53.14 → 12.87 | 7.40 → 3.20 | 0 → 0 | 4.00 → 0 |
| Pocket MAX | 76.27 → 24.02 | 14.90 → 5.10 | 0.017 → 0 | 9.90 → 3.22 |
| Workshop base | 65.48 → 18.13 | 13.50 → 3.67 | 0.025 → 0 | 6.60 → 0 |
| Workshop MAX | 119.76 → 28.33 | 19.83 → 4.39 | 0.133 → 0 | 12.30 → 3.05 |

## Production operating profiles

One new authored **`RapidRunBalance.asset`** extends the existing Workshop/motor/run ledger. It has eight stable-ID operating profiles, existing reward-source tuning, charge duration, and explicit launch-coast tables. There is one motor and progression service. No extra run controller or save system was added.

| Form | Preserved Energy base–MAX | Moving / s | BLOCKED / s | Drilling base / s | Moving-only base–MAX seconds |
| --- | ---: | ---: | ---: | ---: | ---: |
| Pocket | 100–180 | 6 | 7 | 6.2 | 16.67–30.00 |
| Workshop | 200–300 | 9 | 10 | 9.2 | 22.22–33.33 |
| Industrial | 320–460 | 14 | 15 | 14.2 | 22.86–32.86 |
| Heavy Borer | 480–650 | 20 | 21 | 20.2 | 24.00–32.50 |
| Hydraulic | 680–900 | 27 | 28 | 27.2 | 25.19–33.33 |
| Crystal Core | 950–1,250 | 32 | 33 | 32.2 | 29.69–39.06 |
| Fusion | 1,320–1,700 | 43 | 44 | 43.2 | 30.70–39.53 |
| Quantum | 1,800–2,300 | 57 | 58 | 57.2 | 31.58–40.35 |

**Drilling rate = form drilling base + 0.1 × original material rate.** Only that state's rate is charged; movement is not stacked on drilling. For Pocket, Wood/Brick/Stone/Ore consume **7/7.4/8/7.2 per second**. This raises the minimum travel cost while reducing punishing low-Power mining totals, rather than multiplying every drain by one factor. A complete obstacle still costs its actual HP / (Power × 10) mining time multiplied by this rate. Hardness, HP, damage, footprint, and material definitions are unchanged.

Profiles are fixed for a form, independent of its Energy upgrade level. Capacity therefore increases endurance and route freedom directly. Rates are captured between attempts; changing authoring/configuration during a run cannot rubber-band its active costs. The three states have positive costs, so even fully open travel ends naturally. There is no elapsed-time, distance, obstacle-count, or forced-death ending. Diagnostic workload limits fail tests rather than end gameplay.

The 0.35-second reaction allowance plus approximately 0.26-second steering response remains. A 0.61-second BLOCKED interval costs 4.27 Energy at starting Pocket, leaving ample escape budget. Actual BLOCKED escape and no damage below Stone hardness 9 are tested. Material-specific ordering remains, and stronger Power reduces both mining time and total Energy on familiar materials within each form.

## Upgrade effectiveness and launch

All M8 **Power, Capacity, cruise/peak speed, minimum/maximum boost, recovery, and purchase-price tables** remain. Pocket peak stays 24; other forms peak stays 26. Charge is a predictable monotonic **1.1 seconds**, reduced from 1.5. Existing exponential boost decay is tuned by Drive level to create measurable launch-distance gains even on late forms whose cruise speed is already at its cap. There is no dash, lane, new movement mode, or speed-cap increase.

Explicit decay coefficients (lower means longer carry):

| Form | Drive levels 0, 1, 2, 3, 4, 5 |
| --- | --- |
| Pocket | 1.8, 1.2, 0.9, 0.7, 0.56, 0.45 |
| Workshop | 0.44, 0.4, 0.36, 0.32, 0.29, 0.26 |
| Industrial | 0.25, 0.23, 0.21, 0.195, 0.18, 0.165 |
| Heavy Borer | 0.16, 0.155, 0.15, 0.145, 0.14, 0.135 |
| Hydraulic | 0.13, 0.125, 0.12, 0.115, 0.11, 0.105 |
| Crystal Core | 0.1, 0.097, 0.094, 0.091, 0.088, 0.085 |
| Fusion | 0.084, 0.081, 0.078, 0.075, 0.072, 0.069 |
| Quantum | 0.068, 0.065, 0.062, 0.059, 0.056, 0.053 |

Each next-form base preserves the previous form's MAX coast. All source attribute/history checks remain. Per-form operating costs do rebase on evolution to keep attempts compact: a previous MAX form can outlast the next form at level zero, even though Power/Capacity/Drive attributes and launch carry never regress. This pacing tradeoff is explicit; no Capacity purchase increases its consumption rate.

`M8R-Upgrades.csv` measures **144 attempts: eight forms × three isolated categories × six levels**. Energy upgrades retain identical rates and increase actual duration/distance; Power reduces time/cost for the same Wood and unlocks unchanged hardness thresholds; Drive increases actual safe-route distance with unchanged duration/rates. Constant-speed transition tests remain conservative because all new coast trajectories stay below the same 24/26 ceilings. The initial small coast increment failed its >1-meter adjacent-distance criterion; the curve was corrected and the assertion retained.

## Short-run encounters and risk/reward

The **40-meter introduction, six categories, fourteen prefabs, 40-meter section dimensions, full collision footprint, and 200-ahead/80-behind streaming window remain**. No models, environment meshes, decorative colliders, or encounter geometry were rebuilt.

Only scheduling changes: the first Copper opportunity is due by the **second encounter selection** (targets around 62 or 102 m); first selection remains calm. Guarded choices can become eligible from selection ordinal 2, sections starting 120 m, with a small early weight. Dense/guarded pressure still requires relief, category/family repetition controls remain, and later Copper cadence stays within three selections. This preserves seeded variety and gives short attempts access to resources before their natural ending. A prefetched selection is not claimed as a visited encounter.

The original simple Wood at Z=35 is met at roughly 2.6 seconds after a full base Pocket launch; the first multi-target choice is around 5–6 seconds without that stop. Charge, route choice, and mining change those times. This is close to the 3–6-second interaction guideline without lengthening the introduction or manufacturing a rigid tutorial sequence.

| Power-5 approach | Drilling Energy | Normal Coins | Copper |
| --- | ---: | ---: | ---: |
| Collision-free bypass | 0 | 0 | 0 |
| Safe Wood | 9.80 | 25 | 0 |
| Unguarded Ore | 12.96 | 0 | 3 |
| Wood, then maneuver to Ore | 22.76 | 25 | 3 |
| Aligned Brick then Ore | 33.68 | 50 | 3 |

The aligned guard previously cost **51.6** and its independent Wood option **11.2**. A moderate route uses the existing Wood alternative, waits for the full rear footprint to clear the still-solid Brick row, allows the reaction interval, then steers to Ore. Actual motor tests exercise Wood, moderate, and full-guard routes at **all eight base forms (24 cases)**, checking complete-only Copper, exact consumed drilling Energy, no BLOCKED, and full-footprint clearance. No invisible rule forces a player to pay the Brick cost. Unguarded/Recovery Ore remain, and a player with depleted Energy must still judge affordability. Early Capacity/Power purchases make later guarded opportunities more practical; a bare starter is not promised every guarded Ore after previous spending.

## Rewards, progression, and fast flow

Production rewards are **9 Coins per completed 5 m** and **25 × the original normal-obstacle Coin amount**: Wood 25, Brick 50, Stone 75. Ore still pays **3 Copper only on complete destruction**, with no normal Coins. The existing ledger snapshots tuning, rejects partial/duplicate claims, freezes completed results, and checks overflow. No reward source was added. Upgrade prices and the 12/24-Copper evolution requirements were not changed.

These rates were tested against the unchanged **2,480-Coin Pocket** and **5,230-Coin Workshop** budgets, rather than chosen to max a tier immediately. Early attempts give affordable choices; later upgrades make destruction faster and more profitable relative to missed travel. Three genuinely unfunded seed families (8100, 9200, 10300) buy upgrades, mine Copper, finish/bank runs, and evolve through the real progression service. No starting funding, changed obstacles, relocated player, free refill, price change, or advanced resource grant is used in these journeys.

Results appear on the normal completion frame, rewards bank once, and the existing top/reset control becomes **RETRY** at completion. Workshop and Retry stay immediately accessible in the same scene. UI-start actions reject duplicate callbacks within the same frame; a completed ledger cannot be banked twice or reattached to a new attempt. A brief **0.15-second** existing housing response gives mechanical shutdown feedback without delaying Results, banking, or navigation. Loops/contact effects stop through the existing presentation logic.

The existing Workshop keeps its three cards, MAX states, resources, requirements, and evolution flow. Drive now shows launch coast alongside existing stats. Numeric Energy text refreshes at a bounded 10 Hz during rapid runs (Ready, changed attributes, and zero refresh immediately); the fill bar remains continuous. This avoids near-per-frame text rebuilds at high-form drains. Temporary M8 stage shortcuts remain in memory, including purchases, evolution, and banking. Leaving Play Mode restores normal progression. Real save schema, checksums, backup/recovery, balances, purchased history, and durable receipts are unchanged.

## Reproducible simulation and limits

Production matrix: **15 representative configurations × four policies × five seeds = 300 attempts**, including Pocket/Workshop base, intermediate, MAX; Industrial base/MAX; intermediate Heavy/Hydraulic/Fusion; Crystal base/MAX; Quantum base/MAX. Uniform profile levels and all three isolated upgrade categories are separately reported. All matrix/journey attempts start ordinarily and use the real motor, swept contacts, HP, Energy, streamer, ledger, and banking. Statistics use actual simulated seconds, not distance as a time surrogate.

- **Safe:** favors collision-free routes and deliberately skips resources.
- **Balanced:** takes an early affordable normal target, preserves enough opportunity to seek one Ore, then takes affordable destruction choices.
- **Resource:** prioritizes feasible Ore, including guards, and avoids unrelated normal mining.
- **Power:** prefers harder affordable drillable targets, with optional Copper. Below the Stone threshold it avoids Stone.

The driver uses deterministic scripted decisions and synthetic pointer targets, a 0.35-second observation allowance, current-section geometry, and a 30–32 m Ore horizon. It is an informed policy, not measured human behavior. Macro samples and adjacent-upgrade runs use the motor's explicit clock; five additional rendered episodes use its normal **Update** clock without teleportation. Staged state/route fixtures reposition targets/player and sometimes force cleanup; their displayed distance/rewards are excluded from income analysis.

Only Workshop Yard and Copper sources exist. Later forms are temporary QA profiles; no Iron/Quartz/Alloy/Shards are earned or fake regions introduced. Higher Power makes old materials easy. Advanced Coin/resource balance remains provisional: the archived M8 minute-scale income estimates no longer describe these rapid attempts, and unchanged advanced prices cannot be claimed to meet future 18–25-run goals in today's Yard. M9 must validate its actual regions, earnings, resource supply, and session durations.

## Executed results and evidence

Complete final operating-profile runs passed **170 EditMode / 89 graphical PlayMode**, **0 failures, 0 skips**: **259 unique cases**, including all 238 earlier cases. M8-R adds thirteen EditMode and eight PlayMode cases. Test-name comparison and final preservation checks are recorded with the audit below. A final focused live-visual rerun checks the coast readout against the common starter reference, so evolution does not misleadingly display a reset in purchased carry.

That focused rerun also passed **1/1**, zero failures/skips, after the final UI-only coast-label change. It is one of the 89 PlayMode cases, not an additional unique case. Evidence: `Logs/M8R-EditMode.xml`, `M8R-PlayMode.xml`, and `M8R-FinalVisual.xml` with their corresponding Editor logs.

The ten archived PlayMode fixtures explicitly use `HistoricalRunFixture.LoadScene/UnloadScene` and the original operating/reward settings. They retain their existing assertions, including precise old costs, long travel, and progression budgets. A source comparison against their pre-revision contents confirmed they are identical after normalizing only those load/unload calls. No old case/assertion was removed, weakened, or silently skipped. New production tests independently cover the actual rapid tuning, state accounting, snapshot isolation, all-form routes, direct attribute/HP behavior, four policies, upgrades, complete earnings/progression, prompt UI, duplicate input, banking, sandbox isolation, and live presentation. Pure data/safety/save tests continue against the current authored assets.

### Equivalent-policy old/new comparison

Paired seed set **1337, 904, 2026**, same policy and starting state. Values are medians; this table is separate from the larger four-policy distribution below.

| Profile/policy | Original seconds | Rapid seconds | Original meters | Rapid meters |
| --- | ---: | ---: | ---: | ---: |
| Pocket base, Balanced | 60.54 | 16.07 | 516.9 | 126.9 |
| Pocket MAX, Balanced | 91.89 | 29.12 | 858.5 | 278.8 |
| Workshop base, Balanced | 79.56 | 21.80 | 745.6 | 216.2 |
| Workshop MAX, Balanced | 139.71 | 32.79 | 1,462.3 | 357.6 |
| Pocket base, Safe | 142.86 | 16.67 | 1,435.3 | 173.3 |
| Pocket MAX, Safe | 257.14 | 30.00 | 3,093.0 | 386.7 |
| Workshop base, Safe | 285.71 | 22.22 | 3,491.3 | 299.0 |
| Workshop MAX, Safe | 428.57 | 33.33 | 5,580.3 | 483.3 |

For the paired Balanced cohorts, mean moving/drilling Energy changed from **33.5/66.5 → 74.0/26.0** at base Pocket, and **45.8/154.2 → 158.9/40.9** at base Workshop. BLOCKED was a very small contribution for these informed policies. This shows the redesign shifts the budget toward travel rather than relying on punishing mining to end an attempt. It does not claim to reproduce the exact unrecorded 827-meter manual route.

### Production duration and reward distribution

Each row contains **20 attempts** (four policies × five seeds). All **300 ended naturally from empty Energy**. Median distance/Coins/Copper are per completed attempt; the Safe cohort deliberately earns no Copper, and an aggressive weak Power policy can also miss it. Five-seed tails are sample observations, not population guarantees.

| Form / levels | Seconds min / median / max | Median meters | Median Coins | Median Copper |
| --- | ---: | ---: | ---: | ---: |
| Pocket base | 15.84 / 16.19 / 16.67 | 137.2 | 261 | 3 |
| Pocket intermediate | 20.26 / 21.03 / 21.67 | 189.0 | 390 | 3 |
| Pocket MAX | 28.45 / 29.13 / 30.00 | 293.3 | 654.5 | 3 |
| Workshop base | 21.54 / 21.84 / 22.22 | 236.3 | 528.5 | 3 |
| Workshop intermediate | 25.88 / 26.25 / 26.67 | 290.1 | 654.5 | 3 |
| Workshop MAX | 32.21 / 32.86 / 33.33 | 383.1 | 873 | 3 |
| Industrial base | 22.50 / 22.69 / 22.86 | 277.8 | 621 | 3 |
| Industrial MAX | 32.38 / 32.67 / 32.86 | 406.0 | 918 | 3 |
| Heavy Borer intermediate | 26.81 / 26.93 / 27.00 | 344.9 | 783 | 3 |
| Hydraulic intermediate | 28.04 / 28.11 / 28.15 | 366.9 | 846 | 3 |
| Crystal base | 29.61 / 29.66 / 29.69 | 390.2 | 909 | 3 |
| Crystal MAX | 38.97 / 39.03 / 39.06 | 509.8 | 1,218 | 3 |
| Fusion intermediate | 33.91 / 33.94 / 33.95 | 447.1 | 1,085 | 3 |
| Quantum base | 31.56 / 31.57 / 31.58 | 420.4 | 1,035 | 3 |
| Quantum MAX | 40.33 / 40.34 / 40.35 | 532.9 | 1,346.5 | 3 |

Workshop MAX's approximately 33-second high-upgrade tail is slightly beyond the 15–30-second early-form guideline. Rates remain fixed so Capacity retains its full benefit; this is an intentional tradeoff rather than level-dependent drain. Every tested ordinary movement/drilling policy remains well below a minute. Advanced-stage samples concern today's Yard materials and temporary profiles, not M9 economy claims.

Policy distinctions are visible even in a bare Pocket cohort (five seeds each):

| Policy | Median seconds | Median meters | Median Coins | Median Copper | Approached Ore opportunities |
| --- | ---: | ---: | ---: | ---: | ---: |
| Safe | 16.67 | 173.33 | 306 | 0 | 1 |
| Balanced | 16.07 | 126.89 | 250 | 3 | 1 |
| Resource | 16.31 | 147.55 | 261 | 3 | 1 |
| Power | 15.97 | 111.65 | 273 | 0 | 1 |

All end from empty Energy. Travel-only play can earn more Coins at base Power but cannot evolve without Copper. At higher Power, normal mining becomes faster and cheaper. The policy driver deliberately makes different tradeoffs; it does not guarantee Copper or income for arbitrary human input.

Across the 144 isolated-category runs, each adjacent Capacity purchase added **1.48–3.33 seconds** and positive distance at identical rates. Each adjacent Drive purchase added **2.34–11.89 meters** on the controlled safe route at identical duration/rates, within the existing speed envelope. Power reduced the same Wood's predicted Energy by **0.112–1.633 units per adjacent purchase** and reduced its HP-clear time; actual exclusive mining consumption and complete-only payouts are separately verified by the state and route tests. Power does not make a completely obstacle-free policy faster.

### Actual unfunded early progression

| Seed family | Runs to Workshop | Additional runs to Industrial | Attempts with a purchase | Total earned Coins | Copper |
| --- | ---: | ---: | ---: | ---: | ---: |
| 8100 | 6 | 8 | 13 of 14 | 9,030 | 36 |
| 9200 | 6 | 8 | 13 of 14 | 8,958 | 36 |
| 10300 | 6 | 8 | 13 of 14 | 9,059 | 36 |

These journeys preserve all MAX requirements and spend the unchanged 12 + 24 Copper and 2,480 + 5,230 upgrade Coins. Frequent purchases coexist with a final saving/resource attempt; an entire form is not maxed after one run. Efficient resource routes can collect more Copper per attempt than the one-Ore Balanced strategy; skipping every resource never satisfies evolution. Advanced balances stay zero throughout production journeys, and Industrial's Iron requirement correctly prevents a normal EVO 4 in M8-R.

### Visual/performance evidence

The real-time episode test enables the authoritative motor's normal Update, keeps the existing camera/HUD at **540 × 960**, uses synthetic steering without relocation, and renders Pocket base/intermediate, Workshop base/MAX, and Crystal intermediate. It also renders Energy-empty Results, a real Workshop Capacity purchase, and the immediate next attempt in the same scene. Quantum base/MAX Workshop cards check the longest coast/readout layouts. Later-form images are visibly isolated QA, not advanced-resource acquisition claims. No video or physical listening session is claimed.

| Inspected evidence | Files under `Logs/` |
| --- | --- |
| Base/upgraded Pocket | `M8R-Evo1-L{0,2}-{Charging,Moving,Drilling,Copper,Results,Workshop}.png` |
| Base/MAX Workshop | `M8R-Evo2-L{0,5}-{Charging,Moving,Drilling,Copper,Results,Workshop}.png` |
| Later-form runtime | `M8R-Evo6-L2-{Charging,Moving,Copper,Results,Workshop}.png` |
| Same-scene next attempt | `M8R-Evo1-L0-ImmediateNextAttempt.png` |
| Long final-form layouts | `M8R-Evo8-L{0,5}-Workshop.png` |

The motor's live clock is measured separately in `M8R-NaturalEpisodes.csv`; sample durations agree with explicit-clock distributions while charge strength and capture/input timing cause small differences in distance/rewards. Banked results are frozen and immediately readable. The HUD, bit/model silhouettes, existing feedback, clear gaps, and camera motion remain; no arrows, new debug HUD, or rebuilt art is used.

The live fixed-resolution probe samples sixty driver frames per episode and records main-thread/draw/GC counters in `M8R-Profiler.csv`. It includes scripted-policy LINQ allocations, Editor/test work and screenshot readbacks, so it is **not an isolated runtime-allocation or matched GPU benchmark**. Section/target peaks remain bounded; source/audio/particle caps remain five sources, five emitters and 112 particles. M8-R adds no meshes, materials, colliders, audio clips, runtime mesh creation, physics fragments, pooling framework, or per-frame production pathfinder. Editor diagnostics record numbers without allocating per motor substep; numeric Energy text rebuilds are bounded to 10 Hz.

Latest focused-run counters (mean/peak main-thread milliseconds; draw calls and GC are observed frame peaks):

| Episode | Mean / peak main ms | Draw peak | GC bytes peak | Sections / targets peak |
| --- | ---: | ---: | ---: | ---: |
| Pocket base | 6.59 / 49.60 | 319 | 196,401 | 8 / 16 |
| Pocket intermediate | 6.36 / 42.54 | 344 | 169,213 | 8 / 17 |
| Workshop base | 6.03 / 36.93 | 352 | 169,794 | 8 / 19 |
| Workshop MAX | 20.95 / 123.38 | 383 | 171,910 | 8 / 19 |
| Crystal intermediate | 10.05 / 53.31 | 397 | 137,644 | 8 / 19 |

These measurements include a **123.38 ms Editor/test spike** and do not establish a production frame budget or zero-allocation claim. No matched before/after GPU measurement was taken. The preservation audit and live counts verify that this revision did not add rendering/physics content or unbounded streaming/effects; owner runtime profiling can exclude the test driver and capture overhead.

Final live motor seconds were **15.840, 20.743, 21.544, 32.749, 33.067** for the five episodes above, with Coins/Copper **230/3, 379/3, 524/3, 896/3, 1,154/3** and successful once-only banking in every episode. Ten final copied `M8R-Review-*.png` frames were inspected: charge, upgraded travel, early drilling, MAX Workshop, Crystal Copper feedback, empty Results, purchased-Capacity next attempt, and both Quantum Workshop layouts. Text/cards fit the portrait viewport; no new clipping was observed in these samples.

## Final integration and preservation audit

**Tools > DrillRunner > Setup M8-R Rapid Runs** was executed through `DrillRunner.Editor.RapidRunSetup.Setup`; the final batch rerun exited **0** and reported already configured. SHA-256 comparison of **all 722 Assets files** before/after found **zero changed, added, or missing files** (`Logs/M8R-SetupHashes.json`, `M8R-SetupAudit.json`, `M8R-Setup-Repeat.log`). Integration, serialized references, tuning asset creation and scene saving use Unity Editor APIs; no routine manual wiring or direct YAML edits are required. Tests also cover repeated setup, custom-scene selection and dirty-scene refusal.

The original **704 Assets files are all present**. Exactly **25** differ from the pre-revision snapshot: eleven runtime/Inspector files, the Gameplay scene, three encounter scheduling assets, and ten archived PlayMode fixtures. All original models, prefabs, materials, textures, audio, VFX authoring, stat/price progression assets, save/progression implementation and metadata remain byte-identical to the uncommitted M8 baseline. Nine new source/config/test files plus nine metadata files bring Assets to **722 files / 374 GUIDs**, with **zero duplicate GUIDs or missing metadata**.

Comparing full case names with `M8-EditMode.xml` and `M8-PlayMode.xml` found **zero missing original cases** (157/81 retained). `git diff --check` is clean for C# and Markdown. Final Git state is **main**, unchanged HEAD `b9cdf9b536dcee30efe7dfa14752bee6f2f4b772`, **33 modified / 204 untracked / 0 staged** files, including the preserved M8 work. No commit or push was performed.

## Four focused manual checks

1. **First two forms:** Play fresh Pocket and Workshop attempts, buy affordable upgrades, then launch again. Judge the short-loop pace, progress per purchase, and charge control.
2. **Routes:** Compare unguarded Ore, Wood, the Wood-to-Ore maneuver, Brick/Ore, and bypasses. Check Energy, complete-only Copper, and how early Capacity/Power purchases change the decision.
3. **Retry/banking:** At Energy empty, use Retry and Workshop rapidly. Confirm prompt Results, one bank, one new attempt, accurate earnings, and tidy audio/effects.
4. **Eight-form QA:** Use the existing temporary stage controls for base/MAX runs and late Drive. Check model/bit visibility, coast progress, safe steering, and return to the unchanged real profile after Play Mode.

Remaining owner review covers human reaction/route comprehension, physical audio comfort, less informed play styles, and provisional future-region balance. PC Editor probes are not device/GPU/mobile FPS guarantees. Stop after M8-R for Project Owner review; no M9 work, commit, or push.


## M8-R2 - Ultra-short early run pacing (29 October 2026)

This section supersedes M8-R's 15-30-second early-form target. The earlier sections remain historical evidence. The owner wants a first attempt that ends naturally after about 7-8 active seconds, then an immediate affordable purchase and retry.

### Reproduced baseline and final measurements

Before changing production, the existing actual-motor distribution test was rerun: **300 samples, one executed PlayMode case, passed**. `Logs/M8R2-Before.csv` preserves that reproduction and `M8R2-Before.xml` records execution. It reproduced the reported balanced medians. Active seconds exclude Ready and Charging.

| Balanced cohort (five seeds) | M8-R seconds | M8-R2 seconds | Final meters | Final Coins / Copper |
|---|---:|---:|---:|---:|
| Pocket base | 16.07 | 7.57 | 64.1 | 169 / 0 |
| Pocket MAX | 29.12 | 24.23 | 220.2 | 678 / 3 |
| Workshop base | 21.80 | 14.14 | 143.3 | 411 / 3 |
| Workshop MAX | 32.79 | 27.24 | 303.5 | 907 / 3 |

### Authoritative Capacity and operating costs

These are real `ProgressionConfig.Energy.Values`, used by the existing service, motor Energy resource and HUD. There is no display-only rescale, forced timer, distance kill, scripted death, capacity-level drain multiplier, or second progression/movement system.

| Form | Energy at Capacity levels 0 / 1 / 2 / 3 / 4 / 5 | Moving/s | BLOCKED/s | Drilling base/s |
|---|---|---:|---:|---:|
| Pocket | 25 / 35 / 45 / 55 / 65 / 80 | 3.35 | 3.7 | 2.7 |
| Workshop | 90 / 105 / 120 / 140 / 160 / 180 | 7.1 | 8.1 | 2.5 |
| Industrial | 190 / 205 / 220 / 240 / 260 / 280 | 12.5 | 13.5 | 2.7 |
| Heavy Borer | 300 / 320 / 340 / 365 / 390 / 420 | 17 | 18 | 2.9 |
| Hydraulic | 440 / 465 / 490 / 520 / 550 / 580 | 23 | 24 | 3.1 |
| Crystal Core | 600 / 630 / 665 / 700 / 740 / 780 | 29 | 30 | 3.4 |
| Fusion | 800 / 845 / 890 / 940 / 990 / 1040 | 36 | 37 | 3.6 |
| Quantum | 1080 / 1140 / 1200 / 1265 / 1330 / 1400 | 45 | 46 | 3.9 |

**Drilling = form base + 0.05 x the unchanged material rate.** Pocket Wood/Brick/Stone/Copper rates are **3.1 / 3.3 / 3.6 / 3.2**. Only the current operating state consumes Energy. Costs are fixed per form, snapshotted between attempts, and independent of purchased Capacity. Stationary drilling may consume less per second than powered travel; the obsolete validator requiring every drill rate to exceed travel was replaced with positive, finite, bounded independent-state validation. Material cost ordering is retained.

Charge stays **1.1 seconds**. All Power tables, damage/HP/hardness rules, Drive tables, launch-coast improvements, recovery, steering and **24/26 speed ceilings** are unchanged. There is no new waiting or run-ending animation.

| Capacity-only Pocket (Power/Drive 0, ten balanced seeds) | Median active s | Meters | Coins | Copper | Physical contacts / destroyed |
|---|---:|---:|---:|---:|---:|
| 0/5 (25 Energy) | 7.57 | 64.1 | 169 | 0 | 1 / 1 |
| 1/5 (35 Energy) | 10.55 | 93.9 | 241 | 0 | 1 / 1 |
| 2/5 (45 Energy) | 13.62 | 102.3 | 265 | 3 | 2 / 2 |
| 3/5 (55 Energy) | 16.71 | 115.1 | 326 | 3 | 3 / 3 |
| 4/5 (65 Energy) | 19.69 | 145.0 | 386 | 3 | 3 / 3 |
| 5/5 (80 Energy) | 24.38 | 155.4 | 472 | 3 | 5 / 5 |

The requested 25-80 table is preserved exactly. The Capacity-only MAX median is about **24.38 s**, slightly above the suggested 24-second edge; all-category MAX is about **24.23 s**. Workshop MAX has a balanced median below 28 s, with a resource-heavy observed tail near **28.35 s**. These small tails are retained instead of adding upgrade-dependent consumption or a hard cutoff. The 7-8-second initial target is met.

### Measured encounters (balanced medians, five seeds)

An encounter below means a distinct actual obstacle approached within 12 meters, including a bypass. Physical contacts and destructions are separate.

| Cohort | Approached / contacted / destroyed |
|---|---:|
| Pocket base | 3 / 1 / 1 |
| Pocket MAX | 13 / 7 / 7 |
| Workshop base | 9 / 4 / 4 |
| Workshop MAX | 16 / 9 / 8 |

### First decisions, Ore and route safety

The starter was inspected and retained: balanced Pocket reaches its first real Wood contact at about **2.63 active seconds**, fully destroys one normal obstacle, and completes near **7.57 seconds**. Safe play actively avoids obstacles and takes about **7.46 seconds**; avoiding is a valid decision, not an empty introductory timeout.

Only the two existing **OreOpenLeft/Right** Copper transforms moved from local **Z=22 to Z=19**, via PrefabUtility. X, scale, HP, hardness, rewards, section length and obstacle counts are unchanged. Guarded Ore's minimum selection ordinal moves **2 to 3**, introducing it one selected section later. The old guard intent text was updated to describe Capacity/Power tradeoffs instead of obsolete numerical costs. Fourteen patterns/six categories, continuous full-footprint bypasses, all pairwise transitions, reaction allowance, arrangement variation, relief rules and seeded determinism remain validated. No density increase or new region/material was added.

At starting Power, a full Brick+Ore line costs **15 Energy** (60% of base 25, 43% of the first 35-Capacity purchase); the Wood-then-Ore alternative costs **10.1**. Travel to the target still costs Energy, so not every line is affordable from every position. Guards are optional and deferred; unguarded routes introduce Copper earlier. Tests verify complete-only Copper, no payout for partial damage, no unavoidable hard wall, and actual route costs at an early Capacity purchase.

Among ten starting seeds, the resource policy collected Copper in **3/10** first attempts. Balanced first attempts intentionally take the normal obstacle and generally earn no Copper; Capacity-only level 1 can still miss it, while level 2 has median 3 Copper. The real unfunded journeys purchase combinations and acquire the required Copper within the first few attempts. This is reachable resource play, not guaranteed/free Copper.

The policy driver keeps its 0.35-second reaction allowance and actual steering response. Its old fixed 4-Energy reserve is now the actual form's **0.61-second moving cost** (minimum 1) for R2, so rescaled units do not silently turn the same reserve into 16% of the initial resource. The frozen-before CSV uses the original policy unchanged. A new `approached` export counts distinct obstacles within 12 meters ahead of the actual motor, including avoided obstacles; `contacts` remains physical contacts. Prefetched distant objects are not counted as encounters.

### Economy and genuine progression

Distance payout changes **9 to 12 Coins per 5 m**. The normal-obstacle multiplier stays **25**; Copper remains **3 per completely destroyed Ore**, without a multiplier. No currency source was added. Balanced first-run **169 Coins** can fund a 50-Coin Capacity purchase and other inexpensive choices (all three first purchases total 160).

**No upgrade prices changed:**

| Form/category | Prices for levels 1-5 |
|---|---|
| Pocket Power | 60 / 100 / 160 / 240 / 340 |
| Pocket Capacity | 50 / 90 / 140 / 210 / 300 |
| Pocket Drive | 50 / 90 / 140 / 210 / 300 |
| Workshop Power | 150 / 230 / 340 / 480 / 650 |
| Workshop Capacity | 130 / 210 / 310 / 440 / 600 |
| Workshop Drive | 130 / 210 / 310 / 440 / 600 |

All-category MAX remains mandatory. First/second evolution still spend **12 / 24 Copper** and require **2,480 / 5,230 total upgrade Coins**. Advanced-form prices/resources remain unchanged.

| Unfunded seed family | Attempts to Workshop | Additional attempts to Industrial | Attempts making a purchase | Total earned Coins / Copper |
|---|---:|---:|---:|---:|
| 10300 | 7 | 8 | 13 / 15 | 9806 / 36 |
| 8100 | 7 | 8 | 14 / 15 | 9542 / 36 |
| 9200 | 7 | 8 | 13 / 15 | 9601 / 36 |

These are existing motor, HP, contacts, Energy, ledger, purchases and evolution transactions starting with an empty memory profile. They receive no funding or balance edits. Three-category MAX and Copper gates are never bypassed. Separate parameter sweeps use explicitly temporary funded profiles only to select purchased levels; their earnings are still measured from gameplay.

### Upgrade benefits and advanced forms

Every adjacent Capacity purchase retains the same operating rates and extends actual safe-route duration/distance. Pocket gains about **2.99 s per +10 Energy**, then **4.48 s for the final +15**. Safe-route Drive 0-to-5 increases Pocket reach roughly **81.3 to 115.3 m** at equal duration; Workshop roughly **182.4 to 212.9 m**. All adjacent Drive levels improve reach within existing ceilings. Power halves base Pocket Wood clear time **1.4 to 0.7 s** and Energy **4.34 to 2.17**, while actual production combinations exercise the resulting route choices. Power does not improve a route that deliberately mines nothing.

Evolution intentionally returns to a compact baseline duration, as requested. It does not preserve the previous MAX run's wall-clock length or guarantee greater open-road distance. It does preserve higher numerical Capacity, all Drive/speed benefits, and improves familiar mining: Pocket MAX to Workshop base changes Wood clear time **0.70 to 0.58 s**, and its actual Energy **2.17 to 1.69** (about 22% cheaper). Tests check lower mining time and Energy per HP for every adjacent evolution and all current materials. The existing stronger visuals/Power progression are preserved.

Advanced profiles remain compact in today's Yard; they are not claims about future-region economy or obtaining advanced resources:

| Profile | All-policy seconds min / median / max | Balanced meters | Balanced contacted / destroyed |
|---|---:|---:|---:|
| Industrial base | 15.20 / 16.48 / 17.63 | 190.2 | 6 / 6 |
| Industrial MAX | 22.40 / 23.46 / 24.71 | 288.0 | 7 / 7 |
| Heavy Borer intermediate | 20.00 / 20.71 / 21.77 | 262.7 | 6 / 6 |
| Hydraulic intermediate | 21.30 / 21.90 / 22.51 | 281.5 | 7 / 7 |
| Crystal base | 20.69 / 21.18 / 21.60 | 273.9 | 8 / 8 |
| Crystal MAX | 26.90 / 27.35 / 27.79 | 354.3 | 9 / 9 |
| Fusion intermediate | 24.72 / 25.03 / 25.45 | 326.5 | 10 / 10 |
| Quantum base | 24.00 / 24.23 / 24.55 | 318.5 | 10 / 10 |
| Quantum MAX | 31.11 / 31.37 / 31.59 | 409.9 | 12 / 12 |

### Save compatibility and implementation scope

No profile schema, balances, purchased level, ownership record or banking receipt is reset/repriced. Existing levels map to the new authoritative Energy table; e.g. purchased Pocket Capacity 5 stays 5 and now means 80 Energy. There is no refund or repurchase because prices are unchanged. Save tests retain all five resource balances, Coins, all eight forms' levels/history and durable receipts exactly; real player saves are isolated from tests and authoring.

`UltraShortRunSetup` extends the existing `RapidRunSetup` using guarded, versioned Editor migration (balance revision 5). It preflights recognized M8-R tables/placements and preserves unfamiliar custom authoring. Healthy reruns never overwrite later Inspector tuning. Initial application used Unity Editor APIs for all ScriptableObject and prefab changes. Production Gameplay scene/reference wiring is unchanged. The existing Workshop Inspector gains **QA: Fresh Pocket - 0 upgrades (no disk writes)**, reusing `UseTemporaryStage` with zero Coins/resources. It creates no competing service or motor, and needs no JSON editing.

Changed runtime source is limited to balance validation: Pocket Energy is no longer hardcoded to 100, and stationary drilling is allowed to have an independently authored positive rate. The motor, movement, damage, steering, launch, visuals/VFX, save/banking/evolution implementations and Workshop transitions are unchanged. The existing historical PlayMode fixture now restores original Capacity tables in **already-created session clones**, using its existing BalanceFactory hook before service initialization. Archived scalar assertions are retained. Production short-run tests always use R2 assets.

### Actual tests, exports and graphics

- **182/182 EditMode**, **91/91 Direct3D11 PlayMode**, zero failures/skips, both Unity exits 0. Durations: **19.75 s / 431.74 s**.
- The test-only approached-obstacle CSV extension was added afterward; **3/3 focused production simulation cases** passed again, zero failures/skips, exit 0. No production code/tuning changed after the full suites.
- All **170 original EditMode and 89 original PlayMode case names** remain (0 missing). Twelve EditMode cases and two PlayMode cases were added.
- Initial full runs exposed obsolete 200/220/300 Energy assertions, archived 100/200-Capacity fixtures using new production stats, a sandbox assertion incorrectly including a legitimate new normal-run receipt after reload, and a test-caption encoding issue. These were repaired without removing cases, altering the motor, granting rewards, or widening the initial-run target. Earlier XML remains under Logs.
- `M8R2-Before.csv`: reproduced 300 M8-R samples before edits. `M8R2-Policies.csv`: 300 actual R2 runs, 15 form/level profiles x five seeds x four policies. `M8R2-CapacityAndCombinations.csv`: **520 runs**, all six Pocket Capacity levels plus seven Power/Drive combinations x ten seeds x four policies. `M8R2-Upgrades.csv`: **144 isolated-category runs**. `M8R2-EvolutionJourneys.csv`: three genuine unfunded journeys. Every pacing sample ends from empty Energy and reconciles consumption to Capacity.
- `M8R2-NaturalEpisodes.csv`: normal-Update graphics episodes, with real-time active seconds **7.57 / 13.64 / 14.49 / 28.14 / 23.66** for Pocket base/intermediate, Workshop base/MAX and Crystal intermediate. Different charge strength and capture/input frame timing cause modest reach/reward differences from explicit-clock sweeps. All banked exactly once.
- Evidence: `M8R2-EditMode.xml`, `M8R2-PlayMode.xml`, `M8R2-ExportValidation.xml`, corresponding final logs, and `M8R2-Preservation.json`.

Actual 540x960 (9:16) gameplay, drilling, results, purchased-Upgrade Workshop and immediate-next-attempt captures are `Logs/M8R2-Evo*-L*-*.png`. Reviewed examples: Pocket base Drilling/Results/Workshop, Workshop MAX Drilling, Workshop base Workshop, and Quantum MAX Workshop. Current 25/35/90/180/1400 Energy labels, buttons, models, resource gates and text fit without new clipping. Funded cohort images visibly say DEVELOPMENT SANDBOX; their large wallet is not the unfunded progression simulation. No artwork was generated/replaced. No standalone GPU/device performance claim or listening session is made.

The setup test invokes the command twice and verifies **all 728 Assets files** remain byte-identical on reruns. Source snapshot: 763 task-start files retained, no missing files. The Gameplay scene, drill models/prefabs/materials/textures/audio/VFX, Power/Drive data and all existing metadata remain unchanged from task start, except the two explicitly moved Ore prefabs. No duplicate GUID or missing metadata was found. C#/Markdown diff whitespace checks pass; the two Unity-YAML whitespace lines reported by unrestricted `git diff --check` already existed in the uncommitted Gameplay scene, which R2 did not change.

### Three focused owner checks

1. Open **Gameplay**, enter Play, select **M4 Workshop > WorkshopController**, and click **QA: Fresh Pocket - 0 upgrades (no disk writes)**. Start, charge/release and steer through or around the first Wood. Expect about **7-8 active seconds**, a real early decision, Energy-empty Results and affordable upgrades. No real save reset or JSON editing is needed.
2. In the same temporary session, buy Capacity upgrades (or use the existing temporary funded stage controls) and compare 25/35/45/55/65/80 endurance. Check early unguarded Copper on several seeds, the optional guarded route after upgrades, and Workshop base/MAX's faster, cheaper drilling. Judge whether the small MAX tails near 24-28 seconds feel right.
3. Use Results -> Workshop -> purchase -> Start/Retry repeatedly. Verify prompt transitions, once-only Coin/Copper banking and purchases, persistent temporary progress, all-three-MAX evolution gating and stronger Workshop presentation. Leave Play to return to the untouched real profile. Human reaction, route comprehension, fun and subjective pace remain owner acceptance; advanced-region progression stays out of scope.

No Android/Google Play work, M9 content, commit, push, reset, clean or revert was performed. Stop after M8-R2 for review.

### M8-R2 final working tree

Branch **main**, unchanged HEAD **b9cdf9b536dcee30efe7dfa14752bee6f2f4b772**. Git status: **37 modified / 210 untracked / 0 staged** (includes all prior uncommitted M8/M8-R work). Relative to the captured task start, **25 existing files changed, 6 new source/metadata files, 0 missing**. The new files are `UltraShortRunSetup.cs`, `UltraShortRunTests.cs`, `UltraShortRunPlayModeTests.cs` and their metadata. Existing changes are the balance/stat assets, two Ore prefabs, guard metadata, two runtime validators, setup/Inspector integration, necessary test fixtures, this document and README. Full paths are enumerated in `Logs/M8R2-FinalAudit.json`; before/after Git inventories are retained. Nothing committed or pushed.


## M8-R3 - Faster early Capacity upgrades (29 October 2026)

This is an early Capacity adjustment to M8-R2, not another redesign. The owner's recorded Power 8 / Capacity level 3 run used 55 Energy and lasted approximately 18 active seconds (launch ~3 s, depletion ~21 s), ending at 109 m / 362 Coins / 3 Copper. The successful fresh Pocket experience must remain.

### Final tables and implementation

| Capacity level | Pocket R2 -> R3 | Workshop R2 -> R3 |
|---|---:|---:|
| 0 | 25 -> **25** | 90 -> **90** |
| 1 | 35 -> **30** | 105 -> **100** |
| 2 | 45 -> **35** | 120 -> **110** |
| 3 | 55 -> **40** | 140 -> **120** |
| 4 | 65 -> **45** | 160 -> **130** |
| 5 | 80 -> **55** | 180 -> **140** |

Pocket uses the proposed table exactly. Workshop retains its already successful base 90, then increases by 10 per purchase to 140. The measured MAX duration supports this conservative choice. The existing `UltraShortRunSetup` adds a guarded early-only migration (authoring revision 6), exposed by **Tools > DrillRunner > Setup M8-R3 Fast Upgrades** and the existing setup commands. It updates only the two early Energy tables and the authoring revision marker, using Unity Editor APIs. Unrecognized owner tables are preserved; healthy reruns do not overwrite subsequent tuning. No scene wiring is needed.

There are **no operating-cost, Power, Drive, speed-ceiling, encounter, price, reward, movement, damage, camera, audio, VFX, runtime lifecycle or save-system changes**. The motor still ends naturally when its authoritative Energy is consumed; no timer, artificial death or capacity-dependent drain was added. EVO 3-8 tables are retained.

### Paired gameplay measurements

The new comparison uses actual motor movement, material HP, contacts, steering, consumption, rewards and banking. It runs **600 attempts**: R2/R3 x both early forms x six Capacity levels x five seeds x four policies, plus paired **Power 8 / Capacity level 3** at Drive levels 0, 3 and 5. Tables are changed only on the existing session clones for the paired control, then restored; production assets and real profiles are not modified by simulations. Seeds: 1337/904/2026/8101/9201. Policies: safe, balanced, resource and power. Input reaction, geometry, rates and rewards are identical between versions.

The following medians use balanced play with base Power/Drive to isolate Capacity. Ranges cover all four policies and five seeds; they are observations, not guaranteed human durations. Seconds exclude Ready/Charging. Increased Capacity can enable a Copper detour, so distance/Coins need not increase on a policy that chooses a different route; controlled identical safe routes increase both endurance and reach.

| Pocket Capacity | R2 median s | R3 median s | R3 observed s min-max | Median m | Coins | Copper |
|---|---:|---:|---:|---:|---:|---:|
| 0/5 | 7.57 | **7.57** | 7.46-7.57 | 64.1 | 169 | 0 |
| 1/5 | 10.55 | **9.06** | 8.96-9.16 | 79.0 | 205 | 0 |
| 2/5 | 13.62 | **10.55** | 10.45-10.66 | 93.9 | 241 | 0 |
| 3/5 | 16.71 | **12.04** | 11.94-12.25 | 108.8 | 277 | 0 |
| 4/5 | 19.69 | **13.62** | 13.43-13.75 | 102.3 | 265 | 3 |
| 5/5 | 24.38 | **16.71** | 16.42-16.73 | 115.1 | 326 | 3 |

| Workshop Capacity | R2 median s | R3 median s | R3 observed s min-max | Median m | Coins | Copper |
|---|---:|---:|---:|---:|---:|---:|
| 0/5 | 14.14 | **14.14** | 12.68-15.48 | 143.3 | 411 | 3 |
| 1/5 | 16.60 | **15.90** | 14.08-16.89 | 154.4 | 460 | 3 |
| 2/5 | 19.06 | **17.31** | 15.49-18.64 | 171.6 | 508 | 3 |
| 3/5 | 22.22 | **19.06** | 16.90-20.39 | 182.8 | 557 | 3 |
| 4/5 | 25.04 | **20.81** | 18.31-21.80 | 193.9 | 606 | 3 |
| 5/5 | 28.20 | **22.22** | 19.72-23.87 | 211.1 | 654 | 3 |

Pocket level 0 is unchanged at **7.57 s**. Level 3 falls from **16.71 to 12.04 s**, and Capacity-only MAX from **24.38 to 16.71 s**. Workshop Capacity-only MAX falls from **28.20 to 22.22 s**. These MAX rows isolate Capacity; the full MAX-Power/Drive production cohorts are reported separately below. The slowest observed base-Power resource-heavy Workshop MAX sample is **23.87 s**, slightly above the suggested 23-second edge. Balanced and fully upgraded medians meet the target; the tail is kept without a forced cutoff.

### Owner-like Power 8 comparison

The original recording's exact seed, steering, charge strength and Drive level were not supplied, so these are controlled comparisons rather than a claimed replay.

| Drive level, Power 8 / Capacity level 3 | R2 median s (55 Energy) | R3 median s (40 Energy) | R3 meters / Coins / Copper |
|---|---:|---:|---:|
| 0 | 16.60 | **12.06** | 99.0 / 253 / 3 |
| 3 | 16.66 | **12.12** | 105.3 / 302 / 3 |
| 5 | 16.66 | **12.12** | 114.7 / 314 / 3 |

### Upgrade value, safety and persistence

Every Pocket Capacity purchase adds real Energy at unchanged operating costs: early +5 purchases add approximately **1.49 safe-route seconds**, and the final +10 adds approximately **2.99 seconds**. Each Workshop +10 adds approximately **1.41 safe-route seconds**. Paired tests require strictly increasing balanced medians for all six levels, and the retained eight-form adjacent-upgrade test verifies actual safe-route endurance/reach. Power and Drive benefits remain unchanged.

New Workshop base retains Power 12 versus Pocket MAX 10, the approved model/Drive benefits, and cheaper/faster familiar drilling: Wood **0.70 -> 0.58 s**, **2.17 -> 1.69 Energy**. Thus shorter baseline duration does not remove the measurable evolution improvement. Exact production MAX comparisons follow below.

Early Wood remains a meaningful drill/avoid decision, with a balanced contact around **2.63 s** and one destruction before fresh-Pocket depletion. Fresh Energy/rates/geometry are unchanged, including the R2 immediately reachable Copper seeds. Normal progress still needs actual completed Ore. All seeded encounter categories, route certificates, reaction allowance, full-footprint bypasses and BLOCKED escape checks are retained. Guarded Copper still costs **15 Energy**, now exactly 50% of the first 30-Capacity purchase; it remains optional and deferred. No hard-material wall or density change was introduced.

**Prices and rewards remain unchanged**: 12 Coins per 5 m, normal-obstacle multiplier 25, Copper 3 per completely destroyed Ore, no partial-damage payout. Upgrade totals remain 2,480 / 5,230 Coins; evolution still requires MAX in all three categories and 12 / 24 Copper. No free currency or automatic purchase was added.

The recorded integer upgrade levels retain their meanings. An existing Pocket Capacity level 3 stays level 3 and now supplies 40 Energy; its balance, Power, Drive, form ownership and banking receipts are retained. No profile reset, schema migration, refund, disk-save rewrite or price change is performed. Existing eight-form save-compatibility tests remain.

The existing fresh Pocket Inspector button and temporary funded/stage controls are reused without new manual wiring or JSON editing. Enter Play, select **M4 Workshop**, and use the existing **QA: Fresh Pocket - 0 upgrades (no disk writes)** button. It continues to create an isolated zero-fund memory profile.

### Production MAX and variation

In this separate production cohort, **all three categories** are upgraded together. It uses five seeds and four policies (300 actual runs over 15 form/level profiles). Capacity-only tables above deliberately hold Power/Drive at zero.

| Profile | R2 balanced seconds | R3 balanced seconds | R3 balanced meters / Coins / Copper | R3 all-policy seconds range |
|---|---:|---:|---:|---:|
| Pocket base | 7.57 | **7.57** | 64.1 / 169 / 0 | 7.46-7.57 |
| Pocket all-category MAX | 24.23 | **16.67** | 152.8 / 460 / 3 | 16.42-16.72 |
| Workshop base | 14.14 | **14.14** | 143.3 / 411 / 3 | 12.68-15.48 |
| Workshop all-category MAX | 27.24 | **21.38** | 237.7 / 714 / 3 | 19.72-22.28 |

Workshop base's balanced distance/reward is 143.3 m / 411 Coins versus Pocket all-category MAX 152.8 m / 460 Coins, over a shorter 14.14 versus 16.67-second run. Evolution does not promise every raw distance exceeds the old MAX on every route. Its Power 12 (versus 10), lower mining cost/time, stronger Drive and reward potential remain; Workshop's all-category MAX reaches 237.7 m / 714 Coins. This meets the requested drilling-performance/reward-potential improvement without extending early runs artificially.

The following ranges show variation across the **five seeds and all four policies**, for the Capacity-only cohort. Balanced medians and seconds ranges are above; these are actual earnings, without QA wallet funding included.

| Form / Capacity level | Distance range m | Coins range | Copper range |
|---|---:|---:|---:|
| Pocket 0/5 | 60.0-81.3 | 132-192 | 0-3 |
| Pocket 1/5 | 61.8-96.2 | 168-228 | 0-3 |
| Pocket 2/5 | 72.5-111.1 | 193-264 | 0-3 |
| Pocket 3/5 | 70.4-126.1 | 218-300 | 0-3 |
| Pocket 4/5 | 85.3-141.0 | 254-336 | 0-3 |
| Pocket 5/5 | 115.0-170.8 | 326-408 | 0-3 |
| Workshop 0/5 | 123.4-182.4 | 396-463 | 0-3 |
| Workshop 1/5 | 140.5-199.6 | 444-511 | 0-3 |
| Workshop 2/5 | 151.7-216.8 | 492-560 | 0-3 |
| Workshop 3/5 | 162.9-234.0 | 528-609 | 0-6 |
| Workshop 4/5 | 180.0-251.2 | 564-657 | 0-6 |
| Workshop 5/5 | 187.9-268.4 | 600-719 | 0-6 |

The retained 520-run, ten-seed mixed-upgrade sweep also passed. All ten fresh balanced runs contacted/destroyed Wood at about **2.63 s**, with **169 Coins** and about **64.1 m**. Fresh resource play earned real Copper in **3/10 seeds** (3 each); a player avoiding all mining earns distance Coins but no Copper. At least one route choice remains in ordinary runs (median three obstacles within 12 m, not distant prefetched geometry). Guarded Copper is an optional 15-Energy commitment, never mandatory for survival. Existing full-footprint bypass, safe/moderate/guarded route, BLOCKED steering escape, mineability and no-partial-Copper tests all pass. No price/reward change is warranted by these measurements.

### Unfunded evolution pacing

| Seed family | R2 attempts to Workshop / additional to Industrial | R3 attempts to Workshop / additional to Industrial | R3 purchase attempts | R3 total earned Coins / Copper | R3 active seconds to Workshop / additional to Industrial |
|---|---:|---:|---:|---:|---:|
| 10300 | 7 / 8 | **8 / 9** | 17 / 17 | 8399 / 36 | 110.41 / 181.84 |
| 8100 | 7 / 8 | **8 / 9** | 17 / 17 | 8302 / 36 | 110.65 / 181.21 |
| 9200 | 7 / 8 | **8 / 9** | 17 / 17 | 8254 / 36 | 110.93 / 180.93 |

The first evolution is still within **5-8 short attempts**, and the second takes **9 additional attempts**. Every one of the 17 attempts funds a purchase in each journey. Although the count increases from R2's 7 + 8, total active time is lower: about **110-111 s** to Workshop instead of **130-131 s**, then **181-182 s** to Industrial instead of **202-209 s**. Workshop-stage income is 5,655-5,719 Coins and 24 Copper, sufficient for the unchanged 5,230-Coin MAX upgrades and Copper evolution cost. These are three controlled policies/seed families, not a guarantee for every human strategy. No free rewards, forced unlocks, save reset or bypassed MAX gates occur.

### Later forms

All six later Capacity assets and their metadata are **byte-identical to the M8-R3 starting snapshot**. All **180 matched EVO 3-8 production samples** have exactly identical metrics to the copied R2 export, ignoring the CSV mode label. Current balanced medians are:

| Profile | R2 = R3 seconds |
|---|---:|
| Industrial base | 16.85 |
| Industrial MAX | 23.68 |
| Heavy Borer intermediate | 20.91 |
| Hydraulic intermediate | 22.02 |
| Crystal base | 21.30 |
| Crystal MAX | 27.58 |
| Fusion intermediate | 25.16 |
| Quantum base | 24.33 |
| Quantum MAX | 31.45 |

Eight-form validation, adjacent Capacity/Drive/Power benefit tests and save compatibility remain in the full suite. Later-region resource acquisition remains outside this early-stage adjustment.

### Actual Unity execution and graphics

Unity **6000.3.8f1** executed both complete suites sequentially:

- **182/182 EditMode**, zero failures/skips, exit **0**, **22.84 s**. XML/log: `Logs/M8R3-EditMode.xml`, `Logs/M8R3-EditMode.log`.
- **92/92 PlayMode** with **Direct3D11 graphics**, zero failures/skips, exit **0**, **496.20 s**. XML/log: `Logs/M8R3-PlayMode.xml`, `Logs/M8R3-PlayMode.log`.
- All **182 previous EditMode and 91 previous PlayMode case names** are retained; one meaningful PlayMode comparison case was added. No regression case was removed. Expectations were updated only for intentional new Capacity values, target windows and the guard's now-50% first-upgrade cost.
- The new `EarlyUpgradePacingPairsR2AndR3TablesAndOwnerLikePowerEight` executes the 600 paired samples and asserts every early Capacity level's duration and monotonic benefit. It reuses the actual motor/policy driver and session-cloned configs, restoring them in `finally`. Existing 520-run combinations, 300-run eight-form profiles, 144 isolated upgrade comparisons, three unfunded journeys, route/escape, save/banking, retry and temporary-QA tests also execute.
- `Logs/M8R3-PairedCapacity.csv`, `M8R3-CapacityAndCombinations.csv`, `M8R3-Policies.csv`, `M8R3-Upgrades.csv`, `M8R3-EvolutionJourneys.csv`, `M8R3-RouteCosts.csv` contain measured data. All 1,420 pacing samples in the first three exports ended through **EnergyEmpty**, with consumed Energy equal to starting Capacity. No artificial termination was introduced.
- `Logs/M8R3-NaturalEpisodes.csv` contains five graphics-enabled normal-Update runs (banked once each): Pocket base **7.57 s**, Pocket intermediate **10.65 s**, Workshop base **14.49 s**, Workshop MAX **22.04 s**, Crystal intermediate **23.66 s**. Capture input/charge/frame timing differs from explicit-clock policy sweeps; these are not duplicate statistical samples.
- Earlier focused calibration **2/2 passed**, exit 0 (`Logs/M8R3-Calibration.xml`), before the successful complete suites. No gameplay or test code changed after the final full suites.

Graphics evidence is the existing actual **540x960 (9:16)** workflow under `Logs/M8R3-Evo*-L*-*.png`. Inspected fresh Pocket Drilling (Power 5, 25 Energy), first-purchase Pocket Workshop (30 -> 35), and Workshop MAX Workshop (140 Energy), plus results. Scene/model/HUD presentation remains intact, with readable Capacity labels and no new clipping in the inspected frames. The funded parameter captures explicitly display DEVELOPMENT SANDBOX and are not evidence of unfunded wallet balances. Graphics enabled execution is not a standalone device-performance, audio-listening or human-fun claim.

### Files and preservation verification

R3 changes relative to the captured R2 working tree:

- `Assets/_DrillRunner/Editor/UltraShortRunSetup.cs`: early-only guarded Editor migration, new R3 menu alias, canonical early tables; older migrations retained.
- `Assets/_DrillRunner/ScriptableObjects/PocketDrillProgression.asset` and `WorkshopDrillProgression.asset`: **Energy Values only**, authored through Unity Editor APIs.
- `Assets/_DrillRunner/ScriptableObjects/RapidRunBalance.asset`: authoring revision **5 -> 6 only**, no rate/reward changes.
- `Assets/_DrillRunner/Tests/PlayMode/EarlyUpgradePacingTests.cs` and `.meta`: new paired comparison.
- `Assets/_DrillRunner/Tests/EditMode/EvolutionTests.cs`, `UltraShortRunTests.cs`: expected new Capacity values / early guarded-route budget.
- `Assets/_DrillRunner/Tests/PlayMode/RapidRunIntegrationTests.cs`, `RapidRunPlayModeTests.cs`, `RapidRunProgressionTests.cs`, `UltraShortRunPlayModeTests.cs`: updated expected Capacity/target windows and separate R3 export filenames, preserving R2 evidence.
- This M8-R3 section and `README.md`: current tuning, measurements and verification.

SHA-256 comparison retains all **769 task-start source files**, with **0 missing**, **12 existing files changed** and **2 new files** (comparison script/metadata). **649 protected task-start files** are unchanged, including all runtime source, production Gameplay scene, models, prefabs, materials, audio, textures, metadata, Packages and ProjectSettings. Prices/rewards, encounter assets, scene geometry, saves and temporary-profile implementation are untouched. All **730 current Assets files** pass the existing twice-run setup byte/GUID/idempotence check; metadata audit finds no duplicate GUID, orphan metadata or missing metadata. R3 setup was applied once via Editor APIs before validation; repeated setup preserves all authored bytes and later Inspector tuning.

Audit evidence: `Logs/M8R3-Baseline.json`, `M8R3-Preservation.json`, before/after Git inventories, setup log, paired CSV and original copied R2 exports. C#/Markdown diff whitespace checks pass. Two existing blank-name/text Unity-YAML trailing-space lines in Gameplay were present before R3 and were left untouched.

### Two owner QA checks

1. Open **Gameplay**, enter Play, select **M4 Workshop > WorkshopController**, and click **QA: Fresh Pocket - 0 upgrades (no disk writes)**. Launch, drill/avoid the first Wood and expect about **7-9 active seconds** with affordable upgrades. In the same temporary profile, buy Capacity and check **25/30/35/40/45/55**. For the recorded comparison, use the existing temporary funded profile, buy Power three times (8) and Capacity three times (40): expect roughly **12-14 s**, with route/charge variation. Check Energy-empty Results, purchase and Retry.
2. Using **QA: Select temporary stage (no disk writes)** with **Temporary EVO = 2**, compare **MAX current-form upgrades** off/on (90/140 Energy). Expect roughly **14 / 21-22 active seconds** for balanced play, stronger mining, a reachable Copper option and steering out of BLOCKED. Repeat Results -> Workshop -> Start to check once-only banking. Leave Play to restore the real profile. Human reaction, route choice and subjective pace remain owner acceptance.

### Final Git status

Branch **main**, unchanged HEAD **b9cdf9b536dcee30efe7dfa14752bee6f2f4b772**. **37 modified / 212 untracked / 0 staged**, including all pre-existing M8/M8-R changes (R3 began with 37 modified / 210 untracked). Nothing was committed, pushed, reset, reverted or discarded. No M9, Android or unrelated work. **Stop after M8-R3 for review.**


## M8-R4 - Coin economy and upgrade pacing (29 October 2026)

### Diagnosis and final reward table

The owner's 97 m / Power 8 / 40 Energy result is exactly **228 distance + 25 Wood = 253 Coins**. The 628-Coin Workshop total includes approximately **375 saved Coins**; the subsequent 240 + 340 upgrades used savings as well as that run. The remaining 48 Coins is consistent. It would be incorrect to attribute both purchases to the run alone.

Selected: **6 Coins per completed 5 m**, down from 12. It preserves forward rewards, supports a meaningful first purchase, and makes normal drilling more valuable relative to avoidance. No reward compensation or price change is needed by the measured journeys.

| Source | Before | M8-R4 |
|---|---:|---:|
| Completed forward 5 m | 12 Coins | **6 Coins** |
| Completely destroyed Wood | 25 Coins | 25 Coins |
| Completely destroyed Brick | 50 Coins | 50 Coins |
| Completely destroyed Stone | 75 Coins | 75 Coins |
| Completely destroyed Copper Ore | 3 Copper, 0 Coins | 3 Copper, 0 Coins |
| Partial damage, BLOCKED, duplicate destruction, backtracking | 0 | 0 |

The two recorded results would become **139 / 189 Coins** with the same distance/destruction totals: 114 distance + 25 / 75 obstacle Coins. These are **counterfactual ledger calculations, not new gameplay recordings**. No existing balance or banked run is recalculated.

### Reproducible candidate evaluation

The new tests reuse actual motor simulation, contacts, material HP, steering, streamed encounters, Energy, reward ledger, purchasing and once-only banking. Only session-cloned reward settings change; no real save or production asset is edited by a simulation.

**624 static attempts** compare 12/6/7/8 over 13 profiles x seeds 1337/904/2026 x four policies. Profiles: fresh Pocket; Power 8 / Capacity 3; all-MAX Pocket; Workshop base/intermediate/MAX; Industrial base/MAX; EVO 4-8 intermediate. Funded temporary profiles select the parameters only; income columns report the run's earned ledger, never that wallet.

Fresh journeys use **zero Coins/resources/levels**, families 8100/9200/10300, seed family + attempt, all four rates and four strategies. Purchases use real prices and transactions, never a forced number per run. Safe avoids mining until MAX, then **explicitly switches to feasible Ore** to satisfy Copper; a player who refuses Ore forever cannot evolve under the existing resource rule. Balanced takes affordable normal targets/needed Ore and buys the cheapest affordable next upgrade. Drilling-focused (`Power`) favors harder affordable targets and buys Power/Capacity/Drive. Resource-focused prioritizes needed Ore and buys Capacity/Power/Drive. Safe buys Capacity/Drive/Power. The same policy/purchase definitions apply to all rates. The CSV identifies actual policy on every attempt, including Safe's Ore phase.

These are informed, reproducible policies with the retained 0.35 s reaction allowance and actual steering, not a claim of human-optimal strategy. Later-resource progression is not invented.

### Candidate comparison

Balanced medians use three identical seeds per static profile. Coins are total (**distance + obstacle**).

| Coins / 5 m | Fresh Pocket | Pocket all-MAX | Workshop base | Workshop all-MAX | Balanced attempts to EVO 2 / additional to EVO 3 |
|---|---:|---:|---:|---:|---:|
| 12 | 169 (144+25) | 460 (360+100) | 411 (336+75) | 765 (540+225) | 9 / 9 |
| 6 | 97 (72+25) | 280 (180+100) | 243 (168+75) | 495 (270+225) | 14 / 14 |
| 7 | 109 (84+25) | 310 (210+100) | 271 (196+75) | 540 (315+225) | 12-13 / 13 |
| 8 | 121 (96+25) | 340 (240+100) | 299 (224+75) | 585 (360+225) | 12 / 11-12 |

At 6, the typical fresh attempt pays **97 Coins**, allowing one chosen first purchase (50/50/60), while any two first purchases cost at least 100. At 7/8/12 the same fresh attempt pays 109/121/169. Multiple purchases later remain allowed whenever accumulated savings afford them. Six is preferred over 7/8 because it removes effortless first-run multiple purchases while retaining regular advancement.

The matched comparison asserts identical **duration, distance, drilling Energy, obstacle Coins and Copper** across every rate. All samples end from natural empty Energy and reconcile consumed Energy to starting Capacity. No timer, forced termination, per-level drain or gameplay rule was added.

### Selected-rate rewards and strategy tradeoffs

Three-seed medians per attempt. Copper is separate from Coins. Components are independently aggregated.

| Profile / policy | Active s | Meters | Coins (distance + obstacle) | Copper | Drilling Energy |
|---|---:|---:|---:|---:|---:|
| Pocket base / Safe | 7.46 | 81.3 | 96 (96+0) | 0 | 0.00 |
| Pocket base / Balanced | 7.57 | 64.1 | 97 (72+25) | 0 | 4.34 |
| Pocket base / Power | 7.57 | 64.1 | 97 (72+25) | 0 | 4.34 |
| Pocket base / Resource | 7.46 | 81.3 | 96 (96+0) | 0 | 0.00 |
| Pocket Power 8 / Capacity 3 / Safe | 11.94 | 126.1 | 150 (150+0) | 0 | 0.00 |
| Pocket Power 8 / Capacity 3 / Balanced | 12.06 | 99.0 | 139 (114+25) | 3 | 6.31 |
| Pocket Power 8 / Capacity 3 / Power | 12.14 | 89.1 | 177 (102+75) | 0 | 8.14 |
| Pocket Power 8 / Capacity 3 / Resource | 11.99 | 111.1 | 132 (132+0) | 3 | 3.60 |
| Pocket MAX / Safe | 16.42 | 223.7 | 264 (264+0) | 0 | 0.00 |
| Pocket MAX / Balanced | 16.67 | 152.8 | 280 (180+100) | 3 | 11.56 |
| Pocket MAX / Power | 16.72 | 141.7 | 293 (168+125) | 3 | 13.73 |
| Pocket MAX / Resource | 16.46 | 208.3 | 246 (246+0) | 3 | 2.88 |
| Workshop base / Safe | 12.68 | 182.4 | 216 (216+0) | 0 | 0.00 |
| Workshop base / Balanced | 14.14 | 143.3 | 243 (168+75) | 3 | 7.33 |
| Workshop base / Power | 14.49 | 137.4 | 262 (162+100) | 3 | 9.02 |
| Workshop base / Resource | 13.11 | 173.4 | 204 (204+0) | 3 | 2.25 |
| Workshop MAX / Safe | 19.72 | 306.0 | 366 (366+0) | 0 | 0.00 |
| Workshop MAX / Balanced | 22.04 | 227.6 | 495 (270+225) | 3 | 11.81 |
| Workshop MAX / Power | 22.26 | 224.1 | 514 (264+250) | 3 | 13.09 |
| Workshop MAX / Resource | 20.73 | 274.2 | 374 (324+50) | 6 | 5.41 |
| Industrial base / Safe | 15.20 | 248.4 | 294 (294+0) | 0 | 0.00 |
| Industrial base / Balanced | 16.85 | 190.2 | 353 (228+125) | 3 | 6.87 |
| Industrial base / Power | 17.63 | 182.1 | 416 (216+200) | 3 | 10.26 |
| Industrial base / Resource | 16.05 | 224.8 | 314 (264+50) | 3 | 3.75 |

The recorded 97 m example's distance share drops **90.1% -> 82.0%**; normal destruction rises 9.9% -> 18.0%. Measured fresh balanced distance share drops **85.2% -> 74.2%**. At Pocket MAX drilling-focused play, normal obstacles provide about **42.7%**; Workshop MAX about **48.6%**. Mining is competitive: fresh balanced 97 versus safe 96; Pocket MAX drilling-focused 293 versus safe 264; Workshop MAX drilling-focused 514 versus safe 366. Resource play may sacrifice Coins for required Copper, and stops seeking extra Ore after meeting the quota. The controlled Power 8 sample is about 99 m, not a claimed exact replay of the owner's 97 m inputs.

Unchanged Pocket mining costs at Power 5: Wood **4.34 Energy / 25 Coins**, Brick **9.24 / 50**. Stone is unavailable below hardness 9; at Power 9 it costs **9.60 / 75**, versus Wood 2.41 / 25 and Brick 5.13 / 50. Coin/drilling-Energy at common Power 9 is roughly **10.37 / 9.74 / 7.81**. No material is dramatically inflated. These isolated HP/rate costs omit travel, recovery and routing; actual tradeoffs are measured above.

BLOCKED pays nothing and consumes Energy without forward progress or qualifying destruction. Duplicate/partial contacts never pay. Existing seeded variety, safe bypasses, guarded/moderate routes and actual escape tests remain. No repeated-hit or deliberately blocked reward source exists. Finite scripted seeds are not an exhaustive adversarial farming search.

### Selected-rate affordability and evolution

Counts span three unfunded families. No-affordable counts exclude attempts already at all-three MAX. Multi-purchase attempts use the actual accumulated balance, not necessarily that attempt's income alone.

| Strategy | Attempts to EVO 2 | Additional to EVO 3 | Attempts buying / total | No affordable improvement | Multi-purchase attempts | Coins left at EVO 2 / EVO 3 |
|---|---:|---:|---:|---:|---:|---:|
| Balanced | 14 | 14 | 25-26 / 28 | 2-3 | 4-5 | 130-150 / 21-140 |
| Power | 14-15 | 13 | 25-26 / 27-28 | 2 | 4-5 | 71-227 / 33-255 |
| Resource | 15 | 17-18 | 26-28 / 32-33 | 5-6 | 2-4 | 58-208 / 84-284 |
| Safe | 16-17 | 17-18 | 23-24 / 34 | 3 | 3-4 | 736-1006 / 1346-1414 |

The longest selected-rate no-affordable stretch is **one attempt**; the regression allows at most two. Balanced reaches EVO 2 at the upper edge of the flexible 10-14 target; drilling-focused is 14-15, resource-focused 15, and avoidance-heavy 16-17 including explicit Ore recovery. No attempt count is enforced by gameplay. Workshop takes 13-18 additional short attempts depending on strategy.

Balanced Copper is ready at Pocket attempts 8-9, with MAX at 14. Resource-focused Copper is ready at 6-7 / 6, while Coin purchases continue through 15 / 17-18. This remaining Coin work still contains regular upgrades or saving, with no consecutive dry stretch beyond one attempt. Avoidance-heavy MAX precedes Copper by 3-4 Pocket / 4-5 Workshop attempts; refusing resources forever cannot pass the Copper gate. Switching to feasible Ore resolves that policy limitation without a free reward or requirement change. Players may change strategy at any time.

**No prices changed.** Pocket Power: 60/100/160/240/340; Capacity and Drive: 50/90/140/210/300; total **2,480 Coins**. Workshop Power: 150/230/340/480/650; Capacity and Drive: 130/210/310/440/600; total **5,230 Coins**. There are still 15 purchases per form, all-three-MAX and **12 / 24 Copper** gates. No mandatory one-purchase rule, free upgrade, automatic gameplay purchase or compensating reward was added.

The retained older unfunded test uses Capacity-first purchasing; the new balanced comparison uses cheapest-next. R3's historical 8 + 9 count must not be mixed with the new rate-12 balanced 9 + 9 baseline. Within each candidate comparison, input/purchase policies are held identical.

### Savings, persistence and retry

Journey exports separate `earnedCoins`, `distanceCoins`, `obstacleCoins`, `balanceBefore`, `balanceAfterBank`, purchases/costs, `balanceAfterPurchases` and post-evolution wallet. Every attempt asserts **balanceAfterBank = balanceBefore + earnedCoins** and that repeated banking leaves the total unchanged.

The focused save/ledger counterfactual starts with **375 saved**, banks **139 new**, reloads **514 total**, and preserves levels, resources and receipts. This does not recalculate the real player's already banked runs. Live UI coverage starts at zero: 97 earned / 97 Workshop balance, Capacity costs 50, 47 remains, Power is unaffordable and rejected, a 30-Energy attempt starts, and its new earnings bank once. Saved versus earned income is explicit.

The existing ledger snapshots the reward for each new attempt. Active/completed ledgers keep their original payout snapshot. Save format/migration, balances, purchased history, ownership, Copper/advanced resources, durable bank receipts, retry and temporary QA isolation are unchanged. Existing actual disk roundtrip/backup/failure tests remain; test stores use memory or unique OS-temp paths, never the real persistent profile.

### All-form compatibility and limitations

EVO 3-8 stats/prices and gameplay are byte-preserved. The global existing ledger now pays six distance Coins for them too. No new resource source, advanced region or later price change is introduced.

| Representative later profile | Old -> new balanced Coins | New distance + obstacle Coins |
|---|---:|---:|
| Industrial base | 581 -> 353 | 228 + 125 |
| Heavy intermediate | 774 -> 462 | 312 + 150 |
| Hydraulic intermediate | 822 -> 486 | 336 + 150 |
| Crystal intermediate | 870 -> 510 | 360 + 150 |
| Fusion intermediate | 1005 -> 615 | 390 + 225 |
| Quantum intermediate | 1090 -> 670 | 420 + 250 |

Industrial's cheapest first purchase is 400 Coins. A zero-balance base Industrial needs roughly two measured Yard runs at 328-391 Coins; legitimate evolution savings can shorten this. EVO 4-8 economics remain **provisional**: for example Quantum's cheapest upgrade is 4,600 versus ~670 intermediate Coins, so several Yard attempts are needed and the missing regions cannot supply later resources yet. No full advanced progression balance claim is made before M9. All eight forms remain playable; exact matching per-profile gameplay checks separate this economy change from pacing changes.

### Editor integration and files

**Tools > DrillRunner > Setup M8-R4 Coin Economy** reuses `UltraShortRunSetup` / `RapidRunSetup`. The guarded revision **7** migration accepts known 12/6 distance reward settings at 5 m / multiplier 25, rejects unfamiliar custom reward tuning, and updates only CoinsPerPayout and the revision. Healthy reruns preserve subsequent owner Inspector tuning. No new manager, scene edit, YAML patch or manual wiring. Existing Fresh Pocket and temporary-stage controls remain.

R4 changes:

- `Assets/_DrillRunner/Editor/UltraShortRunSetup.cs`: economy migration/menu and selected constant.
- `Assets/_DrillRunner/ScriptableObjects/RapidRunBalance.asset`: **CoinsPerPayout 12 -> 6; Revision 6 -> 7**, via Unity Editor APIs; all other fields unchanged.
- `Assets/_DrillRunner/Tests/EditMode/UltraShortRunTests.cs`: intentional configured-reward expectation.
- New `Assets/_DrillRunner/Tests/EditMode/EconomyRebalanceTests.cs` and metadata: configured payout, boundaries, actual material rewards, counterfactuals, saved balances/receipts, prices and gates.
- New `Assets/_DrillRunner/Tests/PlayMode/EconomyRebalancePlayModeTests.cs` and metadata: candidates, fresh journeys, affordability, source/balance separation and UI/retry.
- `RapidRunPlayModeTests.cs`: actual DistanceCoins/ObstacleCoins in its test-only export. Existing `RapidRunIntegrationTests.cs`, `RapidRunProgressionTests.cs`, `UltraShortRunPlayModeTests.cs`, `EarlyUpgradePacingTests.cs`: separate R4 export paths preserve R3 evidence; only economy-dependent progression bounds/workload updated.
- This document and README: current economy and executed validation.

All runtime source, production scene, stat/price assets, encounters, models, prefabs, art, audio, VFX, Packages and ProjectSettings remain unchanged. The exact balance-asset scope is checked by reconstructing the two original fields **in memory only** and comparing its task-start SHA-256.


### Full candidate progression distributions

All 48 journeys completed through the normal transactions. Ranges below span three families per rate/strategy; zero-affordable excludes already-MAX waiting, and savings are after the second evolution's Copper cost. No forced purchase restriction or budget edit is involved.

| Rate | Strategy | To EVO 2 | Additional to EVO 3 | Zero-affordable attempts | Multiple-purchase attempts | Final Coin savings |
|---|---|---:|---:|---:|---:|---:|
| 12 | Safe | 10 | 9-10 | 0 | 9 | 2360-3152 |
| 12 | Balanced | 9 | 9 | 0 | 8-9 | 615-677 |
| 12 | Power | 10 | 8-9 | 0 | 8-9 | 85-588 |
| 12 | Resource | 8 | 8-9 | 0 | 10-11 | 22-486 |
| 6 | Safe | 16-17 | 17-18 | 3 | 3-4 | 1346-1414 |
| 6 | Balanced | 14 | 14 | 2-3 | 4-5 | 21-140 |
| 6 | Power | 14-15 | 13 | 2 | 4-5 | 33-255 |
| 6 | Resource | 15 | 17-18 | 5-6 | 2-4 | 84-284 |
| 7 | Safe | 14-16 | 14-17 | 1-3 | 4-5 | 1594-2007 |
| 7 | Balanced | 12-13 | 13 | 1 | 4-6 | 36-299 |
| 7 | Power | 13 | 12 | 0-2 | 5-6 | 284-487 |
| 7 | Resource | 13 | 15 | 2 | 4 | 235-243 |
| 8 | Safe | 13 | 14 | 0-1 | 7-8 | 1842-2022 |
| 8 | Balanced | 12 | 11-12 | 0-1 | 5-6 | 20-380 |
| 8 | Power | 11-12 | 11-12 | 0 | 6-7 | 198-338 |
| 8 | Resource | 11 | 14 | 1 | 6 | 370-380 |


### Actual Unity tests, graphics and setup verification

Unity **6000.3.8f1** executed the complete suites sequentially, with Direct3D11 enabled for PlayMode:

- **196/196 EditMode passed, 0 failed, 0 skipped**, exit **0**, **23.73 s**. XML/log: `Logs/M8R4-EditMode.xml` / `.log`.
- **95/95 PlayMode passed, 0 failed, 0 skipped**, exit **0**, **757.09 s**. XML/log: `Logs/M8R4-PlayMode.xml` / `.log`.
- All **182 previous EditMode** and **92 previous PlayMode** case names remain. **14 EditMode** and **3 PlayMode** cases were added; no case removed or skipped. Only intentionally economy-dependent configured values and progression bounds/workload were revised.
- The full run completed **624 static comparisons**, **1,205 unfunded attempts / 48 complete journeys**, the original 300-profile / 520-combination / 144 isolated-upgrade sweeps, plus route, retry, save, banking, presentation and temporary-profile regressions. No production gameplay/test source changed after these final suites.
- All **300 matched production samples** have exactly identical non-Coin gameplay metrics to the preserved R3 CSV (duration, distance, Energy, contacts, Copper, etc.). The new candidate comparisons separately assert invariant obstacle rewards and exact distance payout arithmetic.
- The aggregate journey case uses a **600-second test-only timeout**. Initial candidate execution passed the static comparison but timed out the journey case at the old 180-second runner limit near the last 8-Coin family (1 pass / 1 timeout). The complete case subsequently passed in the full suite. Initial EditMode execution was 195 pass / 1 fail because the new save fixture incorrectly specified legacy FormSchema 0 while expecting no existing migration to 1; explicitly declaring current schema 1 corrected that fixture. Production migration code was unchanged. Initial XML/logs and initial measurements are preserved separately.
- Setup was applied through the Editor menu method and exited **0**. The retained full Assets snapshot test invokes setup twice and verifies unchanged file/metadata bytes and no added/deleted files on reruns. Custom scene work, scene references, prices, source assets and subsequent authoring tuning remain protected. No manual wiring.

The retained Capacity-first production journeys now measured: 10300: **13 + 14 attempts**; 8100: **14 + 13 attempts**; 9200: **13 + 14 attempts**. These are separate from the cheapest-next balanced strategy comparison above.

Graphics-enabled normal-Update runs and **540x960 (9:16)** captures executed. Inspected the new Fresh Results, Fresh Workshop, Purchased/Unaffordable, Next Results and Next Workshop frames: **97 earned -> 97 wallet -> 50 spent -> 47 wallet -> 115 newly earned -> 162 wallet**. First Capacity is 25 -> 30, the unaffordable Power purchase is disabled/rejected, Start returns to normal gameplay, and replay/receipt assertions pass. Screens remain readable without new clipping in inspected frames. These isolated test-store images say "Run rewards saved" because they use the normal memory-backed transactions; they do not involve the real player's save. No UI redesign, generated artwork or audio edit. No device-performance or listening claim.

Visual evidence: `Logs/M8R4-Economy-Fresh-Results.png`, `M8R4-Economy-Fresh-Workshop.png`, `M8R4-Economy-Purchased-Unaffordable.png`, `M8R4-Economy-Next-Results.png`, `M8R4-Economy-Next-Workshop.png`; existing natural episode frames are `M8R4-Evo*-L*-*.png`.

Reproducible exports: `M8R4-EconomyCandidates.csv`, `M8R4-EconomyJourneys.csv`, `M8R4-Policies.csv`, `M8R4-CapacityAndCombinations.csv`, `M8R4-EvolutionJourneys.csv`, `M8R4-Upgrades.csv`, `M8R4-RouteCosts.csv`, `M8R4-NaturalEpisodes.csv` under ignored `Logs/`. `M8R4-ValidationSummary.json` records final case preservation and metric comparison. Run EditMode, then graphics-enabled PlayMode to regenerate them. Earlier R3 evidence and initial R4 measurements are retained separately.

### Three focused owner checks

1. Open **Gameplay**, enter Play, select **M4 Workshop > WorkshopController**, and use **QA: Fresh Pocket - 0 upgrades (no disk writes)**. Charge/launch, drill or avoid the first Wood. Expect the accepted ~7-9-second pacing and approximately 97 Coins for a 60-65 m / one-Wood run; charge, route and seed vary income. Verify one chosen first purchase is meaningful.
2. Buy Capacity, try an unaffordable upgrade, then Start/Retry twice. Check the wallet subtracts only successful prices and adds each completed attempt's new Coins/Copper once. Multiple purchases remain valid when accumulated savings afford them. Earned Results and accumulated Workshop balance should differ correctly.
3. Continue the temporary progression or use existing temporary EVO 2 controls to judge mining versus bypass, Copper and compact retry flow. Expect roughly 14 attempts to Workshop for balanced play, then about 13-18 depending on strategy. Leave Play to restore the real profile. Human satisfaction and unmodeled routes remain owner review.

### Preservation and Git status

All **771 task-start source files retained**, no missing files. All **679 protected files** are byte-unchanged, including runtime source, production scene, all eight stat/price assets, encounters, prefabs/models/art/audio/VFX, existing metadata, Packages and ProjectSettings. The balance asset's only field differences are **CoinsPerPayout 12 -> 6** and **Revision 6 -> 7**, verified by reconstructing the prior values in memory and matching its original SHA-256. No asset YAML was manually edited. Full changed/new paths, GUID audit and before/after inventories: `Logs/M8R4-Preservation.json`, `M8R4-Git-Before.txt`, `M8R4-Git-After.txt`.

Branch **main**, unchanged HEAD **b9cdf9b536dcee30efe7dfa14752bee6f2f4b772**. Final status **37 modified / 216 untracked / 0 staged**, including all existing M8/M8-R/R2/R3 changes (start 37 / 212 / 0). R4 changes **10 existing files** and adds **4 source/metadata files**. No commit, push, reset, revert, discard, Android work, M9, region/resource invention or gameplay redesign. **Stop after M8-R4 for Project Owner review.**
