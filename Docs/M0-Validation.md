# M0 delivery validation — 2026-10-26

M0 foundation is complete. No playable gameplay or M1 systems were implemented.

## Delivered files

- `Assets/_DrillRunner/Runtime/`: runtime assembly definition and assembly metadata; no gameplay classes.
- `Assets/_DrillRunner/Editor/`: Editor-only assembly definition and `FoundationSetup.cs`, with one menu command: **Tools > DrillRunner > Setup Project Foundation**.
- `Assets/_DrillRunner/Tests/EditMode/`: test assembly definition, `FoundationTests.cs`, and `FoundationInputTests.cs`.
- `Assets/_DrillRunner/Scenes/Gameplay.unity`: generated and saved using Unity Editor APIs, with Main Camera, Audio Listener, Directional Light, ambient lighting, and a temporary track.
- `Assets/_DrillRunner/Settings/DrillRunner.inputactions`: Gameplay Charge and Steer actions with mouse and primary-touch bindings.
- `Assets/_DrillRunner/Materials/TrackReference.mat`: URP/Lit material generated with Unity APIs.
- Asset destinations: `Prefabs`, `ScriptableObjects`, `Models`, `Textures`, `Audio`, `VFX`, and `UI`. Unity-generated `.meta` files accompany all new assets and folders.
- Root `.gitignore`, `.gitattributes`, `README.md`, and this report.

## Packages and settings

No packages were installed, removed, or upgraded. Existing versions are URP **17.3.0**, Input System **1.18.0**, and Test Framework **1.6.0**. Both package JSON files remain byte-for-byte unchanged.

Only two pre-existing project files changed:

| File | Intended change |
| --- | --- |
| `ProjectSettings/ProjectSettings.asset` | Portrait orientation; 540 x 960 default player window; native resolution override off; upside-down and landscape autorotation flags off |
| `ProjectSettings/EditorBuildSettings.asset` | Gameplay first and enabled; existing enabled SampleScene and project-wide template input reference preserved |

Active Input Handling was already **Input System Package (New)** and remains unchanged; no Editor restart is required. The existing URP assets, graphics settings, quality settings, Force Text serialization, Visible Meta Files, and active build target were preserved. No Android target switch was performed.

## Checks actually executed

| Check | Result |
| --- | --- |
| Unity 6000.3.8f1 batch compilation and foundation setup | **Passed**, process exit code 0 |
| Unity EditMode tests | **9 passed, 0 failed, 0 skipped**, process exit code 0 |
| Gameplay scene opened through Editor scene APIs | **Passed**, camera/audio/light/URP material references valid; no missing scripts |
| Global and effective enabled build scene inclusion | **Passed** |
| Runtime/player versus Editor/test assembly separation | **Passed** |
| Portrait, modern-input-only, and URP checks | **Passed** |
| Mouse and primary-touch press, hold, horizontal delta, and release | **Passed**, isolated synthetic devices using InputTestFixture |
| Action-disable cancellation without modifying source input asset | **Passed** |
| Setup twice with unsaved work in an isolated Editor preview scene | **Passed**; saved scene/material/input bytes, GUIDs, settings, build entries, and scene setup preserved |
| JSON and assembly definition parsing, unique input IDs and bindings, no placeholder gameplay types | **Passed** |
| Unity Git ignores, referenceable source `.meta` files, and text/binary attributes | **Passed** |
| All new asset/folder `.meta` files, unique project asset GUIDs, and source/documentation whitespace | **Passed** |
| SHA-256 audit of every pre-existing asset, package file, and project settings file | **Passed**; no files deleted, no existing assets changed; only the two intended settings files differ |

Local evidence (intentionally ignored by Git): `Logs/M0-Setup.log`, `Logs/M0-EditMode.log`, `Logs/M0-EditMode.xml`, and the original-file hash inventory `Logs/M0-Baseline.json`.

## Not executed and remaining limitations

- **Unity PlayMode tests: Not run.** No PlayMode assembly was added because M0 has no runtime behavior.
- Batch validation used `-nographics`; actual graphical rendering and the local Game View aspect selection were not visually verified.
- No mobile player build, physical touchscreen test, or performance check was performed in this foundation milestone.
- No custom Build Profile existed. The code supports an active profile's scene-list override; that conditional branch was not exercised by this delivery's tests. Inactive profiles are intentionally left untouched.

## Exact user actions

1. Open the project in Unity **6000.3.8f1**, then open `Assets/_DrillRunner/Scenes/Gameplay.unity`.
2. In **Game View > aspect/resolution dropdown > +**, choose **Aspect Ratio**, enter **9** by **16**, label it **Portrait 9:16**, click **OK**, and select it.

Setup has already run. No package installation, setup-menu execution, restart, platform switch, or other project configuration is required for review. Running the setup command later explicitly reapplies M0 portrait defaults and restores Gameplay's build inclusion while preserving existing scene/material/input contents.

## Git

A local repository was initialized because none existed. Unity-specific ignores and attributes are in place. All project files remain untracked and unstaged, ready for user review. **No commits, pushes, remotes, Git LFS configuration, or destructive Git operations were performed.**
