# FPS Game with Dynamic AI Difficulty Adjustment 

## Overview 
This project is a first-person shooter game prototype featuring a dynamic AI-Director that adjusts the game's difficulty, pacing and resource allocation based on player performance. Inspired by games like _Call of Duty: Zombies_, the goal is to provide an engaging experience where the AI responds to the player's actions, to ensure the game remains challenging to every player's skill level.

## AI-Director  
### Game Manager

Tracks the player's performance per round.
  - Player Accuracy,
  - Damage Taken,
  - Time taken to complete round. 
    
Resets calcualted performance after each round

### AI-Director

Adjusts game based on those metrics
  - AI spawn Rate,
  - AI Speed,
  - AI Damage,
  - AI Health,
  - Wall Buy ammo cost,
  - Max Ammo drop chance. 

Metrics are changed by the corresponding performance, poor accuracy will lead to ammo cost being decreased and ammo drops increased. Low Damage taken will result in AI Damage getting buffed. Quickly completing rounds will cause the AI to more, faster, have more health and be quicker. 
Each metric also has a range which they can change between and has a capped change per round, making the increase less dramatic. 

## Future Improvements 

-  More Fine tuning to the adjustments made.
-  More and accurate metrics to track.
-  Implement and polish the rest of the game mechanics. 

### Development
  - Player animations, controller and guns were source from KINEMATION FPS Animation Framework https://assetstore.unity.com/packages/tools/animation/fps-animation-framework-238641
  - Effects were sourced from POLOYGON Particle FX Pack. https://assetstore.unity.com/packages/vfx/particles/polygon-particle-fx-low-poly-3d-art-by-synty-168372
  - Sound Effects were sourced from ZAPSPLAT. https://www.zapsplat.com/
    
