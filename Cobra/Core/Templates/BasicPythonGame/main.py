import pygame
import random
import sys
import math

pygame.init()

# settings
WIDTH, HEIGHT = 800, 600
GRID_SIZE = 30
ROWS = WIDTH // GRID_SIZE
COLS = HEIGHT // GRID_SIZE

screen = pygame.display.set_mode((WIDTH, HEIGHT))
pygame.display.set_caption("Cobra - Wraparound Edition")

clock = pygame.time.Clock()

# colors
BLACK = (20, 20, 20)
GREEN = (0, 255, 100)
DARK_GREEN = (0, 200, 80)
RED = (255, 60, 60)
YELLOW = (255, 220, 50)
WHITE = (240, 240, 240)
BLUE = (50, 150, 255)
PURPLE = (150, 50, 255)
ORANGE = (255, 140, 50)

# game variables
snake = [[ROWS // 2, COLS // 2]]
dx, dy = 1, 0  # start moving right

food = [random.randint(0, ROWS - 1), random.randint(0, COLS - 1)]
score = 0
high_score = 0
game_speed = 10
speed_increase_score = 5  # increase speed every 5 points

core_pos = (WIDTH // 2, HEIGHT // 2)
pulse = 0

# game states
MENU = 0
PLAYING = 1
GAME_OVER = 2
game_state = MENU

# helper functions
def get_random_empty_position():
    """get a random position that is not occupied by the snake"""
    while True:
        pos = [random.randint(0, ROWS - 1), random.randint(0, COLS - 1)]
        if pos not in snake:
            return pos

# draw functions
def draw_grid():
    for x in range(0, WIDTH, GRID_SIZE):
        for y in range(0, HEIGHT, GRID_SIZE):
            pygame.draw.rect(screen, (40, 40, 40), (x, y, GRID_SIZE, GRID_SIZE), 1)

def draw_snake():
    for i, segment in enumerate(snake):
        x, y = segment
        # gradient effect, head is brighter
        color = GREEN if i == 0 else DARK_GREEN
        rect = pygame.Rect(x * GRID_SIZE, y * GRID_SIZE, GRID_SIZE - 1, GRID_SIZE - 1)
        pygame.draw.rect(screen, color, rect)
        pygame.draw.rect(screen, (0, 150, 50), rect, 1)

def draw_food():
    x, y = food
    center = (x * GRID_SIZE + GRID_SIZE // 2, y * GRID_SIZE + GRID_SIZE // 2)
    pygame.draw.circle(screen, RED, center, GRID_SIZE // 2 - 2)
    pygame.draw.circle(screen, YELLOW, center, GRID_SIZE // 4)

def draw_core():
    global pulse
    pulse += 0.1
    radius = 50 + int(8 * math.sin(pulse))
    
    # draw pulsing glow effect
    for r in range(radius, radius - 20, -5):
        alpha = 255 - (radius - r) * 15
        color = (min(255, BLUE[0] + alpha // 3), 
                 min(255, BLUE[1] + alpha // 3), 
                 min(255, BLUE[2] + alpha // 3))
        pygame.draw.circle(screen, color, core_pos, r, 2)
    
    pygame.draw.circle(screen, BLUE, core_pos, radius - 15)
    pygame.draw.circle(screen, WHITE, core_pos, radius - 25)

def draw_score():
    font = pygame.font.Font(None, 36)
    score_text = font.render(f"Score: {score}", True, WHITE)
    screen.blit(score_text, (10, 10))
    
    high_score_text = font.render(f"Best: {high_score}", True, WHITE)
    screen.blit(high_score_text, (10, 50))
    
    if game_state == PLAYING:
        speed_text = pygame.font.Font(None, 24).render(f"Speed: {game_speed}", True, WHITE)
        screen.blit(speed_text, (10, 90))

def draw_menu():
    # semi-transparent overlay
    overlay = pygame.Surface((WIDTH, HEIGHT))
    overlay.set_alpha(128)
    overlay.fill(BLACK)
    screen.blit(overlay, (0, 0))
    
    font_title = pygame.font.Font(None, 72)
    font_text = pygame.font.Font(None, 36)
    font_small = pygame.font.Font(None, 24)
    
    title = font_title.render("Cobra", True, GREEN)
    title_rect = title.get_rect(center=(WIDTH // 2, HEIGHT // 2 - 100))
    screen.blit(title, title_rect)
    
    subtitle = font_text.render("Wraparound Snake Game", True, WHITE)
    subtitle_rect = subtitle.get_rect(center=(WIDTH // 2, HEIGHT // 2 - 40))
    screen.blit(subtitle, subtitle_rect)
    
    controls = [
        "Controls:",
        "Arrow Keys - Move the snake",
        "ESC - Quit game",
        "R - Restart (when game over)",
        "",
        "Features:",
        "- No walls - wrap around the screen!",
        "- Score increases near the core",
        "- Speed increases every 5 points",
        "- Press SPACE to start"
    ]
    
    y_offset = HEIGHT // 2 + 20
    for line in controls:
        text = font_small.render(line, True, WHITE if line else BLACK)
        text_rect = text.get_rect(center=(WIDTH // 2, y_offset))
        if line:
            screen.blit(text, text_rect)
        y_offset += 25

def draw_game_over():
    # semi-transparent overlay
    overlay = pygame.Surface((WIDTH, HEIGHT))
    overlay.set_alpha(180)
    overlay.fill(BLACK)
    screen.blit(overlay, (0, 0))
    
    font_game_over = pygame.font.Font(None, 64)
    font_text = pygame.font.Font(None, 36)
    
    game_over_text = font_game_over.render("GAME OVER", True, RED)
    game_over_rect = game_over_text.get_rect(center=(WIDTH // 2, HEIGHT // 2 - 50))
    screen.blit(game_over_text, game_over_rect)
    
    score_text = font_text.render(f"Final Score: {score}", True, WHITE)
    score_rect = score_text.get_rect(center=(WIDTH // 2, HEIGHT // 2 + 20))
    screen.blit(score_text, score_rect)
    
    high_score_text = font_text.render(f"High Score: {high_score}", True, YELLOW)
    high_score_rect = high_score_text.get_rect(center=(WIDTH // 2, HEIGHT // 2 + 60))
    screen.blit(high_score_text, high_score_rect)
    
    restart_text = font_text.render("Press R to Restart or ESC to Quit", True, WHITE)
    restart_rect = restart_text.get_rect(center=(WIDTH // 2, HEIGHT // 2 + 120))
    screen.blit(restart_text, restart_rect)

# game logic
def distance_to_core(pos):
    """calculate distance from a position to the core"""
    px = pos[0] * GRID_SIZE + GRID_SIZE // 2
    py = pos[1] * GRID_SIZE + GRID_SIZE // 2
    return math.sqrt((px - core_pos[0])**2 + (py - core_pos[1])**2)

def move_snake():
    global food, score, game_speed, high_score
    
    head = snake[0]
    new_head = [head[0] + dx, head[1] + dy]
    
    # wraparound logic without walls
    new_head[0] = new_head[0] % ROWS
    new_head[1] = new_head[1] % COLS
    
    snake.insert(0, new_head)
    
    if new_head == food:
        # score depends on distance to core
        dist = distance_to_core(new_head)
        points_earned = max(1, int(100 / (dist + 1)) + random.randint(0, 5))
        score += points_earned
        
        # update high score
        if score > high_score:
            high_score = score
        
        # increase speed every speed_increase_score points
        game_speed = min(25, 10 + (score // speed_increase_score))
        
        # spawn new food at empty position
        food = get_random_empty_position()
    else:
        snake.pop()

def check_collision():
    head = snake[0]
    
    # self collision without wall collision
    if head in snake[1:]:
        return True
    
    return False

def reset_game():
    global snake, dx, dy, food, score, game_speed, game_state
    snake = [[ROWS // 2, COLS // 2]]
    dx, dy = 1, 0
    score = 0
    game_speed = 10
    food = get_random_empty_position()
    game_state = PLAYING

# game loop
while True:
    clock.tick(game_speed)
    
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
                # prevent 180-degree turns
                if event.key == pygame.K_UP and dy != 1:
                    dx, dy = 0, -1
                elif event.key == pygame.K_DOWN and dy != -1:
                    dx, dy = 0, 1
                elif event.key == pygame.K_LEFT and dx != 1:
                    dx, dy = -1, 0
                elif event.key == pygame.K_RIGHT and dx != -1:
                    dx, dy = 1, 0
            
            elif game_state == GAME_OVER:
                if event.key == pygame.K_r:
                    reset_game()
    
    if game_state == PLAYING:
        move_snake()
        
        if check_collision():
            game_state = GAME_OVER
        
        # draw game
        screen.fill(BLACK)
        draw_grid()
        draw_core()
        draw_snake()
        draw_food()
        draw_score()
    
    elif game_state == MENU:
        screen.fill(BLACK)
        draw_grid()
        draw_core()
        draw_menu()
    
    elif game_state == GAME_OVER:
        screen.fill(BLACK)
        draw_grid()
        draw_core()
        draw_snake()
        draw_food()
        draw_score()
        draw_game_over()
    
    pygame.display.flip()
