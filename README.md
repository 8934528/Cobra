# Cobra IDE — Python Game Development Environment

desktop Integrated Development Environment (IDE) built for Python game developers. It combines a Monaco-powered code editor, Python virtual environment and `pip` package management, 2D and 3D scene editors, visual scripting, integrated debugging, and one-click executable packaging into a unified Windows desktop workspace.

## Core Identity & Supported Frameworks

- **Primary Language**: Python 3.10+
- **Primary & Default Game Framework**: **Pygame (`pygame-ce`)** for 2D game development
- **Optional 3D Python Frameworks**: **Panda3D** and **Ursina Engine** for 3D Python games
- **Desktop Application Stack**: C# / .NET 8 (`net8.0-windows`), WPF, WebView2, Monaco Editor, and an embedded Three.js 3D viewport

## Technology Stack

### IDE Core & Desktop Architecture

![.NET 8](https://img.shields.io/badge/.NET-8.0-512bd4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-12.0-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![WPF](https://img.shields.io/badge/WPF-Desktop_UI-512bd4?style=for-the-badge&logo=windows&logoColor=white)
![XAML](https://img.shields.io/badge/XAML-Markup-0C54C2?style=for-the-badge&logo=xaml&logoColor=white)
![Python.NET](https://img.shields.io/badge/Python.NET-2.7.9-3776AB?style=for-the-badge&logo=python&logoColor=white)
![MSAGL](https://img.shields.io/badge/MSAGL-Graph_Layout-0078D6?style=for-the-badge&logo=microsoft&logoColor=white)
![MahApps.Metro](https://img.shields.io/badge/MahApps.Metro-IconPacks-0078D6?style=for-the-badge&logo=windows&logoColor=white)
![DPAPI](https://img.shields.io/badge/Windows_DPAPI-Encryption-0078D6?style=for-the-badge&logo=windows&logoColor=white)

### Embedded Web & 3D Editor Viewports

![WebView2](https://img.shields.io/badge/WebView2-1.0.3912-0078D7?style=for-the-badge&logo=microsoft-edge&logoColor=white)
![Monaco Editor](https://img.shields.io/badge/Monaco_Editor-Code_Engine-007ACC?style=for-the-badge&logo=visual-studio-code&logoColor=white)
![Three.js](https://img.shields.io/badge/Three.js-3D_Viewport-000000?style=for-the-badge&logo=three.js&logoColor=white)
![JavaScript](https://img.shields.io/badge/JavaScript-ES6+-F7DF1E?style=for-the-badge&logo=javascript&logoColor=black)
![HTML5](https://img.shields.io/badge/HTML5-Canvas_&_WebGL-E34F26?style=for-the-badge&logo=html5&logoColor=white)
![CSS3](https://img.shields.io/badge/CSS3-Styling-1572B6?style=for-the-badge&logo=css3&logoColor=white)

### Game Development & Python Runtime Stack (For Game Developers)

![Python](https://img.shields.io/badge/Python-3.10+-3776AB?style=for-the-badge&logo=python&logoColor=white)
![Pygame-CE](https://img.shields.io/badge/Pygame--CE-2D_Engine-FFBE00?style=for-the-badge&logo=python&logoColor=black)
![Panda3D](https://img.shields.io/badge/Panda3D-3D_Engine-8B5CF6?style=for-the-badge&logo=panda3d&logoColor=white)
![Ursina Engine](https://img.shields.io/badge/Ursina-3D_Framework-10B981?style=for-the-badge&logo=python&logoColor=white)
![Pymunk](https://img.shields.io/badge/Pymunk-2D_Physics-F97316?style=for-the-badge&logo=python&logoColor=white)
![NumPy](https://img.shields.io/badge/NumPy-Math_&_Arrays-013243?style=for-the-badge&logo=numpy&logoColor=white)
![Pillow](https://img.shields.io/badge/Pillow-Sprite_Imaging-3776AB?style=for-the-badge&logo=python&logoColor=white)
![OpenGL](https://img.shields.io/badge/OpenGL-GLSL_Shaders-5586A4?style=for-the-badge&logo=opengl&logoColor=white)

### Infrastructure, Packaging & Developer Tooling

![pip & venv](https://img.shields.io/badge/pip_&_venv-Package_Isolation-3776AB?style=for-the-badge&logo=pypi&logoColor=white)
![PyInstaller](https://img.shields.io/badge/PyInstaller-Game_Packaging-3776AB?style=for-the-badge&logo=python&logoColor=white)
![debugpy & bdb](https://img.shields.io/badge/debugpy_&_bdb-Python_Debugger-DC2626?style=for-the-badge&logo=python&logoColor=white)
![Ruff](https://img.shields.io/badge/Ruff-Linter_&_Formatter-D7FF64?style=for-the-badge&logo=ruff&logoColor=black)
![Pytest](https://img.shields.io/badge/Pytest-Unit_Testing-0A9EDC?style=for-the-badge&logo=pytest&logoColor=white)
![Git](https://img.shields.io/badge/Git-Source_Control-F05032?style=for-the-badge&logo=git&logoColor=white)
![PowerShell](https://img.shields.io/badge/PowerShell-Integrated_Shell-5391FE?style=for-the-badge&logo=powershell&logoColor=white)
![Windows](https://img.shields.io/badge/Windows-10_%2F_11_Target-0078D6?style=for-the-badge&logo=windows&logoColor=white)

## Documentation Index

- **[IDE Overview](Docs/ide_overview.md)** — Core capabilities, workspace layout, and architecture summary.
- **[User Guide](Docs/user_guide.md)** — Step-by-step workflows for creating projects, designing scenes, debugging, and packaging games.
- **[System Architecture](ARCHITECTURE.md)** — Technical design, services, C# <-> WebView2 bridge, and scene serialization format.
- **[Python Environment & Intelligence](Docs/python_environment.md)** — Interpreter detection, `venv` management, `pip` package manager, and AST/Ruff diagnostics.
- **[Supported Game Frameworks](Docs/game_frameworks.md)** — Pygame 2D workflow, Panda3D/Ursina 3D workflows, and 2D/3D scene synchronization.
- **[Build & Packaging Guide](Docs/build_and_packaging.md)** — Packaging standalone executables with `PyInstaller` and bundling game assets.
- **[Configuration Guide](Docs/configuration.md)** — Editor, theme, Python, engine, and layout preferences.
- **[AI Assistant Guide](Docs/ai_integration.md)** — Privacy-aware optional AI assistant configuration and DPAPI key encryption.
- **[Security Architecture](Docs/security.md)** — Path traversal prevention, asset header validation, extension verification, and policy compliance.
- **[Data Protection Policy](policies/data_protection.md)** — POPIA-aligned privacy and local data handling policy.
- **[Access Control & Incident Policy](policies/access_incident.md)** — Access boundaries, extension controls, and incident response procedures.

## Key Capabilities

1. **Professional IDE Workspace**:
   - Main Toolbar with project actions, undo/redo, run, stop, debug, build, and run configuration.
   - Activity Sidebar with Project Explorer, Asset Browser, Scene Hierarchy, Project Search, Git Source Control, Extensions, Debugger, and Project Settings.
   - Multi-tab and split-view Monaco Editor with Python symbol navigation (`Ctrl+P`, `F12`), find/replace, and live syntax diagnostics.
   - 7-Tab Resizable Bottom Panel: `Output`, `Terminal`, `Debug Console`, `Problems`, `Test Results`, `Build Output`, and `Game Logs`.
2. **Python Environment & Package Management**:
   - Automatic discovery of system Python installations (`PATH`, `py -0p`, standard Windows install paths) and project `.venv` environments.
   - Integrated `pip` manager for installing, upgrading, uninstalling, and exporting `requirements.txt` strictly inside the project interpreter.
   - Live Python AST syntax validation, symbol outline extraction, and optional `ruff` linting/formatting and `pyright` type checking.
3. **9 Ready-to-Run Python Game Templates**:
   - `Pygame 2D Starter`, `Pygame Platformer`, `Pygame Top-Down`, `Pygame RPG Starter`, `Pygame Arcade`, `Pygame Puzzle`, `Pygame Multiplayer Starter`, `Panda3D 3D Starter`, and `Ursina 3D Starter`.
4. **2D & 3D Scene Editors**:
   - Versioned `.scene` JSON format (`v2` with automatic `v1` migration) and one-click Python scene data generation (`scene_data.py`).
   - 2D Canvas Viewport with configurable grid, grid snapping, zoom/pan, layers, camera preview, box/circle collider gizmos, and undo/redo history.
   - Embedded Three.js 3D Viewport for Panda3D and Ursina projects with orbit camera, perspective/orthographic toggle, transform gizmos, lighting controls, and bidirectional C# synchronization.
5. **Integrated Python Debugger & Performance Profiler**:
   - Built-in `bdb` Python debugger harness supporting line breakpoints, conditional breakpoints, step over/into/out, continue, call stack inspection, local variables, and watch expression evaluation.
   - Real-time Performance Profiler tracking CPU usage, IDE memory, game process memory, and estimated FPS.

## Getting Started

### Prerequisites

- [.NET 8.0 SDK (Windows Desktop)](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Python 3.10 or newer](https://www.python.org/downloads/)
- [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/) (pre-installed on Windows 10/11)

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
