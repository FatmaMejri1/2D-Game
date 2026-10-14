# Escape Room 2D - Course version + Jump mode

## Install
1. Close Unity (back up first).
2. Copy ALL files from `Assets/scripts/` into your project's `Assets/scripts/` (overwrite).
   DELETE the old `SceneFlow.cs` if you have it (it was split into MainMenu.cs, EndScreen.cs,
   GameSceneSetup.cs, WorldLabel.cs, GameResult.cs).
3. Copy ALL files from `Assets/Editor/` into `Assets/Editor/` (overwrite).
4. Open Unity, wait for compile (Console: no red errors).
5. Menu **Escape Room > Build Full Course Project** (20-60 s). The MainMenu scene opens: press Play.

## Build the level inside YOUR scene (everything visible in the Scene view)
Open your scene (for example `level1` with your Background), then use the menu
**Escape Room > Build Level Into Current Scene**. It keeps what is already in the scene and adds:
Grid (Floor + Walls Tilemaps), Items, Hazards, Enemies, Doors, Props, Player, Main Camera and the managers.
Everything is a normal object you can select, move, duplicate or delete, including the HUD
("HUD Canvas": panels, hearts, texts, banner...). Only the behaviour is code.
Nothing is generated automatically at Play any more: if the Hierarchy is empty, the scene is empty.
(The old code-built version only runs if you add the LevelSetupHelper component yourself.)
Note: "Build Full Course Project" saves Assets/Scenes/Level1.unity - on Windows that is the same file as
`level1.unity`, so it would overwrite your own scene. Use "Into Current Scene" to keep it.

## Scenes (Assets/Scenes, already in Build Settings)
MainMenu - Level1 (escape room, top-down) - Platformer (jump mode) - GameOver - Victory.

## Jump mode (side-scroller)
- LEFT/RIGHT (or A/D) move, SPACE (or W / UP) jump, stomp enemies, springs launch you.
- Gravity (Rigidbody2D gravity scale 3), jump with AddForce, ground check, coyote time, no-friction material.
- Tilemap ground (tiles merged with a CompositeCollider2D), checkpoints, falling in a pit costs a life.
- Collect 3 gems, then touch the flag.
- Obstacles: spikes (tag "danger"), a saw moved by an AnimationClip, walking enemies.

## Tag "danger" (slide 14)
PlayerMovement.OnCollisionEnter2D / OnTriggerEnter2D check `other.tag != "danger"` and call
GameManager.Damage(). Spikes, MovingSaw, Saw, Laser, SpikeTile, Spinner and Sentinel prefabs use that tag.

## Sprite sheet (Sprite Editor)
`Assets/Sprites/player_sheet.png` is imported with Sprite Mode = Multiple, 32 pixels per unit
(4 x 2 frames of 32 x 32). The tool tries to slice it automatically (player_sheet_0 ... _7).
If it says it could not: select the file > Sprite Editor > Slice > Type "Grid By Cell Size" 32 x 32
> Slice > Apply, then run the menu again. The jump-mode player animations use those sprites.

## Controls
Escape room: WASD/arrows, Shift sprint, E interact. Both: Esc pause, R restart, Q menu (paused), M mute.
