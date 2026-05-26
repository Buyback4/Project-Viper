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
        private List<Sentinel> sentinels;

        private int directionX;
        private int directionY;

        private int score = 0;
        private int pendingGrowth = 0;

        private int tickCounter = 0;
        private int currentStage = 1;

        private int enemiesKilled = 0;
        private bool sentinelUnlocked = false;

        private GameState currentState = GameState.Menu;
        private GameState previousState = GameState.Menu;
        private int menuSelection = 0;
        private string gameOverReason = "";
        private int gameOverSelection = 0;
        private int settingsSelection = 0;
        private int pauseSelection = 0;
        private bool isFullscreen = false;

        private const int BaseMaxDevourers = 3;
        private const int BaseMaxHunters = 2;
        private const int MaxStoneBlocks = 100;
        private const int BaseMaxSentinels = 2;

        private const int cellSize = 25;
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

            InitGame();

            gameTimer = new Timer();
            gameTimer.Interval = 150;
            gameTimer.Tick += Update;
            gameTimer.Start();
        }

        private void ToggleFullscreen()
        {
            isFullscreen = !isFullscreen;
            if (isFullscreen)
            {
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
            }
            else
            {
                this.FormBorderStyle = FormBorderStyle.FixedSingle;
                this.WindowState = FormWindowState.Normal;
                this.ClientSize = new Size(cellSize * gridWidth, cellSize * gridHeight);
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (currentState == GameState.Menu)
            {
                if (e.KeyCode == Keys.W || e.KeyCode == Keys.Up) menuSelection = (menuSelection - 1 + 3) % 3;
                else if (e.KeyCode == Keys.S || e.KeyCode == Keys.Down) menuSelection = (menuSelection + 1) % 3;
                else if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
                {
                    if (menuSelection == 0)
                    {
                        InitGame();
                        currentState = GameState.Instructions;
                    }
                    else if (menuSelection == 1)
                    {
                        previousState = GameState.Menu;
                        currentState = GameState.Settings;
                        settingsSelection = 0;
                    }
                    else Application.Exit();
                }
            }
            else if (currentState == GameState.Instructions)
            {
                if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
                {
                    currentState = GameState.Playing;
                }
            }
            else if (currentState == GameState.Settings)
            {
                if (e.KeyCode == Keys.W || e.KeyCode == Keys.Up) settingsSelection = (settingsSelection - 1 + 2) % 2;
                else if (e.KeyCode == Keys.S || e.KeyCode == Keys.Down) settingsSelection = (settingsSelection + 1) % 2;
                else if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
                {
                    if (settingsSelection == 0) ToggleFullscreen();
                    else currentState = previousState;
                }
                else if (e.KeyCode == Keys.Escape) currentState = previousState;
            }
            else if (currentState == GameState.Paused)
            {
                if (e.KeyCode == Keys.W || e.KeyCode == Keys.Up) pauseSelection = (pauseSelection - 1 + 3) % 3;
                else if (e.KeyCode == Keys.S || e.KeyCode == Keys.Down) pauseSelection = (pauseSelection + 1) % 3;
                else if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
                {
                    if (pauseSelection == 0) currentState = GameState.Playing;
                    else if (pauseSelection == 1)
                    {
                        previousState = GameState.Paused;
                        currentState = GameState.Settings;
                        settingsSelection = 0;
                    }
                    else if (pauseSelection == 2) currentState = GameState.Menu;
                }
                else if (e.KeyCode == Keys.Escape) currentState = GameState.Playing;
            }
            else if (currentState == GameState.GameOver)
            {
                if (e.KeyCode == Keys.W || e.KeyCode == Keys.Up) gameOverSelection = 0;
                else if (e.KeyCode == Keys.S || e.KeyCode == Keys.Down) gameOverSelection = 1;
                else if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
                {
                    if (gameOverSelection == 0)
                    {
                        InitGame();
                        currentState = GameState.Playing;
                    }
                    else currentState = GameState.Menu;
                }
            }
            else if (currentState == GameState.Playing)
            {
                if (e.KeyCode == Keys.Escape)
                {
                    currentState = GameState.Paused;
                    pauseSelection = 0;
                }
                else if (e.KeyCode == Keys.W && directionY != 1) { directionX = 0; directionY = -1; }
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
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;

            if (currentState == GameState.Menu)
            {
                g.Clear(Color.DarkSlateGray);

                string title = "Боевой питон";
                Font titleFont = new Font("Impact", 72, FontStyle.Italic);
                SizeF titleSize = g.MeasureString(title, titleFont);
                g.DrawString(title, titleFont, Brushes.LimeGreen, (this.ClientSize.Width - titleSize.Width) / 2, 200);

                Font menuFont = new Font("Arial", 36, FontStyle.Bold);

                string startText = menuSelection == 0 ? "> ИГРАТЬ <" : "  ИГРАТЬ  ";
                Brush startBrush = menuSelection == 0 ? Brushes.Yellow : Brushes.White;
                SizeF startSize = g.MeasureString(startText, menuFont);
                g.DrawString(startText, menuFont, startBrush, (this.ClientSize.Width - startSize.Width) / 2, 400);

                string optText = menuSelection == 1 ? "> НАСТРОЙКИ <" : "  НАСТРОЙКИ  ";
                Brush optBrush = menuSelection == 1 ? Brushes.Yellow : Brushes.White;
                SizeF optSize = g.MeasureString(optText, menuFont);
                g.DrawString(optText, menuFont, optBrush, (this.ClientSize.Width - optSize.Width) / 2, 500);

                string exitText = menuSelection == 2 ? "> ВЫХОД <" : "  ВЫХОД  ";
                Brush exitBrush = menuSelection == 2 ? Brushes.Yellow : Brushes.White;
                SizeF exitSize = g.MeasureString(exitText, menuFont);
                g.DrawString(exitText, menuFont, exitBrush, (this.ClientSize.Width - exitSize.Width) / 2, 600);

                return;
            }

            if (currentState == GameState.Instructions)
            {
                g.Clear(Color.DarkSlateGray);

                Font titleFont = new Font("Impact", 60);
                string titleText = "ИНСТРУКЦИЯ";
                SizeF titleSize = g.MeasureString(titleText, titleFont);
                g.DrawString(titleText, titleFont, Brushes.LimeGreen, (this.ClientSize.Width - titleSize.Width) / 2, 100);

                Font headerFont = new Font("Arial", 28, FontStyle.Bold);
                Font textFont = new Font("Arial", 22, FontStyle.Regular);

                int yPos = 220;

                string controlsHeader = "УПРАВЛЕНИЕ:";
                SizeF chSize = g.MeasureString(controlsHeader, headerFont);
                g.DrawString(controlsHeader, headerFont, Brushes.Yellow, (this.ClientSize.Width - chSize.Width) / 2, yPos);
                yPos += 50;

                string[] controls = {
                    "W, A, S, D - Движение змеи",
                    "ПРОБЕЛ - Выстрел (тратит 1 сегмент хвоста)",
                    "ESC - Пауза"
                };

                foreach (string line in controls)
                {
                    SizeF lineSize = g.MeasureString(line, textFont);
                    g.DrawString(line, textFont, Brushes.White, (this.ClientSize.Width - lineSize.Width) / 2, yPos);
                    yPos += 40;
                }

                yPos += 40;

                string enemiesHeader = "ОБЪЕКТЫ И ВРАГИ:";
                SizeF ehSize = g.MeasureString(enemiesHeader, headerFont);
                g.DrawString(enemiesHeader, headerFont, Brushes.Yellow, (this.ClientSize.Width - ehSize.Width) / 2, yPos);
                yPos += 50;

                string[] enemies = {
                    "Красное яблоко: +1 очко",
                    "Золотое яблоко: +3 очка, рост на 2 сегмента",
                    "Камни (Серые): Препятствия. Можно разрушить выстрелом",
                    "Пожиратель (Фиолетовый): Ищет и съедает яблоки",
                    "Охотник (Оранжевый): Гоняется за вами. Укус отсекает хвост",
                    "Страж (Розовый): Неподвижен. Если вы на линии огня, копит",
                    "заряд и стреляет. Попадание в голову - смерть!"
                };

                foreach (string line in enemies)
                {
                    SizeF lineSize = g.MeasureString(line, textFont);
                    g.DrawString(line, textFont, Brushes.White, (this.ClientSize.Width - lineSize.Width) / 2, yPos);
                    yPos += 40;
                }

                Font continueFont = new Font("Arial", 28, FontStyle.Bold);
                string continueText = "> НАЖМИТЕ ПРОБЕЛ ДЛЯ СТАРТА <";
                SizeF continueSize = g.MeasureString(continueText, continueFont);
                g.DrawString(continueText, continueFont, Brushes.LimeGreen, (this.ClientSize.Width - continueSize.Width) / 2, yPos + 60);

                return;
            }

            if (currentState == GameState.Settings)
            {
                g.Clear(Color.DarkSlateGray);

                Font titleFont = new Font("Impact", 60);
                string title = "НАСТРОЙКИ";
                SizeF titleSize = g.MeasureString(title, titleFont);
                g.DrawString(title, titleFont, Brushes.Gray, (this.ClientSize.Width - titleSize.Width) / 2, 200);

                Font menuFont = new Font("Arial", 36, FontStyle.Bold);

                string fsStatus = isFullscreen ? "ВКЛ" : "ВЫКЛ";
                string fsText = settingsSelection == 0 ? $"> ПОЛНЫЙ ЭКРАН : {fsStatus} <" : $"  ПОЛНЫЙ ЭКРАН : {fsStatus}  ";
                Brush fsBrush = settingsSelection == 0 ? Brushes.Yellow : Brushes.White;
                SizeF fsSize = g.MeasureString(fsText, menuFont);
                g.DrawString(fsText, menuFont, fsBrush, (this.ClientSize.Width - fsSize.Width) / 2, 450);

                string backText = settingsSelection == 1 ? "> НАЗАД <" : "  НАЗАД  ";
                Brush backBrush = settingsSelection == 1 ? Brushes.Yellow : Brushes.White;
                SizeF backSize = g.MeasureString(backText, menuFont);
                g.DrawString(backText, menuFont, backBrush, (this.ClientSize.Width - backSize.Width) / 2, 550);

                return;
            }

            g.Clear(Color.DarkSlateGray);
            g.FillRectangle(Brushes.LightGreen, 0, 0, gridWidth * cellSize, gridHeight * cellSize);

            Pen gridPen = new Pen(Color.SeaGreen, 1);
            for (int i = 0; i <= gridWidth; i++) g.DrawLine(gridPen, i * cellSize, 0, i * cellSize, gridHeight * cellSize);
            for (int i = 0; i <= gridHeight; i++) g.DrawLine(gridPen, 0, i * cellSize, gridWidth * cellSize, i * cellSize);

            Pen wallPen = new Pen(Color.SaddleBrown, 2);
            g.DrawRectangle(wallPen, 1, 1, (gridWidth * cellSize) - 2, (gridHeight * cellSize) - 2);

            foreach (var sen in sentinels)
            {
                int alpha = 60 + (sen.Charge * 18);
                if (alpha > 255) alpha = 255;

                using (SolidBrush laserBrush = new SolidBrush(Color.FromArgb(alpha, 255, 0, 0)))
                {
                    int[] dx = { 0, 0, -1, 1 };
                    int[] dy = { -1, 1, 0, 0 };

                    for (int dir = 0; dir < 4; dir++)
                    {
                        Point checkPos = new Point(sen.Pos.X + dx[dir], sen.Pos.Y + dy[dir]);
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
            foreach (var s in sentinels) g.FillRectangle(Brushes.Magenta, s.Pos.X * cellSize, s.Pos.Y * cellSize, cellSize, cellSize);

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

            if (currentState == GameState.Paused)
            {
                using (SolidBrush overlay = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
                {
                    g.FillRectangle(overlay, 0, 0, this.ClientSize.Width, this.ClientSize.Height);
                }

                Font titleFont = new Font("Impact", 72);
                string titleText = "ПАУЗА";
                SizeF titleSize = g.MeasureString(titleText, titleFont);
                g.DrawString(titleText, titleFont, Brushes.White, (this.ClientSize.Width - titleSize.Width) / 2, 250);

                Font optionsFont = new Font("Arial", 28, FontStyle.Bold);

                string resumeText = pauseSelection == 0 ? "> ПРОДОЛЖИТЬ <" : "  ПРОДОЛЖИТЬ  ";
                Brush resumeBrush = pauseSelection == 0 ? Brushes.Yellow : Brushes.White;
                SizeF resumeSize = g.MeasureString(resumeText, optionsFont);
                g.DrawString(resumeText, optionsFont, resumeBrush, (this.ClientSize.Width - resumeSize.Width) / 2, 450);

                string optText = pauseSelection == 1 ? "> НАСТРОЙКИ <" : "  НАСТРОЙКИ  ";
                Brush optBrush = pauseSelection == 1 ? Brushes.Yellow : Brushes.White;
                SizeF optSize = g.MeasureString(optText, optionsFont);
                g.DrawString(optText, optionsFont, optBrush, (this.ClientSize.Width - optSize.Width) / 2, 530);

                string menuText = pauseSelection == 2 ? "> В ГЛАВНОЕ МЕНЮ <" : "  В ГЛАВНОЕ МЕНЮ  ";
                Brush menuBrush = pauseSelection == 2 ? Brushes.Yellow : Brushes.White;
                SizeF menuSize = g.MeasureString(menuText, optionsFont);
                g.DrawString(menuText, optionsFont, menuBrush, (this.ClientSize.Width - menuSize.Width) / 2, 610);
            }
            else if (currentState == GameState.GameOver)
            {
                using (SolidBrush overlay = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
                {
                    g.FillRectangle(overlay, 0, 0, this.ClientSize.Width, this.ClientSize.Height);
                }

                Font overFont = new Font("Impact", 72);
                string overText = "GAME OVER";
                SizeF overSize = g.MeasureString(overText, overFont);
                g.DrawString(overText, overFont, Brushes.Red, (this.ClientSize.Width - overSize.Width) / 2, 250);

                Font infoFont = new Font("Arial", 24, FontStyle.Bold);

                string reasonText = gameOverReason;
                SizeF reasonSize = g.MeasureString(reasonText, infoFont);
                g.DrawString(reasonText, infoFont, Brushes.White, (this.ClientSize.Width - reasonSize.Width) / 2, 400);

                string statsText = $"Итоговый счет: {score}  |  Достигнута стадия: {currentStage}";
                SizeF statsSize = g.MeasureString(statsText, infoFont);
                g.DrawString(statsText, infoFont, Brushes.Yellow, (this.ClientSize.Width - statsSize.Width) / 2, 470);

                Font optionsFont = new Font("Arial", 28, FontStyle.Bold);

                string restartText = gameOverSelection == 0 ? "> НАЧАТЬ ЗАНОВО <" : "  НАЧАТЬ ЗАНОВО  ";
                Brush restartBrush = gameOverSelection == 0 ? Brushes.Yellow : Brushes.White;
                SizeF restartSize = g.MeasureString(restartText, optionsFont);
                g.DrawString(restartText, optionsFont, restartBrush, (this.ClientSize.Width - restartSize.Width) / 2, 580);

                string menuText = gameOverSelection == 1 ? "> В ГЛАВНОЕ МЕНЮ <" : "  В ГЛАВНОЕ МЕНЮ  ";
                Brush menuBrush = gameOverSelection == 1 ? Brushes.Yellow : Brushes.White;
                SizeF menuSize = g.MeasureString(menuText, optionsFont);
                g.DrawString(menuText, optionsFont, menuBrush, (this.ClientSize.Width - menuSize.Width) / 2, 660);
            }
        }

        private void GameOver(string reason)
        {
            gameOverReason = reason;
            gameOverSelection = 0;
            currentState = GameState.GameOver;
        }
    }
}