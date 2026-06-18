# Shared

Shared contains reusable SEN CITY assets that are not owned by one gameplay feature.

Use this folder for:

- Shared UI widgets and presenters.
- Shared prefabs that multiple features instantiate.
- Shared materials, ScriptableObject templates, and lightweight VFX/audio wrappers.
- Small gameplay utilities that are project-specific but not low-level enough for `Core`.

Do not place feature-specific behavior here. Prefer `Features/<FeatureName>` until a second feature truly needs the asset.
