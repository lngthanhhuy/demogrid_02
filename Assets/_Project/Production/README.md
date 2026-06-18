# Production

Production contains runtime composition and integration glue for builds beyond local MVP prototypes.

Use this folder for:

- Bootstrap scenes and startup installers.
- Environment-specific configs.
- Remote save, account, room, and multiplayer service adapters.
- Build/runtime settings that compose multiple features.

Gameplay features should depend on interfaces or small adapters, not directly on production backend details.
