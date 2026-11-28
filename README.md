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
the Inspector. Setup also wires the employee wage config, the section config,
and the separate loading-zone, shelf, equipment and per-section storage upgrade
profiles into the scene.

The build grid covers the whole store floor except the bakery, butcher, deli,
storage and alcohol sections. Only storage and deli have physical walls; the
other fixed sections appear as build-mode-only barriers with a red outline,
gray fill and centered name label. The general build area has a green outline
and checkout has a purple outline in build mode. General fixtures build in the
aisles; `4 x 3` checkout lanes can only be placed in the checkout section, and
no other placeable can go there. Each placed lane supports one checkout
employee, so hiring is capped by the number of lanes in the store. See
`Assets/__Scripts/Building/README.md` for details.

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
New games start from the shipped `Assets/Resources/DefaultBuildLayout.json`
layout. See `Assets/__Scripts/Building/README.md` for the layout save format.

## HUD

The HUD displays are authored uGUI Canvas prefabs in `Assets/_Prefabs/UI/`
(`TycoonHUD.prefab` and `BuildMenu.prefab`). The controllers keep all text
formatting and state logic; they instantiate the prefabs at runtime and fill
the authored labels/buttons. The store manager's Employees, Upgrades and Orders
tab bodies are ordinary GameObjects inside `TycoonHUD.prefab`; open that prefab
in the Unity Editor to move, restyle, add to, or rewire them. Selecting a tab
shows one static body and hides the other two; no tab rows are generated at
runtime. `Tools > Supermarket > UI > Create Authored Canvas Prefabs` only
creates missing prefabs and validates the HUD — it never overwrites manual
Canvas edits.

- Top-left: business day and in-game time of day (`h:mm AM/PM`) on one line.
- Top-right: current balance with net dollars per real hour underneath.
- Bottom-left: **Store** button opening a tabbed manager:
  - **Employees**: daily-wage total and six hire/fire rows grouped under Checkout, Storage, Butcher, Bakery and Deli headings. Each row shows the role, current/maximum headcount, that role's configured **daily** wage, and `−`/`+` buttons. Wages come from `EmployeeConfig`.
  - **Upgrades**: five sections — Storage, Butcher, Bakery, Deli and Alcohol. Each section title shows its unlock price to the left of a right-side **Buy** button. Storage has the add-loading-zone row, three loading-zone upgrades and the storage-shelf upgrade; Butcher/Bakery/Deli each have an equipment and a separate storage upgrade; Alcohol has the liquor-license status (`Complete`, `In Progress`, `Incomplete`), two filing rows, a section-purchase row and rows for placed alcohol shelves. Locked sections, unbuilt loading zones and unavailable actions are grayed out/disabled.
  - **Orders**: cart-style ordering. Each catalog product shows its unit cost, suggested restock quantity, an editable order-amount box and an **Order** button that adds to the cart. A top summary shows `Order Suggested Amounts` with the running total and **Buy** submit button; a right-aligned bottom summary repeats the total and submit button. Totals turn green when affordable and red when not, and products that cannot be stored or sold yet (for example alcohol before the license/section is unlocked) are grayed out.
- Bottom-right: build mode button.

## Tests

- EditMode tests: `Assets/Tests/EditMode/` (`Supermarket.Tests.EditMode`). `AuthoredUITests` guards the authored HUD/Build Menu prefab references, section/license display helpers, configured employee wages, order totals and product-order gating.
- Run them from `Window > General > Test Runner > EditMode`.
