# Glass World

## Overview

Glass World is a short PC simulation game where the player manages a living jar by adjusting daily light and fish food. The challenge is balancing algae, fish, snails, and nitrates without triggering an algae bloom.

## Core Loop

1. Read the jar state.
2. Adjust `Light`.
3. Adjust `Fish Food`.
4. Advance one day.
5. React to warnings, random events, and population changes.
6. Repeat until the ecosystem stabilizes or collapses.

## Win And Loss

- Win: maintain ecosystem stability for 12 straight days.
- Bonus mastery: unlock `Perfect Ecosystem` by surviving 10 stable days with no warnings.
- Lose: a species goes extinct or an algae bloom kills the fish.

## Main Systems

### Light

- Controlled by the player each day.
- More light increases algae growth.
- Too much light increases bloom risk.

### Fish Food

- Controlled by the player each day.
- More food helps fish directly but increases nitrates.
- Less food makes fish graze algae.

### Algae

- Grows from light and nitrates.
- Supports snails.
- Can be reduced by fish and snails.
- Too much algae creates bloom danger.

### Fish

- Need enough daily food to remain healthy.
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

1. Random daily events.
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
14. Stronger random daily events.
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

These should force adaptation without instantly ending the run.

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
- Light control
- Fish food control
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
- No inventory
- No save system
- No pathfinding
- No large progression system

## Why It Works

- Small enough for a jam
- Clear cause-and-effect loop
- Fast restarts
- Good replay value from events, difficulty, and starting jars
