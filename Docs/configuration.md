# Configuration Guide

Cobra IDE stores user preferences in `settings.json` managed by `SettingsManager`, while per-project settings are stored in each project's `cobra.project.json` manifest.

## 1. Global IDE Settings (`SettingsView`)

Open **Settings** from the gear icon on the Main Toolbar or Welcome Dashboard.

### General

- **Language**: Interface language selection.
- **Startup Behavior**: Open Dashboard, Reopen Last Project, or Create New Project.
- **Auto Save & Interval**: Automatically save modified scripts at a configurable interval (`5` to `3600` seconds).

### Theme & Editor

- **Theme**: `Dark` or `Light` workspace theme.
- **Editor Font Family & Size**: Customize Monaco Editor typography (`8` to `48` pt).
- **Minimap, Word Wrap & Line Numbers**: Toggle code editor visual aids.

### Python Integration

- **Python Interpreter Path**: Default system Python executable (`python` or full path to `python.exe`).
- **Auto-Create Virtual Environments**: Automatically create `.venv` when initializing new projects.
- **Pip Index URL**: Optional custom PyPI mirror URL (validated for `http://` or `https://`).
- **Linting & Formatting**: Enable AST syntax checks, `ruff` formatting, and `pyright` type diagnostics.

### Game Engine Defaults

- **Target FPS**: Default frame rate target (`15` to `360` FPS).
- **Default Resolution**: Default game window width and height (`320x240` up to `7680x4320`).
- **Physics Engine**: Default 2D physics backend (`Pymunk` or `Arcade`).

### AI Assistant (Privacy-Aware Opt-In)

- **Enable AI Assistant**: Explicitly opt in or out of external AI API calls.
- **Provider & Endpoint**: Select `Local Offline Helper`, `Google Gemini`, `Groq Cloud`, or `OpenAI Compatible`.
- **Encrypted API Key**: Stored using Windows DPAPI (`ProtectedData`) encryption at rest.

## 2. Per-Project Configuration (`cobra.project.json`)

Click the **Project Config** wrench button on the Main Toolbar inside the editor to configure:

- **Startup Python File**: Entry-point script (default `main.py`).
- **Command-Line Arguments**: Arguments passed to your Python script on launch.
- **Environment Variables**: Key-value pairs injected into the game process environment.
- **Workspace Layout Preset**: Saved layout mode (`Default`, `CodeFocus`, `SceneDesign`, `Debug`).
