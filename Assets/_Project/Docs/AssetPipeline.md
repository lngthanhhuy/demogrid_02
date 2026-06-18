# SEN CITY Asset Pipeline

This project keeps source code, gameplay data, and heavy art assets separated so each pull request stays reviewable.

## Folder Structure

- `Assets/_Project/Core`: shared runtime systems such as grid primitives and UI helpers.
- `Assets/_Project/Shared`: reusable SEN CITY assets that are shared by more than one feature but are not low-level enough for Core.
- `Assets/_Project/Features`: gameplay features grouped by domain.
- `Assets/_Project/Features/FurniturePlacement/Scripts`: feature-owned runtime code, input adapters, save snapshots, and placement behavior.
- `Assets/_Project/Features/FurniturePlacement/Data`: light YAML ScriptableObject data for furniture items, grid profiles, and catalogs.
- `Assets/_Project/Features/FurniturePlacement/Scenes`: prototype or feature scenes used to test task slices.
- `Assets/_Project/Art`: project-owned art source imports and prefabs when they are reviewed and ready to be versioned.
- `Assets/_Project/Production`: bootstrap, environment config, remote service adapters, and build/runtime integration glue.
- `Assets/_Project/Docs/HybridFolderStructure.md`: the source of truth for the hybrid horizontal/vertical folder rules.
- `Assets/Models`: temporary imported model drops while art is still being reviewed or renamed.

## Git And LFS Rules

- Code, `.meta`, `.asset`, `.prefab`, and `.unity` files can be reviewed in normal pull requests when they are text/YAML.
- Heavy binaries such as `.fbx`, `.png`, `.psd`, `.wav`, `.mp4`, and archives must be tracked by Git LFS before push.
- Do not mix large model imports with placement logic in the same pull request.
- Prefer one pull request for behavior and data wiring, then a separate pull request for approved art assets.
- Run `Tools/SEN CITY/Furniture Placement/Audit Catalog And Assets` before pushing an art-related branch.
- Use `Assets/_Project/Art` only for approved assets. Temporary drops should stay out of the production tree until names, pivots, scale, materials, and ownership are checked.

## Furniture Placement Data Flow

1. Create or import a prefab.
2. Create a `FurnitureItemDefinition` in `Assets/_Project/Features/FurniturePlacement/Data`.
3. Assign item id, display name, category, footprint, quantity, icon, and prefab.
4. Run `Tools/SEN CITY/Furniture Placement/Rebuild Furniture Catalog`.
5. Use `SenCityFurnitureCatalog.asset` on `FurnitureInventoryRuntime`.

This keeps live placement code independent from whether final FBX/model assets are already committed.
