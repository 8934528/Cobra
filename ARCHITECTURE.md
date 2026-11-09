# Cobra IDE — System Architecture

Cobra IDE is a Windows desktop application built on **.NET 8 (`net8.0-windows`)** and **WPF**, combining native C# desktop services with embedded **WebView2** components (**Monaco Editor** for Python code editing and **Three.js** for optional 3D scene visualization).

## Technology Stack

### Core & Desktop Host

![.NET 8](https://img.shields.io/badge/.NET-8.0-512bd4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-12.0-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![WPF](https://img.shields.io/badge/WPF-Desktop_UI-512bd4?style=for-the-badge&logo=windows&logoColor=white)
![XAML](https://img.shields.io/badge/XAML-UI_Layout-0C54C2?style=for-the-badge&logo=xaml&logoColor=white)
![Python.NET](https://img.shields.io/badge/Python.NET-2.7.9-3776AB?style=for-the-badge&logo=python&logoColor=white)
![DPAPI](https://img.shields.io/badge/DPAPI-Secret_Protection-0078D6?style=for-the-badge&logo=windows&logoColor=white)

### Embedded Web & Visualization Layer

![WebView2](https://img.shields.io/badge/WebView2-1.0.3912-0078D7?style=for-the-badge&logo=microsoft-edge&logoColor=white)
![Monaco Editor](https://img.shields.io/badge/Monaco_Editor-Code_Editing-007ACC?style=for-the-badge&logo=visual-studio-code&logoColor=white)
![Three.js](https://img.shields.io/badge/Three.js-3D_Viewport-000000?style=for-the-badge&logo=three.js&logoColor=white)
![MSAGL](https://img.shields.io/badge/MSAGL-1.1.6-0078D6?style=for-the-badge&logo=microsoft&logoColor=white)

### Target Game Development Ecosystem

![Python](https://img.shields.io/badge/Python-3.10+-3776AB?style=for-the-badge&logo=python&logoColor=white)
![Pygame-CE](https://img.shields.io/badge/Pygame--CE-2D_Engine-FFBE00?style=for-the-badge&logo=python&logoColor=black)
![Panda3D](https://img.shields.io/badge/Panda3D-3D_Engine-8B5CF6?style=for-the-badge&logo=panda3d&logoColor=white)
![Ursina Engine](https://img.shields.io/badge/Ursina-3D_Framework-10B981?style=for-the-badge&logo=python&logoColor=white)
![Pymunk](https://img.shields.io/badge/Pymunk-2D_Physics-F97316?style=for-the-badge&logo=python&logoColor=white)
![PyInstaller](https://img.shields.io/badge/PyInstaller-Build_System-3776AB?style=for-the-badge&logo=python&logoColor=white)

## Architectural Principles

1. **Python-First Game Development**: All game projects, templates, runtime execution, debugging, and packaging target Python (`Pygame`, `Panda3D`, `Ursina`).
2. **Decoupled Core Services**: Project management, Python environment isolation, asset validation, scene serialization, execution/debugging, Git source control, and build packaging live in `Cobra.Core` and `Cobra.Engine` with zero hard dependencies on WPF controls, making them testable and maintainable.
3. **Defense-in-Depth Security**: All filesystem paths, archive entries, pip package specifications, command arguments, and asset headers pass through `SecurityValidator` before execution.

## Key Subsystems

### 1. Python Environment & Intelligence (`Cobra.Core.Environment`)

- **`PythonService`**: Scans `PATH`, Windows `py.exe -0p` launcher registrations, and standard install locations (`%LOCALAPPDATA%\Programs\Python`, `C:\Python3*`) to list verified interpreters. Resolves a project's active interpreter by preferring `.venv\Scripts\python.exe`.
- **`VenvService`**: Creates and validates isolated virtual environments (`python -m venv .venv`), upgrades `pip`, and installs framework dependencies (`pygame-ce`, `panda3d`, `ursina`, `pymunk`).
- **`PipPackageService`**: Executes `pip list --format=json`, `pip install`, `pip uninstall`, and `pip freeze` strictly against the project interpreter after validating package specifications via `SecurityValidator.IsValidPipPackageSpec`.
- **`PythonIntelligenceService`**: Executes a read-only Python `ast` inspection pass (never importing or executing user code) to report syntax diagnostics and symbol outlines (classes, functions, imports), supplemented by `ruff` and `pyright` when available.

### 2. Scene Serialization & 2D/3D Synchronization (`Cobra.Engine.SceneSystem`)

- **`Scene` (`FormatVersion = 2`)**: Stores scene metadata (`SceneName`, `Framework`, `GridSize`, `SnapToGrid`, `CameraWidth`, `CameraHeight`, `BackgroundColor`, `AmbientLightColor`, `DirectionalLightIntensity`) and a hierarchical list of `SceneObject` instances.
- **Backward Compatibility**: Automatically migrates legacy `v1` `.scene` files on load by normalizing missing scales, layers, and collider fields.
- **Python Scene Export (`ExportToPythonModule`)**: Generates a clean, self-contained Python dictionary module (`scene_data.py`) so Pygame, Panda3D, and Ursina games can load editor scenes directly without runtime C# dependencies.
- **Three.js 3D Bridge (`viewport3d.html`)**: Uses `CoreWebView2.PostWebMessageAsJson` to synchronize `SceneObject` transforms, primitive meshes (`Cube`, `Sphere`, `Plane`, `Cylinder`, `Cone`), colors, and lighting between C# and the embedded Three.js viewport.

### 3. Game Execution, Debugging & Profiling (`Cobra.Core.Execution`)

- **`GameRunnerService`**:
  - Launches Python games with `PYTHONUNBUFFERED=1`, custom working directory, CLI arguments, and per-project environment variables.
  - Terminates the entire process tree (`Process.Kill(entireProcessTree: true)`) when stopping a game or closing the IDE.
  - Generates a lightweight `.cobra_dbg_harness.py` wrapper around Python's standard `bdb.Bdb` module when starting in Debug Mode, streaming JSON debug events (`__COBRA_DBG__:{...}`) for breakpoints, conditional expressions, call stack frames, local variables, and watch evaluations.
  - Samples process CPU time and working set memory on a background timer to feed the real-time Performance Profiler.

### 4. Security & Data Protection (`Cobra.Core.Security`)

- **`SecurityValidator`**:
  - `TryResolveSafeChildPath`: Canonicalizes paths via `Path.GetFullPath` and verifies strict containment inside the project root.
  - `ValidateAssetFile`: Verifies file extensions, enforces a 100 MB size cap, and checks binary magic-byte headers (`PNG`, `JPEG`, `GIF`, `BMP`, `WEBP`, `WAV`, `OGG`, `MP3`, `TTF`, `OTF`, `GLB`) and UTF-8 text validity.
  - `ProtectSecret` / `UnprotectSecret`: Encrypts sensitive credentials at rest using Windows DPAPI (`ProtectedData` with `DataProtectionScope.CurrentUser`).
