# Cobra IDE

This is an editor for making Python games on Windows. It brings together code editing, scene design, debugging, package management, and game building in one simple tool.

## Key Facts

- **Language:** Python 3.10 or newer
- **Main 2D Engine:** Pygame (`pygame-ce`)
- **Supported 3D Engines:** Panda3D and Ursina Engine
- **Platform:** Windows 10 and 11 (.NET 8 and WPF)

## Tech Stack

### Desktop App

![.NET 8](https://img.shields.io/badge/.NET-8.0-512bd4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-12.0-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![WPF](https://img.shields.io/badge/WPF-Desktop_UI-512bd4?style=for-the-badge&logo=windows&logoColor=white)
![XAML](https://img.shields.io/badge/XAML-Markup-0C54C2?style=for-the-badge&logo=xaml&logoColor=white)
![Python.NET](https://img.shields.io/badge/Python.NET-2.7.9-3776AB?style=for-the-badge&logo=python&logoColor=white)
![MSAGL](https://img.shields.io/badge/MSAGL-Graph_Layout-0078D6?style=for-the-badge&logo=microsoft&logoColor=white)
![MahApps.Metro](https://img.shields.io/badge/MahApps.Metro-IconPacks-0078D6?style=for-the-badge&logo=windows&logoColor=white)
![DPAPI](https://img.shields.io/badge/Windows_DPAPI-Encryption-0078D6?style=for-the-badge&logo=windows&logoColor=white)

### Web & 3D Viewport

![WebView2](https://img.shields.io/badge/WebView2-1.0.3912-0078D7?style=for-the-badge&logo=microsoft-edge&logoColor=white)
![Monaco Editor](https://img.shields.io/badge/Monaco_Editor-Code_Engine-007ACC?style=for-the-badge&logo=visual-studio-code&logoColor=white)
![Three.js](https://img.shields.io/badge/Three.js-3D_Viewport-000000?style=for-the-badge&logo=three.js&logoColor=white)
![JavaScript](https://img.shields.io/badge/JavaScript-ES6+-F7DF1E?style=for-the-badge&logo=javascript&logoColor=black)
![HTML5](https://img.shields.io/badge/HTML5-Canvas_&_WebGL-E34F26?style=for-the-badge&logo=html5&logoColor=white)
![CSS3](https://img.shields.io/badge/CSS3-Styling-1572B6?style=for-the-badge&logo=css3&logoColor=white)

### Game Tools

![Python](https://img.shields.io/badge/Python-3.10+-3776AB?style=for-the-badge&logo=python&logoColor=white)
![Pygame-CE](https://img.shields.io/badge/Pygame--CE-2D_Engine-FFBE00?style=for-the-badge&logo=python&logoColor=black)
![Panda3D](https://img.shields.io/badge/Panda3D-3D_Engine-8B5CF6?style=for-the-badge&logo=panda3d&logoColor=white)
![Ursina Engine](https://img.shields.io/badge/Ursina-3D_Framework-10B981?style=for-the-badge&logo=python&logoColor=white)
![Pymunk](https://img.shields.io/badge/Pymunk-2D_Physics-F97316?style=for-the-badge&logo=python&logoColor=white)
![NumPy](https://img.shields.io/badge/NumPy-Math_&_Arrays-013243?style=for-the-badge&logo=numpy&logoColor=white)
![Pillow](https://img.shields.io/badge/Pillow-Sprite_Imaging-3776AB?style=for-the-badge&logo=python&logoColor=white)
![OpenGL](https://img.shields.io/badge/OpenGL-GLSL_Shaders-5586A4?style=for-the-badge&logo=opengl&logoColor=white)

### Build & Dev Tools

![pip & venv](https://img.shields.io/badge/pip_&_venv-Package_Isolation-3776AB?style=for-the-badge&logo=pypi&logoColor=white)
![PyInstaller](https://img.shields.io/badge/PyInstaller-Game_Packaging-3776AB?style=for-the-badge&logo=python&logoColor=white)
![debugpy & bdb](https://img.shields.io/badge/debugpy_&_bdb-Python_Debugger-DC2626?style=for-the-badge&logo=python&logoColor=white)
![Ruff](https://img.shields.io/badge/Ruff-Linter_&_Formatter-D7FF64?style=for-the-badge&logo=ruff&logoColor=black)
![Pytest](https://img.shields.io/badge/Pytest-Unit_Testing-0A9EDC?style=for-the-badge&logo=pytest&logoColor=white)
![Git](https://img.shields.io/badge/Git-Source_Control-F05032?style=for-the-badge&logo=git&logoColor=white)
![PowerShell](https://img.shields.io/badge/PowerShell-Integrated_Shell-5391FE?style=for-the-badge&logo=powershell&logoColor=white)
![Windows](https://img.shields.io/badge/Windows-10_%2F_11_Target-0078D6?style=for-the-badge&logo=windows&logoColor=white)

## Documentation

- **[IDE Overview](Docs/ide_overview.md)** — Main features and layout.
- **[User Guide](Docs/user_guide.md)** — Steps to create, edit, run, and export games.
- **[System Architecture](ARCHITECTURE.md)** — App structure, services, and scene format.
- **[Python Environment](Docs/python_environment.md)** — Interpreters, virtual environments, and packages.
- **[Game Frameworks](Docs/game_frameworks.md)** — Pygame, Panda3D, and Ursina guides.
- **[Build & Packaging](Docs/build_and_packaging.md)** — Turn games into Windows executable files.
- **[Configuration](Docs/configuration.md)** — User and project settings.
- **[AI Assistant](Docs/ai_integration.md)** — Optional offline and online coding help.
- **[Security](Docs/security.md)** — File safety and data protection rules.
- **[Data Protection Policy](policies/data_protection.md)** — Local storage and privacy policy.
- **[Access Control Policy](policies/access_incident.md)** — Security boundaries and incident handling.

## Features

1. **Workspace:** Monaco code editor, file tree, scene hierarchy, Git tools, and a 7-tab bottom panel.
2. **Python Tools:** Auto-detects Python, creates virtual environments (`.venv`), and installs packages.
3. **Game Templates:** 9 starter projects for 2D and 3D games.
4. **Scene Designers:** 2D grid canvas and 3D viewport with Three.js.
5. **Debugger & Profiler:** Breakpoints, variable inspection, call stack, and real-time CPU and FPS graphs.
6. **Executable Export:** Package your game into a single `.exe` file with PyInstaller.

## Requirements

- Windows 10 or 11
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Python 3.10+](https://www.python.org/downloads/)
- [Microsoft Edge WebView2](https://developer.microsoft.com/en-us/microsoft-edge/webview2/) (included with Windows 10/11)

## Quick Start

### Option 1: Terminal (Bash or PowerShell)

1. Open a terminal in the project folder.
2. Restore packages:

   ```bash
   dotnet restore Cobra.sln
   ```

3. Run the app:

   ```bash
   dotnet run --project Cobra/Cobra.csproj
   ```

4. Run tests:

   ```bash
   dotnet run --project Cobra.Tests/Cobra.Tests.csproj
   ```

### Option 2: Visual Studio

1. Open `Cobra.sln` in Visual Studio (2019, 2022, or 2026).
2. Right-click the **Cobra** project and select **Set as Startup Project**.
3. Press **F5** to build and run.
