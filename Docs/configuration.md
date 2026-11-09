# Configuration Guide

Cobra IDE lets you customize the editor and your projects. Settings are saved in simple JSON files.

## 1. Global Settings

Click the gear icon in the top toolbar to open **Settings**.

### General

- **Language:** Choose your interface language.
- **Startup:** Open the welcome page or reopen your last project.
- **Auto Save:** Automatically save changed files on a timer.

### Theme & Editor

- **Theme:** Switch between Dark and Light mode.
- **Font & Size:** Change the editor font family and text size.
- **Minimap & Line Numbers:** Turn editor helpers on or off.

### Python

- **Default Python:** Set the default Python interpreter for new projects.
- **Virtual Environments:** Automatically create a `.venv` for each new project.
- **Code Tools:** Turn on code formatting and error checking.

### Game Defaults

- **Target FPS:** Set the default frame rate (such as 60 FPS).
- **Resolution:** Set default game window width and height.

### AI Assistant (Optional)

- **Enable AI:** Turn external AI suggestions on or off. Disabled by default.
- **API Key:** Encrypted securely using Windows DPAPI. Never saved in plain text.

## 2. Project Settings

Click the wrench icon on the editor toolbar to open project settings:

- **Startup File:** The main script to run (default: `main.py`).
- **Arguments:** Command-line flags passed to your game.
- **Environment Variables:** Custom variables for your game process.
