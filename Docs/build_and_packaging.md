# Build & Packaging Guide

Cobra IDE includes an integrated build and distribution pipeline (`BuildPackagingService`) powered by **PyInstaller** so developers can package their Python games into standalone executables.

## 1. Prerequisites

Ensure `pyinstaller` is installed in your project's Python environment. You can install it with one click from the **Python Environment & Packages** panel (`pyinstaller`).

## 2. Build Modes

Click **Build Game** on the Main Toolbar to select a packaging action:

1. **Development Build (Console Window)**:
   - Passes `--onedir` and `--console` to `PyInstaller`.
   - Keeps the standard output console visible so runtime print statements and tracebacks can be inspected during testing.
2. **Release Build (Windowed Executable)**:
   - Passes `--onefile` (or `--onedir`) and `--noconsole` (`--windowed`) to `PyInstaller`.
   - Produces a clean desktop game executable in `dist/<GameName>.exe` without a terminal window.
3. **Clean Build Artifacts**:
   - Safely removes the project's `build/`, `dist/`, and `*.spec` artifacts inside the project root.
4. **Test Packaged Executable**:
   - Launches the generated `.exe` inside `dist/` with its working directory set to the distribution folder so you can immediately verify asset paths in the packaged build.

## 3. Asset & Scene Bundling

During packaging, `BuildPackagingService` automatically detects project subdirectories (`assets/`, `scenes/`, `scripts/`) and passes `--add-data` arguments (`folder;folder` on Windows) so sprites, audio files, fonts, and `.scene` files are bundled alongside the executable.

## 4. Platform Scope

Because `PyInstaller` produces executables for the host operating system, running the build pipeline on Windows generates a **Windows executable (`.exe`)**. Building macOS or Linux binaries requires running the packaging step on those respective operating systems or inside a CI runner.
