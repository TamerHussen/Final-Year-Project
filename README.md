### Final-Year-Project
# The Hunt: Predator and Prey Arena

"The Hunt" is a survival-stealth game developed for an Individual Honours Project at Birmingham City University. The project focuses on evaluating whether Machine Learning (ML) can provide a more adaptive and lifelike hunting experience compared to traditional scripted Behavior Trees (BT).

## Game Overview
Set in a dense, atmospheric forest arena, you play as the Prey. Your goal is to survive and collect intel fragments while being hunted by The Predator.

## The Core Experiment: Enemy A vs. Enemy B
### To evaluate AI performance, the game features two distinct AI architectures:
* Enemy A (ML Agent): Driven by Reinforcement Learning (PPO). It learns through trial and error, using a reward system based on proximity, detection, and successful captures. It utilizes custom sensors for vision, hearing, and scent trail tracking.
* Enemy B (BT Agent): A traditional Behavior Tree baseline. It uses defined states (Wander, Stalk, Chase) and relies on Unity NavMesh for navigation. It serves as the control group for the experiment.

## Controls & Mechanics
* Move - WASD
* Sprint - Left Shift
* Crouch - Left Ctrl
* Jump - Space
* Throw - Left mouse Click

## Technical Features
* Multi-Sensory AI: Both agents utilize a vision cone, a hearing radius (12m sphere), and a Scent Trail system that tracks the player's recent path.
* Adaptive Learning: The ML agent is trained using Unity ML-Agents (Python 3.9) with specific reward shaping for "stalking" behavior.
* Player Stealth System: Includes "Soft Object" detection (hiding in bushes) and an exposure meter.
* Stun Mechanics: Use throwable objects to temporarily disable the predator (3-second stun duration).


## Project Status (April 2026 Update)
### Completed
* Full integration of ML-Agents (PPO pipeline).
* Functional Behavior Tree baseline for comparison.
* Sensory systems: Raycast-based vision, hearing, and multi-point scent trails.
* Environmental balancing: Removed "Predator Stamina" to ensure a fair performance comparison.
* Input System with custom ControlsLoader for rebindable keys.

### In Progress
* Pilot Testing: Gathering player feedback via Microsoft Forms (Comparison of Enemy A vs B).
* Data Analysis: Comparing training success rates against player enjoyment metrics.
* Final Report: Documenting the trade-offs between ML adaptability and BT reliability.

## Tools & Technologies
* Engine: Unity 6.2 (using AI Inference & AI Navigation packages).
* AI: Unity ML-Agents (PPO Algorithm), C# Behavior Trees.
* Modelling: Blender (Predator and Player models).
* Environment: ProBuilder and foliage assets.

## Supervisor & Ethics
* Supervisor: Jan Krasniewicz
* Ethics: This project follows BCU ethical guidelines. All participant data for the "Enemy Comparison" survey is collected anonymously with informed consent.


## Contact
Tamer Hussen - Tamer.Hussen@mail.bcu.ac.uk
