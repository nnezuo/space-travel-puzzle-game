# Space Travel Puzzle Game

A compact 3D puzzle game about travelling between small planets, built with **Unity 6** and **C#**. Each planet has its own gravity rule, and the player has to use that rule to solve puzzles and move on to the next world.

This is a university project by a team of three students: **Ana Késia**, **Ntombifuthi** and **Júlia**.

## Overview

The player explores 2-3 planets, each with a distinct gravity rule and puzzle (for example, low gravity and long jumps on one planet, and localized gravity wells on another). A connecting objective and a travel transition link the planets into one short game.

## Core Technologies

| Technology | Purpose |
|---|---|
| Unity 6 | Game engine |
| C# | Gameplay scripting |
| Git | Version control |
| Rigidbody physics | Player movement and custom gravity |

## Key Features and Mechanics Implemented

- **Spherical (radial) gravity.** The player is pulled toward the center of whichever planet they are on, so they can walk all the way around it.
- **Flexible gravity rules.** Each planet or zone can use normal, low or reversed gravity, and can have a limited range with a priority, which allows localized gravity wells.
- **Surface orientation.** The player's feet stay aligned with the planet's surface as they move around the curve.
- **Camera-relative movement and jumping.** The jump uses a fixed speed, so lower gravity automatically gives higher, floatier jumps.
- **Custom planet mesh with collision.** The placeholder sphere was replaced by the pixel-art planet model `pixel-planet-kelper-452-b`, with a corrected Mesh Collider.
- **Space-themed camera.** A smooth third-person camera follows the player and keeps its "up" direction aligned with the planet's surface. The background is set up for a space look.

## Project Structure (core scripts)

| Script | Attach to | Description |
|---|---|---|
| `GravitySource.cs` | Every planet or gravity zone | Defines the gravity rule, strength, range and priority. Also finds which source currently affects the player. |
| `PlayerController.cs` | Player | Rigidbody-based movement, jumping, custom gravity and surface alignment. |
| `PlanetCamera.cs` | Main Camera | Smooth third-person follow camera with mouse look. |

> `PlanetController.cs` was the first prototype of the movement and has been replaced by `PlayerController.cs` and `GravitySource.cs`.

## Setup and Installation

### Requirements

- **Unity Hub** and **Unity 6** (use the exact editor version shown in `ProjectSettings/ProjectVersion.txt`)
- **Git**
- A code editor such as Visual Studio Code or Visual Studio (optional, for editing scripts)

### Steps

1. **Clone the repository**
```bash
   git clone <REPOSITORY-URL>
   cd <PROJECT-FOLDER>
```
2. **Open the project in Unity Hub.** Click *Add → Add project from disk*, select the cloned folder and choose the matching Unity 6 version. The first import can take a few minutes.
3. **Check the input setting.** Go to *Edit → Project Settings → Player → Other Settings → Active Input Handling* and set it to **Both** (or *Input Manager (Old)*). The scripts use the classic `Input` class.
4. **Open the main scene** from the `Assets/Scenes` folder and press **Play**.

### Controls

| Key | Action |
|---|---|
| W A S D | Move |
| Space | Jump |
| Mouse | Look around |

### Scene Setup Notes

- **Player:** a Rigidbody, a Capsule Collider and `PlayerController`. Assign the Main Camera to *Camera Transform*, and put the player on its own layer that is excluded from *Ground Mask*.
- **Planet:** a collider (Mesh Collider for custom meshes) and `GravitySource`. The planet's pivot should be at its center, and the player should not be parented to the planet.
- **Camera:** `PlanetCamera` with the player assigned as *Target*.

## Current Status

- ✅ Custom pixel-art planet imported and its Mesh Collider fixed
- ✅ Space-style camera background
- ✅ Git repository configured with a standard Unity `.gitignore`
- ✅ Radial gravity, surface orientation and jump scripts written
- 🔄 Testing spherical movement and jumping on the custom planet

## Next Steps

1. Replace the placeholder cylinder with the **animated 3D alien character** and connect its animations (idle, walk, jump) to the player's movement.
2. Test and tune the movement around the whole planet.
3. Hand the gravity system over to the team so Planet 1 and Planet 2 can set their own gravity rules.
4. Add the travel system, objectives and UI between planets.

## Team

| Member | Responsibility |
|---|---|
| Ana Késia | Core systems: player, gravity, physics, camera and integration |
| Ntombifuthi | Procedural generation and Planet 1 |
| Júlia | Planet 2, travel system, UI and game flow |

## Git Tips for the Team

- Pull before you start working: `git pull`
- Commit small, clear changes: `git commit -m "Short description"`
- Avoid editing the same scene at the same time as someone else, since Unity scene files are hard to merge.