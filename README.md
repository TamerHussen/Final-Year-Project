# The Hunt: Predator and Prey Arena

**Individual Honours Project | Birmingham City University**

"The Hunt" is a survival-stealth environment developed to evaluate the adaptability and effectiveness of Reinforcement Learning (ML-Agents) against a traditional scripted Behaviour Tree (BT) baseline.

## Final Project Status (May 2026)

This project is now complete. All primary research objectives, including agent training, user testing, and comparative analysis, have been fulfilled.

### Project Deliverables

* **Final Report:** Completed and submitted May 2026.
* **Innovation Fest Poster:** Presented May 2026.
* **User Study:** Successfully conducted with 11 participants.

### Playable Artefacts

Two distinct versions are available in the Releases section:

* **Version 1.0 (Baseline):** The build used for the 11-person user testing phase. This version corresponds directly to the data presented in the Final Report and Poster.
* **Version 1.1 (Final Viva Build):** Updated version featuring improvements based on participant feedback, including crosshair implementation, corrected object grounding, and a revised gameplay loop to facilitate longer AI observation.

---

## Game Overview

Set in a dense forest arena, the player takes the role of the Prey. The objective is to survive and collect intel fragments while being pursued by a Predator. The environment is designed to test how different AI architectures handle line-of-sight breaks and complex navigation.

### The Core Experiment: Enemy A vs. Enemy B

To evaluate AI performance, the game features two distinct AI architectures:

* **Enemy A (ML Agent):** Driven by Reinforcement Learning (Proximal Policy Optimization). It utilizes a multi-sensory observation system to learn hunting behaviours through trial and error.
* **Enemy B (BT Agent):** A traditional Behaviour Tree baseline utilizing A* NavMesh pathfinding. This serves as the control group to measure the RL agent's believability and efficiency.

---

## Technical Features

* **Multi-Sensory Perception:** Both agents utilize a vision cone, a 12m hearing radius, and a multi-point scent trail system that tracks the player's recent path.
* **Adaptive Learning:** The ML agent was trained using Unity ML-Agents (Python 3.9) with specific reward shaping for stalking and capture metrics.
* **Stealth System:** Includes soft-object detection (hiding in foliage) and a player exposure meter.
* **Stun Mechanics:** Throwable objects provide a 3-second stun to the predator to allow for tactical retreats.

---

## Controls and Mechanics

* **Movement:** WASD
* **Sprint:** Left Shift
* **Crouch:** Left Ctrl
* **Jump:** Space
* **Interact:** E
* **Throw:** Left Mouse Click

---

## Tools and Technologies

* **Engine:** Unity 6.2 (AI Inference and AI Navigation packages)
* **AI:** Unity ML-Agents (PPO Algorithm), C# Behaviour Trees
* **Modelling:** Blender (Custom Predator and Player models)
* **Analysis:** TensorBoard, Microsoft Forms, Excel

---

## Supervisor and Ethics

* **Supervisor:** Jan Krasniewicz
* **Ethics Compliance:** This project follows Birmingham City University ethical guidelines. All participant data for the user study was collected anonymously with informed consent. Signed consent forms and raw data are archived via Moodle.

## Contact

Tamer Hussen - Tamer.Hussen@mail.bcu.ac.uk
Student ID: S23130437