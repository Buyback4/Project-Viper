using System;
using System.Collections.Generic;
using System.Drawing;

namespace Snake
{
    public partial class GameForm
    {
        private void InitGame()
        {
            snake = new List<Point>();
            snake.Add(new Point(gridWidth / 2, gridHeight / 2));

            bullets = new List<Projectile>();
            apples = new List<Apple>();
            stones = new List<Point>();
            devourers = new List<Point>();
            hunters = new List<Point>();
            sentinels = new List<Sentinel>();

            score = 0;
            pendingGrowth = 0;
            tickCounter = 0;
            currentStage = 1;
            directionX = 1;
            directionY = 0;

            enemiesKilled = 0;
            sentinelUnlocked = false;

            gameOverReason = "";

            GenerateFood();
        }

        private Point GetFreePosition(bool safeZone = false)
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
                foreach (var sen in sentinels) if (sen.Pos == p) isFree = false;

                if (safeZone && isFree && snake.Count > 0)
                {
                    int distanceX = Math.Abs(p.X - snake[0].X);
                    int distanceY = Math.Abs(p.Y - snake[0].Y);
                    if (distanceX <= 5 && distanceY <= 5) isFree = false;
                }
            } while (!isFree);
            return p;
        }

        private void GenerateFood()
        {
            int targetApples = 1;
            int goldenChance = 10;

            if (currentStage == 3) targetApples = 2;
            if (currentStage >= 4)
            {
                int extraPhases = currentStage - 4;
                targetApples = 3 + extraPhases;
                goldenChance = Math.Min(50, 15 + extraPhases * 5);
            }

            while (apples.Count < targetApples)
            {
                FoodType newType = rnd.Next(100) < goldenChance ? FoodType.Golden : FoodType.Normal;
                apples.Add(new Apple { Pos = GetFreePosition(), Type = newType });
            }
        }

        private void SpawnStone()
        {
            if (stones.Count >= MaxStoneBlocks) return;
            Point p = GetFreePosition(true);
            if (rnd.Next(2) == 0)
            {
                if (p != snake[0]) stones.Add(p);
            }
            else
            {
                List<Point> newBlock = new List<Point>
                {
                    p, new Point(p.X + 1, p.Y), new Point(p.X, p.Y + 1), new Point(p.X + 1, p.Y + 1)
                };
                foreach (var cell in newBlock) if (cell != snake[0]) stones.Add(cell);
            }
        }

        private bool IsWalkable(Point p)
        {
            if (p.X < 0 || p.X >= gridWidth || p.Y < 0 || p.Y >= gridHeight) return false;
            foreach (var st in stones) if (st == p) return false;
            return true;
        }

        private Point GetNextEnemyStep(Point current, Point target)
        {
            int dx = 0, dy = 0;
            if (current.X < target.X) dx = 1; else if (current.X > target.X) dx = -1;
            if (current.Y < target.Y) dy = 1; else if (current.Y > target.Y) dy = -1;

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

        private void CheckBulletCollisions()
        {
            for (int i = bullets.Count - 1; i >= 0; i--)
            {
                Point bPos = bullets[i].Pos;
                bool bulletDestroyed = false;

                if (bPos.X < 0 || bPos.X >= gridWidth || bPos.Y < 0 || bPos.Y >= gridHeight) bulletDestroyed = true;

                for (int s = stones.Count - 1; s >= 0; s--)
                {
                    if (stones[s] == bPos) { stones.RemoveAt(s); bulletDestroyed = true; break; }
                }

                if (!bulletDestroyed && bullets[i].IsPlayerBullet)
                {
                    for (int d = devourers.Count - 1; d >= 0; d--)
                        if (devourers[d] == bPos) { devourers.RemoveAt(d); bulletDestroyed = true; enemiesKilled++; break; }

                    if (!bulletDestroyed)
                        for (int h = hunters.Count - 1; h >= 0; h--)
                            if (hunters[h] == bPos) { hunters.RemoveAt(h); bulletDestroyed = true; enemiesKilled++; break; }

                    if (!bulletDestroyed)
                        for (int s = sentinels.Count - 1; s >= 0; s--)
                            if (sentinels[s].Pos == bPos) { sentinels.RemoveAt(s); bulletDestroyed = true; enemiesKilled++; break; }
                }

                if (bulletDestroyed) bullets.RemoveAt(i);
            }
        }

        private void Update(object sender, EventArgs e)
        {
            if (currentState != GameState.Playing)
            {
                this.Invalidate();
                return;
            }

            tickCounter++;
            int oldStage = currentStage;

            if (score >= 12) currentStage = 4 + (score - 12) / 10;
            else if (score >= 7) currentStage = 3;
            else if (score >= 4) currentStage = 2;
            else currentStage = 1;

            if (currentStage > oldStage) GenerateFood();

            int extraPhases = Math.Max(0, currentStage - 4);
            int currentMaxDevourers = BaseMaxDevourers + extraPhases;
            int currentMaxHunters = BaseMaxHunters + extraPhases;
            int currentMaxSentinels = BaseMaxSentinels + extraPhases;

            if (currentStage >= 2 && tickCounter % 40 == 0) SpawnStone();
            if (currentStage >= 3 && tickCounter % 50 == 0 && devourers.Count < currentMaxDevourers) devourers.Add(GetFreePosition(true));
            if (currentStage >= 4 && tickCounter % 60 == 0 && hunters.Count < currentMaxHunters) hunters.Add(GetFreePosition(true));

            if (!sentinelUnlocked && enemiesKilled >= 2)
            {
                sentinelUnlocked = true;
                sentinels.Add(new Sentinel { Pos = GetFreePosition(true), Charge = 0 });
            }
            if (sentinelUnlocked && tickCounter % 90 == 0 && sentinels.Count < currentMaxSentinels)
            {
                sentinels.Add(new Sentinel { Pos = GetFreePosition(true), Charge = 0 });
            }

            CheckBulletCollisions();

            foreach (var b in bullets)
            {
                b.Pos.X += b.Dir.X;
                b.Pos.Y += b.Dir.Y;
            }

            CheckBulletCollisions();

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
                            if (dist < minDistance) { minDistance = dist; target = apple.Pos; }
                        }
                        devourers[i] = GetNextEnemyStep(devourers[i], target);
                        for (int a = apples.Count - 1; a >= 0; a--)
                        {
                            if (devourers[i] == apples[a].Pos) { apples.RemoveAt(a); GenerateFood(); break; }
                        }
                    }
                }
            }

            if (tickCounter % 3 == 0)
            {
                for (int i = 0; i < hunters.Count; i++) hunters[i] = GetNextEnemyStep(hunters[i], snake[0]);
            }

            foreach (var sen in sentinels)
            {
                bool seesSnake = false;
                Point targetDir = new Point(0, 0);

                int[] dx = { 0, 0, -1, 1 };
                int[] dy = { -1, 1, 0, 0 };

                for (int dir = 0; dir < 4; dir++)
                {
                    Point checkPos = new Point(sen.Pos.X + dx[dir], sen.Pos.Y + dy[dir]);

                    while (IsWalkable(checkPos))
                    {
                        if (snake.Contains(checkPos))
                        {
                            seesSnake = true;
                            targetDir = new Point(dx[dir], dy[dir]);
                            break;
                        }
                        checkPos.X += dx[dir];
                        checkPos.Y += dy[dir];
                    }
                    if (seesSnake) break;
                }

                if (seesSnake)
                {
                    sen.Charge++;
                    if (sen.Charge >= 10)
                    {
                        bullets.Add(new Projectile { Pos = new Point(sen.Pos.X + targetDir.X, sen.Pos.Y + targetDir.Y), Dir = targetDir, IsPlayerBullet = false });
                        sen.Charge = 0;
                    }
                }
                else
                {
                    if (sen.Charge > 0) sen.Charge--;
                }
            }

            CheckBulletCollisions();

            Point head = snake[0];
            Point newHead = new Point(head.X + directionX, head.Y + directionY);

            if (newHead.X < 0 || newHead.X >= gridWidth || newHead.Y < 0 || newHead.Y >= gridHeight)
            { GameOver("Вы покинули пределы поля!"); return; }

            for (int i = 0; i < snake.Count; i++)
            {
                if (snake[i] == newHead) { GameOver("Вы врезались в свой хвост!"); return; }
            }

            foreach (var st in stones) if (st == newHead) { GameOver("Вы разбились о камень!"); return; }
            foreach (var d in devourers) if (d == newHead) { GameOver("Пожиратель уничтожил вас!"); return; }
            foreach (var h in hunters) if (h == newHead) { GameOver("Охотник поймал вас!"); return; }
            foreach (var s in sentinels) if (s.Pos == newHead) { GameOver("Вы врезались в Стража!"); return; }

            snake.Insert(0, newHead);

            bool wasBitten = false;

            for (int i = bullets.Count - 1; i >= 0; i--)
            {
                if (!bullets[i].IsPlayerBullet)
                {
                    for (int s = 0; s < snake.Count; s++)
                    {
                        if (bullets[i].Pos == snake[s])
                        {
                            if (s == 0)
                            {
                                GameOver("Вас застрелил Страж!");
                                return;
                            }
                            else
                            {
                                snake.RemoveRange(s, snake.Count - s);
                                wasBitten = true;
                                bullets.RemoveAt(i);
                                break;
                            }
                        }
                    }
                }
            }

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
                    if (apples[i].Type == FoodType.Golden) { score += 3; pendingGrowth += 2; }
                    else { score++; }

                    apples.RemoveAt(i);
                    GenerateFood();
                    break;
                }
            }

            if (!ateSomething && !wasBitten)
            {
                if (pendingGrowth > 0) pendingGrowth--;
                else if (snake.Count > 1) snake.RemoveAt(snake.Count - 1);
            }

            this.Invalidate();
        }
    }
}