# Tile / Build System Plan — Supermarket Startup

## Project context

- **Unity 6.3** (`6000.3.28f1`), **Built-in Render Pipeline**.
- **Legacy Input Manager only** (`activeInputHandler: 0`); the Input System package is not installed.
- uGUI (`com.unity.ugui` 2.0.0) and the Unity Test Framework are installed.
- One scene, `Assets/_Scenes/__Scene_0.unity`, containing:
  - `Ground` (a large cube).
  - An angled camera (50° pitch) driven by `CameraMovement` (WASD).
  - A `Person` prefab **2 units tall**.
  - A `ShopCell` prefab **16 × 0.5 × 16** at origin, top surface at **y = 0.75**.
- That makes **1 unit ≈ 1 m**, so `ShopCell` reads as a **16 × 16 m shop plot** — a good fit for a **1 m grid** where shelves and counters occupy 1–3 cells.

Unity's built-in `Tilemap` is not used (it is 2D-only and does not handle 3D prefab footprints or the required persistence). The plan instead uses a small custom grid with a runtime build mode, layered so gameplay can query it later.

## Locked decisions

| Decision | Choice |
|---|---|
| Buildable content | **Objects only** — `ShopCell` remains the floor; no floor painting |
| Interaction | **Hotbar UI + mouse** |
| Footprints | **Multi-cell from day one**, 90° rotation |
| Persistence | **JSON save/load now** |
| Grid | 16 × 16 cells, 1 m cells, origin `(-8, 0.75, -8)` (matches the ShopCell plot) |

## Architecture

```
BuildMenuUI ── PlaceableCatalog ── PlaceableDefinition (SO)
                                        │
Player input ── BuildController ── PlacementValidator ── BuildGrid (bounds + occupancy)
                    │                                        │
                    ├── BuildGhost (preview)                 └── events: Placed / Removed
                    └── GridOverlay (lines + cell highlights)
                                        │
                              BuildLayoutStore (JSON save/load)
```

## Conventions (lock these before coding)

- **Cell coordinates:** `GridCoord { int X, Z }`, `(0,0)` = plot corner at `origin`; world center = `origin + (x + 0.5, 0, z + 0.5) * cellSize`.
- **Origin anchor:** min corner of the footprint. A 2×1 object at origin `(4,4)` occupies `(4,4)` and `(5,4)`.
- **Prefab pivot:** center of footprint at the bottom, forward = +Z. Optional `pivotOffset` for exceptions.
- **Rotation:** `quarterTurns` normalized `0–3`; occupied cells rotate **clockwise viewed from above**, matching `Quaternion.Euler(0, turns * 90, 0)`. Rotation math is isolated and unit-tested.
- **Cursor→cell:** math-plane intersection at floor height (`y = origin.y`), **no physics raycast**. Works over placed objects and ignores the ShopCell collider.
- **Removal hover:** resolve the cell's occupant from the grid, not by raycasting.

## Components

### Core (`Assets/__Scripts/Building/Core/`)

- `GridCoord` — readonly struct, equality/hash/operators.
- `FootprintUtility` — rotated size and occupied-cell enumeration for any `Vector2Int` footprint and 0–3 turns; including turn mapping (for turn 1: `(x,z) → (z, W-1-x)`), verified by tests.
- `PlaceableDefinition` (SO) — `id`, `displayName`, `prefab`, `Vector2Int footprint`, `int cost`, `PlaceableCategory category`, `bool rotatable`, `Sprite icon`, optional `Vector3 pivotOffset`. `OnValidate` auto-fills `id` and clamps footprint ≥ 1.
- `PlaceableCatalog` (SO) — list plus `id → definition` dictionary; used by the hotbar and save/load.
- `PlacedObject` — stores definition, origin, and turns; registered/unregistered by the grid; `OnDestroy` reports destruction so external deletes cannot leave stale occupancy.

### Runtime (`Assets/__Scripts/Building/Runtime/`)

- `BuildGrid` (MonoBehaviour): serialized `Size`, `CellSize`, `Origin`, `PlacedRoot`; flat `PlacedObject[] _occupants`; gizmos.

  ```csharp
  bool IsInside(GridCoord c);
  GridCoord WorldToCoord(Vector3 world);
  Vector3 CoordToWorldCorner(GridCoord c);
  Vector3 CoordToWorldCenter(GridCoord c);
  PlacementResult CanPlace(PlaceableDefinition def, GridCoord origin, int turns);
  PlacedObject Place(PlaceableDefinition def, GridCoord origin, int turns);
  bool Remove(PlacedObject obj);
  event Action<PlacedObject> ObjectPlaced;
  event Action<PlacedObject> ObjectRemoved;
  ```

  Validation order: null/invalid definition → any cell out of bounds → any cell occupied.
  Result enum: `Valid, InvalidDefinition, OutOfBounds, Occupied`. Affordability is checked by the controller, which adds `InsufficientFunds` before calling `Place`.

- `BuildInput` — static legacy-input wrapper (the project has `activeInputHandler: 0`), so an Input System migration later touches one file.
- `BuildController` — build-mode state, hotbar selection, rotation, hover coordinate, ghost/overlay updates, place/remove; optional `IBuildEconomy { int Balance; bool TrySpend(int); }` (null = free).

### Presentation

- `BuildGhost` — spawns a preview instance only when the definition changes; disables all colliders; swaps renderers to `Mat_GhostValid` / `Mat_GhostInvalid`; positions with footprint-center pivot and yaw.
- `GridOverlay` — runtime-generated mesh of thin quads (1.6 cm) at `y + 0.01`, plus a rebuilt highlight mesh for the footprint; `SetVisible`, `SetHighlight(cells, valid)`. No per-frame allocations in steady state.
- `BuildMenuUI` — **builds its own uGUI hierarchy at runtime** from the catalog: bottom-center panel, one button per definition (name + cost), selected state, rotate/close buttons, transient message label, control hints. Ensures an `EventSystem` with `StandaloneInputModule` (not `InputSystemUIInputModule` — that package isn't installed). Uses `LegacyRuntime.ttf` for the font. A pointer-over-UI guard prevents placing through buttons.

### Persistence (`Assets/__Scripts/Building/Persistence/`)

- `BuildLayout { int version = 1; List<Record> objects; }`, `Record { string id; int x, z, rotation; }`.
- `BuildLayoutStore` — `Save(BuildGrid)`, `Load(BuildGrid, PlaceableCatalog)` to `Application.persistentDataPath`; auto-save on a dirty flag (roughly 0.5 s debounce) and on quit/pause; unknown IDs are logged and skipped.

## Input map

| Input | Action |
|---|---|
| `B` | toggle build mode (grid + hotbar) |
| `1–9` / hotbar click | select placeable |
| Mouse move | hover + ghost |
| LMB | place if valid |
| RMB | remove hovered object |
| `R` | rotate 90° |
| `Esc` | clear selection, else exit build mode |

No conflicts with `CameraMovement` (WASD/arrows).

## File layout

```
Assets/__Scripts/Building/Core/          GridCoord, FootprintUtility, PlaceableDefinition, PlaceableCatalog, PlacedObject
Assets/__Scripts/Building/Runtime/       BuildGrid, BuildController, BuildInput, PlacementValidator
Assets/__Scripts/Building/Presentation/  BuildGhost, GridOverlay
Assets/__Scripts/Building/UI/            BuildMenuUI
Assets/__Scripts/Building/Persistence/   BuildLayout, BuildLayoutStore
Assets/__Scripts/Supermarket.Runtime.asmdef
Assets/__Scripts/Editor/Supermarket.Editor.asmdef
Assets/__Scripts/Editor/BuildSystemSetup.cs
Assets/Tests/EditMode/                   footprint, bounds, occupancy, save round-trip
Assets/_Materials/                       Mat_BuildGrid, Mat_HighlightValid/Invalid, Mat_GhostValid/Invalid
Assets/_Scenes/__Scene_0.unity           add BuildSystem root + PlacedObjects container
```

Code style matches `CameraMovement.cs` (2-space indents, brace on new line, `[Header]` fields).

## Implementation phases

1. **Foundation**
   - `Building/` folders.
   - `Supermarket.Runtime.asmdef` at `Assets/__Scripts/` (covers `CameraMovement.cs`).
   - `Assets/__Scripts/Editor/Supermarket.Editor.asmdef` (Editor-only).
   - `Assets/Tests/EditMode/` plus asmdef.
   - Material assets (`Mat_BuildGrid`, `Mat_HighlightValid/Invalid`, `Mat_GhostValid/Invalid`) using BiRP-safe shaders (`Sprites/Default`, `Legacy Shaders/Transparent/Diffuse`).
   - Compiles clean.

2. **Grid + occupancy core**
   - `GridCoord`, `FootprintUtility`, `BuildGrid` storage/validation/events, `PlaceableDefinition`, `PlacedObject`, `PlaceableCatalog`.
   - Tests: conversions at plot edges, footprint dims/cells for 1×1, 2×1, 2×3 at 0–3 turns, overlap reject, partial out-of-bounds reject, removal frees all cells, turn normalization.

3. **Placement + persistence**
   - Instantiate under `PlacedObjects`, rotation wiring, save/load plus round-trip test.
   - Verifiable via tests and a temporary debug binding.

4. **Build mode**
   - `BuildController`, `BuildInput`, `BuildGhost`, `GridOverlay`.
   - Full mouse/keyboard loop without UI.

5. **Hotbar + economy hook**
   - `BuildMenuUI`, invalid-reason messages.
   - Optional `SimpleWallet` demo (e.g., 1,000 starting; shelf 120, counter 200, plant 40).

6. **Content, scene wiring, docs**
   - `Tools › Supermarket › Build System › Setup Scene` editor utility: creates missing assets, `BuildSystem` root, `PlacedObjects` container, wires references, ensures an EventSystem, marks the scene dirty; rerunnable.
   - Three cube-based placeholder prefabs and catalog entries.
   - `Building/README.md` covering pivot/footprint conventions and adding new items.
   - Optional polish: `Ctrl+Z` undo stack.

## Acceptance checklist

- [ ] `B` shows/hides the grid overlay and hotbar; `Esc` backs out correctly.
- [ ] Highlight matches the ghost's exact cells, including after rotation.
- [ ] A 2×1 shelf at the plot edge is red and rejected; overlapping is red and rejected.
- [ ] Valid placement spawns under `PlacedObjects`, occupies all cells, and does not click through the UI.
- [ ] `R` rotates ghost and placed object consistently (visual yaw matches occupied cells).
- [ ] RMB removes the hovered object and frees every cell.
- [ ] Layout survives play → exit → play.
- [ ] Built player renders overlay/ghost correctly (materials included).
- [ ] Profile shows no steady-state allocations; 256 cells remains trivial.
- [ ] EditMode tests green.

## Non-goals for v1

- Floor painting (architecture allows adding it later).
- Walls/rooms.
- NavMesh/pathfinding integration (only events and `IsCellBlocked` hooks).
- Real economy/money management (hook only).
- Multiplayer.
- Advanced art.

## Risks

- **Prefab pivot mistakes** — mitigated by the authoring rule and `pivotOffset`.
- **Visual-vs-cell rotation mismatch** — mitigated by a single rotation convention and unit tests.
- **BiRP transparency shader choices** — mitigated by explicit material assets and verifying in a build.
- **Scene YAML wiring** — mitigated by the rerunnable editor setup utility.
- **Save/load with catalog changes** — mitigated by definition IDs and skipping unknown entries with a warning.
