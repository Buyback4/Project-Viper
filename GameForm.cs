using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Snake
{
    public partial class GameForm : Form
    {
        private List<Point> snake;
        private List<Projectile> bullets;
        private List<Apple> apples;

        private List<Point> stones;
        private List<Point> devourers;
        private List<Point> hunters;
        private List<Point> sentinels;

        private int directionX;
        private int directionY;

        private int score = 0;
        private int pendingGrowth = 0;

        private int tickCounter = 0;
        private int currentStage = 1;

        private int enemiesKilled = 0;
        private bool sentinelUnlocked = false;

        private const int MaxDevourers = 3;
        private const int MaxHunters = 2;
        private const int MaxStoneBlocks = 100;
        private const int MaxSentinels = 2;

        private const int cellSize = 25;
        private const int gridWidth = 64;
        private const int gridHeight = 40;

        private Timer gameTimer;
        private Random rnd = new Random();

        public GameForm()
        {
            this.Text = "Боевая змейка";
            this.ClientSize = new Size(cellSize * gridWidth, cellSize * gridHeight);
            this.DoubleBuffered = true;
            this.KeyPreview = true;
            this.KeyDown += OnKeyDown;

            this.BackColor = Color.LightGreen;

            InitGame();

            gameTimer = new Timer();
            gameTimer.Interval = 150;
            gameTimer.Tick += Update;
            gameTimer.Start();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.W && directionY != 1) { directionX = 0; directionY = -1; }
            else if (e.KeyCode == Keys.S && directionY != -1) { directionX = 0; directionY = 1; }
            else if (e.KeyCode == Keys.A && directionX != 1) { directionX = -1; directionY = 0; }
            else if (e.KeyCode == Keys.D && directionX != -1) { directionX = 1; directionY = 0; }
            else if (e.KeyCode == Keys.Space)
            {
                if (snake.Count > 1 && pendingGrowth == 0)
                {
                    snake.RemoveAt(snake.Count - 1);
                    Point head = snake[0];
                    Point bulletStartPos = new Point(head.X + directionX, head.Y + directionY);

                    bullets.Add(new Projectile { Pos = bulletStartPos, Dir = new Point(directionX, directionY), IsPlayerBullet = true });
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;

            Pen gridPen = new Pen(Color.SeaGreen, 1);
            for (int i = 0; i <= gridWidth; i++) g.DrawLine(gridPen, i * cellSize, 0, i * cellSize, gridHeight * cellSize);
            for (int i = 0; i <= gridHeight; i++) g.DrawLine(gridPen, 0, i * cellSize, gridWidth * cellSize, i * cellSize);

            Pen wallPen = new Pen(Color.SaddleBrown, 2);
            g.DrawRectangle(wallPen, 1, 1, (gridWidth * cellSize) - 2, (gridHeight * cellSize) - 2);

            using (SolidBrush laserBrush = new SolidBrush(Color.FromArgb(60, 255, 0, 0)))
            {
                foreach (var sen in sentinels)
                {
                    int[] dx = { 0, 0, -1, 1 };
                    int[] dy = { -1, 1, 0, 0 };

                    for (int dir = 0; dir < 4; dir++)
                    {
                        Point checkPos = new Point(sen.X + dx[dir], sen.Y + dy[dir]);
                        while (IsWalkable(checkPos))
                        {
                            g.FillRectangle(laserBrush, checkPos.X * cellSize, checkPos.Y * cellSize, cellSize, cellSize);
                            checkPos.X += dx[dir];
                            checkPos.Y += dy[dir];
                        }
                    }
                }
            }

            foreach (var st in stones) g.FillRectangle(Brushes.DimGray, st.X * cellSize, st.Y * cellSize, cellSize, cellSize);

            foreach (var apple in apples)
            {
                if (apple.Type == FoodType.Golden)
                {
                    g.FillEllipse(Brushes.Gold, apple.Pos.X * cellSize, apple.Pos.Y * cellSize, cellSize, cellSize);
                    g.DrawEllipse(Pens.DarkOrange, apple.Pos.X * cellSize, apple.Pos.Y * cellSize, cellSize, cellSize);
                }
                else g.FillEllipse(Brushes.Red, apple.Pos.X * cellSize, apple.Pos.Y * cellSize, cellSize, cellSize);
            }

            foreach (var d in devourers) g.FillRectangle(Brushes.Purple, d.X * cellSize, d.Y * cellSize, cellSize, cellSize);
            foreach (var h in hunters) g.FillRectangle(Brushes.DarkOrange, h.X * cellSize, h.Y * cellSize, cellSize, cellSize);

            foreach (var s in sentinels) g.FillRectangle(Brushes.Magenta, s.X * cellSize, s.Y * cellSize, cellSize, cellSize);

            foreach (var b in bullets)
            {
                Brush bulletColor = b.IsPlayerBullet ? Brushes.Yellow : Brushes.OrangeRed;
                g.FillEllipse(bulletColor, b.Pos.X * cellSize + 5, b.Pos.Y * cellSize + 5, cellSize - 10, cellSize - 10);
            }

            for (int i = 0; i < snake.Count; i++)
            {
                Brush b = i == 0 ? Brushes.Navy : Brushes.Blue;
                g.FillRectangle(b, snake[i].X * cellSize, snake[i].Y * cellSize, cellSize, cellSize);
            }


            g.DrawString($"Стадия: {currentStage} | Счет: {score} | Фраги: {enemiesKilled}", new Font("Arial", 12, FontStyle.Bold), Brushes.Black, new Point(5, 5));
        }

        private void GameOver(string reason)
        {
            gameTimer.Stop();
            MessageBox.Show($"{reason}\n\nВаш итоговый счет: {score}\nДостигнутая стадия: {currentStage}", "Game Over");
            InitGame();
            gameTimer.Start();
        }
    }
}