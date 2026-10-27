# M4 — Workshop, upgrades, and persistent progression

Implemented against accepted M3 commit `ceca8fd` on `main`. Before edits, M3 was committed and the working tree was clean. No commits or pushes are part of this task. M5 Evolution, new forms, regions, resources, and Android builds are not included.

## Files and ownership

Paths below are relative to `Assets/_DrillRunner/`.

| Created | Purpose |
| --- | --- |
| `Runtime/PlayerProgress.cs` | Versioned first-form profile, validation, immutable-by-copy snapshots, JSON integrity envelope |
| `Runtime/ProgressionConfig.cs` | Pocket Drill level/price tables and effective attribute calculations |
| `Runtime/ProgressStore.cs` | Explicit-directory disk store and isolated memory store |
| `Runtime/ProgressionService.cs` | One permanent purchase, attempt-receipt, and banking authority |
| `Runtime/WorkshopController.cs` | Existing run lifecycle integration, navigation, pending banking, attribute application |
| `Runtime/WorkshopView.cs` | Portrait Workshop, balances, upgrades, MAX/affordability, save status, navigation |
| `Editor/WorkshopSetup.cs` | One idempotent M4 setup command using Editor APIs |
| `Editor/WorkshopInspector.cs` | Save diagnostics, confirmed backup recovery, temporary funded development profile |
| `ScriptableObjects/PocketDrillProgression.asset` | Tunable first-form upgrade configuration |
| `Tests/EditMode/ProgressionTests.cs`, `WorkshopSceneTests.cs` | Persistence/economy/configuration and setup validation |
| `Tests/PlayMode/WorkshopPlayModeTests.cs`, `ProgressionTestIsolation.cs` | Gameplay/Workshop integration and save isolation for all PlayMode fixtures |

Modified: `DrillMotor.cs` adds between-run attribute application, validated movement recapture on reset, and a pending-bank reset guard. `RunProgress.cs` publishes attempt-created and fully-frozen completion events. `PrototypeHud.cs` adds current Power/Energy and completed Retry integration. `AssemblyInfo.cs` exposes an internal test-injection seam to the two test assemblies. Existing motor/seed Inspector tools identify development resets and prevent temporary power overrides from banking permanent rewards. `Gameplay.unity` receives Workshop wiring and UI. Root `README.md` is updated; this report is new. Unity generated companion metadata and serialized scene/configuration data.

The M3 ledger, movement mathematics, swept contacts, hardness/material data, energy consumption, track catalog/sections, input bindings/adapter, camera, original prefabs, all earlier tests, package files, project settings, and assembly definitions remain unchanged.

## Workshop and gameplay flow

Workshop uses the existing HUD Canvas and a simple in-scene panel. No second player, camera, input handler, run tracker, or persistent singleton is created. CanvasGroups hide gameplay UI without moving existing HUD objects from the hierarchy paths expected by earlier setup tools. The panel blocks pointer raycasts; gameplay input is disabled while it is open. Current drill presentation is a basic UI placeholder.

Normal flow is **Workshop → Start Run → charge/release gameplay → RunEnded/results → Workshop or Retry**. Start Run makes the upgraded attempt Ready; it preserves the existing charging gesture. Player builds enter Workshop. The serialized **Editor Gameplay Entry** option defaults on to retain direct M0–M3 testing; click WORKSHOP in Ready/results, or turn that option off outside Play for Workshop-first Editor entry.

Purchasing is accepted only in Workshop between attempts. Rows display current/max level, effective value, next value, next Coin price, disabled unaffordable buttons, and MAX. Start Run and completed Retry apply all purchased stats immediately without restarting Unity. New attempts use normal independent M3 random seeds; fixed-seed replay is still available through the existing RunProgress Inspector.

The existing public HUD/input Restart is a development reset: **during an active attempt it discards unbanked earnings**. The visible Retry/QA Reset button uses progression-aware Retry after completion. Both preserve permanent balances. A pending failed bank blocks every motor reset, including the development API, until saving succeeds.

## Upgrade configuration and balance

Configuration: `ScriptableObjects/PocketDrillProgression.asset`. Values are captured in a private Play-session copy; edit the source outside Play. All three categories have levels **0–5**. Costs below buy levels 1–5. Current level is saved in the profile; values are derived from validated tables rather than independently saved floats.

| Category | Values at levels 0, 1, 2, 3, 4, 5 | Coin costs | Total |
| --- | --- | --- | --- |
| Drill Power | 5, 6, 7, 8, **9**, 10 | 60, 100, 160, 240, 340 | 900 |
| Energy Capacity | 100, 115, 130, 145, 160, 180 | 50, 90, 140, 210, 300 | 790 |
| Drive cruise speed | 10, 10.4, 10.8, 11.2, 11.6, 12 | 50, 90, 140, 210, 300 | 790 |

Total first-form maximum milestone: **2,480 Coins**. Initial Coins/Copper/levels are zero. Early upgrades cost 50–60 Coins (250–300 distance units before obstacle bonuses), supporting visible improvements after practical early runs. Final economy balance is deferred.

Drive minimum boost is **2, 2.2, 2.4, 2.6, 2.8, 3**; maximum boost stays **12**. Post-contact acceleration is **12, 14, 16, 18, 20, 22**. Cruise never exceeds 12 and full-charge speed never exceeds the existing 24-unit ceiling. Steering and track safety remain unchanged and certified for base power 5/max speed 24.

Damage remains **Drill Power × 10 per second**, with the existing strict power ≥ hardness condition. Power 5 breaks Wood in 1.4 seconds; Power 9 takes about 0.778 seconds. Stone hardness stays **9**: level 4 Power 9 breaks its 240 durability in about **2.667 seconds**, consuming about **48 drilling Energy**. Drive alone cannot drill Stone. Capacity changes only starting maximum Energy; consumption rates are unchanged and never stacked.

## Authoritative transactions and banking

M3 `RunRewards` owns all temporary distance, Coins, and Copper. The permanent service owns available Coins, Copper, upgrade levels, profile identity, save version/revision, and attempt receipts. UI never maintains independent currency or purchase rules. Copper is display-only inventory with no M4 spending behavior.

Each new attempt reserves a monotonically increasing persisted ID before accepting earnings. On the authoritative RunProgress completion event, after the M3 ledger is frozen, the controller captures that ledger and commits its Coin/Copper totals together with the banked receipt in one transaction. It marks the run banked only after success. Duplicate completion, old receipts, repeated Retry, and returning to Workshop cannot pay again. A later attempt cannot revive an older abandoned receipt.

A purchase validates category, level, next price, affordability, profile validity, and persistence. The service clones a candidate, deducts Coins/increments only that level, validates it, then saves. Only after success does it publish the new snapshot and refresh UI. Main-thread rapid clicks are processed sequentially against the latest committed state. Failure leaves balances, levels, and revision unchanged. Checked arithmetic prevents balance/revision/attempt-counter overflow. Negative/invalid rewards cannot enter balances.

On a failed run bank, the frozen summary remains available and **RETRY SAVE** is shown in results/Workshop. Purchases, new runs, and all resets remain blocked until the transaction succeeds. No late destruction event alters the completed M3 ledger. Partial drilling grants no reward; Copper grants Copper only, preserving M3 rules.

## Local persistence, schema, and recovery

The sole production path is **`Application.persistentDataPath/drillrunner-progress.json`**. The live **M4 Workshop** Inspector displays the exact path for the platform. The store always requires an explicit absolute directory, so tests inject memory or a unique OS-temp directory instead of using the production path.

Version **1** JSON has a versioned envelope containing `Payload` JSON and a SHA-256 `Checksum`. The payload contains `Version`, `ProfileId`, stable `FormId: pocket_drill`, `Revision`, `Coins`, `Copper`, `PowerLevel`, `EnergyLevel`, `DriveLevel`, `IssuedAttempt`, and `BankedAttempt`. A future form/schema can be introduced by explicit migration; unsupported versions/forms are rejected instead of guessed or reset. No speculative other-form records exist.

Writes use a unique same-directory staging file, flush data to disk, atomically replace the primary, and retain the previous primary in `.bak`. An exclusive `.lock` file covers compare-and-replace. The original loaded content is compared before writing, rejecting stale sessions/external edits. Candidate state becomes visible in memory only after replacement succeeds. Ordinary staging files are removed on success/failure; an abrupt process/power failure may leave a preserved orphan staging file. The primary/backup remain authoritative. SHA-256 detects accidental corruption, not malicious tampering; no encryption/anti-cheat is claimed.

Missing primary **and** backup initialize valid zero defaults. Malformed, truncated, oversized, invalid-balance/level, unsupported-version/form, or inaccessible data cannot be silently overwritten. When a primary cannot decode, a decodable backup can be displayed read-only; semantic validation still applies. Without usable data the UI displays safe defaults read-only with an explanatory status. No automatic downgrade or destructive reset occurs.

In the Editor, open Workshop and use **Recover previous valid save revision**, then explicitly confirm. The backup must pass current schema/configuration validation. The rejected primary is copied unchanged to `.rejected-<unique id>` before restoration. This intentionally loses at most the latest valid transaction when recovering an ordinary backup; newer unsupported saves should normally be opened with a compatible game. No production recovery/reset UI, multi-slot system, cloud save, account, or database is included.

## Development QA and setup

Select **M4 Workshop** in Play Mode to inspect the actual save path, balances, levels, and status. **QA: Temporary funded profile (no disk writes)** requires confirmation and switches only this Play session to an isolated 10,000-Coin profile. It retains current upgrade levels, supports all purchases and gameplay, and writes no subsequent transactions to the real profile. Leave Play to restore normal disk-backed progress. M2 power/energy override attempts cannot bank permanent rewards or save override levels.

Executed **Tools → DrillRunner → Setup M4 Workshop** in Unity 6000.3.8f1. No manual reference assignments remain. Reruns validate existing configuration/references and preserve tuning and scene bytes; dirty loaded scenes are refused. One M4 setup menu item exists. Earlier setup commands remain functional. Scene/configuration changes were authored through Unity Editor APIs; no YAML was hand-edited.

Batch setup uses `-batchmode -nographics -quit -projectPath <project> -executeMethod DrillRunner.Editor.WorkshopSetup.Setup -logFile Logs/M4-Setup.log`. Test commands follow README, using `Logs/M4-EditMode.xml` and `Logs/M4-PlayMode.xml` outputs.

## Executed validation

Actual Unity Test Framework runs, Unity **6000.3.8f1**, Windows Editor batch mode (graphical Direct3D11 for PlayMode):

| Check | Result |
| --- | --- |
| M4 compilation and automated scene/configuration setup | Passed, exit 0 |
| Actual setup rerun, all 221 asset/source/meta SHA-256 comparisons | Passed; every file unchanged |
| EditMode, complete assembly | **92 passed, 0 failed, 0 skipped** |
| PlayMode, complete assembly | **38 passed, 0 failed, 0 skipped** |
| Earlier M0–M3 tests | **All 71 EditMode + 29 PlayMode unchanged and passing** |
| New M4 tests | **21 EditMode + 9 PlayMode passing** |
| Real temporary-directory JSON save/reload, receipt/level restoration | Passed |
| Corruption/unsupported schema, backup recovery/archive, stale writes, lock contention | Passed |
| Purchase validation, rapid clicks, MAX, numeric/counter overflow, failure rollback | Passed |
| Actual purchased Power 9 Stone destruction, faster Wood, increased Energy, controlled Drive | Passed |
| Generated Copper, once-only banking, post-completion duplicates, results/Retry/Workshop | Passed |
| Failed purchase/bank, frozen pending ledger, reset guard, retry save | Passed |
| Corrupt profile opens a read-only Workshop; start/purchases preserve original bytes | Passed |
| Temporary development funding and power overrides cannot modify disk-backed progression | Passed |
| Repeated navigation/cleanup and earlier accelerated >30 km streaming regression | Passed |
| Four 540×960 GPU captures: Workshop, all MAX, saved results, Power 9 drilling Stone | Generated and visually inspected |
| Static source/JSON checks, complete `.meta` coverage, unique GUIDs, original-file preservation | Passed |

Evidence is local and ignored by Git: `Logs/M4-Setup.log`, `M4-Setup-Repeat.log`, `M4-EditMode.log/.xml`, `M4-PlayMode.log/.xml`, `M4-Workshop.png`, `M4-WorkshopMax.png`, `M4-BankedResults.png`, `M4-StonePower9.png`, and `M4-Baseline.json`.

The original-file hash audit covers 230 tracked baseline files: exactly the eight expected implementation/documentation files changed, with no deletions. All prior tests, original metadata, prefab/material/track assets, input, packages, and project settings are unchanged. All 102 owned asset/folder entries have companion metadata; all 122 asset GUIDs are unique. Source/JSON validation is separate from the actual Unity test results above.

Final Git state: `main`, HEAD `ceca8fd`; **8 modified, 27 untracked, 0 staged**. New-file totals include Unity-generated `.meta` files and this report.

## Limitations and checks not executed

- No Android/player build, physical touchscreen QA, performance profiling, real-time endurance run, physical Inspector-button/confirmation clicks, or manual close/reopen of a standalone executable. File-store reconstruction and Gameplay scene reloads test persistent state restoration automatically.
- Save writes are synchronous but small and limited to attempt reservation, completed banking, and purchases. Target-device latency/power-loss durability still needs platform QA. Atomic replacement was exercised on this Windows/.NET environment; other platform filesystems were not exercised.
- A crash/force quit during an active attempt discards that attempt's unbanked earnings. If storage remains unavailable and the application is forcibly closed while banking is pending, its in-memory completed ledger cannot be recovered automatically. Banked transactions are durable and duplicate-safe; there is no active-run resume/journal system.
- Corrupt/unsupported data is preserved read-only. Recovery requires a valid compatible backup and the confirmed development tool; no production recovery flow or historical backup browser is included. Orphan staging/rejected files are preserved for manual investigation rather than automatically deleted.
- M3's static axis-aligned obstacles, conservative authored route certificates, spawn/despawn cadence, and world-float precision limits remain. No floating origin or large pooling framework is introduced.
- Upgrade prices and UI presentation are prototype values. The profile contains only Pocket Drill and Copper; Evolution and additional forms/resources are deferred to M5.

## Minimum manual Unity QA

1. Open Gameplay, choose **Portrait 9:16**, press Play, then **WORKSHOP**. Check three rows, initial/current balances, next benefits/prices, and disabled unaffordable buttons. Production players enter Workshop directly; Editor direct entry remains convenient for older QA.
2. Complete a normal run, drilling teal Copper if possible. Confirm frozen results and **Run rewards saved**, then open Workshop. Retry/return repeatedly and confirm the same earnings are not banked again. Leave Play and re-enter to confirm real balances/levels reload. Active-run QA Reset intentionally discards earnings.
3. For fast upgrade QA, select M4 Workshop and confirm **QA: Temporary funded profile**. Buy Power to level 4 (9), Energy, and Drive; check balances/next values and MAX at level 5. Start Run and confirm Power/current-max Energy on HUD, improved drive, faster Wood drilling, and Stone drillable at Power 9. Copper remains unspent. Leave Play to discard the sandbox.
4. Re-enter Play and confirm sandbox purchases did not change real progress. In real progression, purchase one affordable upgrade after earning Coins, leave/re-enter Play, and confirm it remains owned and applies on the next Start Run. Check Workshop → Start → results → Retry/Workshop navigation and one player/camera throughout.

M4 stops here for review. No commit or push was performed; M5 is not started.
