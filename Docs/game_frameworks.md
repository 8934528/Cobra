# Supported Python Game Frameworks & Scene Editors

Cobra IDE is dedicated to Python game development, with **Pygame (`pygame-ce`)** as the primary default framework and optional support for **Panda3D** and **Ursina Engine** for 3D Python games.

## Technology Stack

### Core Game Languages & Frameworks

![Python](https://img.shields.io/badge/Python-3.10+-3776AB?style=for-the-badge&logo=python&logoColor=white)
![Pygame-CE](https://img.shields.io/badge/Pygame--CE-2D_Game_Engine-FFBE00?style=for-the-badge&logo=python&logoColor=black)
![Panda3D](https://img.shields.io/badge/Panda3D-3D_Game_Engine-8B5CF6?style=for-the-badge&logo=panda3d&logoColor=white)
![Ursina Engine](https://img.shields.io/badge/Ursina-3D_Framework-10B981?style=for-the-badge&logo=python&logoColor=white)
![GLSL](https://img.shields.io/badge/OpenGL-GLSL_Shaders-5586A4?style=for-the-badge&logo=opengl&logoColor=white)

### Physics, Math & Asset Libraries

![Pymunk](https://img.shields.io/badge/Pymunk-2D_Physics-F97316?style=for-the-badge&logo=python&logoColor=white)
![NumPy](https://img.shields.io/badge/NumPy-Vector_&_Matrix_Math-013243?style=for-the-badge&logo=numpy&logoColor=white)
![Pillow](https://img.shields.io/badge/Pillow-Sprite_&_Texture_Processing-3776AB?style=for-the-badge&logo=python&logoColor=white)
![JSON](https://img.shields.io/badge/Cobra_Scene-JSON_v2-000000?style=for-the-badge&logo=json&logoColor=white)

### Scene Viewport & Editor Visualization

![WPF Canvas](https://img.shields.io/badge/WPF_Canvas-2D_Scene_Editor-512bd4?style=for-the-badge&logo=windows&logoColor=white)
![Three.js](https://img.shields.io/badge/Three.js-3D_Scene_Viewport-000000?style=for-the-badge&logo=three.js&logoColor=white)
![WebGL](https://img.shields.io/badge/WebGL-3D_Rendering-990000?style=for-the-badge&logo=webgl&logoColor=white)

### Debugging, Quality & Distribution

![pip & venv](https://img.shields.io/badge/pip_&_venv-Virtual_Environments-3776AB?style=for-the-badge&logo=pypi&logoColor=white)
![PyInstaller](https://img.shields.io/badge/PyInstaller-Standalone_Builds-3776AB?style=for-the-badge&logo=python&logoColor=white)
![debugpy](https://img.shields.io/badge/debugpy_&_bdb-Interactive_Debugging-DC2626?style=for-the-badge&logo=python&logoColor=white)
![Ruff](https://img.shields.io/badge/Ruff-Linting_&_Formatting-D7FF64?style=for-the-badge&logo=ruff&logoColor=black)
![Pytest](https://img.shields.io/badge/Pytest_&_unittest-Automated_Tests-0A9EDC?style=for-the-badge&logo=pytest&logoColor=white)

## 1. Pygame (`pygame-ce`) — Primary 2D Framework

Pygame is the default framework for Cobra projects. Cobra provides 7 distinct Pygame templates:

- **`pygame_starter`** — 2D Starter with delta-time loop, player entity, and debug overlay.
- **`pygame_platformer`** — Side-scrolling platformer with gravity, jump physics, coin collection, and AABB tile collisions.
- **`pygame_topdown`** — 8-directional top-down adventure with camera follow and world boundaries.
- **`pygame_rpg`** — Overworld exploration with NPC interaction, dialogue system, and inventory HUD.
- **`pygame_arcade`** — Fast-paced space shooter with wave spawning, score tracking, and particle effects.
- **`pygame_puzzle`** — 4x4 sliding tile puzzle with mouse interaction, move counter, and win state.
- **`pygame_multiplayer`** — Non-blocking UDP loopback server and client demonstrating real-time 2-player state synchronization.

## 2. Panda3D & Ursina — Optional 3D Python Frameworks

Developers building 3D games in Python can choose:

- **`panda3d_starter` (`Panda3D`)**: Sets up a `ShowBase` application with ambient and directional lighting, procedural 3D geometry, and an orbiting camera controller.
- **`ursina_starter` (`Ursina`)**: Sets up an `Ursina` entity scene with directional lighting, skybox, textured floor grid, interactive cubes, and keyboard/mouse camera controls.

Both 3D frameworks are completely optional and isolated to their respective project `.venv` environments.

## 3. 2D & 3D Scene Editors and Python Synchronization

Pygame does not include a built-in level editor. Cobra provides an editor-side scene representation (`Scene` format `v2`, `.scene` JSON) that can be exported directly to a pure Python module (`scene_data.py`):

- **2D Canvas Editor**: Drag-and-drop sprite placement, configurable grid and grid snapping, zoom/pan, layer sorting, camera bounds preview, rectangle/circle collider gizmos, and undo/redo history.
- **Embedded Three.js 3D Viewport**: Provides perspective and orthographic views, orbit/pan/zoom camera controls, translation/rotation/scale gizmos, grid/axes helpers, primitive 3D meshes (`Cube`, `Sphere`, `Plane`, `Cylinder`, `Cone`), and lighting controls.
- **Python Scene Export (`ExportToPythonModule`)**: Clicking **Export to Python** writes `scene_data.py` containing `SCENE_METADATA`, `SCENE_OBJECTS`, and helper functions (`get_objects_by_layer`, `get_object_by_name`) that any Pygame, Panda3D, or Ursina script can import directly.
