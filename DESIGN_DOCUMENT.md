# Glass World

## Overview

Glass World is a short PC strategy-simulation game where the player manages a living jar by playing cards that affect the ecosystem. Instead of adjusting sliders every day, the player draws a small hand, chooses 1 to 2 actions, and then watches the jar simulate the consequences. The challenge is balancing algae, fish, snails, and nitrates without triggering an algae bloom.

## Core Idea

Cards represent actions on the ecosystem.

Each day:

1. Draw 3 cards.
2. Play 1 to 2 cards.
3. Update the ecosystem.
4. Read the results and plan the next turn.

This makes the game more strategic, less repetitive, and more surprising.

## Core Loop

1. Read the jar state.
2. Draw 3 cards from the ecosystem deck.
3. Choose 1 to 2 cards to play.
4. Advance one day.
5. React to warnings, random events, and population changes.
6. Repeat until the ecosystem stabilizes or collapses.

## Win And Loss

- Win: maintain ecosystem stability for 12 straight days.
- Bonus mastery: unlock `Perfect Ecosystem` by surviving 10 stable days with no warnings.
- Lose: a species goes extinct or an algae bloom kills the fish.

## Main Systems

### Card Actions

- The player does not directly tune sliders.
- Cards apply ecosystem actions such as changing light, feeding fish, cleaning water, or adding organisms.
- Most cards create a clear benefit plus a trade-off.
- Some cards have immediate effects, while others create delayed consequences.

### Algae

- Grows from light and nitrates.
- Supports snails.
- Can be reduced by fish and snails.
- Too much algae creates bloom danger.

### Fish

- Need enough support from feeding cards to remain healthy.
- Produce nitrates through waste.
- When underfed, they eat algae.
- Reproduce slowly when the ecosystem is healthy.
- Can spawn with simple personality traits such as hungry, lazy, or fragile.

### Shrimp

- Fourth species that slowly cleans algae.
- Sensitive to high nitrates.
- Helps the ecosystem without replacing snails.

### Temperature

- Cold water slows algae growth.
- Warm water is neutral.
- Hot water speeds algae growth and stresses fish.

### Delayed Consequences

- Extra feeding should continue affecting the jar later.
- Example: too much food today can create a nitrate spike two days later.

### Ecosystem Memory

- If nitrates stay high for several days, algae becomes faster to regrow.
- This gives the jar long-term health instead of only daily state.

### Snails

- Depend on algae for survival.
- Help clean excess algae.
- Can starve if algae is too low.
- Can suffer from competition if there are too many.

### Nitrates

- Increase from fish waste and excess food.
- Increase algae growth.
- Must remain readable and understandable to the player.

## Card Strategy Layer

The player should regularly face questions like:

- Do I remove algae now or reduce nitrates first?
- Should I underfeed fish to control algae?
- Is a powerful risk card worth the danger?
- Do I build long-term stability or solve today's emergency?

Each turn should feel like a small ecosystem puzzle.

## Card Categories

### Environment Cards

- `Increase Light`: +15 percent light today, algae grows faster.
- `Cloudy Day`: light reduced, algae growth slows.
- `Dim Light`: safer algae control, but may reduce food for algae-eaters.
- `Sunny Day`: strong algae growth this turn.

### Fish Cards

- `Feed Fish`: fish hunger satisfied, small nitrate increase.
- `Underfeed Fish`: fish eat algae, but repeated use risks weak fish.
- `Overfeed Fish`: fish health improves, but nitrates rise sharply.
- `Add Fish`: add 1 fish and increase long-term nitrate production.
- `Remove Fish`: remove 1 fish and reduce long-term nitrate production.

### Snail Cards

- `Add Snail`: add 1 snail.
- `Snail Eggs`: add 2 snails.
- `Snail Loss`: remove 1 snail.

### Algae Cards

- `Remove Algae`: remove 20 algae.
- `Algae Growth`: moderate algae increase.
- `Algae Surge`: large algae increase.
- `Algae Die-Off`: large algae decrease.
- `Balanced Growth`: algae grows slowly and safely.

### Water And Nitrate Cards

- `Clean Water`: reduce nitrates significantly.
- `Partial Water Change`: reduce nitrates and algae slightly.
- `Nutrient Spike`: increase nitrates significantly.
- `Filter System`: reduce nitrates for the next 2 days.
- `Waste Build-Up`: nitrates increase slowly for 2 days.

### Risk Cards

- Risk cards are powerful but dangerous.
- `Strong Light`: major algae growth with high bloom risk.
- `Overfeed Fish`: immediate fish relief with a heavy nitrate cost.
- These cards add tension and make high-pressure turns more interesting.

### Random Event Cards

- Some cards appear automatically as events instead of being drawn from the normal deck.
- Examples: `Snail Eggs`, `Fish Disease`, and `Nutrient Spike`.
- These events make each run feel different and force adaptation.

## Example Turn

Day 4 draw:

- `Feed Fish`
- `Clean Water`
- `Increase Light`

Player chooses:

- `Feed Fish`
- `Clean Water`

Then the simulation runs.

Result:

- Fish stay healthy.
- Nitrates drop.
- Algae remains stable.

## Balanced Deck

The base deck contains 22 cards.

### Fish Control (5)

1. `Feed Fish`
2. `Underfeed Fish`
3. `Overfeed Fish`
4. `Add Fish`
5. `Remove Fish`

### Snail Control (3)

6. `Add Snail`
7. `Snail Eggs`
8. `Snail Loss`

### Algae Control (5)

9. `Remove Algae`
10. `Algae Growth`
11. `Algae Surge`
12. `Algae Die-Off`
13. `Balanced Growth`

### Light Control (4)

14. `Increase Light`
15. `Dim Light`
16. `Sunny Day`
17. `Cloudy Day`

### Water And Nitrates (5)

18. `Clean Water`
19. `Partial Water Change`
20. `Nutrient Spike`
21. `Filter System`
22. `Waste Build-Up`

## Why This Deck Works

It creates three main strategic tensions:

- Fish versus algae: underfeeding lowers algae, but feeding raises nitrates.
- Light versus bloom: more light accelerates algae, but too little light can starve algae eaters.
- Population versus stability: more fish create waste, while more snails improve algae control.

## Suggested Card Rarity

- Common: `Feed Fish`, `Dim Light`, `Remove Algae`
- Uncommon: `Clean Water`, `Snail Eggs`
- Rare: `Filter System`, `Algae Surge`
- Danger: `Nutrient Spike`, `Waste Build-Up`

## Optional Wildcard Card

`Ecosystem Shock`

- Random major change:
- Add 2 snails.
- Or algae +30.
- Or nitrates -20.

This pushes the player to adapt quickly and makes runs less predictable.

## Critical Foundations

1. The player must understand why events happened through warnings and summaries.
2. Numbers should change gradually so players can react.
3. The system should be recoverable after small mistakes.
4. The early game should be slightly unstable but solvable.
5. Warning signs must appear before collapse.
6. Every choice must include a trade-off.
7. The jar should feel alive through motion and visual change.
8. Values should stay easy to read.
9. Restart must be instant.
10. Runs should stay in the 3 to 10 minute range.

## Features To Include

1. Random event cards.
2. Water color feedback.
3. Species icons with health.
4. Bloom failure feedback.
5. Tooltips for systems.
6. Difficulty modes.
7. Population soft caps.
8. Ecosystem stability score.
9. Different starting jars.
10. Perfect ecosystem achievement.
11. Ecosystem diagram.
12. Day reports after each turn.
13. The jar visually reacts to ecosystem health.
14. Stronger random event cards.
15. A fourth species.
16. Fish personalities.
17. Water temperature.
18. Delayed nitrate consequences.
19. Ecosystem milestones.
20. Jar identity and history.

## Difficulty Modes

- Easy: slower algae growth, softer nitrate pressure, more bloom forgiveness.
- Normal: balanced default mode.
- Hard: faster algae growth, stronger nitrate pressure, less bloom forgiveness.

## Starting Jars

- Balanced: standard learning start.
- HighNitrates: starts closer to algae pressure.
- SnailHeavy: more cleanup power but more algae competition.
- Overgrown: starts with too much algae.
- Fragile: starts with very little algae.

## Additional Events

- Cloudy Day
- Algae Spores
- Sick Fish
- Dirty Water
- Snail Egg
- Heat Wave
- Cold Snap

These should appear as event cards or turn modifiers that force adaptation without instantly ending the run.

## Milestones

Examples:

- Balanced Ecosystem
- Snail Paradise
- Perfect Water
- Cleaner Crew

These provide extra goals beyond simple survival.

## UI Requirements

- Day number
- Stability score
- Stable day count
- Perfect run count
- Nitrate value
- Bloom threshold
- Draw pile
- Discard pile
- Current hand of 3 cards
- Play area for chosen cards
- Daily event panel
- Warning panel
- Species counts
- Day report panel
- Restart button
- Run next day button
- Ecosystem diagram

## Ecosystem Feedback Diagram

The UI should include a simple loop such as:

- Fish -> Nitrates
- Food -> Fish
- Light -> Algae
- Nitrates -> Algae
- Algae -> Snails

Whenever a system is the main cause of change that day, the related node or link should be highlighted.

## Living Jar Feedback

The jar should react visually:

- Healthy ecosystem: clear water and calm motion
- High algae: greener water
- High nitrates: murkier water
- Bloom risk: darker green warning state
- Collapse: murky, dead-looking jar

This makes the simulation readable even before the player reads the text.

## Day Reports

At the end of each day, show a short automatic report that explains what happened.

Examples:

- Fish were slightly underfed.
- Fish grazed on algae.
- Light and nitrates increased algae growth.
- High nitrates caused bloom pressure.

## Scope Rules

- One scene
- One jar
- Three species
- No deckbuilding between runs
- No save system
- No pathfinding
- No large progression system

## Why It Works

- Small enough for a jam
- Clear cause-and-effect loop
- Fast restarts
- Good replay value from events, difficulty, and starting jars
