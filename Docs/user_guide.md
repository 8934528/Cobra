# User Guide: Developing Python Games in Cobra IDE

This guide walks through creating, designing, running, debugging, and packaging Python games in Cobra IDE.

## 1. Creating a New Python Game Project

1. Launch Cobra IDE and click **New Project** on the Welcome Dashboard (or click a template card directly in the **Python Game Templates** showcase).
2. Choose one of the 9 Python project templates:
   - **Pygame 2D Starter** — Clean 60 FPS game loop with delta-time movement and HUD.
   - **Pygame Platformer** — Side-scrolling platformer with gravity, jump physics, and AABB tile collisions.
   - **Pygame Top-Down** — 8-directional exploration with smooth camera follow and world grid.
   - **Pygame RPG Starter** — Turn-based and overworld RPG starter with NPC dialogue and inventory HUD.
   - **Pygame Arcade** — Fast-paced space shooter with wave spawning, lasers, and particle explosions.
   - **Pygame Puzzle** — Grid-based sliding tile puzzle with move counter and win detection.
   - **Pygame Multiplayer Starter** — Non-blocking UDP loopback server + client starter for 2-player sync.
   - **Panda3D 3D Starter** — 3D scene graph starter with dynamic lighting and orbiting camera.
   - **Ursina 3D Starter** — 3D voxel/entity starter with WASD movement and mouse orbit controls.
3. Select a Python interpreter (or keep **System Default**), leave **Create isolated `.venv` and install framework dependencies** checked, and click **Create Project**.

## 2. Managing Your Python Environment & Packages

- Open the **Python Environment & Packages** panel from the left Activity Sidebar (Python icon).
- Click **Create / Validate .venv** to create or verify the project's `.venv` virtual environment.
- Type a package specification (e.g., `pygame-ce`, `pymunk`, `pytest`, `pyinstaller`) and click **Install** to install it into the project's interpreter.
- Select any installed package in the list to **Upgrade** or **Remove** it, or click **Sync requirements.txt** to freeze dependencies.

## 3. Editing Code & Using Python Intelligence

- Double-click any `.py` file in the **Project Explorer** to open it in a Monaco Editor tab.
- Click the **Split Editor** icon in the tab bar to view a reference file side-by-side.
- Use `Ctrl+P` for Quick File Open, `F12` for Go to Symbol Definition, and the **Format / Lint (Ruff)** button in the Python panel to format the active script.
- Syntax and linter diagnostics appear automatically in the **Problems** tab of the bottom panel.

## 4. Designing 2D & 3D Scenes

1. Switch to the **Scene Editor** workspace tab.
2. Use the scene toolbar to create a **New Scene**, **Open Scene**, **Save Scene**, **Duplicate Scene**, or **Export to Python (`scene_data.py`)**.
3. Click **+ Sprite**, **+ Physics Box**, **+ Tile**, or **+ 3D Mesh** (in 3D mode) to add objects to the scene.
4. Drag objects in the 2D canvas (with optional **Snap to Grid** and configurable grid size) or toggle **3D View** to inspect and manipulate objects in the embedded Three.js 3D viewport.
5. Select any object in the viewport or **Scene Hierarchy** to edit its position, rotation, scale, layer, sorting order, color, sprite path, script path, collider (`Rectangle` / `Circle`), and physics properties in the right-hand **Inspector**.

## 5. Visual Scripting

1. Switch to the **Visual Scripting** workspace tab.
2. Search or filter the categorized node library (`Events`, `Movement`, `Collision`, `Audio`, `Variables`, `Scene`, `Logic`, `Timers`) and click a node to add it to the graph.
3. Click **Connect Selected -> Last**, **Validate**, **Save Graph**, or **Generate Python (`visual_script_generated.py`)** to compile the node graph into executable Python code.

## 6. Running, Debugging & Profiling Your Game

- **Run Game (`F5`)**: Launches the selected startup `.py` file using the project's Python interpreter and streams stdout/stderr to the **Game Logs** tab.
- **Debug Game**: Starts the Python `bdb` debugger harness, pausing at any gutter breakpoints set in Monaco Editor. Use **Continue**, **Step Over**, **Step Into**, and **Step Out** in the **Debugger** sidebar, inspect the **Call Stack** and **Local Variables**, and evaluate expressions in the watch box.
- **Profiler**: Switch to the **Profiler** workspace tab while a game is running to monitor real-time CPU %, IDE memory, Game memory, and FPS.

## 7. Packaging Your Game for Distribution

- Click the **Build Game** button on the Main Toolbar.
- Choose **Development Build (Console Window)**, **Release Build (Windowed Executable)**, **Clean Build Artifacts**, or **Test Packaged Executable**.
- Cobra invokes `PyInstaller` inside your project environment, bundles the `assets/` and `scenes/` directories, and outputs the standalone build to `dist/`.
