# AI Assistant Guide

Cobra IDE includes an optional AI assistant to help you write Python game code.

## Privacy & Safety

1. **Off by Default:** No code or data is sent to external services unless you explicitly enable external AI in Settings.
2. **Offline Mode:** If you do not configure an API key, Cobra uses built-in local templates to give code advice and examples without internet access.
3. **Encrypted Keys:** Any API keys you save are encrypted on your machine using Windows DPAPI. Keys are never saved in plain text.
4. **Secret Filtering:** Cobra automatically removes tokens or passwords from prompts before sending them.

## Available Actions

Open the **AI Assistant** tab in the sidebar to use these tools:

- **Explain Active Code:** Get a short summary of how the current script works.
- **Explain Last Error:** View simple advice on how to fix a recent crash or error.
- **Suggest Improvements:** Get ideas to make your game code cleaner or faster.
- **Generate Game Object Script:** Create starter code for movement, collisions, or states.
- **Game Mechanics Guide:** Read tips on common game design patterns.
- **Generate Unit Tests:** Create basic tests to verify game logic.
