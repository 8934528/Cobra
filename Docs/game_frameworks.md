# Game Frameworks & Scene Editors

Cobra IDE is built for Python game development. It focuses on **Pygame (`pygame-ce`)** for 2D games and supports **Panda3D** and **Ursina Engine** for 3D games.

## Supported Frameworks

### 1. Pygame (pygame-ce) — Primary 2D Engine

Pygame is the default engine for Cobra IDE projects. Cobra includes 7 ready-to-use templates:

- **Pygame Starter:** Basic loop with delta time, player movement, and debug text.
- **Platformer:** Gravity, jumping, platforms, and coin pickup.
- **Top-Down:** 8-direction movement with smooth camera tracking.
- **RPG Starter:** NPC dialogue, item pickups, and inventory display.
- **Arcade:** Space shooter with enemy waves and particle effects.
- **Puzzle:** 4x4 sliding tile puzzle with move tracking and win detection.
- **Multiplayer:** 2-player local network starter using UDP sockets.

### 2. Panda3D & Ursina — 3D Engines

If you want to build 3D games in Python, you can use:

- **Panda3D:** Uses a 3D scene graph with dynamic lights and camera controls.
- **Ursina:** Easy-to-use 3D engine with simple entities and built-in controls.

Each framework runs in its own virtual environment with its own dependencies.

## Scene Editors

### 2D Canvas Editor

- Add sprites, boxes, and shapes to your scene.
- Snap objects to a customizable grid.
- Adjust layers and draw order.
- Add circle or box colliders for physics.
- Preview camera bounds and game boundaries.

### 3D Viewport

- Built with Three.js to show 3D scenes.
- Orbit, pan, and zoom camera controls.
- Tools to move, rotate, and scale 3D objects.
- Add 3D shapes like cubes, spheres, and planes.

### Exporting Scenes to Python

Click **Save Scene** to save your level data to a `.scene` file. Then click **Export to Python** to create `scene_data.py`. Your Python game can import this file and load your levels directly.
