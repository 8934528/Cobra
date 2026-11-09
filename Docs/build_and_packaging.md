# Build & Packaging Guide

Cobra IDE uses **PyInstaller** to turn your Python game into a standalone Windows program (`.exe`). Users do not need Python installed to play your game.

## 1. Setup

Make sure `pyinstaller` is installed in your project. If it is not installed:

1. Open the **Python & Packages** panel.
2. Type `pyinstaller` in the package box.
3. Click **Install Package**.

## 2. Packaging Options

Click **Build & Package** in the left sidebar to open the build tool:

- **Release Build:** Creates a standalone `.exe` file without a terminal window. Best for finished games.
- **Development Build:** Keeps the console window open so you can see errors and print statements. Best for testing.
- **Clean Build Folder:** Deletes older files in `build/` and `dist/` before packaging.
- **Test Packaged Game:** Runs the newly built `.exe` file so you can make sure everything works.

## 3. Bundling Assets

Cobra automatically bundles your game folders (`assets/`, `scenes/`, and `scripts/`) into the build. Sprites, music, sounds, and scenes will be included with the executable.

## 4. Output Location

Finished builds are placed in the `dist/` folder inside your project.
