# Build System

Runtime grid-based object placement for the supermarket. This is the "objects only" variant:
`ShopCell` is the floor, and the player places furniture/decor on a 16 x 16 grid of 1 m cells.

## Controls

| Input | Action |
|---|---|
| `B` | toggle build mode |
| `1`-`9` or hotbar click | select a placeable |
| Mouse move | hover + ghost preview |
| LMB | place (valid only) |
| RMB | remove hovered object |
| `R` | rotate 90 degrees |
| `Esc` | clear selection, or leave build mode |

## Scene setup

Run `Tools > Supermarket > Build System > Setup Scene`. This creates the materials,
placeholder prefabs, definitions and catalog, then wires a `BuildSystem` object into
`Assets/_Scenes/__Scene_0.unity`. It is safe to rerun; existing assets are left alone.

The grid is fitted to the combined bounds of every `ShopCell_*` floor tile (1 m
cells) with the surface at the top of the floor (y = 0.75). On the stock scene
that is 96 x 256 cells starting at `(-8, 0.75, -8)`. Without floor tiles it falls
back to a 16 x 16 plot.

## Buildable area

The plot is the whole store floor. `BuildGrid.exclusionZones` holds world-space
XZ rectangles, populated from `StoreBuildZones.CreateDefaultZones()` by
scene/final-game setup:

- bakery: columns 0-1, rows 0-1 (blocked)
- butcher: columns 4-5, rows 0-3 (blocked)
- deli: column 2, rows 3-4 (blocked)
- storage: columns 4-5 behind the butcher, plus column 3 at the back (blocked)
- checkout: column 0, rows 4-11 (checkout lanes only)
- alcohol: column 0, rows 12-15 (blocked)

Everything else is the general build area. `IsInside` returns false for blocked
cells; `PlacementValidator` reports `BlockedArea` or `WrongSection` so the HUD
explains why a placement failed. Checkout lanes are `4 x 3` cells and have
`allowedArea = Checkout`; every other placeable defaults to `General`, so only
lanes can enter checkout and lanes cannot be placed anywhere else. Each placed
lane allows one checkout employee; removing a lane also removes any checkout
hire above the new cap. The overlay only draws grid lines beside cells that are
inside the plot. To change the layout, edit the zones on `BuildSystem/BuildGrid`
or update `StoreBuildZones` and rerun setup.

### Section visuals

`SectionOverlay` (on `BuildSystem/SectionOverlay`) draws the section treatment
only while build mode is active:

- general build area: green outline, no label
- checkout: purple outline, no label
- bakery, butcher, deli, storage and alcohol: red outline, gray fill and a
  centered section-name label

Storage and deli keep physical walls and colliders. The other fixed sections
use `BuildModeBarrier`, which hides their wall renderers outside build mode and
keeps their colliders disabled, so they read as visual barriers rather than
physical walls. Placement permissions are unchanged.

Run `Tools > Supermarket > Build System > Apply Section Visuals` to reapply the
overlay, barrier components and materials without rebuilding the rest of the
build system.

## Adding a placeable

1. Create a prefab in `Assets/_Prefabs/Placeables/`.
2. Put the pivot at the **center of the footprint, on the bottom face**, with +Z forward.
3. Add a `PlacedObject` component to the root (the grid adds one if missing).
4. Use one collider on the root; child visual meshes do not need colliders.
5. Create a `PlaceableDefinition` (`Create > Supermarket > Building > Placeable Definition`):
   - `id` is used by save files and must stay stable.
   - `footprint` is (cells along X, cells along Z). Checkout lanes are `4 x 3`.
   - `allowedArea` is `General` for shelves/coolers/freezers/decor and `Checkout`
     for checkout lanes; the validator enforces it for every footprint cell.
   - `cost` is charged through `SimpleWallet` if assigned.
6. Add the definition to `Assets/_ScriptableObjects/Building/PlaceableCatalog.asset`.

The hotbar is generated from the catalog at runtime, so no UI wiring is required.

## Grid conventions

- `GridCoord { X, Z }`, `(0,0)` is the plot's minimum corner. For the stock scene,
  cell `(x, z)` covers world `[-8+x, -8+z]` to `[-7+x, -7+z]`.
- A footprint is anchored at its minimum corner; a 2x1 at `(4,4)` occupies `(4,4)` and `(5,4)`.
- Rotation is clockwise viewed from above and matches `Quaternion.Euler(0, turns * 90, 0)`.
- Hovering uses a math plane at the floor height, not a physics raycast; the cursor works
  above placed objects and the `ShopCell` collider never blocks it.
- Footprint matches the occupied cells after rotation; see `FootprintUtility`.

## Persistence

`BuildLayoutStore` saves the layout JSON through `PlayerPrefs`, under the key
`supermarket.build-layout.<fileName>` (default `supermarket.build-layout.build-layout.json`):

```json
{
  "version": 1,
  "objects": [
    { "id": "shelf_2x1", "x": 3, "z": 4, "rotation": 1 }
  ]
}
```

The layout is saved immediately after every placement/removal and flushed on
pause, focus loss, and quit. `PlayerPrefs` is WebGL-safe and avoids blocking
`System.IO`. Corrupt payloads are quarantined under a `.corrupt` key and ignored;
unknown definition IDs are skipped with a warning. Unsupported `version` values
are rejected and the plot starts empty.

Use `Tools > Supermarket > Build System > Clear Saved Layout` to reset the
configured store's key. If the store has objects placed, `BuildGrid.size` cannot
change at runtime (the grid keeps the old size and logs a warning).

## Extending

- **Economy:** implement `IBuildEconomy` and assign it to `BuildController.economySource`.
  `SimpleWallet` is the placeholder implementation.
- **Gameplay:** subscribe to `BuildGrid.ObjectPlaced` / `ObjectRemoved`, and query
  `BuildGrid.GetOccupant` or `IsInside` for pathfinding and customer logic.
- **Floor tiles:** add a separate floor layer/array to `BuildGrid`; the object occupancy
  model does not need to change.

## Tests

EditMode tests live in `Assets/Tests/EditMode/` and cover coordinate conversion, footprint
rotation, occupancy/overlap, bounds rejection, buildable-area exclusions, removal, pivot
rotation, persistence (round-trip, corrupt data, version mismatch, unknown IDs), economy
ordering, and hotbar index mapping. Run them from `Window > General > Test Runner > EditMode`.

## Layout

```
Assets/__Scripts/Building/Core/          GridCoord, FootprintUtility, PlaceableDefinition, PlaceableCatalog, PlacedObject
Assets/__Scripts/Building/Runtime/       BuildGrid, BuildController, BuildInput, PlacementResult, PlacementValidator, IBuildEconomy, SimpleWallet
Assets/__Scripts/Building/Presentation/  BuildGhost, GridOverlay, SectionOverlay, BuildModeBarrier
Assets/__Scripts/Building/UI/            BuildMenuUI
Assets/__Scripts/Building/Persistence/   BuildLayout, BuildLayoutStore
Assets/__Scripts/Editor/                 BuildSystemSetup, WebGLBuild
```
