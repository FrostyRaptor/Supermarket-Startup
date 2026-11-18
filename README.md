# Supermarket Startup
CS 382 FA26 - Semester Project

## Requirements

- **Unity 6000.3.28f1** (Unity 6.3 LTS), installed through Unity Hub.
- **Built-in Render Pipeline** (no URP or HDRP).
- **Legacy Input Manager** (`activeInputHandler: 0`); the Input System package is not installed.

## Opening the project

1. Install Unity `6000.3.28f1` in Unity Hub.
2. Open this folder as a Unity project and wait for Package Manager to finish restoring packages.
3. Open `Assets/_Scenes/__Scene_0.unity` and press Play.
4. Move the camera with WASD or the arrow keys.

## Building

`Assets/_Scenes/__Scene_0.unity` is registered in the build scene list. Use **File > Build Profiles** (or **Build Settings**) to build for Standalone (macOS).

## Optional: AI Assistant / Unity MCP

The project includes the pre-release `com.unity.ai.assistant` package used by the Unity MCP relay. The relay path in `opencode.jsonc` is machine-specific, so update it for your setup or remove the MCP block if you do not use the assistant.
