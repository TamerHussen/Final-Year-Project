@echo off
title ML-Agents Training - Predator

REM === 1. Activate your ML-Agents venv ===
call D:\GithubProjects\Final-Year-Project\MyFinalProject\mlagents_env\Scripts\activate

REM === 2. Change directory to your Unity project ===
cd /d D:\GithubProjects\Final-Year-Project\MyFinalProject

REM === 3. Resume training ===
mlagents-learn Config\predator.yaml --run-id=PredatorRun1 --results-dir=D:\MLAgentsResults --resume --no-graphics

pause
