# Furniture Placement MVP Checklist

Source task: `Task dat do vat - Feature.pdf`

This document maps the PDF requirements for the Furniture Placement MVP to the current SEN CITY prototype implementation. It is intended as the final review checklist for Issue #1 and as a handoff note for future art/model integration.

## Implementation Coverage

| PDF area | Current implementation |
| --- | --- |
| Placement state machine | `FurniturePlacementController`, `PlacementSession`, and `PlacementSessionState` cover idle, new placement, moving, valid/invalid preview, store confirmation, saving, and error-adjacent feedback. |
| Ghost preview | `FurnitureGhostPreview` renders valid and invalid preview states before real objects are committed. |
| Grid snap and footprint validation | `SenCityGridProfile`, `GridFootprint`, `GridOccupancyMap`, and `PlacementValidator` validate bounds, rotated footprints, and overlap. |
| Place new | `FurnitureInventoryRuntime`, `FurniturePlacementHud`, and `FurniturePlacementRuntime` start placement from inventory and consume quantity after commit. |
| Move existing | `FurniturePlacementInputAdapter` supports hold-to-move; `FurniturePlacementRuntime` keeps the real object unchanged until valid confirm. |
| Rotation | `RotatePreviewClockwise` uses 90 degree steps and revalidates after every rotate. |
| Selected item panel | `FurniturePlacementHud` wires selected item text, move, rotate, store, close, save, and load controls. |
| Store to inventory | Store enters `RemoveConfirm`, uses `SenCityConfirmDialog`, releases occupied cells, destroys the placed object, and returns inventory quantity. |
| Save/load | `FurniturePlacementSaveService` persists room layout and inventory snapshots as JSON for the prototype. |
| Feedback | Toasts are routed through `SenCityToastPresenter`; placement failure, successful actions, and save failure have user-facing feedback paths. |
| Catalog and asset pipeline | `FurnitureCatalogDefinition`, `SenCityFurnitureCatalog.asset`, editor catalog tools, and `AssetPipeline.md` keep code/data separate from heavy art assets. |

## Acceptance Criteria

| ID | Status | Evidence |
| --- | --- | --- |
| AC-01 Select item from Inventory and show Ghost Preview | Covered | Inventory buttons call `BeginPlaceNew`; runtime creates active ghost preview. |
| AC-02 Move Ghost Preview to target position | Covered | Pointer input updates preview cell while a session is active. |
| AC-03 Rotate Ghost Preview with R key or Rotate Button | Covered | Keyboard adapter and HUD both call `RotatePreview`. |
| AC-04 Preview green when placement is valid | Covered | `FurnitureGhostPreview.SetValidity(true)` and validation tests. |
| AC-05 Preview red when placement is invalid | Covered | `FurnitureGhostPreview.SetValidity(false)` and invalid validation tests. |
| AC-06 Confirm only enabled when valid | Covered | `CanConfirmActiveSession` gates the HUD confirm button. |
| AC-07 Invalid position does not create/update real object | Covered | Controller tests cover overlap/out-of-bounds confirm rejection. |
| AC-08 Cancel removes preview and keeps old data | Covered | Controller cancel and store-cancel tests cover safe cancellation. |
| AC-09 Hold/click placed object to move | Covered | `FurniturePlacementInputAdapter` starts move after hold threshold. |
| AC-10 Existing object updates only after valid release/confirm | Covered | Move controller flow commits only through `ConfirmActiveSession`. |
| AC-11 Invalid move release keeps old object position | Covered | Invalid confirm path rejects commit and leaves source instance unchanged. |
| AC-12 Select placed object and open Selected Item Panel | Covered | Runtime selection events drive HUD selected panel state. |
| AC-13 Store button opens Confirm Dialog | Covered | Store requests emit `StoreConfirmationRequested` and HUD shows confirm dialog. |
| AC-14 Confirm Store removes room object and returns inventory | Covered | Store commit releases cells and runtime returns inventory quantity. |
| AC-15 Locked/in-use object cannot Store and shows error | Covered | Locked store test verifies rejection and failure message. |
| AC-16 Successful action or Cancel resets to Idle | Covered | Controller tests verify idle after placement, store, and cancel flows. |

## QA Coverage

Current EditMode tests cover:

- Grid occupancy reserve/release behavior.
- Placement validation for bounds, rotated footprints, and overlap.
- New placement commit and duplicate-confirm protection.
- Move existing flow that ignores its own occupied cells.
- Store request, cancel, confirm, and locked-object rejection.
- Inventory quantity consume/return, snapshot restore, catalog loading, and change events.
- Save-data round trips and fallback footprint restore.
- Auto-save success and save-failure toast behavior.
- Hover highlight, selection highlight, and deselect behavior.

Recommended validation before merging any Issue #1 follow-up:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.4.6f1\Editor\Unity.exe" -batchmode -projectPath "<worktree>" -runTests -testPlatform EditMode -testResults "<worktree>\Logs\FurniturePlacementEditModeResults.xml" -logFile "<worktree>\Logs\FurniturePlacementEditMode.log"
dotnet build .\demogrid_02.slnx
& "C:\Program Files\Unity\Hub\Editor\6000.4.6f1\Editor\Unity.exe" -batchmode -quit -projectPath "<worktree>" -executeMethod SenCity.Features.FurniturePlacement.Editor.FurniturePlacementCatalogTools.AuditCatalogAndAssets -logFile "<worktree>\Logs\FurniturePlacementAssetAudit.log"
git diff --cached --name-only
```

The staged-file audit must not include heavyweight binaries unless the branch is explicitly an approved art-asset PR with Git LFS enabled.

## Asset And Model Policy

Keep feature logic and large art imports in separate pull requests.

- Behavior PRs: code, `.meta`, Unity YAML scenes, ScriptableObject `.asset` data, docs.
- Art PRs: approved `.fbx`, source textures, baked textures, animation clips, audio, or other heavyweight files.
- Heavy binaries must be tracked with Git LFS before push.
- Imported model drops should be reviewed and renamed before moving into `Assets/_Project/Art`.
- The catalog can point at cube fallback prefabs while final models are private, pending, or stored outside the public GitHub repo.

Suggested `.gitattributes` coverage for asset PRs:

```gitattributes
*.fbx filter=lfs diff=lfs merge=lfs -text
*.obj filter=lfs diff=lfs merge=lfs -text
*.blend filter=lfs diff=lfs merge=lfs -text
*.psd filter=lfs diff=lfs merge=lfs -text
*.png filter=lfs diff=lfs merge=lfs -text
*.jpg filter=lfs diff=lfs merge=lfs -text
*.jpeg filter=lfs diff=lfs merge=lfs -text
*.tga filter=lfs diff=lfs merge=lfs -text
*.wav filter=lfs diff=lfs merge=lfs -text
*.mp3 filter=lfs diff=lfs merge=lfs -text
*.mp4 filter=lfs diff=lfs merge=lfs -text
*.mov filter=lfs diff=lfs merge=lfs -text
*.zip filter=lfs diff=lfs merge=lfs -text
```

## Known MVP Boundaries

These points are intentionally outside the current prototype slice or need production hardening:

- Final SEN CITY furniture models and textures are not bundled in behavior PRs.
- Store fade-out animation is not required for the current MVP implementation.
- Ambiguous mobile selection from a list is a post-MVP idea in the PDF.
- Full transactional rollback for remote save/backend failure should be implemented with the real production save service. The current prototype reports save failure and avoids success feedback, but local runtime state is already committed before the save attempt.
- Multiplayer/owner/room backend IDs are represented only by local prototype data fields where available; production identity should be integrated with the account/room service later.

## Definition Of Done

Issue #1 is ready for MVP review when:

- All current EditMode tests pass.
- `dotnet build .\demogrid_02.slnx` succeeds with no warnings or errors.
- Furniture catalog/assets audit passes.
- The prototype scene opens with inventory, preview, selected panel, confirm dialog, toast, save, and load controls wired.
- PR file lists remain small and reviewable, with no accidental model or texture drops in behavior branches.
