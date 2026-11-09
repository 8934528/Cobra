# System Architecture

Cobra IDE is a Windows desktop application. It is built with **.NET 8** and **WPF**. It uses **WebView2** to run the Monaco code editor and a 3D Three.js viewport.

## Tech Stack

### Desktop Host
![.NET 8](https://img.shields.io/badge/.NET-8.0-512bd4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-12.0-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![WPF](https://img.shields.io/badge/WPF-Desktop_UI-512bd4?style=for-the-badge&logo=windows&logoColor=white)
![XAML](https://img.shields.io/badge/XAML-UI_Layout-0C54C2?style=for-the-badge&logo=xaml&logoColor=white)
![Python.NET](https://img.shields.io/badge/Python.NET-2.7.9-3776AB?style=for-the-badge&logo=python&logoColor=white)
![DPAPI](https://img.shields.io/badge/DPAPI-Secret_Protection-0078D6?style=for-the-badge&logo=windows&logoColor=white)

### Web & Viewport Layer
![WebView2](https://img.shields.io/badge/WebView2-1.0.3912-0078D7?style=for-the-badge&logo=microsoft-edge&logoColor=white)
![Monaco Editor](https://img.shields.io/badge/Monaco_Editor-Code_Editing-007ACC?style=for-the-badge&logo=visual-studio-code&logoColor=white)
![Three.js](https://img.shields.io/badge/Three.js-3D_Viewport-000000?style=for-the-badge&logo=three.js&logoColor=white)
![MSAGL](https://img.shields.io/badge/MSAGL-1.1.6-0078D6?style=for-the-badge&logo=microsoft&logoColor=white)

### Python Game Tools
![Python](https://img.shields.io/badge/Python-3.10+-3776AB?style=for-the-badge&logo=python&logoColor=white)
![Pygame-CE](https://img.shields.io/badge/Pygame--CE-2D_Engine-FFBE00?style=for-the-badge&logo=python&logoColor=black)
![Panda3D](https://img.shields.io/badge/Panda3D-3D_Engine-8B5CF6?style=for-the-badge&logo=panda3d&logoColor=white)
![Ursina Engine](https://img.shields.io/badge/Ursina-3D_Framework-10B981?style=for-the-badge&logo=python&logoColor=white)
![Pymunk](https://img.shields.io/badge/Pymunk-2D_Physics-F97316?style=for-the-badge&logo=python&logoColor=white)
![PyInstaller](https://img.shields.io/badge/PyInstaller-Build_System-3776AB?style=for-the-badge&logo=python&logoColor=white)

## Design Principles

1. **Python First:** All games, templates, and tools target Python.
2. **Modular Services:** Core logic lives in `Cobra.Core` and `Cobra.Engine`. It does not depend on UI controls. This keeps the code easy to test.
3. **Safe by Default:** File paths, packages, and commands are checked before running.

## Main Subsystems

### 1. Python Environment (`Cobra.Core.Environment`)

- **`PythonService`:** Finds installed Python versions on your computer. Uses the project `.venv` when available.
- **`VenvService`:** Creates and manages virtual environments (`python -m venv .venv`).
- **`PipPackageService`:** Installs, lists, and removes packages safely inside the active environment.
- **`PythonIntelligenceService`:** Reads Python code without running it to show syntax errors and symbol outlines.

### 2. Scene System (`Cobra.Engine.SceneSystem`)

- **`Scene`:** Stores game objects, positions, layers, colliders, and settings in JSON format.
- **Backward Compatibility:** Reads older scene files and updates them automatically.
- **Python Export:** Saves scenes to `scene_data.py` so your Python game can load them directly.
- **3D Bridge:** Syncs 3D objects with the Three.js viewport in real time.

### 3. Execution & Debugging (`Cobra.Core.Execution`)

- **`GameRunnerService`:** Starts and stops your Python game. It closes all game processes when stopped.
- **Debugger:** Uses a lightweight Python script based on `bdb` to support breakpoints, stepping, and variable inspection.
- **Profiler:** Checks CPU, memory, and estimated FPS while the game is running.

### 4. Security (`Cobra.Core.Security`)

- **`SecurityValidator`:** Blocks directory traversal and keeps files inside the project folder.
- **Asset Checks:** Validates image and audio headers and enforces a 100 MB file size limit.
- **Secret Encryption:** Encrypts API keys using Windows DPAPI so they are never saved in plain text.
