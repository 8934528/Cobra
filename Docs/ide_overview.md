# Cobra IDE — Project Overview

Cobra is a desktop Integrated Development Environment (IDE) built specifically for Python game developers. It combines a Monaco-powered code editor, isolated Python virtual environment and `pip` package management, visual 2D and 3D scene designers, node-based visual scripting, an integrated Python debugger, and `PyInstaller` executable packaging into a single cohesive workspace.

## Core Identity

- **Primary Programming Language**: Python 3.10+
- **Default & Primary Game Framework**: **Pygame (`pygame-ce`)**
- **Optional 3D Python Game Frameworks**: **Panda3D** and **Ursina Engine**
- **Desktop Application Stack**: C# / .NET 8 (`net8.0-windows`), WPF, WebView2, Monaco Editor, and an embedded Three.js 3D viewport for 3D scene visualization.

## Technology Stack

### IDE Desktop & Host Platform

![.NET 8](https://img.shields.io/badge/.NET-8.0-512bd4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-12.0-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![WPF](https://img.shields.io/badge/WPF-Desktop_UI-512bd4?style=for-the-badge&logo=windows&logoColor=white)
![WebView2](https://img.shields.io/badge/WebView2-Chromium_Host-0078D7?style=for-the-badge&logo=microsoft-edge&logoColor=white)
![Monaco Editor](https://img.shields.io/badge/Monaco_Editor-Code_Editor-007ACC?style=for-the-badge&logo=visual-studio-code&logoColor=white)
![Three.js](https://img.shields.io/badge/Three.js-3D_Viewport-000000?style=for-the-badge&logo=three.js&logoColor=white)

### Developer Game Creation Stack

![Python](https://img.shields.io/badge/Python-3.10+-3776AB?style=for-the-badge&logo=python&logoColor=white)
![Pygame-CE](https://img.shields.io/badge/Pygame--CE-2D_Framework-FFBE00?style=for-the-badge&logo=python&logoColor=black)
![Panda3D](https://img.shields.io/badge/Panda3D-3D_Framework-8B5CF6?style=for-the-badge&logo=panda3d&logoColor=white)
![Ursina Engine](https://img.shields.io/badge/Ursina-3D_Engine-10B981?style=for-the-badge&logo=python&logoColor=white)
![Pymunk](https://img.shields.io/badge/Pymunk-2D_Physics-F97316?style=for-the-badge&logo=python&logoColor=white)
![PyInstaller](https://img.shields.io/badge/PyInstaller-Packaging-3776AB?style=for-the-badge&logo=python&logoColor=white)
![Git](https://img.shields.io/badge/Git-Source_Control-F05032?style=for-the-badge&logo=git&logoColor=white)

## Core Features

1. **Professional Game Development Workspace**:
   - **Main Toolbar**: New Project, Open Project, Save, Save All, Undo, Redo, Startup Script selector, Run Game (`F5`), Stop Game, Debug Game, Build Game, Run Configuration, and Settings.
   - **Activity Sidebar**: Quick switching between Project Explorer, Asset Browser, Scene Hierarchy, Project Search, Python Environment Manager, Git Source Control, Extensions, Debugger, and Project Settings.
   - **Main Editor Area**: Multi-tab and split-view Monaco Editor with Python syntax highlighting, symbol outline, Go to Definition (`F12`), Quick Open (`Ctrl+P`), and live syntax diagnostics.
   - **7-Tab Bottom Panel**: Resizable and collapsible tabs for `Output`, `Terminal`, `Debug Console`, `Problems`, `Test Results`, `Build Output`, and `Game Logs`.
   - **Workspace Layouts**: Switch between `Default`, `Code Focus`, `Scene Design`, and `Debugging` presets or toggle full-screen editing (`F11`).

2. **Python Environment & Package Management**:
   - Automatic discovery of system Python interpreters and project `.venv` virtual environments.
   - One-click `.venv` creation and validation.
   - Integrated `pip` package manager to install, upgrade, remove, and export packages to `requirements.txt` strictly within the project interpreter.

3. **9 Ready-to-Run Python Game Templates**:
   - **2D Pygame Templates**: `Pygame 2D Starter`, `Pygame Platformer`, `Pygame Top-Down`, `Pygame RPG Starter`, `Pygame Arcade`, `Pygame Puzzle`, `Pygame Multiplayer Starter`.
   - **3D Python Templates**: `Panda3D 3D Starter`, `Ursina 3D Starter`.

4. **2D & 3D Scene Editors**:
   - Versioned `.scene` JSON format (`v2` with automatic `v1` migration) and one-click export to Python (`scene_data.py`).
   - 2D Canvas editor with configurable grid, grid snapping, zoom/pan, layers, camera viewport bounds, box/circle colliders, and undo/redo history.
   - Optional Three.js 3D viewport for Panda3D and Ursina projects with perspective/orthographic camera, transform gizmos, lighting controls, and bidirectional C# synchronization.

5. **Asset Browser, Animation & Physics Tools**:
   - Magic-byte validated asset importing (`PNG`, `JPEG`, `GIF`, `BMP`, `WEBP`, `WAV`, `OGG`, `MP3`, `TTF`, `OTF`, `OBJ`, `GLTF`, `GLB`), audio preview, duplicate detection, and missing asset reference auditing.
   - 2D Sprite Sheet Animation previewer with configurable frame dimensions and FPS playback.
   - Physics inspector supporting `Rectangle` and `Circle` colliders, `Pymunk` rigidbody mass, friction, bounce, and gravity settings.

6. **Python Debugger & Performance Profiler**:
   - Integrated `bdb` Python debugger supporting line breakpoints, conditional breakpoints, step over, step into, step out, continue, call stack frames, local variables, and watch expressions.
   - Real-time Performance Profiler tracking CPU usage, IDE memory, game process memory, and estimated FPS.

## How to Build & Run Cobra IDE

### 1. Run from Bash (erminal Bash)

Open a **Bash** terminal in the root folder (`c:/Users/Mihlali/Desktop/Cobra`) and run:

```bash
# 1. Restore NuGet packages (only needed once)
dotnet restore Cobra.sln

# 2. Build and launch the Cobra IDE desktop app
dotnet run --project Cobra/Cobra.csproj
```

If you already restored packages and want a fast offline build & launch:

```bash
dotnet run --project Cobra/Cobra.csproj --no-restore
```

To run the automated verification test suite:

```bash
dotnet run --project Cobra.Tests/Cobra.Tests.csproj
```

### 2. Run from PowerShell (Antigravity / VS Code Terminal)

```powershell
# Build and launch Cobra IDE
dotnet run --project .\Cobra\Cobra.csproj

# Or run the compiled executable directly after building:
dotnet build .\Cobra.sln
.\Cobra\bin\Debug\net8.0-windows\Cobra.exe
```

### 3. Run with `F5` in VS Code / Antigravity IDE (Optional)

If you want to press **`F5`** (`Run and Debug`) directly inside VS Code or Antigravity IDE:

1. Install the **C#** (`ms-dotnettools.csharp`) or **C# Dev Kit** extension.
2. Open the Command Palette (`Ctrl+Shift+P`), run **.NET: Generate Assets for Build and Debug**, or create `.vscode/launch.json` with:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Launch Cobra IDE",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/Cobra/bin/Debug/net8.0-windows/Cobra.dll",
      "args": [],
      "cwd": "${workspaceFolder}/Cobra",
      "stopAtEntry": false
    }
  ]
}
```

### 4. Run in Visual Studio (2019 / 2022 / 2026)

1. Ensure the **.NET desktop development** workload and **.NET 8.0 SDK** are installed via the Visual Studio Installer.
2. Open `Cobra.sln` in **Visual Studio 2019 (v16.11+ with .NET 8 SDK)**, **Visual Studio 2022**, or **Visual Studio 2026**.
3. In **Solution Explorer**, right-click the **`Cobra`** project and select **Set as Startup Project**.
4. Press **`F5`** (Start Debugging) or **`Ctrl+F5`** (Start Without Debugging) to build and launch Cobra IDE.
