# Core Collector

A simple 2D game made with **pygame-ce**. Control the snake, collect food, and score points on a grid.

Scoring bonus: You earn more points when you collect food close to the blue core at the center of the screen.

## Requirements

- Python 3.10 or newer
- pygame-ce

## Project Files

```text
BasicPythonGame 
├── assets/
│   └── cobra.png
├── .gitignore
├── main.py
├── README.md
└── requirements.txt
```

## How to Play

- **Arrow Keys:** Move up, down, left, or right
- **Red Squares:** Collect for points
- **Blue Core:** Stay close to earn higher scores
- **Escape:** Quit the game

Avoid hitting the walls or your own tail.

## Run the Game

```bash
pip install -r requirements.txt
python main.py
```

## What This Teaches

- Game loop basics
- Keyboard input and movement
- Grid-based collisions
- Drawing shapes and text with Pygame
