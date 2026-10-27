# M5 — first evolution and the complete progression loop

Implemented on `main` against the approved M4 checkpoint **`3aec208`**. The checkpoint was present and the working tree clean before changes. No commit/push, M6, additional regions, or EVO 3–8 content is included.

## Files and assets

Paths below are relative to `Assets/_DrillRunner/`.

| Created | Purpose |
| --- | --- |
| `Runtime/EvolutionEligibility.cs` | One configuration-driven result for requirements, permission, readiness, and UI explanation |
| `Runtime/DrillVisuals.cs` | Visual-only form switching under the existing motor |
| `Editor/EvolutionSetup.cs` | Single M5 setup command; authors models, previews, progression asset, and UI through Editor APIs |
| `ScriptableObjects/WorkshopDrillProgression.asset` | Workshop Drill metadata, visuals, stats, costs, and limits; no next form |
| `Prefabs/VisualPocketDrill.prefab`, `VisualWorkshopDrill.prefab` | Authored visual-only models; the Pocket copy preserves the original visual design |
| `Materials/WorkshopHull.mat`, `WorkshopHousing.mat`, `WorkshopAccent.mat` | Teal, navy, and copper/gold prototype materials |
| `Textures/PocketDrillPreview.png`, `WorkshopDrillPreview.png` | Static Editor renders of the actual model prefabs |
| `Tests/EditMode/EvolutionTests.cs`, `EvolutionSceneTests.cs` | Eligibility, transactions, migration, history, recovery, configuration, geometry, and setup checks |
| `Tests/PlayMode/EvolutionPlayModeTests.cs` | Evolution UI/lifecycle, interruption/reload, real attribute integration, safety, isolation, and an unfunded earning journey |

Modified: `PlayerProgress.cs` gains additive form history/counters; `ProgressionConfig.cs` describes the active and next tier; `ProgressionService.cs` owns evolution and active-tier purchasing; `WorkshopController.cs` integrates presentation and form selection; `WorkshopView.cs` displays form previews/requirements/new tier; `DrillMotor.cs` captures the form's bounded speed ceiling between runs; the existing Workshop Inspector adds configurable temporary Copper. `Gameplay.unity`, the existing Pocket configuration metadata, and root README are updated. This report is new. Unity generated all companion metadata and serialized scene/prefab/configuration changes.

All earlier test files, original drill/obstacle/section prefabs, materials/hardness, M1 movement math, contact solver, input adapter/bindings, camera, energy rules, M3 registry/streaming/reward ledger, `ProgressStore.cs`, packages, project settings, and assembly definitions remain unchanged. Pocket prices/values are preserved.

## Two forms and effective attributes

The existing Pocket configuration is the root. Its `NextForm` references the only new Workshop configuration. Stable IDs are `pocket_drill` and `workshop_drill`; each has a display name, visual prefab, preview texture, upgrade tables, and bounded drive tuning. No other form definitions are created.

All categories have **five purchases**, levels 0–5. Values below are absolute active-tier values, avoiding duplicate bonuses. Pocket levels remain saved at MAX after evolution. Separate Workshop counters begin at zero. `ProgressionConfig.Calculate` is the single effective-attribute calculation used by the service, gameplay initialization, Retry, and tests; Workshop displays those same active-tier tables.

| Category | Pocket levels 0–5 | Workshop levels 0–5 |
| --- | --- | --- |
| Drill Power | 5, 6, 7, 8, 9, 10 | **12**, 13, 14, 15, 16, 18 |
| Maximum Energy | 100, 115, 130, 145, 160, 180 | **200**, 220, 240, 260, 280, 300 |
| Cruise speed | 10, 10.4, 10.8, 11.2, 11.6, 12 | **12.2**, 12.35, 12.5, 12.65, 12.8, 13 |
| Minimum launch boost | 2, 2.2, 2.4, 2.6, 2.8, 3 | **3.25**, 3.4, 3.6, 3.8, 4, 4.25 |
| Maximum launch boost | 12 at every level | **12.25**, 12.4, 12.5, 12.6, 12.75, 13 |
| Recovery acceleration | 12, 14, 16, 18, 20, 22 | **24**, 26, 28, 30, 32, 34 |
| Speed ceiling | 24 | **26** |

Validation rejects nonfinite/nonpositive values, decreasing tiers, invalid costs, unsupported form chains, and a next tier that loses any acquired attribute. Workshop base Power must be strictly better than max Pocket Power. Every transaction revalidates configuration before committing; invalid live data cannot turn a negative Copper cost into a payout.

| Upgrade | Pocket Coin prices | Workshop Coin prices |
| --- | --- | --- |
| Drill Power | 60, 100, 160, 240, 340 | 150, 230, 340, 480, 650 |
| Energy | 50, 90, 140, 210, 300 | 130, 210, 310, 440, 600 |
| Drive | 50, 90, 140, 210, 300 | 130, 210, 310, 440, 600 |

Pocket maximum total remains **2,480 Coins**. Workshop maximum total is **5,230 Coins**. Evolution has **no Coin fee**. Upgrade/evolution values remain tunable prototype balance.

Existing damage remains Power × 10/second with strict Power ≥ hardness. Stone hardness stays **9**. Max Pocket breaks 240-HP Stone in 2.4 seconds; base Workshop takes 2 seconds, and max Workshop about 1.333 seconds. Base Workshop breaks Wood in about 0.583 seconds versus 0.7 at max Pocket. Energy consumption rules are unchanged.

## Eligibility, transaction, and presentation

Pocket → Workshop requires **all three current-form categories at MAX** plus **12 Copper**, configurable on the Pocket asset. Twelve Copper means four fully destroyed Ore rewards at the unchanged 3-Copper payout. The authoritative eligibility result includes each category, Copper availability, a valid next form, context permission, and a readable requirement explanation. The UI consumes it; it does not implement separate requirements.

The existing `ProgressionService.TryEvolve` rechecks eligibility, clones the profile, deducts Copper, changes FormId, initializes Workshop counters, validates, and uses the existing atomic commit. Only after persistence succeeds is the new snapshot published. Coins, profile identity, Pocket history, and banking receipts remain intact. Failure leaves every field and the visible form unchanged. Following success, Workshop has no next form, so duplicate clicks/callbacks cannot repeat the transaction or spend Copper again.

Controller permission requires Workshop open, Ready/RunEnded, writable progress, no pending bank, and no evolution presentation. Moving, Drilling, Blocked, out-of-Workshop, read-only, and transition requests are rejected. Purchases, Start Run, QA funding/overrides, and the motor reset guard are locked during presentation.

After the successful save, a **0.9-second unscaled** preview pulse and assembly message run, the visual switches, and **WORKSHOP DRILL UNLOCKED** confirms completion. New level-0 rows and Coin costs appear. The save does not depend on animation completion. Disabling skips presentation state without touching possibly destroyed UI; enabling refreshes from committed state. Scene/application reload restores the saved form directly without replaying a purchase or needing an animation flag.

Workshop → Start Run → charge/release Gameplay → Results → Retry/Workshop remains the flow. Start/Retry apply the loaded active-form attributes and visuals on the existing motor. Editor direct gameplay entry remains available. Active-run QA Reset still discards unbanked attempt earnings; completed Retry preserves permanent balances. Durable completed-run banking and pending-save reset protection remain operational.

## Model switching, collision, and portrait UI

`DrillVisuals` uses the original hero's `Visuals` child for Pocket, then lazily instantiates one Workshop visual child. It reuses that child on Retry; only one visual is active. The Workshop model has a wider reinforced hull, larger motor housing, five-step bit, cooling vents, and teal/navy/copper accents. Neither visual prefab contains colliders, rigidbodies, scripts, input, or cameras.

The original motor, tip, and logical collider remain **1.7 wide × 1.3 high × 3 long**, centered at `(0, 0.65, 0.35)`. Decorative model geometry stays within X ±0.85 and Z -1.15–1.85 and above the floor; a taller housing does not change collision. The root is never scaled for evolution. Only the Workshop preview image pulses. The M2 axis-aligned swept solver remains unchanged.

Workshop reuses the existing panel, balance row, three upgrade cards, and Start button. Cards are slightly more compact to fit one requirement panel and EVOLVE at 9:16. Static rendered previews use the same model assets, adding **no runtime preview camera**. After evolution the completed requirement panel offers no EVO 3 action; normal Workshop upgrades remain available.

The conservative 26-unit maximum is tested against all 49 catalog transition pairs, a thin Stone obstacle, strict hardness at temporary power 5, Blocked escape, and portrait camera framing at both lateral bounds. M3's known-safe fallback and base-power-5 route certificates are unchanged.

## Save compatibility and recovery

The path and atomic storage implementation are unchanged: `Application.persistentDataPath/drillrunner-progress.json`, checksum envelope/payload **Version 1**, exclusive writer lock, flushed same-directory staging, atomic replacement, previous-revision `.bak`, stale-writer rejection, and confirmed recovery/archive behavior. No player save was read or reset by setup, and automated tests use isolated stores.

M5 extends version 1 additively with `FormSchema`, `WorkshopPowerLevel`, `WorkshopEnergyLevel`, and `WorkshopDriveLevel`. Old valid M4 profiles with absent/zero extension load as Pocket with new counters zero. The service validates old data, normalizes FormSchema to **1 in memory**, and preserves Coins, Copper, original upgrade fields, ProfileId, Revision, IssuedAttempt, and BankedAttempt. Loading alone does not rewrite the file; the next normal successful transaction writes the extension and retains the old bytes as backup.

An evolved profile requires FormSchema 1, supported Workshop ID, complete retained Pocket history, and valid Workshop levels. Pocket profiles cannot contain nonzero Workshop counters. Unknown forms, schema extensions, excessive/negative levels, negative balances, unsupported envelope versions, and malformed data stay read-only with original files preserved. The authentic M4 fixture test constructs the actual old payload without any new fields, rather than using the M5 encoder.

Committed evolution restores Workshop immediately after interruption. An uncommitted orphan staging file cannot override the valid canonical primary or charge Copper; it remains preserved for investigation. Existing checksum/backup recovery behavior remains covered, including evolved backups. Explicit backup recovery can intentionally roll back the latest transaction; it is not an automatic downgrade. An M4 binary cannot use an evolved ID and should not be used to continue an evolved profile.

## Economy validation

The automated guided journey starts from **zero Coins, zero Copper, zero upgrades**, uses the real M3 section selection, continuous steering, M2 sweeps, damage, energy depletion, and M4 banking/purchases. It does not alter player position, obstacle activity, energy rates, source data, or rewards. It bypasses ordinary obstacles, targets at most one Copper Ore per run until 12 is banked, and buys affordable Energy, Drive, then Power upgrades. Risk/reward pairs are considered together using full-footprint clearances.

| Run | Distance | Earned Coins | Earned Copper |
| --- | --- | --- | --- |
| 1 | 1,174 m | 234 | 3 |
| 2 | 1,667 m | 333 | 3 |
| 3 | 2,113 m | 422 | 3 |
| 4 | 2,503 m | 500 | 3 |
| 5 | 2,990 m | 597 | 0 |
| 6 | 3,093 m | 618 | 0 |

Total **2,704 Coins + 12 Copper**, achieving all Pocket upgrades and evolution in **six completed runs**. After spending 2,480 Coins and 12 Copper, the test buys the first Workshop Power upgrade for 150 Coins and starts with **Power 13 / Energy 200**. No QA funding or production balance edits are involved. This meets the 5–8-run target for the seeded guided effective-player strategy; it is not a claim of measured ordinary-human pacing. Evidence: `Logs/M5-Economy.txt` and guided-run entries in the PlayMode log.

## Temporary QA profile and setup

The existing **QA: Temporary funded profile (no disk writes)** tool remains on the M4 Workshop Inspector. Choose **Temporary Copper** (default **24**, or **0** to test insufficient resources), confirm, and receive 10,000 temporary Coins plus that Copper. Current form/levels are copied; the store switches to memory before any attempt/purchase/evolution writes. No temporary transaction or bank reaches the real file. Leave Play to restore the original profile. The tool cannot run during an active run, pending bank, or transition.

Executed **Tools → DrillRunner → Setup M5 Evolution** through Unity 6000.3.8f1 Editor APIs. It authors two visual prefabs, three materials, two preview textures, one new form configuration, the requirement panel, visual component references, and adjusted card layout. There is one M5 setup menu action. No manual wiring remains. Dirty loaded scenes are refused; repeated setup preserves user work, GUIDs, tuning, and configured bytes. Initial preview generation needs graphical Editor execution; reruns with existing previews work without graphics. Earlier setup commands remain available.

Batch setup: `-batchmode -quit -projectPath <project> -executeMethod DrillRunner.Editor.EvolutionSetup.Setup -logFile Logs/M5-Setup.log`. Test invocations follow README with `Logs/M5-EditMode.xml` and `Logs/M5-PlayMode.xml` outputs.

## Executed validation

Actual Unity Test Framework execution in Unity **6000.3.8f1**, Windows Editor batch mode; graphical PlayMode used Direct3D11:

| Check | Result |
| --- | --- |
| Compilation and automatic M5 asset/scene setup | Passed, exit 0 |
| Complete EditMode assembly | **117 passed, 0 failed, 0 skipped** |
| Complete PlayMode assembly | **46 passed, 0 failed, 0 skipped** |
| Original M0–M4 tests | **All 92 EditMode + 38 PlayMode unchanged and passing** |
| New M5 tests | **25 EditMode + 8 PlayMode passing** |
| Eligibility, exactly-once Copper spending, failed-save rollback, old/new tier preservation | Passed |
| Authentic M4 migration without load-only rewrite; evolved reload and backup recovery | Passed |
| Interrupted presentation/reload, rapid clicks, gameplay permission, temporary funding isolation | Passed |
| Actual evolved drilling/stats, thin-obstacle sweep at 26, strict hardness, escape/framing | Passed |
| Unfunded complete earning/upgrading/evolution/new-tier purchase journey | Passed in **six completed runs**, 2,704 Coins + 12 Copper |
| Four 540×960 GPU renders: eligibility, unlocked Workshop, moving model, results | Generated and visually inspected |
| Actual setup rerun, every asset/source/meta hash | **All 249 unchanged**, exit 0 |
| Static source/JSON, complete metadata, unique GUIDs, original-file preservation | Passed |

The original-file SHA-256 audit covers **257 tracked baseline files**, finding exactly the ten expected implementation/configuration/documentation changes and no deletions. All original tests, prefab/material assets, input, storage implementation, packages/settings, assembly definitions, and metadata remain unchanged. All **116 owned asset/folder entries** have companion metadata; all **136 asset GUIDs** are unique. Static validation is separate from the actual Unity runs above.

Evidence is local and ignored by Git: `Logs/M5-Setup.log`, `M5-Setup-Repeat.log`, `M5-EditMode.log/.xml`, `M5-PlayMode.log/.xml`, `M5-Economy.log/.xml`, `M5-Economy.txt`, `M5-EvolutionReady.png`, `M5-WorkshopDrillUnlocked.png`, `M5-WorkshopDrillMoving.png`, `M5-WorkshopDrillResults.png`, and `M5-Baseline.json`. The moving capture includes the deliberate temporary Power 5 hardness/escape QA override; the evolved results capture shows normal base Power 12.

Final Git state: `main` at **`3aec208`**, **10 modified, 29 untracked, 0 staged**. New-file totals include Unity-generated metadata and this report.

## Limitations and checks not executed

- No Android/player build, physical touchscreen/device QA, performance profiling, real-time endurance, manual standalone close/reopen, or physical Inspector confirmation clicks. Automated tests reconstruct disk-backed services and reload Gameplay, including during presentation.
- Six-run pacing is a seeded guided simulation with effective steering and deliberate resource selection, not a human playtest. Prices, 12-Copper cost, model art, and presentation remain prototype values.
- The Workshop icon uses a static model render rather than live rotation or animation. There is no production transformation cinematic, final VFX, or sound.
- M4's synchronous file writes, filesystem/platform durability assumptions, in-memory pending-bank limitation on force quit, and recovery requirements remain. No active-run resume/journal, cloud storage, or anti-cheat is introduced. SHA-256 detects accidental corruption, not malicious editing.
- Changing tier limits/identifiers after shipping may require an explicit new migration; invalid histories are preserved read-only rather than guessed. Only the M4→M5 additive rules and the two authored tiers are supported.
- M3's static axis-aligned obstacle scope, conservative route certificates, spawn/despawn approach, and world-float precision limits remain. No pooling framework, floating origin, new region, or difficulty scaling is added.

## Exact minimum manual QA

1. Open Gameplay, choose **Portrait 9:16**, press Play, then **WORKSHOP**. Confirm existing real Pocket balances/levels are intact and incomplete evolution requirements are explained.
2. Select **M4 Workshop**, set **Temporary Copper = 0**, and confirm the temporary funded profile. Buy each Pocket category to MAX. Confirm EVOLVE remains unavailable and the panel reports insufficient Copper. Set Temporary Copper to **24** and confirm funding again; existing temporary MAX levels remain.
3. Click EVOLVE once. Confirm a short control lock, one **12-Copper** deduction, no Coin deduction, Workshop Drill appearance/name, new **0/5** rows, and no further evolution action. Buy one upgrade in each new category, then Start Run: expect **Power 13, Energy 220, cruise 12.35**, a different model, faster drilling, and unchanged steering/collision behavior. Complete/retry and return to Workshop; the evolved form and earnings should remain.
4. Leave Play and re-enter. Confirm the temporary evolution/purchases did not alter the original real profile. For normal permanent progression, use earned balances to satisfy requirements, evolve, then leave/re-enter Play to verify the evolved form and new-tier upgrades reload. Disk reload/migration/backup safety is already covered automatically in unique temporary directories.

M5 stops here for review. No commit or push was performed; M6 is not started.
