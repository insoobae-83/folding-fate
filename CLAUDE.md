# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**folding-fate** is a Unity 6 game project using the Universal Render Pipeline (URP). It is currently in early development — the project is a blank URP canvas with infrastructure in place but no game logic implemented yet.

- **Unity Version**: 6000.3.12f1 (Unity 6 LTS)
- **Render Pipeline**: Universal Render Pipeline (URP) 17.3.0
- **License**: MIT

## Opening and Running

This project must be opened through the **Unity Editor** (version 6000.3.12f1):

1. Use Unity Hub to open the project folder
2. Open `Assets/Scenes/SampleScene.unity`
3. Press **Play** in the editor to run

There is no CLI build command — building is done via Unity's **File > Build Settings** menu. Target platforms configured: Standalone (PC/Mac/Linux), Android, iOS, WebGL, Dedicated Server.

## Key Packages

Defined in `Packages/manifest.json`:

| Package | Version | Purpose |
|---|---|---|
| `com.unity.render-pipelines.universal` | 17.3.0 | URP rendering |
| `com.unity.inputsystem` | 1.19.0 | New Input System |
| `com.unity.ai.navigation` | 2.0.11 | NavMesh AI navigation |
| `com.unity.timeline` | 1.8.11 | Cinematic sequencing |
| `com.unity.visualscripting` | 1.9.11 | Node-based scripting |
| `com.unity.test-framework` | 1.6.0 | Unit/integration tests |

## Architecture

### Renderer Configuration

Two renderer assets exist for dual-platform targeting:
- `Assets/Settings/PC_Renderer.asset` — high-fidelity PC rendering
- `Assets/Settings/Mobile_Renderer.asset` — optimized mobile rendering

Post-processing is controlled via `Assets/Settings/DefaultVolumeProfile.asset`.

### Input System

Input is configured via `Assets/InputSystem_Actions.inputactions` using the new Unity Input System (not the legacy `Input` class). New input bindings should be added to this asset.

### Testing

The test framework package is included. Tests go in an `Assets/Tests/` folder (EditMode or PlayMode). Run tests via **Window > General > Test Runner** in the Unity Editor.

## Code Conventions

- All game scripts are C# placed under `Assets/`
- Scripts are organized in subdirectories by feature/system
- `Assets/TutorialInfo/` contains editor-only utility scripts (not game logic)
- The project uses **Linear color space** (set in Player Settings)
