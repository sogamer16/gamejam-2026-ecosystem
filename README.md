# gamejam-2026-ecosystem

Unity prototype scaffold for a small 2D jam game: `Micro-Ecosystem in a Jar`.

## What's in the project

- A runtime-generated jam prototype under `ecosystem gamejam/Assets/Scripts`
- Menu, HUD, restart flow, and result overlay
- Three organism types: algae, snail, and fish
- Auto-ticking ecosystem simulation with health, reproduction, extinction, and a 20 stable day win condition

## How to run

1. Open `ecosystem gamejam` in Unity.
2. Open any scene and press Play.
3. The bootstrap script creates the prototype UI and simulation automatically.

## Prototype controls

- Click `Start Prototype` to begin.
- Click organism buttons to add more life to the jar.
- Press `Space` to force the next day tick.
- Keep all three species alive for `20` stable days.

## Good next steps in Unity

- Replace the runtime placeholder UI with your own scene layout and art.
- Swap the square organisms for sprites or prefabs.
- Tune the numbers in `EcosystemController.cs` to match the feel you want.
- Add sounds, particle pops, and scenario variants once the balance feels right.
