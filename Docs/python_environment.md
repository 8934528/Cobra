# Python Environment, Package Management & Intelligence

Cobra IDE treats Python environment isolation and developer tooling as first-class citizens.

## 1. Python Interpreter Detection (`PythonService`)

Cobra automatically discovers installed Python interpreters across:

- Active `PATH` entries (`python.exe`)
- The Windows Python Launcher (`py -0p`)
- Standard per-user and system-wide directories (`%LOCALAPPDATA%\Programs\Python\Python*`, `C:\Python*`, `%ProgramFiles%\Python*`)

When a project is opened, `PythonService.ResolveProjectPython` resolves the active interpreter in priority order:

1. The project's isolated `.venv\Scripts\python.exe` if present and valid.
2. The project's configured `PythonInterpreter` in `cobra.project.json`.
3. The global interpreter configured in Cobra Settings.

## 2. Virtual Environments (`VenvService`)

Every Cobra project supports an isolated `.venv` directory:

- **Creation**: Runs `python -m venv .venv` inside the project root.
- **Validation**: Verifies that `.venv\Scripts\python.exe`, `pyvenv.cfg`, and `pip` are intact and functional.
- **Dependency Installation**: Installs `requirements.txt` into `.venv` during project creation or when clicking **Create / Validate .venv** in the Python sidebar.

## 3. Integrated Package Manager (`PipPackageService`)

The **Python Environment & Packages** sidebar panel executes all `pip` operations strictly through the project's resolved Python interpreter (`python -m pip ...`), preventing accidental pollution of the global Python installation:

- **View Installed Packages**: Uses `python -m pip list --format=json`.
- **Install / Upgrade Package**: Validates the package specifier via `SecurityValidator.IsValidPipPackageSpec` (allowing PEP 508 specifiers while blocking shell metacharacters) and streams installation output to the Output console.
- **Uninstall Package**: Removes selected packages with `-y`.
- **Sync `requirements.txt`**: Runs `python -m pip freeze` and writes deterministic dependencies to `requirements.txt`.

## 4. Python Intelligence (`PythonIntelligenceService`)

Cobra provides fast, safe static analysis for Python files:

- **AST Syntax & Symbol Extraction**: Runs a read-only Python `ast.parse` pass to detect syntax errors (with exact line/column numbers) and extract top-level classes, methods, functions, and imports for the Symbol Outline and Go to Definition (`F12`).
- **Ruff Integration**: When `ruff` is installed in the environment, Cobra runs `ruff check --output-format=json` for linting diagnostics and `ruff format` for one-click PEP 8 code formatting.
- **Pyright Integration**: Optionally invokes `pyright --outputjson` for static type checking.
