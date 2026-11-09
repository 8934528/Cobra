# Access Control and Security Incident Policy

## 1. Purpose and Scope

This policy defines how Cobra IDE manages access to user data, enforces security boundaries within the desktop application, and handles potential security incidents or data breaches in accordance with industry best practices and South Africa's Protection of Personal Information Act (POPIA).

## 2. User Data Access and Access Control

### Local-First Access Model

Cobra IDE runs under the local operating system account of the developer:

- **No Remote Access**: Cobra maintainers and third parties have no remote access to your local projects, source code, assets, or IDE configuration.
- **Filesystem Boundaries**: File explorer, scene serialization, asset importing, and build services validate and normalize file paths to ensure operations remain within the active project directory or explicitly user-selected paths.
- **Protected Project Files**: Critical project files and directories are guarded against accidental or unauthorized deletion through confirmation checks and path validation.

## 3. Permissions and Execution Controls

- **Python and Process Execution**: Running a game, executing pip, running Git commands, or packaging with PyInstaller requires explicit user initiation. Commands are executed using structured process arguments without arbitrary shell interpolation.
- **WebView2 Isolation**: Embedded WebView2 editors (Monaco and 3D viewport) operate with restricted navigation, disabled external untrusted scripts, and validated JSON message schemas between JavaScript and C#.
- **Extension and Plugin Controls**: Extensions loaded from the `Plugins/` directory are validated for metadata, compatibility, and explicit user enablement before execution. Untrusted plugins are isolated so errors cannot crash or compromise the core IDE.
- **Credential Protection**: Git credentials are delegated to the system Git credential manager and are never stored in plain text by Cobra. Optional AI API keys are encrypted locally or stored in git-ignored `.env` files.

## 4. Prevention of Unauthorized Access

To prevent unauthorized state manipulation or local exploitation, Cobra enforces:

- Service-layer validation for all project, file, asset, scene, and build operations rather than relying solely on UI state.
- File type and size validation during asset imports to reject malformed or oversized payloads.
- Safe archive and template handling that prevents zip-slip or path traversal vulnerabilities.

## 5. Security Incidents and Reporting

A security incident includes any vulnerability or event that compromises the confidentiality, integrity, or availability of user projects, credentials, or the local host environment (for example: path traversal, command injection, malicious plugin execution, or unintended data transmission).

### Reporting an Incident

Users and contributors who discover a security issue should report it immediately by:

1. Opening a security report or issue on the project repository (avoiding public disclosure of exploit details until a fix is available).
2. Providing steps to reproduce, affected Cobra version, and relevant log output from the IDE Output/Security console.

## 6. Incident Response Workflow

When a security incident or vulnerability is identified, maintainers follow a structured four-stage response:

### Stage 1: Investigation

- Verify the report and determine the root cause, affected components, and severity.
- Assess whether any local credentials, project files, or external communications could be impacted.

### Stage 2: Containment

- Issue immediate mitigation guidance (for example, disabling a specific plugin, revoking an exposed API key, or restricting a setting).
- Isolate the vulnerable component or dependency in the codebase.

### Stage 3: Recovery and Remediation

- Develop, test, and release a patched version of Cobra IDE.
- Add automated regression and security tests covering the vulnerability class.

### Stage 4: Data Breach Assessment and Notification (POPIA Alignment)

In the event that an incident involves unauthorized access to or disclosure of personal information (for example, through a compromised third-party integration or exposed API key):

- **Assessment**: Evaluate the scope of affected information and the likelihood of harm to users.
- **Notification**: In accordance with Section 22 of POPIA, if there are reasonable grounds to believe that personal information of a data subject has been accessed or acquired by any unauthorized person, the responsible party will notify the affected users and, where applicable, the Information Regulator as soon as reasonably possible after the discovery of the compromise.
- **Remediation Advice**: Provide clear instructions to affected users on how to rotate credentials, secure their local environment, and verify project integrity.

## 7. Incident Documentation

All confirmed security incidents and vulnerabilities must be documented with:

- Date of discovery and resolution.
- Description of the root cause and affected versions.
- Corrective actions taken and automated tests added to prevent recurrence.
