# Security Architecture & Policies

Cobra IDE implements defense-in-depth security controls across filesystem operations, external process execution, asset importing, secret storage, and plugin loading.

## 1. Policy Documents

- **[Data Protection & Privacy Policy](../policies/data_protection.md)** — Covers local data storage, POPIA compliance, data minimization, and explicit user consent for external services.
- **[Access Control & Incident Response Policy](../policies/access_incident.md)** — Covers privilege boundaries, extension verification, vulnerability reporting, and incident response steps.

## 2. Technical Security Controls (`SecurityValidator`)

### Path Traversal & Archive Slip Prevention

- `SecurityValidator.TryResolveSafeChildPath` resolves canonical paths via `Path.GetFullPath` and enforces that every file or directory created, renamed, deleted, or imported stays strictly inside the project root directory.
- `SecurityValidator.IsSafeRelativeArchivePath` rejects any relative path containing `..` segments or rooted drives.
- Critical project files (`cobra.project.json`, `main.py`, `.git`, `.venv`) are protected against accidental deletion via `SecurityValidator.IsProtectedProjectEntry`.

### Safe Process Execution & Command Validation

- Python, `pip`, `git`, and `PyInstaller` commands are invoked directly via `ProcessStartInfo` with `UseShellExecute = false` rather than concatenating untrusted strings into a shell.
- Package specifiers passed to `pip` are validated against `SecurityValidator.IsValidPipPackageSpec`, rejecting shell metacharacters (`;`, `|`, `&`, `` ` ``, `$`, newlines).

### Validated Asset Importing

- `SecurityValidator.ValidateAssetFile` inspects every imported asset before copying it into a project:
  - Enforces an allowlist of supported game asset extensions.
  - Enforces a 100 MB maximum file size limit to prevent memory exhaustion.
  - Verifies binary magic-byte file headers (`PNG`, `JPEG`, `GIF`, `BMP`, `WEBP`, `WAV`, `OGG`, `MP3`, `TTF`, `OTF`, `GLB`) and validates UTF-8 text assets (`JSON`, `OBJ`, `GLTF`, `PY`).

### Secret Encryption at Rest (DPAPI)

- API keys configured for the optional AI Assistant are encrypted using Windows Data Protection API (`ProtectedData.Protect` with `DataProtectionScope.CurrentUser`) before being saved to `settings.json`.
- Sensitive environment variables and tokens are redacted from IDE logs via `SecurityValidator.RedactSecrets`.

### Controlled Extension Loading (`PluginLoader`)

- Cobra disables arbitrary unverified DLL execution by default.
- Extensions in the `Plugins/` directory are inspected for SHA-256 hash and strong-name signature metadata (`PluginMetadata`), and binary `.dll` assemblies are only loaded when explicitly allowlisted by SHA-256 hash or when the user explicitly opts in via `AllowUnsignedPlugins`.
