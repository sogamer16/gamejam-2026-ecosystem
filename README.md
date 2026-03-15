# gamejam-2026-ecosystem

`Micro-Ecosystem in a Jar` is a Unity card-based ecosystem strategy prototype. The player manages a fragile jar ecosystem by playing cards that affect fish, snails, algae, light, and nitrates. The game is built around short replayable runs where the player tries to stabilize the ecosystem instead of directly tweaking sliders every turn.

## Current game

- A rolling 3-card hand: keep 3 cards, play 1 card each day, then draw 1 replacement.
- A card-driven ecosystem simulation with fish, snails, algae, nitrates, bloom risk, reproduction, starvation, and collapse states.
- A 3D jar presentation with a physical glass jar, water volume, gravel, plants, bubbles, and animated fish.
- Runtime scene generation that can also populate the hierarchy in the editor for easier layout editing.
- Imported card visuals using the Nue Deck and Card Shirts Lite assets.
- Pause flow during gameplay with `Resume`, `Restart Run`, and `Quit`.
- Result screen for win/loss states and restart flow.

## Core loop

1. Start a run.
2. Draw or keep a hand of 3 cards.
3. Choose 1 card to play.
4. The ecosystem updates.
5. A new card is drawn.
6. Repeat until the jar stabilizes or collapses.

## Main systems

### Ecosystem simulation

- Fish need food and can graze algae if underfed.
- Snails help control algae but can starve if algae gets too low.
- Algae grows based on light, nitrates, existing algae, and ecosystem memory.
- Nitrates rise from fish waste and feeding, and can be reduced with water-control cards.
- Too much algae can trigger a bloom and crash the jar.

### Card gameplay

- Fish cards affect feeding and fish population.
- Snail cards affect snail population.
- Algae cards add or remove algae pressure.
- Light cards change the visible lighting and the simulation’s algae growth.
- Water cards reduce or increase nitrate pressure.
- Risk cards give strong effects with dangerous trade-offs.
- Random events make runs less predictable.

### Visual presentation

- Fish use the imported 3D low-poly animal assets.
- Fish movement is constrained to the actual jar space using the `GlassShell`, `WaterVolume`, `JarRim`, and `Gravel`.
- Water color changes with ecosystem health and algae pressure.
- Algae visuals grow larger as algae count increases.
- Card play, discard, and draw all have motion and feedback.

## Scenes

### `main`

Setup scene for choosing the starting jar and settings before the run.

### `SampleScene`

Main gameplay scene. This is the scene to use when testing the jar, cards, pause screen, and overall gameplay loop.

## Controls

- Click a card to select it.
- Click `Play Selected Card` to resolve the turn.
- Press `Space` to play the selected card.
- Click `Pause` or press `Esc` during gameplay to open the pause screen.
- From pause you can `Resume`, `Restart Run`, or `Quit`.

## Important scripts

- [EcosystemController.cs](/C:/Users/sohaib/game%202d%20lit/gamejam-2026-ecosystem/ecosystem%20gamejam/Assets/Scripts/EcosystemController.cs)
  Main gameplay controller, UI builder, card system, turn resolution, 3D jar setup, and pause flow.
- [OrganismView.cs](/C:/Users/sohaib/game%202d%20lit/gamejam-2026-ecosystem/ecosystem%20gamejam/Assets/Scripts/OrganismView.cs)
  3D organism movement and visuals for fish, snails, and algae.
- [SetupSceneController.cs](/C:/Users/sohaib/game%202d%20lit/gamejam-2026-ecosystem/ecosystem%20gamejam/Assets/Scripts/SetupSceneController.cs)
  Setup scene UI and run configuration.
- [EcosystemCardPrefabView.cs](/C:/Users/sohaib/game%202d%20lit/gamejam-2026-ecosystem/ecosystem%20gamejam/Assets/Scripts/EcosystemCardPrefabView.cs)
  Binds ecosystem card data into the imported card prefab visuals.
- [EcosystemSceneAutoSetup.cs](/C:/Users/sohaib/game%202d%20lit/gamejam-2026-ecosystem/ecosystem%20gamejam/Assets/Editor/EcosystemSceneAutoSetup.cs)
  Adds required controllers and event systems when scenes are opened in the editor.

## Editor workflow

- Open `ecosystem gamejam` in Unity.
- Open [SampleScene.unity](/C:/Users/sohaib/game%202d%20lit/gamejam-2026-ecosystem/ecosystem%20gamejam/Assets/Scenes/SampleScene.unity) for gameplay work.
- Select the `EcosystemController` object in the hierarchy to use context actions like:
  - `Rebuild 3D Jar World`
  - `Rebuild Gameplay Canvas`
- Save the scene after rebuilding if you want the generated hierarchy to remain editable in the scene.

## Known limitations

- A lot of the UI is still code-built rather than hand-authored.
- Card art is intentionally blank right now.
- Some imported assets were adapted for the prototype and may still need prefab/material cleanup.
- The prototype has been iterated quickly, so more polish and cleanup are still needed.

## Best next additions

### Highest-value next steps

- Replace more runtime-generated UI with hand-authored scene prefabs.
- Add proper sound effects for card draw, play, warnings, blooms, and collapse.
- Add a clearer tutorial or onboarding for first-time players.
- Improve fish and snail behavior so the jar feels more alive.
- Add better authored algae visuals instead of simple fallback growth pieces.

### Strong gameplay improvements

- Add more card variety, rarity tuning, and deck balance.
- Add scenario goals or challenge modifiers for different runs.
- Add clearer cause-and-effect feedback after each turn.
- Add progression between runs, such as unlockable cards or starting jars.

### Visual polish

- Replace primitive jar parts with a custom jar mesh and materials.
- Add better underwater particles, caustics, and light scattering.
- Add more environmental props around the jar scene.
- Add better collapse and bloom presentation.

## Short summary

This project is no longer a simple 2D jam scaffold. It is now a playable card-based ecosystem management prototype with a 3D jar, animated fish, scene-editable generated layout, imported card visuals, pause flow, and a full short-run gameplay loop.
