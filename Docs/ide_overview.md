# IDE Overview

Cobra IDE is a workspace built for creating Python games on Windows. It combines code editing, scene design, debugging, package management, and packaging in one simple desktop application.

## Overview

- **Language:** Python 3.10+
- **2D Game Engine:** Pygame (`pygame-ce`)
- **3D Game Engines:** Panda3D, Ursina Engine
- **Host App:** Windows 10/11 (.NET 8, WPF, WebView2)

## Tech Stack

### Desktop Host

![.NET 8](https://img.shields.io/badge/.NET-8.0-512bd4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-12.0-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![WPF](https://img.shields.io/badge/WPF-Desktop_UI-512bd4?style=for-the-badge&logo=windows&logoColor=white)
![WebView2](https://img.shields.io/badge/WebView2-Chromium_Host-0078D7?style=for-the-badge&logo=microsoft-edge&logoColor=white)
![Monaco Editor](https://img.shields.io/badge/Monaco_Editor-Code_Editor-007ACC?style=for-the-badge&logo=visual-studio-code&logoColor=white)
![Three.js](https://img.shields.io/badge/Three.js-3D_Viewport-000000?style=for-the-badge&logo=three.js&logoColor=white)

### Game Tools

![Python](https://img.shields.io/badge/Python-3.10+-3776AB?style=for-the-badge&logo=python&logoColor=white)
![Pygame-CE](https://img.shields.io/badge/Pygame--CE-2D_Framework-FFBE00?style=for-the-badge&logo=python&logoColor=black)
![Panda3D](https://img.shields.io/badge/Panda3D-3D_Framework-8B5CF6?style=for-the-badge&logo=panda3d&logoColor=white)
![Ursina Engine](https://img.shields.io/badge/Ursina-3D_Engine-10B981?style=for-the-badge&logo=python&logoColor=white)
![Pymunk](https://img.shields.io/badge/Pymunk-2D_Physics-F97316?style=for-the-badge&logo=python&logoColor=white)
![PyInstaller](https://img.shields.io/badge/PyInstaller-Packaging-3776AB?style=for-the-badge&logo=python&logoColor=white)
![Git](https://img.shields.io/badge/Git-Source_Control-F05032?style=for-the-badge&logo=git&logoColor=white)

## Main Features

### 1. Unified Workspace

- **Top Bar:** Quick access to New, Open, Save, Run, Debug, and Settings.
- **Side Panel:** Switch between Code Editor, Scene Designer, Visual Scripting, Assets, Python Tools, Git, and Settings.
- **Monaco Editor:** Syntax highlighting, code navigation, search, and error markers.
- **Bottom Panel:** Tabs for Output, Terminal (supporting Windows PowerShell, Git Bash, and Command Prompt), Debug Console, Problems, Tests, Build Output, and Game Logs.

### 2. Python Environment Tools

- Automatically finds Python interpreters on your machine.
- Creates and manages project virtual environments (`.venv`).
- Installs and removes packages using an integrated pip interface.

### 3. Starter Templates

- 7 templates for 2D Pygame games (Starter, Platformer, Top-Down, RPG, Arcade, Puzzle, Multiplayer).
- 2 templates for 3D Python games (Panda3D Starter, Ursina Starter).
- Category tabs to quickly filter between 2D and 3D templates.

### 4. Scene Designers

- **2D Canvas:** Place sprites, set grid snap, adjust layers, and add box or circle colliders.
- **3D Viewport:** Complete 3D WebGL renderer supporting 28 tools and objects across Basic Shapes, Buildings, Vehicles, Environments, and Furniture.
- **Drag-and-Drop:** Drag 3D tools or custom model files directly into the 3D scene to place objects.
- **3D Asset Packages:** Import `.glb`, `.gltf`, and `.obj` models, and install ready-to-use 3D asset packages.
- **Bidirectional Python Sync:** Automatically generates clean Python scene code and synchronises changes both ways between visual objects and code.

### 5. Debugger & Profiler

- Set breakpoints by clicking next to line numbers.
- Step through code, inspect local variables, and watch expressions.
- Track CPU usage, memory, and game frame rate in real time.

## How to Run

### Command Line

```bash
dotnet restore Cobra.sln
dotnet run --project Cobra/Cobra.csproj
```

### Run Tests

```bash
dotnet run --project Cobra.Tests/Cobra.Tests.csproj
```

### Visual Studio

1. Open `Cobra.sln`.
2. Set `Cobra` as the startup project.
3. Press **F5**.
