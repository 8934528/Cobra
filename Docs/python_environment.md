# Python Environment & Packages

Cobra IDE helps you manage Python interpreters, virtual environments, and packages easily.

## 1. Finding Python

Cobra looks for Python installations in:

- Your system `PATH`
- The Windows Python launcher (`py.exe`)
- Standard folders (`%LOCALAPPDATA%\Programs\Python` and `C:\Python3*`)

When you open a project, Cobra chooses the interpreter in this order:

1. The project's `.venv` folder (recommended)
2. The interpreter listed in `cobra.project.json`
3. The default interpreter set in global Settings

## 2. Virtual Environments (.venv)

Each project can have its own isolated environment:

- **Create:** Click **Create / Validate Venv** in the Python sidebar.
- **Check:** Cobra checks that `python.exe` and `pip` work correctly.
- **Dependencies:** When you create a project, Cobra automatically installs requirements into this environment.

## 3. Package Management

Use the **Python & Packages** panel to manage project libraries:

- **View Installed:** See all installed packages and their version numbers.
- **Install:** Type a package name (such as `pygame-ce` or `pymunk`) and click **Install Package**.
- **Update:** Select a package and click **Update Selected**.
- **Remove:** Select a package and click **Uninstall Selected**.
- **Save List:** Click **Sync requirements.txt** to save all installed packages to your project.

## 4. Code Analysis & Formatting

- **Syntax Checking:** Cobra checks Python files for syntax errors as you type.
- **Symbol Outline:** Lists classes, functions, and imports in the active file.
- **Formatting (Ruff):** Click **Format Python Code** to clean up and format your code automatically.
