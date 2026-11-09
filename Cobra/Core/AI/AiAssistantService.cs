using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Cobra.Core.AI
{
    public enum AiAssistantTask
    {
        ExplainCode,
        ExplainError,
        SuggestImprovements,
        GenerateGameScript,
        ExplainGameMechanics,
        GenerateUnitTests,
        ReviewProjectStructure,
        PerformanceTips
    }

    public static class AiAssistantService
    {
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(25) };

        public static async Task<string> ExecuteAssistantTaskAsync(
            AiAssistantTask task,
            string contextCodeOrInput,
            string framework,
            string? apiKey,
            bool userConsentedToExternalApi,
            CancellationToken cancellationToken = default)
        {
            string prompt = BuildTaskPrompt(task, contextCodeOrInput, framework);

            if (userConsentedToExternalApi && !string.IsNullOrWhiteSpace(apiKey))
            {
                var (ok, externalReply) = await TryCallGeminiOrOpenAiAsync(prompt, apiKey.Trim(), cancellationToken);
                if (ok && !string.IsNullOrWhiteSpace(externalReply))
                    return externalReply;
            }

            // local offline python game dev assistant fallback (never sends code externally)
            return GenerateLocalPythonGameResponse(task, contextCodeOrInput, framework);
        }

        private static string BuildTaskPrompt(AiAssistantTask task, string input, string framework)
        {
            return task switch
            {
                AiAssistantTask.ExplainCode => $"Explain this {framework} Python game code clearly and concisely:\n\n{input}",
                AiAssistantTask.ExplainError => $"Explain the root cause and fix for this Python/{framework} traceback or error:\n\n{input}",
                AiAssistantTask.SuggestImprovements => $"Suggest idiomatic Python and {framework} game architecture improvements for:\n\n{input}",
                AiAssistantTask.GenerateGameScript => $"Generate a clean, well-structured Python {framework} game object script for:\n\n{input}",
                AiAssistantTask.ExplainGameMechanics => $"Explain how to implement this game mechanic in Python ({framework}) with example code:\n\n{input}",
                AiAssistantTask.GenerateUnitTests => $"Generate Python unittest/pytest test cases for this game logic:\n\n{input}",
                AiAssistantTask.ReviewProjectStructure => $"Review this Python game project structure and recommend organization improvements:\n\n{input}",
                AiAssistantTask.PerformanceTips => $"Analyze this {framework} Python code for frame-rate and memory performance bottlenecks:\n\n{input}",
                _ => input
            };
        }

        private static async Task<(bool Success, string Response)> TryCallGeminiOrOpenAiAsync(string prompt, string apiKey, CancellationToken ct)
        {
            try
            {
                if (apiKey.StartsWith("AIza", StringComparison.Ordinal))
                {
                    string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={Uri.EscapeDataString(apiKey)}";
                    var payload = new
                    {
                        contents = new[]
                        {
                            new { parts = new[] { new { text = prompt } } }
                        }
                    };
                    using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                    using var resp = await Http.PostAsync(url, content, ct);
                    if (!resp.IsSuccessStatusCode) return (false, string.Empty);

                    string body = await resp.Content.ReadAsStringAsync(ct);
                    using var doc = JsonDocument.Parse(body);
                    var text = doc.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();
                    return (true, text ?? string.Empty);
                }
                else
                {
                    using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                    var payload = new
                    {
                        model = "llama-3.3-70b-versatile",
                        messages = new[]
                        {
                            new { role = "system", content = "You are Cobra AI, an expert Python game development assistant specializing in Pygame, Panda3D, and Ursina." },
                            new { role = "user", content = prompt }
                        }
                    };
                    req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                    using var resp = await Http.SendAsync(req, ct);
                    if (!resp.IsSuccessStatusCode) return (false, string.Empty);

                    string body = await resp.Content.ReadAsStringAsync(ct);
                    using var doc = JsonDocument.Parse(body);
                    var text = doc.RootElement
                        .GetProperty("choices")[0]
                        .GetProperty("message")
                        .GetProperty("content")
                        .GetString();
                    return (true, text ?? string.Empty);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return (false, string.Empty);
            }
        }

        private static string GenerateLocalPythonGameResponse(AiAssistantTask task, string? input, string framework)
        {
            string trimmed = (input ?? string.Empty).Trim();
            int lineCount = string.IsNullOrEmpty(trimmed) ? 0 : trimmed.Split('\n').Length;

            return task switch
            {
                AiAssistantTask.ExplainCode => $@"[Local Cobra Python Analyzer — No External Data Sent]
Framework Context: {framework} | Lines Analyzed: {lineCount}

Summary of Code Structure:
• Uses Python game loop patterns ({(trimmed.Contains("pygame") ? "Pygame display, clock, and event queue detected" : trimmed.Contains("ursina") ? "Ursina Entity and update() hook detected" : trimmed.Contains("ShowBase") ? "Panda3D ShowBase task manager detected" : "Standard Python script structure")}).
• Frame timing is managed via clock delta-time or fixed tick rate to keep movement speed independent of monitor refresh rate.
• Input events are polled each frame before updating entity coordinates and blitting/rendering to the active viewport.",

                AiAssistantTask.ExplainError => $@"[Local Cobra Error Diagnostic — No External Data Sent]
Analyzed Input:
{(string.IsNullOrEmpty(trimmed) ? "No error text provided." : trimmed)}

Common Resolution Steps:
1. ModuleNotFoundError (e.g. 'pygame' or 'panda3d'): Open Cobra's Package Manager and install the missing package into the project's virtual environment.
2. pygame.error (FileNotFoundError on image/audio load): Verify the asset path is relative to the project root (e.g. 'assets/sprite.png') and check the Asset Browser for missing references.
3. AttributeError / TypeError: Check variable types in the Debugger Locals panel by setting a breakpoint on the failing line.",

                AiAssistantTask.SuggestImprovements => @"[Local Cobra Code Improvement Suggestions]
1. Separate Game State from Rendering: Keep entity position/physics updates in `update(dt)` and surface drawing in `draw(screen)`.
2. Pre-load Assets Once: Avoid calling `pygame.image.load()` or `pygame.font.Font()` inside the `while True:` game loop; cache surfaces and fonts during initialization.
3. Use Delta Time (`dt = clock.tick(60) / 1000.0`): Multiply movement velocities by `dt` so gameplay runs identically at 60 FPS and 144 FPS.",

                AiAssistantTask.GenerateGameScript => $@"# Generated {framework} Game Object Script
import pygame

class GameActor(pygame.sprite.Sprite):
    def __init__(self, x: float, y: float, width: int = 48, height: int = 48, speed: float = 240.0):
        super().__init__()
        self.image = pygame.Surface((width, height), pygame.SRCALPHA)
        self.image.fill((255, 140, 0))
        self.rect = self.image.get_rect(topleft=(int(x), int(y)))
        self.pos = pygame.Vector2(x, y)
        self.velocity = pygame.Vector2(0, 0)
        self.speed = speed

    def handle_input(self, keys):
        self.velocity.xy = (0, 0)
        if keys[pygame.K_LEFT] or keys[pygame.K_a]:
            self.velocity.x = -1
        if keys[pygame.K_RIGHT] or keys[pygame.K_d]:
            self.velocity.x = 1
        if keys[pygame.K_UP] or keys[pygame.K_w]:
            self.velocity.y = -1
        if keys[pygame.K_DOWN] or keys[pygame.K_s]:
            self.velocity.y = 1
        if self.velocity.length_squared() > 0:
            self.velocity = self.velocity.normalize() * self.speed

    def update(self, dt: float):
        self.pos += self.velocity * dt
        self.rect.topleft = (round(self.pos.x), round(self.pos.y))

    def draw(self, surface: pygame.Surface):
        surface.blit(self.image, self.rect)
",

                AiAssistantTask.ExplainGameMechanics => $@"[Game Mechanics Guide — {framework}]
• AABB Collision Resolution: Move along the X axis first, check `rect.colliderect(wall)`, snap `rect.right = wall.left` (or `rect.left = wall.right`), then repeat independently for the Y axis. This prevents corner snagging.
• Coyote Time & Jump Buffering: Store `coyote_timer = 0.12` when leaving ground so jumps feel responsive even if pressed a few frames late.
• Camera Follow with Lerp: `camera_offset += (target_pos - camera_offset) * min(1.0, 8.0 * dt)` produces smooth camera tracking.",

                AiAssistantTask.GenerateUnitTests => @"# Generated Python Unit Tests for Game Logic
import unittest

class TestGameMechanics(unittest.TestCase):
    def test_bounds_clamping(self):
        width, height = 800, 600
        x, y = -25, 640
        clamped_x = max(0, min(width, x))
        clamped_y = max(0, min(height, y))
        self.assertEqual(clamped_x, 0)
        self.assertEqual(clamped_y, 600)

    def test_aabb_overlap(self):
        # simple rectangle intersection check (x, y, w, h)
        a = (10, 10, 50, 50)
        b = (40, 40, 50, 50)
        overlaps = (a[0] < b[0] + b[2] and a[0] + a[2] > b[0] and
                    a[1] < b[1] + b[3] and a[1] + a[3] > b[1])
        self.assertTrue(overlaps)

if __name__ == '__main__':
    unittest.main()
",

                AiAssistantTask.ReviewProjectStructure => @"[Project Structure Review]
Recommended Cobra Python Game Layout:
• `main.py` — Entry point, window initialization, and top-level scene loop.
• `cobra.project.json` — Project metadata, startup script, and framework settings.
• `requirements.txt` — Pinned Python dependencies (`pygame-ce`, `pymunk`, etc.).
• `assets/` — Organized into `sprites/`, `audio/`, `fonts/`, and `models/`.
• `scenes/` — `.Cobra` scene files and generated Python scene data.
• `scripts/` — Modular gameplay classes (`player.py`, `enemies.py`, `ui.py`, `physics.py`).
• `tests/` — Automated `unittest` or `pytest` test suites.",

                AiAssistantTask.PerformanceTips => $@"[Performance & Profiling Recommendations for {framework}]
1. Convert Loaded Surfaces: Always call `.convert()` or `.convert_alpha()` after `pygame.image.load()` to match the display pixel format (up to 5x faster blitting).
2. Avoid Per-Frame Font Rendering: Re-render `font.render(...)` only when the score or text string actually changes.
3. Spatial Partitioning: If checking collisions among >100 sprites, use `pygame.sprite.groupcollide()` or grid buckets instead of nested O(N^2) loops.",

                _ => "Select an AI assistant action to analyze or generate Python game code."
            };
        }
    }
}

