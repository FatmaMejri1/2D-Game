# Escape Room Game 2D

A Unity 2D game project with a top-down escape-room adventure and a side-scrolling platformer mode.

## Requirements

- Unity **6000.6.2f1** (Unity 6)
- Git, if you want to clone or contribute to the repository

Open the repository folder in Unity Hub. Unity will resolve the packages listed in
`Packages/manifest.json` the first time the project opens.

## Getting started

The repository includes the game assets and an existing `Assets/Scenes/level1.unity` scene.
To generate the complete course project, open the project in Unity and select:

**Escape Room > Build Full Course Project**

This editor command generates sprites, tiles, animations, prefabs, and the game scenes
(`MainMenu`, `Level1`, `Platformer`, `GameOver`, and `Victory`), then configures them in Build
Settings. When it finishes, the `MainMenu` scene opens; press **Play** to start.

> **Warning:** Building the full project recreates `Assets/Scenes/Level1.unity`. Back up any
> changes to that scene before running the command.

To add the escape-room level to a scene you are already editing, open that scene and select:

**Escape Room > Build Level Into Current Scene**

This keeps existing scene objects and adds the level, player, HUD, and managers. Save your work
first. The command saves the scene and adds it to Build Settings; use an empty scene or remove
the existing level first if it already has a Game Manager.

If Unity reports that the player sprite sheet could not be sliced automatically, select
`Assets/Sprites/player_sheet.png`, open **Sprite Editor**, choose **Slice > Grid By Cell Size**,
set the cell size to **32 × 32**, then select **Slice** and **Apply**. Run the full-project build
again afterward.

## Game modes

### Escape room

Explore the top-down level, collect items, avoid hazards, interact with objects, and make your
way to the exit. Keys, gems, levers, doors, checkpoints, and power-ups support the adventure.

### Platformer

Move through a side-scrolling level, jump across platforms, avoid hazards, use jump pads, and
reach the goal after collecting the required gems.

## Controls

| Action | Controls |
| --- | --- |
| Move | **WASD** or **Arrow keys** |
| Sprint (escape room) | **Shift** |
| Interact | **E** |
| Jump (platformer) | **Space**, **W**, or **Up Arrow** |
| Confirm menu selection | **Space** or **Enter** |
| Pause / resume | **Esc** or **P** |
| Restart | **R** |
| Mute / unmute | **M** |
| Return to main menu while paused | **Q** |

## Project structure

- `Assets/scripts/` — gameplay, player, UI, and game-state scripts
- `Assets/Editor/` — editor tools for generating prefabs, assets, and scenes
- `Assets/Scenes/` — Unity scenes, including the existing level scene
- `Assets/Prefabs/` — reusable game objects
- `Assets/Art/`, `Assets/Sprites/`, and `Assets/Animations/` — visual and animation assets
- `Packages/manifest.json` — Unity package dependencies
- `ProjectSettings/` — Unity project configuration

Unity-generated folders and files such as `Library/`, `Temp/`, and generated solution files are
excluded from version control by `.gitignore`.
