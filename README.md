# Supermarket Startup

CS 382 FA26 - Semester Project

## Description

- CS 382 Semester Project
- Supermarket tycoon: build the store, hire staff, order stock, satisfy daily demand, upgrade equipment, unlock sections and obtain a liquor license
- Final build published to **WebGL** (Unity Play)

## Final Game Setup

The enabled scene (`Assets/_Scenes/__Scene_0.unity`) is already wired with the
full game systems under `GameSystems`. To regenerate config assets, placeable
definitions, level materials and scene references, run:

`Tools > Supermarket > Final Game > Setup Final Game`

All economy, wage, demand, license, unlock and upgrade values live in the
ScriptableObjects under `Assets/_ScriptableObjects/Game/` and can be tuned in
the Inspector.

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

The full game state (day, wallet, sections, employees, inventory, deliveries,
upgrades and license progress) and the placed-object layout are stored with
`PlayerPrefs` (WebGL-safe; no blocking file I/O). Saves are written on day change,
hiring/firing, section changes, build changes, and when the tab loses focus.
See `Assets/__Scripts/Building/README.md` for the layout save format.

## HUD

- Top-left: business day, date (`MM/DD/YYYY`) and net dollars per real minute.
- Top-right: current balance.
- Bottom-left: employee hiring/firing panel.
- Bottom-right: build mode button.
- Above build mode: order manager, sections/license and upgrade panels.

## Tests

- EditMode tests: `Assets/Tests/EditMode/` (`Supermarket.Tests.EditMode`).
- Run them from `Window > General > Test Runner > EditMode`.
