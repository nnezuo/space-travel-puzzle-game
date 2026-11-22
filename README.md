# Space Travel Game

A small 3D game made with **Unity 6** and **C#** for our university course. The player walks around small round planets with their own gravity, fights enemies, solves puzzles and travels with portals.

Team: **Ana Kesia**, **Ntombi Kunene** and **Júlia de Mello**.

## Why there are two games (two .exe files)

We wanted both planets in one single game, but we couldn't make the two planets run smoothly together (it was too heavy and had problems). So we made **one run file for each planet**.

## How to play (no Unity needed, the easiest way)

1. **Planet 1:** open the folder `Build Planet 1` and double-click `SpaceTravelGame.exe` (here you can still see the second planet but it's not working properly). 
2. **Planet 2 (the Dream Garden):** open the folder `Build Planet 2` and double-click `SpaceTravelGamePlanet.exe`

Please don't move the .exe out of its folder, it needs the other files next to it.
If Windows shows a blue warning window, click **More info → Run anyway**.

## How to run it inside Unity 6

1. Open the project with **Unity Hub** using the editor version **6000.6.3f1**. The first opening can take a few minutes.
2. **Planet 1:** open the scene `Assets/Scenes/planets_merged.unity` and press **Play**.
3. **Planet 2:** in the top menu click **Tools → Space Travel → Planet 2 → Open Standalone Dream Garden** (it opens the scene `Assets/Scenes/DreamGarden.unity`), then press **Play**.

Click inside the Game view so the mouse controls the camera.

## Controls

| Key | Action |
| --- | --- |
| W A S D | Move |
| Space | Jump |
| Mouse | Look around |
| Enter | Skip the intro message |
| Esc | Close a dialogue |
| **Planet 1** | |
| F | Attack an enemy |
| 1 / 2 / 3 | Change gravity: Low / Normal / Reversed (float up) |
| E | Use the portal |
| **Planet 2** | |
| E | Interact |
| Q | Change the world between Bloom and Ruin (near a pedestal) |
| R | Go back to the last checkpoint |

## Goal of each planet

- **Planet 1:** find and defeat the enemies and touch the crystals they are guarding. Then the portal opens.
- **Planet 2 (Dream Garden):** switch the garden between Bloom and Ruin to open new paths, solve the puzzles and find the 3 memories to unlock the castle portal.

## Please read the documentation

For all the details, please read **`DOCUMENTATION - Project & Code Guide.docx`** (in the main folder of the project). It explains the whole project, how the planets are made, what every script does and who made each part.

Screenshots and a video of the game are in the folder `Videos and Pictures`.

## Team roles

| Member | What they did |
| --- | --- |
| **Ana Kesia** | Player movement, the planet gravity, the camera, the big upgrades of the planet generation (planet shape, colours, trees, rivers), day/night and sky, the game HUD, and putting everything together in the project. |
| **Ntombi Kunene** | Planet 1: the puzzles with the crystals, the enemies and the fight, the portals. She also made the first version of the planet generation (`CubeSphereGenerator` and `PlanetNoise`). |
| **Júlia de Mello** | Everything about Planet 2, the Dream Garden: the scene, the puzzles, the dialogues, the camera and her editor tools. |

## Known problem

The character animations don't work (we didn't have time to fix it), so the character slides when it moves.

## AI Assistance Disclosure

During the development of this project, our team used AI as a supplementary learning and development tool.

AI assistance was mainly used to help us understand and navigate the Unity development environment, particularly the creation and manipulation of 3D objects, the Hierarchy, the Inspector, and the configuration of GameObjects and components.

We also used AI occasionally to help explain C# concepts, understand error messages, troubleshoot issues, and provide guidance on certain parts of the code.

AI was used as a support resource throughout the learning and development process, while our team remained responsible for implementing, testing, integrating, and making decisions about the game's features.
