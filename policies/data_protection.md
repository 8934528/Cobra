# Data Protection and Privacy Policy

## 1. Overview and Scope

Cobra IDE is a desktop Integrated Development Environment designed for Python game development. This document explains how Cobra handles user information, project files, configuration data, and optional third-party integrations, in alignment with data protection principles including South Africa's Protection of Personal Information Act (POPIA).

Cobra operates primarily as a local desktop application. I do not claim external privacy certifications that have not been independently verified.

## 2. Information Collection and Local Storage

### Data Stored Locally on Your Device

By default, all data created or configured in Cobra remains strictly on your local machine:

- **Project Files and Source Code**: Stored in your chosen project directories (for example, `~/CobraProjects`).
- **Application Settings**: Stored locally in `%AppData%/Cobra/settings.json`.
- **Project-Specific Settings**: Stored inside your project directory under `.Cobra/Cobra-settings.json`.
- **Python Environments**: Virtual environments (`venv`) and installed packages remain inside your local project or template folders.
- **Credentials and API Keys**: Any optional API keys configured for AI assistance or external tools are stored locally using Windows Data Protection API (DPAPI) encryption or local project `.env` files excluded from version control.

### Personal Information

Cobra does not require user registration, accounts, or personal identity verification to use its core IDE, game editor, debugging, or packaging features. Cobra does not collect names, email addresses, physical addresses, or payment details.

## 3. Purpose of Processing

Local configuration and project data are processed solely on your machine for the following purposes:

- Opening, editing, saving, running, debugging, and packaging your Python game projects.
- Remembering your workspace layout, theme, editor preferences, and Python interpreter paths.
- Executing local developer tools that you initiate (such as Python, pip, PyInstaller, and Git).

## 4. Data Minimization

Cobra follows strict data minimization practices:

- Telemetry is disabled by default (`Telemetry = false`).
- Cobra only reads files within the project directories or paths you explicitly open or configure.
- No background harvesting of source code, asset files, or system files occurs.

## 5. Third-Party Services and Network Requests

Cobra connects to external networks only when required by features you explicitly use:

- **Monaco Editor & WebView2**: Embedded editor resources load locally or via configured CDN assets for code editing and 3D viewport visualization.
- **Python Package Index (pip / PyPI)**: When you install or update Python packages, `pip` connects to the configured package index (default: `https://pypi.org/simple`).
- **Git Remotes**: When you execute Git Pull, Push, or Fetch operations, Git communicates with the remote repository you configured.
- **Optional AI Assistant**: Source code snippets or prompts are sent to an external AI provider only when you explicitly configure an API key, grant consent, and trigger an AI action.

## 6. Data Retention and Deletion

Because Cobra stores data locally, you have full control over retention and deletion:

- **Project Data**: Delete any project folder from your computer at any time to permanently remove its code, scenes, and virtual environment.
- **IDE Configuration**: Delete `%AppData%/Cobra` or use the **Reset to Defaults** action in Settings to clear saved preferences and encrypted keys.
- **Temporary Build Artifacts**: Use the **Clean Build** option in the Build tool to remove `build/` and `dist/` packaging directories.

## 7. Information Security

Cobra implements technical safeguards to protect your local workspace:

- Path validation prevents directory traversal outside authorized project boundaries.
- Process execution uses explicit argument lists rather than raw shell concatenation to prevent command injection.
- Embedded WebView2 instances restrict navigation and validate inter-process messages.
- Sensitive API keys are protected at rest and excluded from logs and source control templates.

## 8. User Rights and POPIA Responsibilities

Under POPIA and general data protection principles, users have the right to:

- **Transparency**: Know exactly what data is stored and where it resides (documented in Section 2).
- **Access and Portability**: Access all project files and JSON settings directly on disk in standard, human-readable formats.
- **Correction and Deletion**: Modify or delete any configuration or project file at any time without restriction.
- **Consent Control**: Opt in or out of optional external features (such as AI assistance or telemetry) at any time via the Settings panel.

## 9. Data Access Requests and Contact

Because Cobra does not transmit or store your personal data on central servers, all user data is already in your direct possession on your local filesystem. For questions, security reports, or policy inquiries regarding the Cobra IDE repository, please contact the project maintainers through the repository's official issue tracker.
