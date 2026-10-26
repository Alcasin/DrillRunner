# DrillRunner

A portrait mobile 3D evolution runner built with **Unity 6000.3.8f1 (6.3 LTS)** and **Universal Render Pipeline 17.3.0**. The approved game has eight drill forms and four regions; the first vertical slice will cover one region and Form 1 to Form 2.

**M0 is foundation only. There is no playable gameplay, movement, drilling, economy, progression, or evolution.** Stop here for review before M1.

## Open and inspect

1. Open this directory with Unity Hub using 6000.3.8f1.
2. Open `Assets/_DrillRunner/Scenes/Gameplay.unity`.
3. In the **Game** tab, open the aspect/resolution dropdown (often **Free Aspect**), click **+**, select **Aspect Ratio**, enter **Width 9 / Height 16**, name it **Portrait 9:16**, click **OK**, then select it. This is the only local presentation setting not automated; it is an Editor preference, not a camera aspect override.

The scene contains a perspective Main Camera, Audio Listener, Directional Light, flat ambient lighting, and a temporary URP/Lit track reference. It contains no gameplay scripts. Portrait orientation and a 540 x 960 default player window are configured. The Windows build target is retained; Android has not been selected.

`Gameplay` is the first enabled global build scene; the template `SampleScene` remains enabled after it. Setup also includes Gameplay in an active Unity 6 build profile if that profile overrides global scenes; inactive profiles are untouched.

## Structure

All project-owned production assets live under `Assets/_DrillRunner/`:

| Folder | Purpose |
| --- | --- |
| `Runtime/` | `DrillRunner.Runtime`; assembly metadata only in M0, with no placeholder gameplay classes |
| `Editor/` | Editor-only `DrillRunner.Editor` and the single foundation setup command |
| `Tests/EditMode/` | Editor-only `DrillRunner.EditModeTests`, excluded from player builds |
| `Scenes/`, `Settings/`, `Materials/` | Gameplay scene, M1 input asset, temporary track material |
| `Prefabs/`, `ScriptableObjects/`, `Models/`, `Textures/`, `Audio/`, `VFX/`, `UI/` | Asset destinations reserved for later milestones; no placeholder assets |

The original template assets under `Assets/Settings`, `Assets/Scenes`, `Assets/TutorialInfo`, and the root input asset remain in place with their original GUIDs. Existing URP pipeline assets and quality tiers are retained. Empty project folders are kept through their versioned Unity folder `.meta` files; Unity recreates the folders when importing a fresh checkout.

## Input ready for M1

Input System **1.18.0** is already installed; **Active Input Handling = Input System Package (New)**. No restart or legacy Input Manager dependency is needed.

Use `Assets/_DrillRunner/Settings/DrillRunner.inputactions`, map **Gameplay**:

| Action | Type | Mouse | Touch |
| --- | --- | --- | --- |
| `Charge` | Button, default interaction | Left button | Primary touch press |
| `Steer` | Vector2 value, pixel delta | Mouse delta | Primary touch delta |

`Charge.performed` marks a press; its value stays pressed while held; `Charge.canceled` reports release **or cancellation**. There is deliberately no timed Hold interaction or minimum charge duration. M1 must distinguish a real release from disabling actions, focus loss, or touch cancellation before launching. M1 will gate steering to the intended drag state and use `Steer.x`; sensitivity, normalization, and launch behavior are out of scope.

This asset has Mouse and Touch control schemes and no generated C# wrapper. It is not auto-enabled or connected to a PlayerInput component. Reference and enable it explicitly in M1. The template project-wide `InputSystem_Actions` reference is preserved; it is not the M1 gameplay asset.

## Repeatable setup

Run **Tools > DrillRunner > Setup Project Foundation** only when setup or repair is needed. Batch equivalent (close this project in the Editor first):

```powershell
$unityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.3.8f1\Editor\Unity.exe'
& $unityEditor -batchmode -nographics -quit -projectPath . -executeMethod DrillRunner.Editor.FoundationSetup.Setup -logFile Logs/M0-Setup.log
```

The command creates the missing scene/material with Unity APIs, validates the checked-in input asset, applies portrait defaults, and includes Gameplay in the build list. It preserves existing scene/material/input contents, GUIDs, unrelated build entries, and open/unsaved scenes. It logs any project-setting or build-list changes. Rerunning explicitly reapplies M0 portrait defaults if they were changed. A missing scene with an existing `.meta` is reported for restoration rather than regenerated with a different GUID. Missing dependencies and invalid existing assets produce clear errors. There is no automatic setup on import and no broad asset save.

## Testing

Unity Test Framework **1.6.0** is already installed. Open **Window > General > Test Runner**, choose **EditMode**, and **Run All**. Tests cover scene loading and references, effective/global build scenes, assembly separation, portrait/input/URP configuration, repeatability with unsaved scenes, and isolated synthetic mouse/touch input.

Batch run (close this project in the Editor first; omit `-quit` because the test runner exits when finished):

```powershell
& $unityEditor -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -assemblyNames DrillRunner.EditModeTests -testResults Logs/M0-EditMode.xml -logFile Logs/M0-EditMode.log
```

No PlayMode test assembly is added: M0 has no runtime behavior to exercise. Test results and logs are local ignored files. See `Docs/M0-Validation.md` for the checks actually performed on this delivery.

## Source control

Unity Force Text serialization and Visible Meta Files were already enabled and are retained. Commit `Assets` with all `.meta` files, `Packages`, `ProjectSettings`, and documentation. Unity caches, local settings, builds, logs, and generated IDE projects are ignored. `.gitattributes` normalizes Unity text assets to LF and treats common media as binary. Git LFS and remote configuration have not been added. No commit or push is part of M0.
