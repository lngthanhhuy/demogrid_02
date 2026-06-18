# SEN CITY Hybrid Folder Structure

This structure keeps SEN CITY readable as the project grows beyond the Furniture Placement MVP. It combines horizontal shared systems with vertical feature folders.

## Top Level

```text
Assets/_Project/
  Core/
  Shared/
  Features/
  Art/
  Production/
  Tests/
  Docs/
```

## Folder Roles

| Folder | Role | What belongs here |
| --- | --- | --- |
| `Core` | Horizontal engine-facing systems | Grid primitives, base UI helpers, and code that multiple features can depend on. |
| `Shared` | SEN CITY reusable gameplay/UI assets | Shared prefabs, UI widgets, materials, ScriptableObject templates, VFX/audio wrappers that are not owned by one feature. |
| `Features/<FeatureName>` | Vertical gameplay slices | Feature runtime scripts, data, scenes, editor tools, feature prefabs, and feature-specific UI. |
| `Art` | Production art staging and approved art assets | Furniture, environment, pet, character, UI, texture, material, and source-art pipelines. Heavy files must use Git LFS or private asset storage. |
| `Production` | Production integration layer | Bootstrap scenes, environment configs, remote service adapters, build/runtime settings, and backend integration glue. |
| `Tests` | Test assemblies by mode and feature | EditMode and PlayMode tests grouped by feature or system. |
| `Docs` | Human-readable project decisions | Feature checklists, asset policy, folder rules, QA notes, and onboarding docs. |

## Feature Folder Template

Use this template for new gameplay domains:

```text
Assets/_Project/Features/<FeatureName>/
  Scripts/
  Data/
  Prefabs/
  UI/
  Scenes/
  Editor/
```

- `Scripts`: runtime feature behavior, feature-owned input adapters, save snapshots, and feature-owned data models.
- `Data`: light YAML assets such as ScriptableObjects, catalogs, profiles, and tunable configs.
- `Prefabs`: feature-owned prefabs that are small enough for normal review.
- `UI`: feature-owned panels, bindings, and UI prefabs.
- `Scenes`: prototype or validation scenes for the feature.
- `Editor`: editor tools, import helpers, catalog builders, and validators.

Furniture Placement already follows this pattern with `Scripts`, `Data`, `Scenes`, and `Editor`. Add `Prefabs` or `UI` only when the feature needs committed feature-owned assets.

## Production Asset Policy

Behavior and art should stay in separate pull requests.

- Behavior PRs: C# scripts, `.asmdef`, Unity YAML scenes, `.asset` data, lightweight prefabs, docs, tests, and `.meta`.
- Art PRs: approved `.fbx`, `.blend`, texture maps, audio, animation clips, video, and archives.
- Heavy art must be tracked with Git LFS before push, or kept in private asset storage until approved for version control.
- Temporary vendor/model drops should stay outside `_Project/Art` until named, reviewed, and normalized.
- Public GitHub PRs should not mix feature logic with large model imports.

## Dependency Direction

Keep dependencies flowing in one direction:

```text
Production -> Features -> Shared -> Core
Tests can reference any layer they validate.
Editor tools should stay in Editor folders or editor-only assemblies.
```

Rules:

- `Core` must not reference a feature.
- `Shared` must not reference a feature.
- Features can reference `Core` and `Shared`.
- `Production` can compose features and services, but feature runtime code should not depend on production backend details.
- Backend-specific code should live behind interfaces or adapters before it reaches gameplay code.
- Runtime assemblies should mirror this direction: `SenCity.FurniturePlacement` can reference `SenCity.Core`; `SenCity.Core` must stay feature-free.

## PR Naming Guidance

Use branch names that show the lane:

- `codex/postmvp-structure-*` for folder and architecture work.
- `codex/postmvp-production-*` for backend/bootstrap/runtime integration.
- `codex/art-*` for reviewed LFS art asset imports.
- `codex/feature-*` for new gameplay feature slices.

Keep PRs small enough that the file list explains the intent at a glance.
