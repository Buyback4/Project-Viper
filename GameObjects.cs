using System.Drawing;

namespace Snake
{
    public enum GameState { Menu, Settings, Instructions, Playing, GameOver, Paused }

    public class Projectile
    {
        public Point Pos;
        public Point Dir;
        public bool IsPlayerBullet;
    }

    public class Sentinel
    {
        public Point Pos;
        public int Charge;
    }

    public enum FoodType { Normal, Golden }

    public class Apple
    {
        public Point Pos;
        public FoodType Type;
    }
}