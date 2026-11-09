using System.IO;

namespace Cobra.Core.ProjectSystem
{
    public class ProjectTemplateDescriptor
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Framework { get; set; } = "Pygame";
        public string Category { get; set; } = "2D";
        public string Description { get; set; } = string.Empty;
        public string[] Packages { get; set; } = ["pygame-ce"];
    }

    public static class ProjectTemplate
    {
        public static List<ProjectTemplateDescriptor> GetAvailableTemplates()
        {
            return
            [
                new ProjectTemplateDescriptor
                {
                    Id = "pygame-2d-starter",
                    Name = "Pygame 2D Starter",
                    Framework = "Pygame",
                    Category = "2D",
                    Description = "Classic 2D Pygame starter with game loop, scenes, input handling, and HUD.",
                    Packages = ["pygame-ce"]
                },
                new ProjectTemplateDescriptor
                {
                    Id = "pygame-platformer",
                    Name = "Pygame Platformer",
                    Framework = "Pygame",
                    Category = "2D",
                    Description = "Side-scrolling platformer with gravity, jump physics, tile collisions, and camera follow.",
                    Packages = ["pygame-ce"]
                },
                new ProjectTemplateDescriptor
                {
                    Id = "pygame-topdown",
                    Name = "Pygame Top-Down Game",
                    Framework = "Pygame",
                    Category = "2D",
                    Description = "8-directional top-down adventure with smooth movement, obstacles, and collectibles.",
                    Packages = ["pygame-ce"]
                },
                new ProjectTemplateDescriptor
                {
                    Id = "pygame-rpg",
                    Name = "Pygame RPG Starter",
                    Framework = "Pygame",
                    Category = "2D",
                    Description = "RPG foundation featuring player stats, inventory, NPC dialogue box, and tile world.",
                    Packages = ["pygame-ce"]
                },
                new ProjectTemplateDescriptor
                {
                    Id = "pygame-arcade",
                    Name = "Pygame Arcade Game",
                    Framework = "Pygame",
                    Category = "2D",
                    Description = "Fast-paced space arcade shooter with enemy waves, projectiles, particles, and high scores.",
                    Packages = ["pygame-ce"]
                },
                new ProjectTemplateDescriptor
                {
                    Id = "pygame-puzzle",
                    Name = "Pygame Puzzle Game",
                    Framework = "Pygame",
                    Category = "2D",
                    Description = "Grid-based block matching and sliding puzzle starter with move counter and level progression.",
                    Packages = ["pygame-ce"]
                },
                new ProjectTemplateDescriptor
                {
                    Id = "pygame-multiplayer",
                    Name = "Pygame Multiplayer Starter",
                    Framework = "Pygame",
                    Category = "2D",
                    Description = "Local & UDP/socket networked multiplayer arena starter with synchronized player states.",
                    Packages = ["pygame-ce"]
                },
                new ProjectTemplateDescriptor
                {
                    Id = "panda3d-starter",
                    Name = "Panda3D 3D Starter",
                    Framework = "Panda3D",
                    Category = "3D",
                    Description = "Python 3D game starter using Panda3D ShowBase with camera orbit, directional lighting, and 3D scene graph.",
                    Packages = ["panda3d"]
                },
                new ProjectTemplateDescriptor
                {
                    Id = "ursina-starter",
                    Name = "Ursina 3D Starter",
                    Framework = "Ursina",
                    Category = "3D",
                    Description = "Python 3D game starter using Ursina Engine with Entities, 3D controller, skybox, and lighting.",
                    Packages = ["ursina"]
                }
            ];
        }

        public static void ApplyTemplate(string projectPath, string templateId)
        {
            WriteCommonGitIgnore(projectPath);
            Directory.CreateDirectory(Path.Combine(projectPath, "assets"));
            Directory.CreateDirectory(Path.Combine(projectPath, "scenes"));
            Directory.CreateDirectory(Path.Combine(projectPath, "scripts"));

            switch (templateId.ToLowerInvariant())
            {
                case "pygame-platformer":
                    ApplyPlatformerTemplate(projectPath);
                    break;
                case "pygame-topdown":
                    ApplyTopDownTemplate(projectPath);
                    break;
                case "pygame-rpg":
                    ApplyRpgTemplate(projectPath);
                    break;
                case "pygame-arcade":
                    ApplyArcadeTemplate(projectPath);
                    break;
                case "pygame-puzzle":
                    ApplyPuzzleTemplate(projectPath);
                    break;
                case "pygame-multiplayer":
                    ApplyMultiplayerTemplate(projectPath);
                    break;
                case "panda3d-starter":
                    ApplyPanda3DTemplate(projectPath);
                    break;
                case "ursina-starter":
                    ApplyUrsinaTemplate(projectPath);
                    break;
                default:
                    ApplyPygameTemplate(projectPath);
                    break;
            }
        }

        private static void WriteCommonGitIgnore(string projectPath)
        {
            File.WriteAllText(Path.Combine(projectPath, ".gitignore"),
@"venv/
.venv/
__pycache__/
*.pyc
.env
.DS_Store
build/
dist/
*.spec
.vscode/
.idea/
*.log
");
        }

        public static void ApplyPygameTemplate(string projectPath)
        {
            WriteCommonGitIgnore(projectPath);
            Directory.CreateDirectory(Path.Combine(projectPath, "assets"));
            Directory.CreateDirectory(Path.Combine(projectPath, "scenes"));
            Directory.CreateDirectory(Path.Combine(projectPath, "scripts"));

            File.WriteAllText(Path.Combine(projectPath, "main.py"),
@"import pygame
import random
import sys
import math

pygame.init()

WIDTH, HEIGHT = 800, 600
GRID_SIZE = 30
ROWS = WIDTH // GRID_SIZE
COLS = HEIGHT // GRID_SIZE

screen = pygame.display.set_mode((WIDTH, HEIGHT))
pygame.display.set_caption(""Cobra 2D Starter"")
clock = pygame.time.Clock()

BLACK = (20, 20, 20)
GREEN = (0, 255, 100)
DARK_GREEN = (0, 200, 80)
RED = (255, 60, 60)
YELLOW = (255, 220, 50)
WHITE = (240, 240, 240)
BLUE = (50, 150, 255)

snake = [[ROWS // 2, COLS // 2]]
dx, dy = 1, 0
food = [random.randint(0, ROWS - 1), random.randint(0, COLS - 1)]
score = 0
high_score = 0
game_speed = 10
core_pos = (WIDTH // 2, HEIGHT // 2)
pulse = 0.0

MENU, PLAYING, GAME_OVER = 0, 1, 2
game_state = MENU

def get_random_empty_position():
    while True:
        pos = [random.randint(0, ROWS - 1), random.randint(0, COLS - 1)]
        if pos not in snake:
            return pos

def draw_grid():
    for x in range(0, WIDTH, GRID_SIZE):
        for y in range(0, HEIGHT, GRID_SIZE):
            pygame.draw.rect(screen, (35, 35, 35), (x, y, GRID_SIZE, GRID_SIZE), 1)

def draw_snake():
    for i, (x, y) in enumerate(snake):
        color = GREEN if i == 0 else DARK_GREEN
        rect = pygame.Rect(x * GRID_SIZE, y * GRID_SIZE, GRID_SIZE - 1, GRID_SIZE - 1)
        pygame.draw.rect(screen, color, rect)

def draw_food():
    x, y = food
    center = (x * GRID_SIZE + GRID_SIZE // 2, y * GRID_SIZE + GRID_SIZE // 2)
    pygame.draw.circle(screen, RED, center, GRID_SIZE // 2 - 2)
    pygame.draw.circle(screen, YELLOW, center, GRID_SIZE // 4)

def move_snake():
    global food, score, game_speed, high_score
    head = snake[0]
    new_head = [(head[0] + dx) % ROWS, (head[1] + dy) % COLS]
    snake.insert(0, new_head)
    if new_head == food:
        score += 10
        high_score = max(high_score, score)
        game_speed = min(25, 10 + score // 50)
        food = get_random_empty_position()
    else:
        snake.pop()

def reset_game():
    global snake, dx, dy, food, score, game_speed, game_state
    snake = [[ROWS // 2, COLS // 2]]
    dx, dy = 1, 0
    score = 0
    game_speed = 10
    food = get_random_empty_position()
    game_state = PLAYING

def main():
    global dx, dy, game_state
    font = pygame.font.Font(None, 36)
    while True:
        clock.tick(game_speed if game_state == PLAYING else 60)
        for event in pygame.event.get():
            if event.type == pygame.QUIT:
                pygame.quit()
                sys.exit()
            elif event.type == pygame.KEYDOWN:
                if event.key == pygame.K_ESCAPE:
                    pygame.quit()
                    sys.exit()
                elif game_state == MENU and event.key == pygame.K_SPACE:
                    reset_game()
                elif game_state == PLAYING:
                    if event.key in (pygame.K_UP, pygame.K_w) and dy != 1: dx, dy = 0, -1
                    elif event.key in (pygame.K_DOWN, pygame.K_s) and dy != -1: dx, dy = 0, 1
                    elif event.key in (pygame.K_LEFT, pygame.K_a) and dx != 1: dx, dy = -1, 0
                    elif event.key in (pygame.K_RIGHT, pygame.K_d) and dx != -1: dx, dy = 1, 0
                elif game_state == GAME_OVER and event.key == pygame.K_r:
                    reset_game()

        screen.fill(BLACK)
        draw_grid()
        if game_state == PLAYING:
            move_snake()
            if snake[0] in snake[1:]:
                game_state = GAME_OVER
            draw_snake()
            draw_food()
            screen.blit(font.render(f'Score: {score}  Best: {high_score}', True, WHITE), (12, 12))
        elif game_state == MENU:
            screen.blit(font.render('Cobra 2D Starter - Press SPACE to Start', True, GREEN), (160, HEIGHT // 2 - 20))
        elif game_state == GAME_OVER:
            draw_snake()
            screen.blit(font.render(f'Game Over! Score: {score} - Press R to Restart', True, RED), (140, HEIGHT // 2 - 20))

        pygame.display.flip()

if __name__ == '__main__':
    main()
");

            File.WriteAllText(Path.Combine(projectPath, "README.md"),
@"# Cobra Pygame 2D Starter

## Getting Started
1. Ensure your project Python virtual environment has `pygame-ce` installed.
2. Run `main.py` using the Run button (`F5`) in Cobra IDE.
3. Use Arrow Keys or WASD to control the player.
");
            File.WriteAllText(Path.Combine(projectPath, "requirements.txt"), "pygame-ce\n");
            WriteDefaultSceneFile(projectPath, "Pygame");
        }

        private static void ApplyPlatformerTemplate(string projectPath)
        {
            File.WriteAllText(Path.Combine(projectPath, "main.py"),
@"import pygame
import sys

pygame.init()
WIDTH, HEIGHT = 960, 540
screen = pygame.display.set_mode((WIDTH, HEIGHT))
pygame.display.set_caption(""Cobra Platformer Starter"")
clock = pygame.time.Clock()

GRAVITY = 0.65
JUMP_FORCE = -13.5
MOVE_SPEED = 5.5

class Player:
    def __init__(self, x, y):
        self.rect = pygame.Rect(x, y, 36, 48)
        self.vx = 0.0
        self.vy = 0.0
        self.on_ground = False

    def update(self, keys, platforms):
        self.vx = 0.0
        if keys[pygame.K_LEFT] or keys[pygame.K_a]:
            self.vx = -MOVE_SPEED
        if keys[pygame.K_RIGHT] or keys[pygame.K_d]:
            self.vx = MOVE_SPEED
        if (keys[pygame.K_SPACE] or keys[pygame.K_w] or keys[pygame.K_UP]) and self.on_ground:
            self.vy = JUMP_FORCE
            self.on_ground = False

        self.vy = min(self.vy + GRAVITY, 16.0)

        self.rect.x += int(self.vx)
        for plat in platforms:
            if self.rect.colliderect(plat):
                if self.vx > 0: self.rect.right = plat.left
                elif self.vx < 0: self.rect.left = plat.right

        self.rect.y += int(self.vy)
        self.on_ground = False
        for plat in platforms:
            if self.rect.colliderect(plat):
                if self.vy > 0:
                    self.rect.bottom = plat.top
                    self.vy = 0
                    self.on_ground = True
                elif self.vy < 0:
                    self.rect.top = plat.bottom
                    self.vy = 0

def main():
    player = Player(100, 300)
    platforms = [
        pygame.Rect(0, 490, 960, 50),
        pygame.Rect(200, 390, 180, 20),
        pygame.Rect(450, 300, 200, 20),
        pygame.Rect(700, 210, 180, 20),
        pygame.Rect(150, 180, 180, 20),
    ]
    coins = [pygame.Rect(275, 355, 18, 18), pygame.Rect(540, 265, 18, 18), pygame.Rect(780, 175, 18, 18), pygame.Rect(230, 145, 18, 18)]
    score = 0
    font = pygame.font.Font(None, 32)

    while True:
        dt = clock.tick(60)
        for event in pygame.event.get():
            if event.type == pygame.QUIT or (event.type == pygame.KEYDOWN and event.key == pygame.K_ESCAPE):
                pygame.quit()
                sys.exit()

        keys = pygame.key.get_pressed()
        player.update(keys, platforms)

        for coin in coins[:]:
            if player.rect.colliderect(coin):
                coins.remove(coin)
                score += 100

        screen.fill((24, 28, 40))
        for plat in platforms:
            pygame.draw.rect(screen, (75, 95, 130), plat, border_radius=4)
        for coin in coins:
            pygame.draw.ellipse(screen, (255, 210, 50), coin)
        pygame.draw.rect(screen, (255, 140, 0), player.rect, border_radius=6)

        hud = font.render(f'Score: {score}  |  Coins Left: {len(coins)}  |  FPS: {int(clock.get_fps())}', True, (240, 240, 240))
        screen.blit(hud, (16, 16))
        pygame.display.flip()

if __name__ == '__main__':
    main()
");
            File.WriteAllText(Path.Combine(projectPath, "requirements.txt"), "pygame-ce\n");
            File.WriteAllText(Path.Combine(projectPath, "README.md"), "# Cobra Platformer Project\n\nSide-scrolling 2D platformer built with Python and Pygame.\n");
            WriteDefaultSceneFile(projectPath, "Pygame");
        }

        private static void ApplyTopDownTemplate(string projectPath)
        {
            File.WriteAllText(Path.Combine(projectPath, "main.py"),
@"import pygame
import sys
import math

pygame.init()
WIDTH, HEIGHT = 900, 600
screen = pygame.display.set_mode((WIDTH, HEIGHT))
pygame.display.set_caption(""Cobra Top-Down Game"")
clock = pygame.time.Clock()

def main():
    player_pos = pygame.Vector2(WIDTH / 2, HEIGHT / 2)
    speed = 260.0
    walls = [pygame.Rect(200, 150, 140, 40), pygame.Rect(520, 240, 40, 180), pygame.Rect(300, 420, 220, 40)]
    font = pygame.font.Font(None, 28)

    while True:
        dt = clock.tick(60) / 1000.0
        for event in pygame.event.get():
            if event.type == pygame.QUIT or (event.type == pygame.KEYDOWN and event.key == pygame.K_ESCAPE):
                pygame.quit()
                sys.exit()

        keys = pygame.key.get_pressed()
        move = pygame.Vector2(0, 0)
        if keys[pygame.K_w] or keys[pygame.K_UP]: move.y -= 1
        if keys[pygame.K_s] or keys[pygame.K_DOWN]: move.y += 1
        if keys[pygame.K_a] or keys[pygame.K_LEFT]: move.x -= 1
        if keys[pygame.K_d] or keys[pygame.K_RIGHT]: move.x += 1
        if move.length_squared() > 0:
            move = move.normalize() * speed * dt

        next_x = pygame.Rect(int(player_pos.x + move.x) - 18, int(player_pos.y) - 18, 36, 36)
        if not any(next_x.colliderect(w) for w in walls):
            player_pos.x = max(20, min(WIDTH - 20, player_pos.x + move.x))

        next_y = pygame.Rect(int(player_pos.x) - 18, int(player_pos.y + move.y) - 18, 36, 36)
        if not any(next_y.colliderect(w) for w in walls):
            player_pos.y = max(20, min(HEIGHT - 20, player_pos.y + move.y))

        screen.fill((28, 36, 32))
        for x in range(0, WIDTH, 45):
            pygame.draw.line(screen, (36, 46, 40), (x, 0), (x, HEIGHT))
        for y in range(0, HEIGHT, 45):
            pygame.draw.line(screen, (36, 46, 40), (0, y), (WIDTH, y))

        for w in walls:
            pygame.draw.rect(screen, (90, 105, 95), w, border_radius=4)

        pygame.draw.circle(screen, (78, 201, 176), (int(player_pos.x), int(player_pos.y)), 18)
        screen.blit(font.render('WASD / Arrows to explore top-down world', True, (220, 220, 220)), (16, 16))
        pygame.display.flip()

if __name__ == '__main__':
    main()
");
            File.WriteAllText(Path.Combine(projectPath, "requirements.txt"), "pygame-ce\n");
            File.WriteAllText(Path.Combine(projectPath, "README.md"), "# Cobra Top-Down Starter\n\n8-directional top-down adventure template.\n");
            WriteDefaultSceneFile(projectPath, "Pygame");
        }

        private static void ApplyRpgTemplate(string projectPath)
        {
            File.WriteAllText(Path.Combine(projectPath, "main.py"),
@"import pygame
import sys

pygame.init()
WIDTH, HEIGHT = 960, 600
screen = pygame.display.set_mode((WIDTH, HEIGHT))
pygame.display.set_caption(""Cobra RPG Starter"")
clock = pygame.time.Clock()

def main():
    player = {'x': 180.0, 'y': 300.0, 'hp': 100, 'max_hp': 100, 'gold': 50, 'level': 1}
    npc = pygame.Rect(520, 260, 40, 40)
    chest = pygame.Rect(680, 160, 36, 36)
    chest_opened = False
    dialogue = 'Press E near the Elder NPC or Treasure Chest to interact.'
    font = pygame.font.Font(None, 28)

    while True:
        dt = clock.tick(60) / 1000.0
        player_rect = pygame.Rect(int(player['x']), int(player['y']), 38, 38)

        for event in pygame.event.get():
            if event.type == pygame.QUIT or (event.type == pygame.KEYDOWN and event.key == pygame.K_ESCAPE):
                pygame.quit()
                sys.exit()
            elif event.type == pygame.KEYDOWN and event.key == pygame.K_e:
                if player_rect.inflate(36, 36).colliderect(npc):
                    dialogue = 'Elder: Welcome, adventurer! Seek the golden chest to the northeast.'
                elif player_rect.inflate(36, 36).colliderect(chest):
                    if not chest_opened:
                        chest_opened = True
                        player['gold'] += 120
                        player['level'] += 1
                        dialogue = 'You opened the chest and found 120 Gold! Level Up!'
                    else:
                        dialogue = 'The treasure chest is empty.'

        keys = pygame.key.get_pressed()
        speed = 220.0
        if keys[pygame.K_w] or keys[pygame.K_UP]: player['y'] -= speed * dt
        if keys[pygame.K_s] or keys[pygame.K_DOWN]: player['y'] += speed * dt
        if keys[pygame.K_a] or keys[pygame.K_LEFT]: player['x'] -= speed * dt
        if keys[pygame.K_d] or keys[pygame.K_RIGHT]: player['x'] += speed * dt

        screen.fill((30, 42, 34))
        pygame.draw.rect(screen, (195, 155, 60) if not chest_opened else (100, 90, 70), chest, border_radius=4)
        pygame.draw.rect(screen, (110, 160, 240), npc, border_radius=6)
        pygame.draw.rect(screen, (255, 140, 0), player_rect, border_radius=6)

        // dialogue and rpg hud
        pygame.draw.rect(screen, (20, 20, 24), (20, HEIGHT - 100, WIDTH - 40, 80), border_radius=8)
        pygame.draw.rect(screen, (255, 140, 0), (20, HEIGHT - 100, WIDTH - 40, 80), 2, border_radius=8)
        screen.blit(font.render(dialogue, True, (240, 240, 240)), (40, HEIGHT - 70))

        stats = f""Lv.{player['level']}  |  HP: {player['hp']}/{player['max_hp']}  |  Gold: {player['gold']}""
        screen.blit(font.render(stats, True, (255, 215, 80)), (20, 20))
        pygame.display.flip()

if __name__ == '__main__':
    main()
");
            File.WriteAllText(Path.Combine(projectPath, "requirements.txt"), "pygame-ce\n");
            File.WriteAllText(Path.Combine(projectPath, "README.md"), "# Cobra RPG Starter\n\nRPG template with stats, inventory gold, and NPC dialogue system.\n");
            WriteDefaultSceneFile(projectPath, "Pygame");
        }

        private static void ApplyArcadeTemplate(string projectPath)
        {
            File.WriteAllText(Path.Combine(projectPath, "main.py"),
@"import pygame
import random
import sys

pygame.init()
WIDTH, HEIGHT = 800, 650
screen = pygame.display.set_mode((WIDTH, HEIGHT))
pygame.display.set_caption(""Cobra Arcade Shooter"")
clock = pygame.time.Clock()

def main():
    player = pygame.Rect(WIDTH // 2 - 24, HEIGHT - 70, 48, 32)
    bullets = []
    enemies = []
    spawn_timer = 0.0
    score = 0
    font = pygame.font.Font(None, 32)

    while True:
        dt = clock.tick(60) / 1000.0
        for event in pygame.event.get():
            if event.type == pygame.QUIT or (event.type == pygame.KEYDOWN and event.key == pygame.K_ESCAPE):
                pygame.quit()
                sys.exit()
            elif event.type == pygame.KEYDOWN and event.key == pygame.K_SPACE:
                bullets.append(pygame.Rect(player.centerx - 3, player.top - 12, 6, 14))

        keys = pygame.key.get_pressed()
        if keys[pygame.K_LEFT] or keys[pygame.K_a]: player.x = max(10, player.x - int(380 * dt))
        if keys[pygame.K_RIGHT] or keys[pygame.K_d]: player.x = min(WIDTH - 58, player.x + int(380 * dt))

        spawn_timer += dt
        if spawn_timer >= 0.75:
            spawn_timer = 0.0
            enemies.append(pygame.Rect(random.randint(20, WIDTH - 56), -40, 36, 36))

        for b in bullets[:]:
            b.y -= int(550 * dt)
            if b.bottom < 0:
                bullets.remove(b)

        for e in enemies[:]:
            e.y += int(180 * dt)
            if e.top > HEIGHT:
                enemies.remove(e)
            for b in bullets[:]:
                if e.colliderect(b):
                    enemies.remove(e)
                    bullets.remove(b)
                    score += 25
                    break

        screen.fill((12, 14, 24))
        pygame.draw.rect(screen, (78, 201, 176), player, border_radius=4)
        for b in bullets:
            pygame.draw.rect(screen, (255, 220, 60), b)
        for e in enemies:
            pygame.draw.rect(screen, (235, 65, 85), e, border_radius=5)

        screen.blit(font.render(f'SCORE: {score}   (SPACE to Fire)', True, (240, 240, 240)), (16, 16))
        pygame.display.flip()

if __name__ == '__main__':
    main()
");
            File.WriteAllText(Path.Combine(projectPath, "requirements.txt"), "pygame-ce\n");
            File.WriteAllText(Path.Combine(projectPath, "README.md"), "# Cobra Arcade Game\n\nSpace shooter arcade game starter.\n");
            WriteDefaultSceneFile(projectPath, "Pygame");
        }

        private static void ApplyPuzzleTemplate(string projectPath)
        {
            File.WriteAllText(Path.Combine(projectPath, "main.py"),
@"import pygame
import random
import sys

pygame.init()
WIDTH, HEIGHT = 600, 680
screen = pygame.display.set_mode((WIDTH, HEIGHT))
pygame.display.set_caption(""Cobra Puzzle Starter"")
clock = pygame.time.Clock()

GRID_N = 4
TILE = 110
MARGIN = 60

def main():
    board = list(range(1, GRID_N * GRID_N)) + [0]
    moves = 0
    font = pygame.font.Font(None, 42)
    small_font = pygame.font.Font(None, 28)

    # perform valid shuffles
    for _ in range(80):
        zero_idx = board.index(0)
        zr, zc = divmod(zero_idx, GRID_N)
        neighbors = []
        for dr, dc in [(-1,0),(1,0),(0,-1),(0,1)]:
            nr, nc = zr + dr, zc + dc
            if 0 <= nr < GRID_N and 0 <= nc < GRID_N:
                neighbors.append(nr * GRID_N + nc)
        swap_idx = random.choice(neighbors)
        board[zero_idx], board[swap_idx] = board[swap_idx], board[zero_idx]

    while True:
        clock.tick(60)
        for event in pygame.event.get():
            if event.type == pygame.QUIT or (event.type == pygame.KEYDOWN and event.key == pygame.K_ESCAPE):
                pygame.quit()
                sys.exit()
            elif event.type == pygame.MOUSEBUTTONDOWN and event.button == 1:
                mx, my = event.pos
                c = (mx - MARGIN) // TILE
                r = (my - 120) // TILE
                if 0 <= r < GRID_N and 0 <= c < GRID_N:
                    idx = r * GRID_N + c
                    zero_idx = board.index(0)
                    zr, zc = divmod(zero_idx, GRID_N)
                    if abs(r - zr) + abs(c - zc) == 1:
                        board[zero_idx], board[idx] = board[idx], board[zero_idx]
                        moves += 1

        solved = board == list(range(1, GRID_N * GRID_N)) + [0]
        screen.fill((26, 28, 34))
        screen.blit(small_font.render(f'Moves: {moves}   |   Status: {""SOLVED!"" if solved else ""Slide tiles to order 1-15""}', True, (240, 240, 240)), (MARGIN, 50))

        for i, val in enumerate(board):
            if val == 0:
                continue
            r, c = divmod(i, GRID_N)
            rect = pygame.Rect(MARGIN + c * TILE + 6, 120 + r * TILE + 6, TILE - 12, TILE - 12)
            pygame.draw.rect(screen, (78, 201, 176) if solved else (255, 140, 0), rect, border_radius=8)
            txt = font.render(str(val), True, (20, 20, 20))
            screen.blit(txt, txt.get_rect(center=rect.center))

        pygame.display.flip()

if __name__ == '__main__':
    main()
");
            File.WriteAllText(Path.Combine(projectPath, "requirements.txt"), "pygame-ce\n");
            File.WriteAllText(Path.Combine(projectPath, "README.md"), "# Cobra Puzzle Game\n\nGrid-based sliding puzzle game template.\n");
            WriteDefaultSceneFile(projectPath, "Pygame");
        }

        private static void ApplyMultiplayerTemplate(string projectPath)
        {
            File.WriteAllText(Path.Combine(projectPath, "main.py"),
@"import pygame
import socket
import json
import threading
import sys

pygame.init()
WIDTH, HEIGHT = 900, 560
screen = pygame.display.set_mode((WIDTH, HEIGHT))
pygame.display.set_caption(""Cobra Multiplayer Arena Starter"")
clock = pygame.time.Clock()

# local and udp loopback state synchronization starter
UDP_PORT = 54545
peer_state = {'x': 680.0, 'y': 280.0}

def network_listener(stop_event):
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    try:
        sock.bind(('127.0.0.1', UDP_PORT))
        sock.settimeout(0.2)
        while not stop_event.is_set():
            try:
                data, _ = sock.recvfrom(1024)
                msg = json.loads(data.decode('utf-8'))
                peer_state['x'] = float(msg.get('x', peer_state['x']))
                peer_state['y'] = float(msg.get('y', peer_state['y']))
            except socket.timeout:
                pass
    except Exception:
        pass
    finally:
        sock.close()

def main():
    stop_event = threading.Event()
    listener = threading.Thread(target=network_listener, args=(stop_event,), daemon=True)
    listener.start()

    p1 = pygame.Vector2(220, 280)
    p2 = pygame.Vector2(680, 280)
    speed = 250.0
    font = pygame.font.Font(None, 28)

    while True:
        dt = clock.tick(60) / 1000.0
        for event in pygame.event.get():
            if event.type == pygame.QUIT or (event.type == pygame.KEYDOWN and event.key == pygame.K_ESCAPE):
                stop_event.set()
                pygame.quit()
                sys.exit()

        keys = pygame.key.get_pressed()
        if keys[pygame.K_w]: p1.y -= speed * dt
        if keys[pygame.K_s]: p1.y += speed * dt
        if keys[pygame.K_a]: p1.x -= speed * dt
        if keys[pygame.K_d]: p1.x += speed * dt

        if keys[pygame.K_UP]: p2.y -= speed * dt
        if keys[pygame.K_DOWN]: p2.y += speed * dt
        if keys[pygame.K_LEFT]: p2.x -= speed * dt
        if keys[pygame.K_RIGHT]: p2.x += speed * dt

        screen.fill((22, 26, 34))
        pygame.draw.line(screen, (50, 60, 75), (WIDTH // 2, 0), (WIDTH // 2, HEIGHT), 2)
        pygame.draw.circle(screen, (255, 140, 0), (int(p1.x), int(p1.y)), 22)
        pygame.draw.circle(screen, (78, 201, 176), (int(p2.x), int(p2.y)), 22)

        screen.blit(font.render('Player 1 (WASD) vs Player 2 (Arrows) + UDP Socket Sync Ready', True, (235, 235, 235)), (16, 16))
        pygame.display.flip()

if __name__ == '__main__':
    main()
");
            File.WriteAllText(Path.Combine(projectPath, "requirements.txt"), "pygame-ce\n");
            File.WriteAllText(Path.Combine(projectPath, "README.md"), "# Cobra Multiplayer Starter\n\n2-Player local and UDP socket multiplayer starter.\n");
            WriteDefaultSceneFile(projectPath, "Pygame");
        }

        private static void ApplyPanda3DTemplate(string projectPath)
        {
            File.WriteAllText(Path.Combine(projectPath, "main.py"),
@"from direct.showbase.ShowBase import ShowBase
from panda3d.core import AmbientLight, DirectionalLight, Vec4, Vec3
import math

class CobraPanda3DGame(ShowBase):
    def __init__(self):
        super().__init__()
        self.set_background_color(0.1, 0.12, 0.16, 1.0)
        self.disable_mouse()
        self.camera.set_pos(0, -14, 6)
        self.camera.look_at(0, 0, 0)

        # load default environment model included with panda3d
        self.cube = self.loader.load_model('models/box')
        self.cube.reparent_to(self.render)
        self.cube.set_scale(1.5, 1.5, 1.5)
        self.cube.set_pos(-0.75, -0.75, 0)

        # setup lighting
        ambient = AmbientLight('ambient')
        ambient.set_color(Vec4(0.35, 0.35, 0.4, 1))
        self.render.set_light(self.render.attach_new_node(ambient))

        dlight = DirectionalLight('dlight')
        dlight.set_color(Vec4(0.9, 0.85, 0.8, 1))
        dlnp = self.render.attach_new_node(dlight)
        dlnp.set_hpr(45, -45, 0)
        self.render.set_light(dlnp)

        self.angle = 0.0
        self.taskMgr.add(self.spin_task, 'SpinTask')

    def spin_task(self, task):
        dt = globalClock.get_dt()
        self.angle += 45.0 * dt
        self.cube.set_hpr(self.angle, self.angle * 0.5, 0)
        return task.cont

if __name__ == '__main__':
    game = CobraPanda3DGame()
    game.run()
");
            File.WriteAllText(Path.Combine(projectPath, "requirements.txt"), "panda3d\n");
            File.WriteAllText(Path.Combine(projectPath, "README.md"), "# Cobra Panda3D 3D Starter\n\n3D Python game project powered by Panda3D.\n");
            WriteDefaultSceneFile(projectPath, "Panda3D");
        }

        private static void ApplyUrsinaTemplate(string projectPath)
        {
            File.WriteAllText(Path.Combine(projectPath, "main.py"),
@"from ursina import *

app = Ursina(title='Cobra Ursina 3D Game')

window.color = color.rgb(25, 30, 42)
ground = Entity(model='plane', scale=(20, 1, 20), color=color.rgb(55, 65, 80), texture='white_cube', texture_scale=(20, 20), collider='box')
player = Entity(model='cube', color=color.orange, position=(0, 0.5, 0), scale=(1, 1, 1), collider='box')

DirectionalLight(y=2, z=3, shadows=True)
AmbientLight(color=color.rgba(100, 100, 120, 0.4))
EditorCamera()

def update():
    speed = 6 * time.dt
    player.x += (held_keys['d'] - held_keys['a']) * speed
    player.z += (held_keys['w'] - held_keys['s']) * speed
    player.rotation_y += 40 * time.dt

if __name__ == '__main__':
    app.run()
");
            File.WriteAllText(Path.Combine(projectPath, "requirements.txt"), "ursina\n");
            File.WriteAllText(Path.Combine(projectPath, "README.md"), "# Cobra Ursina 3D Starter\n\n3D Python game project powered by Ursina Engine.\n");
            WriteDefaultSceneFile(projectPath, "Ursina");
        }

        private static void WriteDefaultSceneFile(string projectPath, string framework)
        {
            string sceneDir = Path.Combine(projectPath, "scenes");
            Directory.CreateDirectory(sceneDir);
            string scenePath = Path.Combine(sceneDir, "main.Cobra");
            if (File.Exists(scenePath)) return;

            bool is3D = framework.Equals("Panda3D", StringComparison.OrdinalIgnoreCase) || framework.Equals("Ursina", StringComparison.OrdinalIgnoreCase);
            string json = $@"{{
  ""FormatVersion"": 2,
  ""SceneName"": ""MainScene"",
  ""Framework"": ""{framework}"",
  ""Is3D"": {(is3D ? "true" : "false")},
  ""CameraWidth"": 800,
  ""CameraHeight"": 600,
  ""Objects"": [
    {{
      ""Id"": ""player-1"",
      ""Name"": ""Player"",
      ""X"": 160,
      ""Y"": 200,
      ""Z"": 0,
      ""Width"": 64,
      ""Height"": 64,
      ""Depth"": 64,
      ""Rotation"": 0,
      ""ScaleX"": 1,
      ""ScaleY"": 1,
      ""ScaleZ"": 1,
      ""Color"": ""#ff8c00"",
      ""Layer"": ""Default"",
      ""IsVisible"": true,
      ""IsLocked"": false,
      ""ColliderType"": ""Rectangle"",
      ""HasRigidbody"": true,
      ""Mass"": 1.0,
      ""UseGravity"": false
    }}
  ]
}}";
            File.WriteAllText(scenePath, json);
        }

        public static void ApplyBackendTemplate(string backendPath, string backendType)
        {
            Directory.CreateDirectory(backendPath);
            string engineName = string.IsNullOrWhiteSpace(backendType) ? "Python/Flask" : backendType;
            File.WriteAllText(Path.Combine(backendPath, "app.py"),
$@"from flask import Flask, jsonify

app = Flask(__name__)

@app.route('/api/status')
def status():
    return jsonify({{'status': 'Backend is running', 'engine': '{engineName}'}})

if __name__ == '__main__':
    app.run(port=5000, debug=False)
");
            File.WriteAllText(Path.Combine(backendPath, "requirements.txt"), "flask\n");
        }
    }
}
