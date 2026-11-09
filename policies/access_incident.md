# Access Control and Security Incident Policy

## 1. Overview

This document explains how Cobra IDE protects user data, controls access boundaries on your computer, and responds to security issues in line with the Protection of Personal Information Act (POPIA).

## 2. Access Boundaries

### Local Desktop Execution

Cobra runs under your local Windows user account:

- **No Remote Access:** Maintainers and third parties have no access to your computer, projects, or settings.
- **Folder Boundaries:** File actions stay inside your active project folder.
- **Protected Files:** Essential files like `main.py` cannot be deleted without warning.

## 3. Tool Permissions

- **Process Execution:** Running games, pip commands, and Git operations happens only when you click or run them.
- **Embedded Web Views:** Monaco and Three.js run in restricted WebViews without access to your local filesystem.
- **Extensions:** Plugins in the `Plugins/` folder must be explicitly enabled and checked before running.
- **Credentials:** Git credentials use your system Git credential manager. Cobra never stores them in plain text.

## 4. Reporting a Security Issue

If you find a security problem or vulnerability:

1. Open a private security report in the project repository.
2. Include the steps to reproduce the issue.
3. List your Cobra version and Windows version.
4. Do not post exploit code publicly until a fix is published.

## 5. How We Handle Incidents

When a security issue is reported, maintainers follow these steps:

1. **Investigate:** Verify the report and determine what is affected.
2. **Contain:** Share quick workarounds (such as disabling a plugin or setting).
3. **Fix:** Build, test, and release an update with the fix.
4. **Notify:** If personal data was affected, notify users in line with Section 22 of POPIA and provide steps to secure credentials.

## 6. Records

All confirmed security issues are recorded with their cause, affected versions, and the automated tests added to prevent them in the future.
