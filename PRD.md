# The Endless Rooms — Product Requirements Document

You are a senior Unity game developer, game designer, technical architect, and procedural-generation specialist.
Your task is to help me design and develop a first-person psychological horror and exploration game currently titled “The Endless Rooms.”
The game is inspired by the unsettling feeling of endless liminal spaces, but it must have its own original identity, environments, creatures, story, puzzles, visual elements, and terminology. Do not directly copy copyrighted creatures, level designs, lore, names, audio, or other recognizable assets from existing Backrooms games or media.
Use Unity 3D and C#. Target Windows PC first. The game must initially work as a complete single-player experience, but its architecture must be designed so online cooperative multiplayer can be added later without rewriting the principal gameplay systems.

## 1. Product Overview
“The Endless Rooms” is a first-person psychological horror, exploration, puzzle, and survival game set inside an enormous, procedurally generated structure.
The player awakens in a mysterious network of yellow-walled rooms, corridors, abandoned offices, furnished spaces, maintenance areas, and other increasingly strange environments.
There is no complete map when the game begins. As players explore, they gradually construct a map based only on locations they have already visited. The map helps players understand where they are, identify possible unexplored routes, mark important locations, and attempt to find an exit.
Some areas are safe, some contain environmental puzzles, some hide story elements or useful items, and others may contain hostile creatures. The player must decide when to explore, when to hide, and when to retreat.

## 2. Product Vision
Create an atmospheric experience that produces:

* Disorientation without making navigation feel completely random.
* Curiosity about what might exist beyond the next room.
* Tension from limited information and unpredictable danger.
* Satisfaction from understanding the environment.
* Rewarding exploration through secrets, environmental storytelling, and rare discoveries.
* Cooperative opportunities when multiplayer is eventually implemented.

The experience should be frightening because of atmosphere, uncertainty, sound, isolation, and intelligent threats—not because it constantly relies on jump scares.

## 3. Target Platform
Initial platform:

* Windows PC.
* Keyboard and mouse.
* Unity 3D using a current stable LTS release.
* C# scripts.

Possible later platforms:

* Additional PC platforms.
* Gamepads.
* Consoles, if technically and financially practical.

## 4. Game Modes
### Single-Player
This is the initial development priority and must be fully playable before online multiplayer is implemented.
The single player explores the structure, solves puzzles, collects resources, avoids creatures, discovers secrets, updates the map, and searches for exits.

### Online Cooperative Multiplayer
Co-op will be added after the single-player core is stable.
Planned multiplayer characteristics:

* Two to four players.
* Host-authoritative or server-authoritative architecture.
* Players explore the same generated world.
* Players may separate and discover different areas.
* Map discoveries can be shared immediately or only when players reunite, depending on later design decisions.
* Puzzle states, doors, items, creatures, hiding locations, and world-generation seeds must synchronize.
* Downed, rescue, spectator, and respawn behavior will be designed in a later multiplayer milestone.

Do not implement multiplayer in the first prototype unless specifically requested. However, avoid single-player-only assumptions in the architecture.

## 5. Target Audience
The intended audience includes players who enjoy:

* Psychological and atmospheric horror.
* Liminal-space environments.
* Procedural exploration.
* Environmental puzzles.
* Navigation and mapmaking.
* Survival without combat being the principal mechanic.
* Secrets, lore, and environmental storytelling.
* Cooperative horror games.

## 6. Core Gameplay Loop
The principal gameplay loop is:

1. Enter an unknown section of the structure.
2. Explore rooms and corridors.
3. Reveal visited areas on the map.
4. Search for clues, items, secrets, and environmental information.
5. Identify locked paths, puzzles, threats, and hiding locations.
6. Solve puzzles or locate the resources needed to proceed.
7. Avoid, distract, hide from, or escape hostile creatures.
8. Find an exit leading to a new zone or level.
9. Enter a more complex or dangerous environment.
10. Repeat while uncovering the mystery of the structure.

## 7. Player Experience Goals
The player should frequently think:

* “Have I been here before?”
* “What is making that sound?”
* “Should I explore this room or stay on the known route?”
* “Where can I hide if something appears?”
* “This landmark will help me find my way back.”
* “I found something that most players might miss.”
* “I understand part of the maze now.”
* “Is this really the exit, or is it a trap?”

## 8. World and Environment
### Initial Environment
The first environment should include:

* Aged yellow wallpaper.
* Yellow or beige walls with visible wear and variation.
* Old commercial carpeting.
* Fluorescent ceiling lights.
* Electrical humming and intermittent buzzing.
* Repeating architectural patterns.
* Empty rooms and corridors.
* Occasional furnished rooms.
* Offices, waiting areas, storage rooms, maintenance spaces, and unusual transitional rooms.
* Subtle environmental changes that make some sections recognizable.

The environment should not be a single texture repeated everywhere. Use modular visual variations, props, lighting changes, stains, damage, room shapes, landmarks, and sound zones to help navigation.

### Room Categories
The procedural system should support room categories such as:

* Standard room.
* Corridor.
* Junction.
* Dead end.
* Furnished room.
* Puzzle room.
* Resource room.
* Safe room.
* Hiding room.
* Landmark room.
* Lore or secret room.
* Monster encounter area.
* Exit room.
* Transitional room.
* Rare anomaly room.

Each room prefab should contain metadata describing its category, connection points, allowed neighbors, spawn possibilities, difficulty weight, and rarity.

## 9. Procedural Generation
The world should feel extremely large or potentially endless, but the game must not create the entire world at once.
Use deterministic, seed-based procedural generation.
Technical requirements:

* Generate the world in chunks or connected room groups.
* Load nearby chunks and unload distant chunks.
* Preserve the state of previously visited areas.
* Use object pooling where appropriate.
* Prevent disconnected or unreachable mandatory areas.
* Ensure that each generated section has at least one valid progression route.
* Validate door and room connection compatibility.
* Avoid obvious room overlap.
* Support deterministic reconstruction from a saved seed and stored state.
* Allow special handcrafted rooms to appear within procedural layouts.
* Keep puzzle-critical items reachable.
* Keep required exits achievable.
* Provide debugging tools to visualize connections, seeds, room categories, invalid layouts, and progression paths.

The system should create controlled procedural variety, not unrestricted randomness.

## 10. Navigation and Player-Built Map
The map is one of the game’s defining mechanics.

### Map Behavior

* The map starts blank.
* Rooms appear only after the player enters or clearly observes them.
* Corridors and doors are added as they are discovered.
* Unexplored areas must remain hidden.
* The map should show the player’s current known position unless a special game effect temporarily disrupts it.
* Different floors or levels must be visually separated.
* The map should preserve discovered areas after saving and loading.

### Player Map Features
The player should eventually be able to:

* Place custom map markers.
* Select marker types such as danger, puzzle, locked door, supplies, hiding place, exit candidate, and secret.
* Add short notes.
* Remove personal markers.
* Identify explored and partially explored rooms.
* See doors or routes that were discovered but not entered.
* Recognize major landmarks.
* Zoom and pan the map.

### Possible Advanced Mechanics
Do not include these in the first prototype unless requested, but keep them as future options:

* Maps that become inaccurate because of supernatural effects.
* Rooms that move after being visited.
* Areas that interfere with navigation.
* Creatures that react when the map is used.
* A physical in-world map instead of a full-screen interface.
* Separate cooperative maps that synchronize when players reunite.
* Items that improve map accuracy or reveal nearby geometry.

## 11. Puzzle System
Puzzles should be modular and reusable.
Puzzle categories may include:

* Finding keys, fuses, tools, or access cards.
* Restoring electrical power.
* Activating switches in a particular sequence.
* Interpreting environmental symbols.
* Following sound or light patterns.
* Manipulating furniture or room layouts.
* Opening routes using clues found elsewhere.
* Cooperative mechanisms that can later require multiple players.
* Navigation puzzles involving landmarks or map observations.

Puzzle requirements:

* Every puzzle must communicate its rules through environmental clues.
* Randomized solutions should be derived from the world seed where appropriate.
* Required puzzle elements must always spawn in reachable locations.
* Puzzle progress must save correctly.
* Puzzle logic should be separated from room presentation.
* Puzzles should use interfaces and events so they can later synchronize over a multiplayer network.

## 12. Monsters and Threats
Creatures must be original designs with distinct behavior.
Avoid making every enemy a creature that simply sees the player and runs directly toward them.
Potential behavior archetypes:

* A creature attracted to sound.
* A creature that moves only when it is not being observed.
* A creature that patrols specific environmental zones.
* A creature that imitates environmental sounds.
* A creature that searches hiding places after detecting evidence.
* A creature that follows recently opened doors.
* A creature that manipulates lights or nearby rooms.
* A rare creature that observes the player without immediately attacking.

Monster systems should support:

* Idle behavior.
* Patrol behavior.
* Suspicion.
* Investigation.
* Detection.
* Chase.
* Search.
* Losing the player.
* Returning to its territory.
* Attack or capture.
* Audio perception.
* Visual perception.
* Configurable behavior through data assets.
* Difficulty scaling.
* Spawn restrictions and encounter cooldowns.

The first prototype should contain only one simple creature after exploration, mapmaking, and puzzle systems are functional.

## 13. Stealth and Survival
Initial player survival mechanics:

* Walking.
* Running.
* Crouching.
* Interacting.
* Looking around.
* Hiding in designated places.
* Opening and closing doors.
* Producing different amounts of noise.
* Limited sprint stamina, if it improves gameplay.
* Being caught or attacked by a monster.
* Restarting from an appropriate checkpoint.

Potential later mechanics:

* Throwable distractions.
* Flashlights and batteries.
* Sanity or stress effects.
* Limited inventory.
* Healing.
* Defensive tools.
* Environmental traps.

Combat should not be a principal mechanic during the initial version. The preferred actions are avoidance, observation, hiding, distraction, and escape.

## 14. Secrets and Environmental Storytelling
Exploration should reveal information about the world without depending entirely on text exposition.
Possible discoveries:

* Abandoned personal belongings.
* Notes or recordings.
* Strange symbols.
* Evidence of previous explorers.
* Rooms that contradict the surrounding architecture.
* Hidden passages.
* Rare furniture arrangements.
* Unusual sounds.
* Story fragments.
* Optional puzzle chains.
* Clues about creatures.
* Clues about the origin or behavior of the structure.

Secrets should sometimes provide practical advantages, but some should exist only to deepen the mystery.

## 15. Audio Direction
Audio is a critical gameplay system.
Include:

* Directional footsteps.
* Fluorescent light hum.
* Electrical buzzing.
* Distant mechanical sounds.
* Ventilation noise.
* Room-specific ambience.
* Occluded audio through walls and doors.
* Dynamic music used sparingly.
* Creature sounds that communicate behavior.
* False or ambiguous sounds that create tension without becoming unfair.

Sound must help the player make decisions. Important threats should have learnable audio cues.

## 16. Art Direction
The visual style should be:

* Liminal.
* Uncomfortable.
* Familiar but unnatural.
* Realistic or semi-realistic.
* Muted and aged.
* Visually readable enough for navigation.
* Efficient enough to support procedural generation.

Use modular environment kits, material variations, decals, props, lighting profiles, and post-processing. Do not depend on expensive assets during the prototype. Use simple original placeholder geometry until systems are validated.

## 17. User Interface
Initial interface:

* Minimal HUD.
* Interaction prompt.
* Pause menu.
* Settings menu.
* Map screen.
* Marker placement interface.
* Puzzle feedback when necessary.
* Subtle detection or danger feedback.
* Death or capture screen.
* Save/load interface.

Accessibility and settings should eventually include:

* Mouse sensitivity.
* Field of view.
* Audio volume categories.
* Brightness.
* Subtitle options.
* Hold/toggle options for sprint and crouch.
* Reduced camera motion.
* Color-independent map markers.
* Remappable controls.

## 18. Saving and Loading
The save system should preserve:

* World seed.
* Player position and state.
* Generated chunk identities.
* Discovered rooms.
* Player-created map markers and notes.
* Opened and locked doors.
* Collected items.
* Puzzle states.
* Creature state when necessary.
* Discovered secrets.
* Current progression level.
* Relevant configuration version.

Save data should be versioned so future updates can migrate older saves when possible.

## 19. Technical Architecture
Use modular, testable, data-driven systems.
Recommended major systems:

* GameManager or game-state coordinator.
* PlayerController.
* Interaction system.
* Room definition system.
* Room connection system.
* Procedural generation manager.
* Chunk streaming manager.
* World-state persistence system.
* Map discovery system.
* Map rendering and marker system.
* Puzzle framework.
* Inventory framework.
* Door and lock system.
* Creature AI framework.
* Audio manager.
* Save/load system.
* Scene transition system.
* UI manager.
* Event or message system.
* Multiplayer abstraction layer for future networking.

Architecture requirements:

* Prefer composition over deep inheritance.
* Use ScriptableObjects for static definitions and configuration.
* Separate runtime state from design-time data.
* Avoid excessive global singletons.
* Use interfaces for interactable, saveable, detectable, damageable, and network-relevant objects.
* Keep gameplay logic independent of the UI.
* Keep procedural-generation logic testable outside visual presentation.
* Use clear namespaces and assembly definitions where useful.
* Document public classes and non-obvious decisions.
* Do not place the entire game in a few large manager classes.
* Do not add third-party packages without explaining why they are needed.

All gameplay actions that may eventually be networked should have clear ownership and authoritative-state boundaries.

## 20. Co-op Readiness
Even during single-player development:

* Give persistent objects stable IDs.
* Separate player-local state from shared world state.
* Keep random generation deterministic.
* Do not use direct references to a single global player when multiple players could exist later.
* Route world-changing interactions through explicit commands or services.
* Separate input from character actions.
* Avoid relying on local frame timing for authoritative puzzle results.
* Make doors, items, puzzles, enemies, and room states serializable.
* Mark which future data must be synchronized.
* Ensure that map discoveries can support individual and shared knowledge.

Do not prematurely implement complex networking. Prepare clean boundaries for it.

## 21. MVP Scope
The first meaningful playable prototype should include:

* One first-person player controller.
* Walking, running, crouching, and interaction.
* A modular room kit.
* Seed-based generation of a finite test maze.
* Multiple room shapes.
* Doors and compatible connection points.
* Furnished and unfurnished room variants.
* Streaming or controlled spawning of nearby sections.
* A blank map that reveals visited rooms.
* Current player position on the discovered map.
* Basic custom map markers.
* One simple environmental puzzle.
* One locked progression route.
* One exit condition.
* Basic save and load.
* Ambient lighting and audio.
* Debug visualization for generated rooms and connections.

After this is stable, add:

* One original monster.
* Hiding places.
* Detection and chase behavior.
* One secret room.
* One environmental story fragment.
* A checkpoint or capture loop.

The MVP should use placeholder assets when necessary. Functional validation is more important than final visual quality.

## 22. Out of Scope for the Initial MVP
Do not implement these during the first prototype:

* Full online multiplayer.
* Multiple large environment themes.
* Large numbers of monsters.
* Advanced combat.
* Crafting.
* Character progression trees.
* Procedural voice generation.
* A large cinematic story.
* Final commercial-quality assets.
* Console support.
* Monetization.
* User-generated levels.

## 23. Development Milestones

### Milestone 1: Project Foundation

* Establish folder structure and assemblies.
* Configure input.
* Create player movement.
* Create interaction interfaces.
* Establish data definitions.
* Add a small manual test scene.

### Milestone 2: Modular World

* Create modular room prefabs.
* Define compatible connectors.
* Generate a finite seed-based layout.
* Validate connectivity and prevent overlap.
* Add debugging visualization.

### Milestone 3: Map System

* Detect player entry into rooms.
* Reveal visited rooms.
* Display known connections.
* Show the player position.
* Add map pan and zoom.
* Add custom markers.

### Milestone 4: Puzzle and Progression

* Create the puzzle framework.
* Add a basic power or switch puzzle.
* Add a locked route.
* Add an exit room.
* Validate that required items remain reachable.

### Milestone 5: Persistence

* Save the seed and generated state.
* Save map discoveries.
* Save puzzle, door, item, and marker states.
* Load and reconstruct the world correctly.

### Milestone 6: Horror Prototype

* Add one creature.
* Add perception, investigation, chase, and search states.
* Add hiding.
* Add capture and restart behavior.
* Add ambient and directional audio.

### Milestone 7: Expanded Vertical Slice

* Improve procedural variety.
* Add a landmark room.
* Add a secret room.
* Add environmental storytelling.
* Improve lighting, materials, audio, and performance.
* Conduct structured playtesting.

### Future Milestone: Online Co-op

* Select the networking solution after evaluating the completed single-player architecture.
* Define server authority and ownership.
* Synchronize world generation and persistent state.
* Add lobbies and joining.
* Synchronize players, interactions, puzzles, doors, items, and creatures.
* Define shared versus personal map discoveries.
* Test disconnection and reconnection.
* Add multiplayer-specific UI and accessibility.

## 24. MVP Acceptance Criteria
The MVP is successful when:

* Starting a new game with a seed produces a valid traversable layout.
* Using the same seed reproduces the same base layout.
* The player can traverse rooms without visible gaps or major overlaps.
* At least one valid route leads to the exit.
* The map starts blank and reveals only discovered areas.
* Map markers can be placed and removed.
* A puzzle can be completed to open a previously blocked route.
* Saving and loading preserves the world, puzzle, door, and map states.
* The game maintains acceptable performance in the target test environment.
* Invalid generated layouts are detected or rejected.
* The principal systems do not assume that only one player can ever exist.
* After the horror milestone, the player can recognize, avoid, hide from, and escape one creature.

## 25. Risks and Mitigations

**Procedural generation creates impossible levels**
Mitigation:

* Use generation constraints.
* Validate the progression route.
* Reserve required rooms before filling optional spaces.
* Run automated generation tests across many seeds.

**Procedural rooms feel repetitive**
Mitigation:

* Use room categories, landmarks, lighting profiles, props, material variation, sound zones, and handcrafted rare rooms.

**The player becomes frustrated or permanently lost**
Mitigation:

* Use the discovery map, recognizable landmarks, custom markers, partial route information, and optional navigation assistance.

**Multiplayer later requires a rewrite**
Mitigation:

* Use deterministic generation, stable object IDs, separated state, command-based interactions, serializable objects, and multiplayer-aware ownership boundaries.

**Monsters become predictable**
Mitigation:

* Give creatures different perception rules, behaviors, territories, encounter conditions, and reactions to the environment.

**Scope becomes too large**
Mitigation:

* Complete each milestone before adding major features.
* Maintain strict MVP boundaries.
* Use placeholder content.
* Implement only one puzzle and one exit before adding a monster.

## Instructions for the Development Agent
Do not attempt to generate the entire finished game in one response.
Follow this process:

1. Review this PRD and identify contradictions, major risks, and missing decisions.
2. State all necessary assumptions.
3. Propose the Unity project architecture and folder structure.
4. Define the principal data models, components, interfaces, and their responsibilities.
5. Explain how single-player state will remain compatible with future co-op.
6. Divide development into small, testable implementation phases.
7. Begin only with Milestone 1.
8. For each phase:
   * Explain the objective.
   * List the files that will be created or modified.
   * Provide complete, compilable code.
   * Provide exact Unity Editor setup instructions.
   * Explain how to test the result.
   * State the expected behavior.
   * Identify common failure cases.
   * Stop for confirmation before proceeding to the next major milestone.

Code requirements:

* Use clean, idiomatic C#.
* Use descriptive names.
* Keep classes focused.
* Avoid unnecessary abstractions.
* Include namespaces.
* Include error handling and useful debug messages.
* Avoid obsolete Unity APIs.
* Avoid hard-coded scene object names when possible.
* Make configuration accessible through the Unity Inspector or ScriptableObjects.
* Clearly distinguish production code from temporary prototype code.
* Include tests for procedural generation and other deterministic logic when practical.
* Ensure instructions and code agree with each other.

When a design decision has multiple reasonable options, explain the tradeoffs and recommend one. Ask me to choose only when the decision materially affects gameplay, cost, architecture, or development time.

Start by reviewing the PRD. Then propose:

* Save the PRD into a PRD.md file
* Plan before acting and save the plan so it can be reviewed and revisited and used as part of the memory system.
* At the beginning create a GitHub repository to store everything there.
* Save decisions made on a file so you can use it as part of your memory system.
* When moving forward on the development:
* Create feature requests, user stories, design and technical plans, also create a feature branch, create pull requests and review and CI to merge and deploy.

Deliverables for the initial review:

* An original identity and short premise for “The Endless Rooms.”
* The recommended Unity project structure.
* The major systems and their boundaries.
* A practical procedural-generation strategy.
* A map representation strategy.
* The approach for making the architecture co-op ready.
* The implementation plan for Milestone 1.

Do not write Milestone 1 code until the review and proposed architecture have been approved.
