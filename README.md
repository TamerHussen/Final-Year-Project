# Final-Year-Project - Predator and Prey Arena

A survival stealth game where the player is the prey being hunted down by a reinforcement learning predator that adapts, learns and evolves its hunting strategy over time.

## Game Description
Set inside a large forest like arena, you play as the Runner, trying to survive while collecting intel fragments. A learning-based creature called the Tracker hunts you using its sight, hearing, movement predition and ambush tactics. Each match makes the predator smarter, creating an unpredictable and replayable experience.

## Core Features
* RL-Driven Predator - summons familiars, learns ambushing, flanking obstacle navigation and sound tracking.
* Procedural Arena Layerouts - Hiding spots, obstacles, foliage, terrain variation.
* Stealth and Survival Gamplay - collect intel, avoid detection, reach extraction.
* Player Tools - Noise makers, sprint boosts, vision aids.
* Adaptive Difficulty: Predator improves between rounds via ML-Agents.

## Project Goals
* Create a functioning predator RL agent using Unity ML-Agents.
* Compare RL predator behaviour with a scripted basline AI.
* Build a playable demo showing adaptive enemy behaviour.
* Analyse learning performance using reward shaping, sensores and environment design.

## What Has Been Done
* Unity project created + GitHub repo + Kanban setup.
* ML-Agents configured (Python 3.9, ONNX, Torch, dependencies fixed).
* First training sessions completed (1.5M steps, 2 runs, ONNX export successful).
* Player movement, input system, and camera implemented.
* Initial arena prototype (terrain, meshes, assets).
* Raycasts, observations, initial reward system for predator.
* Simplified training map started.
* Batch files created (run + resume).
* Memory + training configuration tuned (YAML updated).

## Work in Progress
* Player & Predator 3D models.
* Predator/Player abilities.
* Animation setup.
* Debug UI & detection visualisation.
* Improved reward shaping.
* Fixing incomplete episodes + observation issues.
* Sensory system (vision cone, hearing, maybe trail tracking).

## Next Steps
* Refine movement logic & rewards.
* Achieve first full successful episode.
* Implement sensory system.
* Add debugging interface.
* Complete models, animations, and basic UI.
* Continue training & adjust arena difficulty.

## Tools & Technologies
* Unity 6.2
* Unity ML-Agents (with AI Inference, no Barracuda)
* Python 3.9.13
* ProBuilder, Cinemachine, AI Navigation
* Blender (arena + character modelling)

## Supervisor Notes
* Concept confirmed as strong for RL demonstration.
* Ensure clear reflection, documentation, and Gantt tracking.
* Keep scope manageable (Adaptive Maze Runner optional).
* Prepare comparison with scripted AI baseline.

## Contact
Tamer Hussen - Tamer.Hussen@mail.bcu.ac.uk
