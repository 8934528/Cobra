# User Guide

This guide shows you how to create, design, run, debug, and package Python games using Cobra IDE.

## 1. Create a Project

1. Open Cobra IDE.
2. Click **New Game Project** on the welcome page (or press **Ctrl+N**).
3. Pick a project template:
   - **Pygame 2D Starter** — Basic 60 FPS game loop with player movement.
   - **Pygame Platformer** — Side-scroller with gravity, jumping, and coins.
   - **Pygame Top-Down** — Top-down game with camera follow and boundaries.
   - **Pygame RPG Starter** — Dialogue system, NPC interaction, and inventory.
   - **Pygame Arcade** — Space shooter with enemy waves and particles.
   - **Pygame Puzzle** — Grid tile puzzle with move counter and win check.
   - **Pygame Multiplayer** — Simple two-player network starter.
   - **Panda3D 3D Starter** — 3D scene with dynamic lights and camera control.
   - **Ursina 3D Starter** — 3D scene with first-person controls and grid floor.
4. Choose a project name and folder.
5. Click **Create Project**.

## 2. Manage Python & Packages

1. Click **Python & Packages** in the left sidebar.
2. Click **Create / Validate Venv** to set up a virtual environment.
3. Type a package name (such as `pygame-ce` or `pymunk`) and click **Install Package**.
4. To remove a package, select it from the list and click **Uninstall Selected**.
5. Click **Sync requirements.txt** to save your package list.

## 3. Edit Code

- Double-click any Python file in the **Project Explorer** to open it.
- Use **Ctrl+S** to save the active file, or **Ctrl+Shift+S** to save all files.
- Press **F12** to jump to the definition of a function or class.
- Press **Ctrl+P** to find and open files quickly.
- Code errors appear in the **Problems** tab at the bottom.

## 4. Design Scenes

1. Click **Scene Editor** in the left sidebar.
2. In 2D scenes, click **+ Add Object** to add sprites or shapes and drag them on the canvas.
3. In 3D scenes, use the **3D Tools & Objects Palette** on the left. Pick from categories: **Basic Shapes**, **Buildings**, **Vehicles**, **Environments**, or **Furniture & Objects**.
4. **Drag and Drop:** Click and drag any 3D tool directly into the 3D viewport to place it at that location.
5. **Import Assets & Packages:** Click **Import 3D Asset** to add `.glb`, `.gltf`, or `.obj` files, or open **3D Packages** to enable preset asset kits.
6. **Transform Gizmos:** Select objects in the 3D viewport to move, rotate, or scale them interactively.
7. In the right-hand **Inspector**, edit position, dimensions (including 3D depth), rotation angles, roughness, metalness, and colliders.
8. **Bidirectional Code Sync:** Click **Sync Code** (or save the scene Python file) to automatically update the Python scene file and synchronise changes back and forth.

## 5. Terminal & Shell Selection

- Open the **Terminal** tab in the bottom panel.
- Select your preferred shell from the dropdown: **Windows PowerShell**, **Git Bash**, or **Command Prompt**.
- Run commands, git commands, and python scripts directly in your chosen terminal.
- Your preferred terminal is saved automatically and restored whenever you open Cobra.

## 5. Visual Scripting

1. Click **Visual Scripting** in the left sidebar.
2. Choose a node from the library dropdown and click **Add Node**.
3. Click two nodes one after the other to connect them.
4. Click **Validate** to check the graph for mistakes.
5. Click **Generate Python Script** to turn the graph into Python code.

## 6. Run & Debug

- **Run Game (F5):** Starts your game. Output shows in the **Game Logs** tab.
- **Debug Game:** Starts your game with breakpoints enabled.
- **Breakpoints:** Click next to any line number in the code editor to add or remove a breakpoint.
- **Stepping:** Use **Continue**, **Step Over**, **Step Into**, and **Step Out** in the Debugger tab.
- **Stop:** Click the red **Stop** button on the toolbar to close the game.
- **Profiler:** Open the **Profiler** tab to check CPU, memory, and FPS.

## 7. Build an Executable

1. Click **Build & Package** in the left sidebar (or click **Build** on the toolbar).
2. Choose **Release Build** (windowed `.exe`) or **Development Build** (with console window).
3. Click **Package Game Now**.
4. PyInstaller packages your game, scenes, and assets into the `dist/` folder.
5. Click **Test Packaged Game** to run the finished `.exe`.
