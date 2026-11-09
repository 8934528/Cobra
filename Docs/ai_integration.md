# AI Assistant Guide

Cobra IDE includes an optional, privacy-aware **AI Assistant** designed specifically for Python game developers (`Pygame`, `Panda3D`, `Ursina`).

## Privacy & Security Architecture

1. **Explicit Opt-In Consent**: External network calls are disabled by default. Cobra never transmits source code or prompts to an external AI provider unless you explicitly enable external AI in **Settings -> AI Assistant** or check the **Allow external AI API** consent box in the AI panel.
2. **Offline Local Mode Always Available**: When external AI is disabled or no API key is configured, `AiAssistantService` provides instant, deterministic local Python game recipes, code explanations, bug checklists, and refactoring suggestions without any network activity.
3. **DPAPI Key Encryption**: When you save an API key in Settings, Cobra encrypts the key at rest using Windows Data Protection API (`ProtectedData.Protect` with `DataProtectionScope.CurrentUser`) via `SecurityValidator.ProtectSecret`. Plaintext API keys are never written to `settings.json` or logs.
4. **Secret Redaction**: Outbound prompts automatically redact inline `API_KEY` or `SECRET` assignments before transmission.

## Supported Actions in the Editor

Open the **AI Assistant** panel or click the **AI Assistant** button in the bottom status bar to:

- **Explain Code**: Summarize the active Python script's classes, game loop, and state flow.
- **Generate Python Game Snippet**: Generate ready-to-use `Pygame`, `Panda3D`, or `Ursina` code for movement, collisions, tilemaps, audio, or state machines.
- **Fix Error / Diagnose Traceback**: Analyze Python tracebacks and common Pygame runtime issues.
- **Refactor**: Get actionable modularity, delta-time, and surface-caching improvements.
- **Insert Last Code Block into Editor**: Paste the generated Python snippet directly at your cursor in Monaco Editor.
