# Supermarket Startup

CS 382 FA26 - Semester Project

## Description

- CS 382 Semester Project
- Grid-based supermarket building demo
- Final build published to **WebGL** (Unity Play)

## Requirements

- **Unity 6 LTS** — use the editor version in `ProjectSettings/ProjectVersion.txt` (`6000.3.28f1`).
- The game must run in a browser (WebGL).

## Build for WebGL

1. Open the project in the matching Unity 6 LTS editor.
2. Run `Tools > Supermarket > Build/WebGL`.
3. The build is written to `Builds/WebGL/` (ignored by git).
4. Zip the **contents** of `Builds/WebGL/` and upload the zip to [Unity Play](https://play.unity.com).
5. Verify the loader reaches the scene in Chrome and Firefox.

Compression is set to gzip. Unity Play serves the required headers, so the
decompression fallback stays off. If a build ever fails to load, enable
`Decompression Fallback` in `Project Settings > Player > Publishing Settings`,
rebuild, and re-upload.

## Persistence

The placed-object layout is stored with `PlayerPrefs` (WebGL-safe; no blocking
file I/O). It is written immediately on every placement/removal and flushed when
the tab loses focus. See `Assets/__Scripts/Building/README.md` for the save format.

## Tests

- EditMode tests: `Assets/Tests/EditMode/` (`Supermarket.Tests.EditMode`).
- Run them from `Window > General > Test Runner > EditMode`.
