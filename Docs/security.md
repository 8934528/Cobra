# Security & Protection

Cobra IDE keeps your computer and projects safe by enforcing strong boundaries.

## 1. File Safety

- **Path Checks:** All file actions (creating, moving, deleting) stay inside your project folder. Cobra blocks path traversal attempts.
- **Protected Files:** Important files like `main.py` and `cobra.project.json` are protected from accidental deletion.
- **Asset Limits:** Imported assets are limited to 100 MB and must match approved image, sound, font, or 3D model formats.

## 2. Safe Process Execution

- Tools like Python, pip, and Git are run with strict argument lists instead of raw shell commands.
- Package names given to pip are checked to block dangerous shell symbols.

## 3. Secret Protection

- API keys are encrypted with Windows DPAPI under your user account.
- Passwords and tokens are automatically removed from log files.

## 4. Controlled Plugins

- Plugin assemblies (`.dll` files) are not loaded unless explicitly enabled and verified.
- Plugin errors are isolated so they cannot crash the main editor.

## Policy Documents

- **[Data Protection Policy](../policies/data_protection.md)** — Privacy rules and local storage details.
- **[Access Control Policy](../policies/access_incident.md)** — Access boundaries and how to report issues.
