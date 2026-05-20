using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Snake
{
    public class GameForm : Form
    {
        // механика стрельбы
        private class Projectile { public Point Pos; public Point Dir; }

        // золотое яблоко
        private enum FoodType { Normal, Golden }
        private class Apple { public Point Pos; public FoodType Type; }

        private List<Point> snake;
        private List<Projectile> bullets;
        private List<Apple> apples;

        // противники и препятствия
        private List<Point> stones;
        private List<Point> devourers;
        private List<Point> hunters;

        private int directionX;
        private int directionY;

        private int score = 0;
        private int pendingGrowth = 0; // золотое яблоко

        // стадии игры
        private int tickCounter = 0;
        private int currentStage = 1;

        // противники и препятствия
        private const int MaxDevourers = 3;
        private const int MaxHunters = 2;
        private const int MaxStoneBlocks = 100;

        private const int cellSize = 25;
        // перекраска и расширение поля
        private const int gridWidth = 64;
        private const int gridHeight = 40;

        private Timer gameTimer;
        private Random rnd = new Random();

        public GameForm()
        {
            this.Text = "Боевой питон";
            this.ClientSize = new Size(cellSize * gridWidth, cellSize * gridHeight);
            this.DoubleBuffered = true;
            this.KeyPreview = true;
            this.KeyDown += OnKeyDown;

            // перекраска и расширение поля
            this.BackColor = Color.LightGreen;

            InitGame();

            gameTimer = new Timer();
            gameTimer.Interval = 150;
            gameTimer.Tick += Update;
            gameTimer.Start();
        }

        private void InitGame()
        {
            snake = new List<Point>();
            snake.Add(new Point(gridWidth / 2, gridHeight / 2));

            bullets = new List<Projectile>();
            apples = new List<Apple>();
            stones = new List<Point>();
            devourers = new List<Point>();
            hunters = new List<Point>();

            score = 0;
            pendingGrowth = 0;
            tickCounter = 0;
            currentStage = 1;
            directionX = 1;
            directionY = 0;

            GenerateFood();
        }

        private Point GetFreePosition()
        {
            Point p;
            bool isFree;
            do
            {
                p = new Point(rnd.Next(0, gridWidth - 1), rnd.Next(0, gridHeight - 1));
                isFree = true;
                foreach (var s in snake) if (s == p) isFree = false;
                foreach (var st in stones) if (st == p) isFree = false;
                foreach (var a in apples) if (a.Pos == p) isFree = false;
            } while (!isFree);
            return p;
        }

        // золотое яблоко
        // стадии игры
        private void GenerateFood()
        {
            int targetApples = 1;
            if (currentStage == 3) targetApples = 2;
            if (currentStage >= 4) targetApples = 3;

            int goldenChance = currentStage >= 4 ? 15 : 10;

            while (apples.Count < targetApples)
            {
                FoodType newType = rnd.Next(100) < goldenChance ? FoodType.Golden : FoodType.Normal;
                apples.Add(new Apple { Pos = GetFreePosition(), Type = newType });
            }
        }

        // противники и препятствия
        private void SpawnStone()
        {
            if (stones.Count >= MaxStoneBlocks) return;

            Point p = GetFreePosition();

            if (rnd.Next(2) == 0)
            {
                if (p != snake[0]) stones.Add(p);
            }
            else
            {
                List<Point> newBlock = new List<Point>
                {
                    p,
                    new Point(p.X + 1, p.Y),
                    new Point(p.X, p.Y + 1),
                    new Point(p.X + 1, p.Y + 1)
                };

                foreach (var cell in newBlock)
                {
                    if (cell != snake[0]) stones.Add(cell);
                }
            }
        }

        // противники и препятствия
        private bool IsWalkable(Point p)
        {
            if (p.X < 0 || p.X >= gridWidth || p.Y < 0 || p.Y >= gridHeight) return false;
            foreach (var st in stones) if (st == p) return false;
            return true;
        }

        // противники и препятствия
        private Point GetNextEnemyStep(Point current, Point target)
        {
            int dx = 0, dy = 0;
            if (current.X < target.X) dx = 1;
            else if (current.X > target.X) dx = -1;

            if (current.Y < target.Y) dy = 1;
            else if (current.Y > target.Y) dy = -1;

            Point moveX = new Point(current.X + dx, current.Y);
            Point moveY = new Point(current.X, current.Y + dy);

            bool canMoveX = dx != 0 && IsWalkable(moveX);
            bool canMoveY = dy != 0 && IsWalkable(moveY);

            if (canMoveX && canMoveY) return rnd.Next(2) == 0 ? moveX : moveY;
            if (canMoveX) return moveX;
            if (canMoveY) return moveY;

            if (dx != 0 && dy == 0)
            {
                if (IsWalkable(new Point(current.X, current.Y + 1))) return new Point(current.X, current.Y + 1);
                if (IsWalkable(new Point(current.X, current.Y - 1))) return new Point(current.X, current.Y - 1);
            }
            else if (dy != 0 && dx == 0)
            {
                if (IsWalkable(new Point(current.X + 1, current.Y))) return new Point(current.X + 1, current.Y);
                if (IsWalkable(new Point(current.X - 1, current.Y))) return new Point(current.X - 1, current.Y);
            }

            return current;
        }

        private void Update(object sender, EventArgs e)
        {
            tickCounter++;

            int oldStage = currentStage;

            // стадии игры
            if (score >= 12) currentStage = 4;
            else if (score >= 7) currentStage = 3;
            else if (score >= 4) currentStage = 2;
            else currentStage = 1;

            if (currentStage > oldStage)
            {
                GenerateFood();
            }

            // противники и препятствия
            if (currentStage >= 2 && tickCounter % 40 == 0) SpawnStone();
            if (currentStage >= 3 && tickCounter % 50 == 0 && devourers.Count < MaxDevourers) devourers.Add(GetFreePosition());
            if (currentStage >= 4 && tickCounter % 60 == 0 && hunters.Count < MaxHunters) hunters.Add(GetFreePosition());

            // механика стрельбы
            for (int i = bullets.Count - 1; i >= 0; i--)
            {
                bullets[i].Pos.X += bullets[i].Dir.X;
                bullets[i].Pos.Y += bullets[i].Dir.Y;

                Point bPos = bullets[i].Pos;
                bool bulletDestroyed = false;

                if (bPos.X < 0 || bPos.X >= gridWidth || bPos.Y < 0 || bPos.Y >= gridHeight)
                    bulletDestroyed = true;

                for (int s = stones.Count - 1; s >= 0; s--)
                {
                    if (stones[s] == bPos) { stones.RemoveAt(s); bulletDestroyed = true; break; }
                }

                for (int d = devourers.Count - 1; d >= 0; d--)
                {
                    if (devourers[d] == bPos) { devourers.RemoveAt(d); bulletDestroyed = true; break; }
                }

                for (int h = hunters.Count - 1; h >= 0; h--)
                {
                    if (hunters[h] == bPos) { hunters.RemoveAt(h); bulletDestroyed = true; break; }
                }

                if (bulletDestroyed) bullets.RemoveAt(i);
            }

            // противники и препятствия
            if (tickCounter % 4 == 0)
            {
                for (int i = 0; i < devourers.Count; i++)
                {
                    if (apples.Count > 0)
                    {
                        Point target = apples[0].Pos;
                        double minDistance = double.MaxValue;

                        foreach (var apple in apples)
                        {
                            double dist = Math.Pow(devourers[i].X - apple.Pos.X, 2) + Math.Pow(devourers[i].Y - apple.Pos.Y, 2);
                            if (dist < minDistance)
                            {
                                minDistance = dist;
                                target = apple.Pos;
                            }
                        }

                        devourers[i] = GetNextEnemyStep(devourers[i], target);

                        for (int a = apples.Count - 1; a >= 0; a--)
                        {
                            if (devourers[i] == apples[a].Pos)
                            {
                                apples.RemoveAt(a);
                                GenerateFood();
                                break;
                            }
                        }
                    }
                }
            }

            // противники и препятствия
            if (tickCounter % 3 == 0)
            {
                for (int i = 0; i < hunters.Count; i++)
                {
                    hunters[i] = GetNextEnemyStep(hunters[i], snake[0]);
                }
            }

            Point head = snake[0];
            Point newHead = new Point(head.X + directionX, head.Y + directionY);

            if (newHead.X < 0 || newHead.X >= gridWidth || newHead.Y < 0 || newHead.Y >= gridHeight)
            { GameOver("Вы покинули пределы поля!"); return; }

            for (int i = 0; i < snake.Count; i++)
            {
                if (snake[i] == newHead) { GameOver("Вы врезались в свой хвост!"); return; }
            }

            // противники и препятствия
            foreach (var st in stones)
            {
                if (st == newHead) { GameOver("Вы разбились о камень!"); return; }
            }

            foreach (var d in devourers) if (d == newHead) { GameOver("Пожиратель уничтожил вас!"); return; }
            foreach (var h in hunters) if (h == newHead) { GameOver("Охотник поймал вас!"); return; }

            snake.Insert(0, newHead);

            // противники и препятствия
            bool wasBitten = false;
            for (int h = 0; h < hunters.Count; h++)
            {
                for (int s = 1; s < snake.Count; s++)
                {
                    if (hunters[h] == snake[s])
                    {
                        snake.RemoveRange(s, snake.Count - s);
                        wasBitten = true;
                        break;
                    }
                }
            }

            bool ateSomething = false;
            for (int i = 0; i < apples.Count; i++)
            {
                if (newHead == apples[i].Pos)
                {
                    ateSomething = true;
                    // золотое яблоко
                    if (apples[i].Type == FoodType.Golden) { score += 3; pendingGrowth += 2; }
                    else { score++; }

                    apples.RemoveAt(i);
                    GenerateFood();
                    break;
                }
            }

            if (!ateSomething && !wasBitten)
            {
                if (pendingGrowth > 0)
                {
                    pendingGrowth--;
                }
                else if (snake.Count > 1)
                {
                    snake.RemoveAt(snake.Count - 1);
                }
            }

            this.Invalidate();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            // WASD управление
            if (e.KeyCode == Keys.W && directionY != 1) { directionX = 0; directionY = -1; }
            else if (e.KeyCode == Keys.S && directionY != -1) { directionX = 0; directionY = 1; }
            else if (e.KeyCode == Keys.A && directionX != 1) { directionX = -1; directionY = 0; }
            else if (e.KeyCode == Keys.D && directionX != -1) { directionX = 1; directionY = 0; }
            // механика стрельбы
            else if (e.KeyCode == Keys.Space)
            {
                if (snake.Count > 1 && pendingGrowth == 0)
                {
                    snake.RemoveAt(snake.Count - 1);
                    Point head = snake[0];
                    Point bulletStartPos = new Point(head.X + directionX, head.Y + directionY);
                    bullets.Add(new Projectile { Pos = bulletStartPos, Dir = new Point(directionX, directionY) });
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;

            // перекраска и расширение поля
            Pen gridPen = new Pen(Color.SeaGreen, 1);
            for (int i = 0; i <= gridWidth; i++) g.DrawLine(gridPen, i * cellSize, 0, i * cellSize, gridHeight * cellSize);
            for (int i = 0; i <= gridHeight; i++) g.DrawLine(gridPen, 0, i * cellSize, gridWidth * cellSize, i * cellSize);

            Pen wallPen = new Pen(Color.SaddleBrown, 2);
            g.DrawRectangle(wallPen, 1, 1, (gridWidth * cellSize) - 2, (gridHeight * cellSize) - 2);

            // противники и препятствия
            foreach (var st in stones) g.FillRectangle(Brushes.DimGray, st.X * cellSize, st.Y * cellSize, cellSize, cellSize);

            // золотое яблоко
            foreach (var apple in apples)
            {
                if (apple.Type == FoodType.Golden)
                {
                    g.FillEllipse(Brushes.Gold, apple.Pos.X * cellSize, apple.Pos.Y * cellSize, cellSize, cellSize);
                    g.DrawEllipse(Pens.DarkOrange, apple.Pos.X * cellSize, apple.Pos.Y * cellSize, cellSize, cellSize);
                }
                else g.FillEllipse(Brushes.Red, apple.Pos.X * cellSize, apple.Pos.Y * cellSize, cellSize, cellSize);
            }

            // противники и препятствия
            foreach (var d in devourers) g.FillRectangle(Brushes.Purple, d.X * cellSize, d.Y * cellSize, cellSize, cellSize);
            foreach (var h in hunters) g.FillRectangle(Brushes.DarkOrange, h.X * cellSize, h.Y * cellSize, cellSize, cellSize);

            // механика стрельбы
            foreach (var b in bullets) g.FillEllipse(Brushes.Yellow, b.Pos.X * cellSize + 5, b.Pos.Y * cellSize + 5, cellSize - 10, cellSize - 10);

            // перекраска и расширение поля
            for (int i = 0; i < snake.Count; i++)
            {
                Brush b = i == 0 ? Brushes.Navy : Brushes.Blue;
                g.FillRectangle(b, snake[i].X * cellSize, snake[i].Y * cellSize, cellSize, cellSize);
            }

            // стадии игры
            g.DrawString($"Стадия: {currentStage} | Счет: {score}", new Font("Arial", 12, FontStyle.Bold), Brushes.Black, new Point(5, 5));
        }

        private void GameOver(string reason)
        {
            gameTimer.Stop();
            MessageBox.Show($"{reason}\n\nВаш итоговый счет: {score}\nДостигнутая стадия: {currentStage}", "Game Over");
            InitGame();
            gameTimer.Start();
        }
    }

    static class Program
    {
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new GameForm());
        }
    }
}