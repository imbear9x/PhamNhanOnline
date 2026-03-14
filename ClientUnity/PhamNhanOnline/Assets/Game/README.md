# Client Structure

This folder is the gameplay-facing root for the Unity client.

## Runtime

- `Bootstrap`: app entry, composition root, startup scene wiring
- `Core`: client-wide primitives that should stay feature-agnostic
- `Infrastructure`: persistence, config loading, scene loading, addressables glue
- `Network`: transport, packet parsing, session lifecycle, protocol adapters
- `Shared`: client-side constants and DTOs that mirror server contracts
- `Features/Auth`: login, reconnect, account-facing flow
- `Features/Character`: character list, character data, selection flow
- `Features/World`: map join, observer sync, movement/world-facing runtime
- `UI`: reusable UI logic and screen presenters

## Content

- `Art`: sprites and visual assets
- `Audio`: music and sound effects
- `Prefabs`: reusable scene objects
- `ScriptableObjects`: client config such as maps, UI tuning, static presentation data

## Scenes

- `Bootstrap`: minimal startup scene
- `Auth`: login and account flow scenes
- `World`: in-game scenes
- `Sandbox`: temporary playground scenes for iteration

## Tests

- `EditMode`: pure logic and mapper tests
- `PlayMode`: scene/runtime integration tests

Keep new code inside `Assets/Game` so the gameplay client stays separate from Unity-generated project files.
