using System.Drawing;

namespace Snake
{
    public class Projectile
    {
        public Point Pos;
        public Point Dir;
        public bool IsPlayerBullet;
    }

    // золотое яблоко
    public enum FoodType { Normal, Golden }

    public class Apple
    {
        public Point Pos;
        public FoodType Type;
    }
}